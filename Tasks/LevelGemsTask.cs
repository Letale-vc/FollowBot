using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;

namespace FollowBot.Tasks
{
    public class LevelGemsTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private readonly WaitTimer _levelWait = WaitTimer.FiveSeconds;
        private bool _needsToCloseInventory;
        private bool _needsToUpdate = true;

        private readonly Func<Inventory, Item, Item, bool> eval = (inv, holder, gem) =>
        {
            // Ignore any "globally ignored" gems. This just lets the user move gems around
            // equipment, without having to worry about where or what it is.
            if (ContainsHelper(gem.Name, gem.SkillGemLevel))
            {
                if (FollowBotSettings.Instance.GemDebugStatements)
                    Log.DebugFormat("[LevelGemsTask] {0}[Lev: {1}] => {2}.", gem.Name, gem.SkillGemLevel,
                        "Is contained in GlobalNameIgnoreList");

                return false;
            }

            // Now look though the list of skillgem strings to level, and see if the current gem matches any of them.
            var ss = string.Format("{0} [{1}: {2}]", gem.Name, inv.PageSlot, holder.GetSocketIndexOfGem(gem));


            var gemsToConsider = new ObservableCollection<string>();
            if (FollowBotSettings.Instance.LevelOffhandOnly)
            {
                var userSkillGems = FollowBotSettings.Instance.UserSkillGemsInOffHands;
                foreach (var g in userSkillGems)
                    gemsToConsider.Add($"{g.Name} [{g.InventorySlot}: {g.SocketIndex}]");
            }
            else if (FollowBotSettings.Instance.LevelAllGems)
            {
                var userSkillGems = FollowBotSettings.Instance.UserSkillGems;
                foreach (var g in userSkillGems)
                    gemsToConsider.Add($"{g.Name} [{g.InventorySlot}: {g.SocketIndex}]");
            }
            if (!gemsToConsider.Any(str => str.Equals(ss, StringComparison.OrdinalIgnoreCase))) return false;
            if (FollowBotSettings.Instance.GemDebugStatements)
                Log.DebugFormat("[LevelGemsTask] Adding {0} To gems to level.", gem.Name);
            return true;
            // No match, we shouldn't level this gem.
        };

        public string Name => "LevelGemsTask";
        public string Description => "This task will Level gems.";
        public string Author => "Alcor75";
        public string Version => "0.0.0.1";


        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }

        public async Task<bool> Run()
        {
            // Don't update while we are not in the game.
            if (!LokiPoe.IsInGame) return false;
            // Don't try to do anything when the escape state is active.
            if (LokiPoe.StateManager.IsEscapeStateActive) return false;

            // Don't level skill gems if we're dead.
            if (LokiPoe.Me.IsDead) return false;
            // Can't level skill gems under this scenario either.
            if (LokiPoe.InGameState.SkillsUi.IsOpened) return false;
            // Can't level gems when favor Ui is open
            if (LokiPoe.InGameState.RitualFavorsUi.IsOpened) return false;
            // Only check for skillgem leveling at a fixed interval.
            if (!_needsToUpdate && !_levelWait.IsFinished) return false;

            // If we have icons on the hud to process.
            if (LokiPoe.InGameState.SkillGemHud.AreIconsDisplayed)
                // If the InventoryUi is already opened, skip this logic and let the next set run.
                if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    // We need to close blocking windows.
                    await Coroutines.CloseBlockingWindows();

                    // We need to let skills finish casting, because of 2.6 changes.
                    await Coroutines.FinishCurrentAction();
                    await Coroutines.LatencyWait();

                    var res = LokiPoe.InGameState.SkillGemHud.HandlePendingLevelUps(eval);

                    Log.InfoFormat("[LevelGemsTask] SkillGemHud.HandlePendingLevelUps returned {0}.", res);

                    return false;
                }

            if (LokiPoe.InGameState.InventoryUi.IsOpened)
                _needsToCloseInventory = false;
            else
                _needsToCloseInventory = true;
            if (_needsToUpdate)
            {
                // We need the inventory panel open.
                if (!await Inventories.OpenInventory())
                {
                    Log.ErrorFormat("[LevelGemsTask] OpenInventoryPanel failed.");
                    return false;
                }

                retry:
                // If we have icons on the inventory ui to process.
                // This is only valid when the inventory panel is opened.
                if (LokiPoe.InGameState.InventoryUi.AreIconsDisplayed)
                {
                    var res = LokiPoe.InGameState.InventoryUi.HandlePendingLevelUps(eval);

                    Log.InfoFormat("[LevelGemsTask] InventoryUi.HandlePendingLevelUps returned {0}.", res);
                    if (res == LokiPoe.InGameState.HandlePendingLevelUpResult.GemDismissed ||
                        res == LokiPoe.InGameState.HandlePendingLevelUpResult.GemLeveled)
                        goto retry;
                }
            }

            // Just wait 5-10s between checks.
            _levelWait.Reset(TimeSpan.FromMilliseconds(LokiPoe.Random.Next(5000, 10000)));

            if (_needsToCloseInventory)
            {
                await Coroutines.CloseBlockingWindows();
                _needsToCloseInventory = false;
            }

            _needsToUpdate = false;
            return false;
        }


        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            if (message.Id == "player_leveled_event")
            {
                _needsToUpdate = true;
                handled = true;
            }

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }

        private static bool ContainsHelper(string name, int level)
        {
            foreach (var entry in FollowBotSettings.Instance.GlobalNameIgnoreList)
            {
                var ignoreArray = entry.Split(',');
                if (ignoreArray.Length == 1)
                {
                    if (ignoreArray[0].Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    if (ignoreArray[0].Equals(name, StringComparison.OrdinalIgnoreCase) &&
                        level >= Convert.ToInt32(ignoreArray[1])) return true;
                }
            }

            return false;
        }
    }
}