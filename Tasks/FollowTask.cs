using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using static DreamPoeBot.Loki.Game.LokiPoe;


namespace FollowBot.Tasks
{
    internal class FollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Vector2i _lastSeenMasterPosition;
        private Stopwatch _leaderzoningSw;
        private static bool IsInGame => true;
        private static dynamic Me => LokiPoe.Me;

        public async Task<bool> Run()
        {
            // Check if follow feature is enabled and the player's state is valid
            if (!IsFollowEnabled() || !IsPlayerStateValid() || !IsLeaderValid() || FollowBot.Leader == null)
                return false;

            var leader = FollowBot.Leader;
            var leaderPos = leader.Position;
            var myPos = Me.Position;

            // Ensure both positions are valid
            if (!IsPositionValid(leaderPos) || !IsPositionValid(myPos))
            {
                ResetMovementKey();
                return false;
            }

            double distance = leaderPos.Distance(myPos);

            // Update last known master (leader) position if a valid path exists
            if (ExilePather.PathExistsBetween(myPos, ExilePather.FastWalkablePositionFor(leaderPos)))
                _lastSeenMasterPosition = leaderPos;

            // Check if transition handling is needed:
            // If the distance exceeds maximum follow distance or if the leader is moving
            if (ShouldHandleTransition(distance, leader))
            {
                // Calculate a target position based on a random distance between FollowDistance and MaxFollowDistance
                var pos = CalculateTargetPosition(myPos, leaderPos);
                if (!IsValidTargetPosition(pos, myPos))
                {
                    KeyManager.ClearAllKeyStates();

                    // Handle the Grace Period (e.g. after zoning)
                    if (await HandleGracePeriod()) return true;

                    // Try to find and handle a delve portal scenario
                    if (await HandleDelvePortal()) return true;

                    // Attempt to handle an Area Transition or a Teleport scenario
                    if (await HandleAreaOrTeleportTransition(myPos)) return true;

                    return false;
                }

                // Normal movement when target position is valid
                if (ExilePather.PathDistance(myPos, pos) < 45)
                    InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos, false);
                else
                    Move.Towards(pos, leader.Name);
                return true;
            }

