using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using FollowBot.SimpleEXtensions;
using log4net;
using static DreamPoeBot.Loki.Game.LokiPoe;

namespace FollowBot.Tasks
{
    internal class PreCombatFollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "PreCombatFollowTask";

        public string Description =>
            "This task will keep the bot under a specific distance from the leader, in combat situation.";

        public string Author => "NotYourFriend, origial code from Unknown";
        public string Version => "0.0.0.1";


        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }

        public Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.ShouldKill) return Task.FromResult(false);
            if (!FollowBotSettings.Instance.ShouldFollow) return Task.FromResult(false);
            if (!IsInGame || Me.IsDead || Me.IsInTown || Me.IsInHideout)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var leader = FollowBot.Leader;

            if (leader == null)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var leaderPos = leader.Position;
            var mypos = Me.Position;
            if (leaderPos == Vector2i.Zero || mypos == Vector2i.Zero)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var distance = leaderPos.Distance(mypos);

            if (distance > FollowBotSettings.Instance.MaxCombatDistance)
            {
                var pos = ExilePather.FastWalkablePositionFor(mypos.GetPointAtDistanceBeforeEnd(
                    leaderPos,
                    Random.Next(FollowBotSettings.Instance.FollowDistance,
                        FollowBotSettings.Instance.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(mypos, pos))
                {
                    ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                    return Task.FromResult(false);
                }


                if (ExilePather.PathDistance(mypos, pos) < 45)
                    InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos, false);
                else
                    Move.Towards(pos, $"{FollowBot.Leader.Name}");
                return Task.FromResult(true);
            }

            ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
            ////KeyManager.ClearAllKeyStates();
            return Task.FromResult(false);
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}