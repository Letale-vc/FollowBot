using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.CommonTasks;
using FollowBot.SimpleEXtensions.Global;
using FollowBot.Tasks;
using GameOverlay;
using JetBrains.Annotations;
using log4net;
using Message = DreamPoeBot.Loki.Bot.Message;
using UserControl = System.Windows.Controls.UserControl;

namespace FollowBot
{
    public class FollowBot : IBot
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private static bool IsOnRun;
        public static Stopwatch RequestPartySw = Stopwatch.StartNew();
        private static int _lastBoundMoveSkillSlot = -1;
        private static Keys _lastBoundMoveSkillKey = Keys.Clear;
        private static Player _leader;

        private static readonly string[] PlayerMetadataList =
        {
            "Metadata/Characters/Dex/Dex",
            "Metadata/Characters/Int/Int",
            "Metadata/Characters/Str/Str",
            "Metadata/Characters/StrDex/StrDex",
            "Metadata/Characters/StrInt/StrInt",
            "Metadata/Characters/DexInt/DexInt",
            "Metadata/Characters/StrDexInt/StrDexInt"
        };

        private static readonly Dictionary<string, int> TileSeenDict = new Dictionary<string, int>
        {
            [MapNames.MaoKun] = 3,
            [MapNames.Arena] = 3,
            [MapNames.CastleRuins] = 3,
            [MapNames.UndergroundRiver] = 3,
            [MapNames.TropicalIsland] = 3,
            [MapNames.Beach] = 5,
            [MapNames.Strand] = 5,
            [MapNames.Port] = 5,
            [MapNames.Alleyways] = 5,
            [MapNames.Phantasmagoria] = 5,
            [MapNames.Wharf] = 5,
            [MapNames.Cemetery] = 5,
            [MapNames.MineralPools] = 5,
            [MapNames.Temple] = 5,
            [MapNames.Malformation] = 5
        };

        private readonly ChatParser _chatParser = new ChatParser();
        private readonly Stopwatch _chatSw = Stopwatch.StartNew();

        private readonly OverlayWindow _overlay = new OverlayWindow(LokiPoe.ClientWindowHandle);
        private readonly TaskManager _taskManager = new TaskManager();
        private Coroutine _coroutine;
        private FollowBotGui _gui;


        internal static int LastBoundMoveSkillSlot
        {
            get
            {
                if (_lastBoundMoveSkillSlot == -1)
                    _lastBoundMoveSkillSlot = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill.Slot;
                return _lastBoundMoveSkillSlot;
            }
        }

        internal static Keys LastBoundMoveSkillKey
        {
            get
            {
                if (_lastBoundMoveSkillKey == Keys.Clear)
                    _lastBoundMoveSkillKey = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill.BoundKeys.Last();
                return _lastBoundMoveSkillKey;
            }
        }