            ResetMovementKey();
            return false;
        }

        // Checks if the follow feature is enabled in settings; resets movement key if disabled.
        private bool IsFollowEnabled()
        {
            if (!FollowBotSettings.Instance.ShouldFollow)
            {
                ResetMovementKey();
                return false;
            }

            return true;
        }

        // Checks if the player's state is valid (in game, alive, not in town or hideout); resets movement key if invalid.
        private bool IsPlayerStateValid()
        {
            if (!IsInGame || Me.IsDead || Me.IsInTown || Me.IsInHideout)
            {
                ResetMovementKey();
                return false;
            }

            return true;
        }

        // Checks if a valid leader is assigned; resets movement key if not.
        private bool IsLeaderValid()
        {
            if (FollowBot.Leader == null)
            {
                ResetMovementKey();
                return false;
            }

            return true;
        }

        // Validates that a position is valid (i.e., not a zero vector).
        private bool IsPositionValid(Vector2i pos)
        {
            return pos != Vector2i.Zero;
        }

        // Resets the movement key state.
        private void ResetMovementKey()
        {
            ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
        }

        // Determines whether to handle transitions based on distance or if the leader is in a "Move" action.
        private bool ShouldHandleTransition(double distance, dynamic leader)
        {
            return distance > FollowBotSettings.Instance.MaxFollowDistance ||
                   (leader.HasCurrentAction == true && leader.CurrentAction?.Skill?.InternalId == "Move");
        }

        // Calculates the target position based on the leader's position and a random distance offset.
        private Vector2i CalculateTargetPosition(Vector2i myPos, Vector2i leaderPos)
        {
            var randomDistance = Random.Next(FollowBotSettings.Instance.FollowDistance,
                FollowBotSettings.Instance.MaxFollowDistance);
            var targetPoint = myPos.GetPointAtDistanceBeforeEnd(leaderPos, randomDistance);
            return ExilePather.FastWalkablePositionFor(targetPoint);
        }

        // Validates that the computed target position is valid and reachable.
        private bool IsValidTargetPosition(Vector2i pos, Vector2i myPos)
        {
            return pos != Vector2i.Zero && ExilePather.PathExistsBetween(myPos, pos);
        }

        // Handles the scenario where a Grace Period might be in effect (e.g., just zoned and waiting for the leader to load).
        private async Task<bool> HandleGracePeriod()
        {
            if (Me.HasAura("Grace Period"))
            {
                if (!_leaderzoningSw.IsRunning)
                {
                    Log.DebugFormat(
                        "Grace period detected, this mean we just zoned and are waiting for the leader to finish loading.");
                    _leaderzoningSw.Start();
                }

                if (_leaderzoningSw.IsRunning && _leaderzoningSw.ElapsedMilliseconds < 10000) return true;
            }

            return false;
        }

        // Attempts to locate and interact with a delve portal.
        private async Task<bool> HandleDelvePortal()
        {
            var delvePortal = ObjectManager.GetObjectsByType<AreaTransition>()
                .FirstOrDefault(x =>
                    x.Name == "Azurite Mine" && x.Metadata == "Metadata/MiscellaneousObject/PortalTransition");
            if (delvePortal != null)
            {
                Log.DebugFormat("[{0}] Found walkable delve portal.", Name);
                // Move towards the portal until within 20 units
                while (Me.Position.Distance(delvePortal.Position) > 20)
                {
                    if (Me.IsDead)
                        return true;
                    var walkablePosition = ExilePather.FastWalkablePositionFor(delvePortal, 20);
                    if (Move.Towards(walkablePosition, "moving to delve portal")) continue;
                    break;
                }

                var teleResult = await Coroutines.InteractWith(delvePortal);
                if (!teleResult) Log.DebugFormat("[{0}] delve portal error.", Name);
                FollowBot.Leader = null;
                return true;
            }

            return false;
        }

        // Attempts to handle an area transition or teleport event if a delve portal was not found.
        private async Task<bool> HandleAreaOrTeleportTransition(Vector2i myPos)
        {
            // Try to find an Area Transition near the last seen leader position
            AreaTransition areaTransition = null;
            if (_lastSeenMasterPosition != Vector2i.Zero)
                areaTransition = ObjectManager.GetObjectsByType<AreaTransition>()
                    .OrderBy(x => x.Position.Distance(_lastSeenMasterPosition))
                    .FirstOrDefault(x =>
                        ExilePather.PathExistsBetween(myPos, ExilePather.FastWalkablePositionFor(x.Position, 20)));

            // If no Area Transition is found, attempt to handle a Teleport (object named "Portal")
            if (areaTransition == null)
            {
                var teleport = ObjectManager.GetObjectsByName("Portal")
                    .OrderBy(x => x.Position.Distance(_lastSeenMasterPosition))
                    .FirstOrDefault(x =>
                        ExilePather.PathExistsBetween(Me.Position,
                            ExilePather.FastWalkablePositionFor(x.Position, 20)));
                if (teleport == null)
                    return false;
                Log.DebugFormat("[{0}] Found walkable Teleport.", Name);
                while (Me.Position.Distance(teleport.Position) > 20)
                {
                    // Cross-check: make sure there's a path from leader to current position
                    if (FollowBot.Leader == null ||
                        !ExilePather.PathExistsBetween(FollowBot.Leader.Position, Me.Position))
                        return false;
                    var walkablePosition = ExilePather.FastWalkablePositionFor(teleport, 20);
                    if (Move.Towards(walkablePosition, "moving to Teleport")) continue;
                    break;
                }

                var teleResult = await Coroutines.InteractWith(teleport);
                if (!teleResult) Log.DebugFormat("[{0}] Teleport error.", Name);
                FollowBot.Leader = null;
                return true;
            }

            Log.DebugFormat("[{0}] Found walkable Area Transition [{1}].", Name, areaTransition.Name);
            if (Me.Position.Distance(areaTransition.Position) > 20)
            {
                if (Me.IsDead)
                    return true;
                var walkablePosition = ExilePather.FastWalkablePositionFor(areaTransition, 20);
                Move.Towards(walkablePosition, "moving to area transition");
                return true;
            }

            var trans = await PlayerAction.TakeTransition(areaTransition);
            if (!trans) Log.DebugFormat("[{0}] Areatransition error.", Name);
            return true;
        }

        private AreaTransition GetRottingCoreTransition(Player leaderPlayerEntry)
        {
            var leaderPosition = leaderPlayerEntry.Position;
            var areatransition = ObjectManager.GetObjectsByType<AreaTransition>()
                .FirstOrDefault(x => x.Name == "The Black Core");
            if (areatransition == null)
                areatransition = ObjectManager.GetObjectsByType<AreaTransition>()
                    .FirstOrDefault(x => x.Name == "The Black Heart" && x.Distance < 140);
            if (areatransition == null && leaderPosition.X < 900)
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Shavronne's Sorrow" && x.Distance < 120);
            else if (areatransition == null && leaderPosition.X < 1325)
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Maligaro's Misery" && x.Distance < 140);
            else if (areatransition == null && leaderPosition.X < 2103)
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Doedre's Despair" && x.Distance < 140);
            return areatransition;
        }

        #region skip

        public string Name => "FollowTask";
        public string Description => "This task will Follow a Leader.";
        public string Author => "Helpless";
        public string Version => "0.0.0.1";

        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
            FollowBot.Leader = null;
            _lastSeenMasterPosition = Vector2i.Zero;
            _leaderzoningSw = new Stopwatch();
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged) _leaderzoningSw.Reset();
            return MessageResult.Unprocessed;
        }

        #endregion
    }
}