        [CanBeNull]
        public static PartyMember LeaderPartyEntry =>
            LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);

        [CanBeNull]
        public static Player Leader
        {
            get
            {
                var leaderPartyEntry = LeaderPartyEntry;
                if (leaderPartyEntry?.PlayerEntry?.IsOnline != true)
                {
                    _leader = null;
                    return null;
                }

                var leaderName = leaderPartyEntry.PlayerEntry.Name;
                if (string.IsNullOrEmpty(leaderName) || leaderName == LokiPoe.Me.Name)
                {
                    _leader = null;
                    return null;
                }

                if (!LokiPoe.InGameState.PartyHud.IsInSameZone(leaderName))
                {
                    _leader = null;
                    return null;
                }

                if (_leader == null)
                {
                    //_leader = LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name == leaderName);
                    var playersOfClass = LokiPoe.ObjectManager.GetObjectsByMetadatas(PlayerMetadataList).ToList();
                    var leaderPlayer = playersOfClass.FirstOrDefault(x => x.Name == leaderName);
                    _leader = leaderPlayer as Player;

                    if (_leader == null)
                        _leader = LokiPoe.ObjectManager.GetObjectsByType<Player>()
                            .FirstOrDefault(x => x.Name == leaderName);
                }

                return _leader;
            }
            set => _leader = value;
        }

        private static int TileSeenRadius => TileSeenDict.TryGetValue(World.CurrentArea.Name, out var radius)
            ? radius
            : ExplorationSettings.DefaultTileSeenRadius;

        public void Start()
        {
            _lastBoundMoveSkillSlot = -1;
            _lastBoundMoveSkillKey = Keys.Clear;

            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            Explorer.CurrentDelegate = _ => CombatAreaCache.Current.Explorer.BasicExplorer;

            ComplexExplorer.ResetSettingsProviders();
            ComplexExplorer.AddSettingsProvider("FollowBot", MapBotExploration, ProviderPriority.Low);

            // Cache all bound keys.
            LokiPoe.Input.Binding.Update();

            // Reset the default MsBetweenTicks on start.
            Log.Debug($"[Start] MsBetweenTicks: {BotManager.MsBetweenTicks}.");
            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

            // Since this bot will be performing client actions, we need to enable the process hook manager.
            LokiPoe.ProcessHookManager.Enable();

            _coroutine = null;

            ExilePather.BlockLockedDoors = FeatureEnum.Disabled;
            ExilePather.BlockLockedTempleDoors = FeatureEnum.Disabled;
            ExilePather.BlockTrialOfAscendancy = FeatureEnum.Disabled;

            ExilePather.Reload();

            _taskManager.Reset();

            AddTasks();

            Events.Start();
            PluginManager.Start();
            RoutineManager.Start();
            _taskManager.Start();

            foreach (var plugin in PluginManager.EnabledPlugins)
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");

            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

            //if (ExilePather.BlockTrialOfAscendancy == FeatureEnum.Unset)
            //{
            //    //no need for this, map trials are in separate areas
            //    ExilePather.BlockTrialOfAscendancy = FeatureEnum.Enabled;
            //}
        }

        public void Tick()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            if (_coroutine == null) _coroutine = new Coroutine(() => MainCoroutine());

            ExilePather.Reload();
            Events.Tick();
            CombatAreaCache.Tick();
            _taskManager.Tick();
            PluginManager.Tick();
            RoutineManager.Tick();

            if (_chatSw.ElapsedMilliseconds > 250)
            {
                _chatParser.Update();
                _chatSw.Restart();
            }

            // Check to see if the coroutine is finished. If it is, stop the bot.
            if (_coroutine.IsFinished)
            {
                Log.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        public void Stop()
        {
            _taskManager.Stop();
            PluginManager.Stop();
            RoutineManager.Stop();

            // When the bot is stopped, we want to remove the process hook manager.
            LokiPoe.ProcessHookManager.Disable();

            // Cleanup the coroutine.
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            var id = message.Id;

            if (id == BotStructure.GetTaskManagerMessage)
            {
                message.AddOutput(this, _taskManager);
                handled = true;
            }
            else if (id == Messages.GetIsOnRun)
            {
                message.AddOutput(this, IsOnRun);
                handled = true;
            }
            else if (id == Messages.SetIsOnRun)
            {
                var value = message.GetInput<bool>();
                GlobalLog.Info($"[FollowBot] SetIsOnRun: {value}");
                IsOnRun = value;
                handled = true;
            }
            else if (message.Id == Events.Messages.AreaChanged)
            {
                Leader = null;
                handled = true;
            }

            Events.FireEventsFromMessage(message);

            var res = _taskManager.SendMessage(TaskGroup.Enabled, message);
            if (res == MessageResult.Processed)
                handled = true;

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }

        public void Initialize()
        {
            BotManager.OnBotChanged += BotManagerOnOnBotChanged;
            TimerService.EnableHighPrecisionTimers();
            _overlay.Start();
        }

        public void Deinitialize()
        {
            BotManager.OnBotChanged -= BotManagerOnOnBotChanged;
        }

        public string Name => "FollowBot";
        public string Author => "NotYourFriend, origial code from Unknown";
        public string Description => "Bot that follow leader.";
        public string Version => "0.0.7.0";
        public UserControl Control => _gui ?? (_gui = new FollowBotGui());
        public JsonSettings Settings => FollowBotSettings.Instance;


        private async Task MainCoroutine()
        {
            while (true)
            {
                if (LokiPoe.IsInLoginScreen)
                {
                    // Offload auto login logic to a plugin.
                    var logic = new Logic("hook_login_screen", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                }
                else if (LokiPoe.IsInCharacterSelectionScreen)
                {
                    // Offload character selection logic to a plugin.
                    var logic = new Logic("hook_character_selection", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                }
                else if (LokiPoe.IsInGame)
                {
                    // To make things consistent, we once again allow user coorutine logic to preempt the bot base coroutine logic.
                    // This was supported to a degree in 2.6, and in general with our bot bases. Technically, this probably should
                    // be at the top of the while loop, but since the bot bases offload two sets of logic to plugins this way, this
                    // hook is being placed here.
                    var hooked = false;
                    var logic = new Logic("hook_ingame", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            hooked = true;
                            break;
                        }

                    if (!hooked)
                    {
                        // Wait for game pause
                        if (LokiPoe.InstanceInfo.IsGamePaused)
                            Log.Debug("Waiting for game pause");
                        // Resurrect character if it is dead
                        else if (LokiPoe.Me.IsDead && World.CurrentArea.Id != "HallsOfTheDead_League")
                            await ResurrectionLogic.Execute();
                        // What the bot does now is up to the registered tasks.
                        else
                            await _taskManager.Run(TaskGroup.Enabled, RunBehavior.UntilHandled);
                    }
                }
                else
                {
                    // Most likely in a loading screen, which will cause us to block on the executor, 
                    // but just in case we hit something else that would cause us to execute...
                    await Coroutine.Sleep(1000);
                    continue;
                }

                // End of the tick.
                await Coroutine.Yield();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public TaskManager GetTaskManager()
        {
            return _taskManager;
        }

        private void BotManagerOnOnBotChanged(object sender, BotChangedEventArgs botChangedEventArgs)
        {
            if (botChangedEventArgs.New == this) ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
        }

        private void AddTasks()
        {
            _taskManager.Add(new ClearCursorTask());
            _taskManager.Add(new LootItemTask());
            _taskManager.Add(new PreCombatFollowTask());
            _taskManager.Add(new CombatTask(50));
            _taskManager.Add(new PostCombatHookTask());
            _taskManager.Add(new LevelGemsTask());
            _taskManager.Add(new CombatTask(-1));
            _taskManager.Add(new CastAuraTask());
            _taskManager.Add(new TravelToPartyZoneTask());
            _taskManager.Add(new TradeTask());
            _taskManager.Add(new FollowTask());
            _taskManager.Add(new JoinPartyTask());
            _taskManager.Add(new DefenseAndFlaskTask());
            _taskManager.Add(new QuestInteractionTask());
            _taskManager.Add(new TrialPickerTask());
            _taskManager.Add(new FallbackTask());
        }

        private static ExplorationSettings MapBotExploration()
        {
            if (!World.CurrentArea.IsMap)
                return new ExplorationSettings();

            OnNewMapEnter();

            return new ExplorationSettings(tileSeenRadius: TileSeenRadius);
        }

        private static void OnNewMapEnter()
        {
            var areaName = World.CurrentArea.Name;
            Log.Info($"[FollowBot] New map has been entered: {areaName}.");
            IsOnRun = true;
            Utility.BroadcastMessage(null, Messages.NewMapEntered, areaName);
        }

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }

        private static class Messages
        {
            public const string NewMapEntered = "MB_new_map_entered_event";
            public const string MapFinished = "MB_map_finished_event";
            public const string MapTrialEntered = "MB_map_trial_entered_event";
            public const string GetIsOnRun = "MB_get_is_on_run";
            public const string SetIsOnRun = "MB_set_is_on_run";
        }
    }
}