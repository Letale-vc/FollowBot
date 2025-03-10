using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DreamPoeBot.Loki.Game.LokiPoe;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Helpers
{
    static class NpcHelper
    {
        public static async Task<bool> MoveToAndTalk(NetworkObject npcObj)
        {
            if (npcObj == null) return false;
            if (!npcObj.IsTargetable) return false;
            if (NpcDialogUi.IsOpened && RewardUi.IsOpened) return true;


            if (Me.Position.Distance(npcObj.Position) > 30)
            {
                GlobalLog.Debug($"[NcpHelper] try come to npc [{npcObj.Name}]");
                await npcObj.WalkablePosition().TryComeAtOnce();
            }

            await Coroutines.CloseBlockingWindows();

            return await PlayerAction.Interact(npcObj, () => NpcDialogUi.IsOpened || RewardUi.IsOpened, "Dialog open");
        }

        public static async Task<bool> TalkAndSkipDialog(NetworkObject npcObj)
        {
            if (!await MoveToAndTalk(npcObj)) return false;
            return await SkipDialog(npcObj);
        }

        public static async Task<bool> SkipDialog(NetworkObject obj)
        {
            if (NpcDialogUi.DialogDepth == 1 || !NpcDialogUi.IsOpened) return true;
            for (var i = 0; i < 10; i++)
            {
                Input.SimulateKeyEvent(Keys.Escape);
                await Wait.SleepSafe(250, 500);
                if (!obj.IsTargetable) return false;
                if (NpcDialogUi.DialogDepth == 1 && NpcDialogUi.IsOpened) return true;
                if (!NpcDialogUi.IsOpened && !RewardUi.IsOpened) return false;
            }

            return false;
        }

        public static bool SelectDialog(string dialogName)
        {
            if (!NpcDialogUi.IsOpened) return false;
            var dialog = NpcDialogUi.DialogEntries.Find(x => x.Text.ContainsIgnorecase(dialogName));
            if (dialog == null)
            {
                GlobalLog.Error($"[NpcHelper]: cannot find dialog : [{dialogName}]");
                return false;
            }

            if (NpcDialogUi.Converse(dialog.Text) != ConverseResult.None)
            {
                GlobalLog.Error($"[NpcHelper]: cannot converse with dialog : [{dialog.Text}]");
                return false;
            }

            return true;
        }


        public static async Task<bool> BanditInteract(NetworkObject bandit)
        {
            if (bandit == null) return false;

            await bandit.WalkablePosition().ComeAtOnce();

            var banditType = QuestHelper.GetTypeBandit(bandit.Name);
            var selectInteract = FollowBotSettings.Instance.SelectedBanditChoise;

            const int maxTries = 5;
            for (var i = 0; i < maxTries; i++)
            {
                if (!await OpenBanditPanel(bandit)) continue;

                GlobalLog.Debug($"[NpcHelper] try select kill bandit [{bandit.Name}]. Try: {i + 1}/{maxTries}");
                TalkToBanditResult result;
                switch (selectInteract)
                {
                    case InteractBanditChoise.KillAllBandits:
                        result = BanditPanel.KillBandit();
                        break;
                    case InteractBanditChoise.HelpAlira:
                        result = banditType == Bandits.Alira ? BanditPanel.HelpBandit() : BanditPanel.KillBandit();
                        break;
                    case InteractBanditChoise.HelpOak:
                        result = banditType == Bandits.Oakm ? BanditPanel.HelpBandit() : BanditPanel.KillBandit();
                        break;
                    case InteractBanditChoise.HelpKraityn:
                        result = banditType == Bandits.Kraityn ? BanditPanel.HelpBandit() : BanditPanel.KillBandit();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (result)
                {
                    case TalkToBanditResult.None:
                        return true;
                    case TalkToBanditResult.ProcessHookManagerNotEnabled:
                        return false;
                    case TalkToBanditResult.UiNotOpen:
                    case TalkToBanditResult.NoButtonFound:
                        continue;
                    case TalkToBanditResult.AlreadyChosen:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return false;
        }

        private static async Task<bool> OpenBanditPanel(NetworkObject bandit)
        {
            if (BanditPanel.IsOpened && !bandit.Components.NpcComponent.HasIconOverHead) return true;

            await PlayerAction.Interact(bandit);
            await Wait.Sleep(100);

            if (!await SkipDialog(bandit)) return false;

            return BanditPanel.IsOpened;
        }

        private static async Task<bool> TryMoveReward(InventoryControlWrapper rewardControl, Item reward,
            int maxTries = 3)
        {
            for (var attempt = 0; attempt < maxTries; attempt++)
            {
                var result = rewardControl.FastMoveReward(reward.LocalId);

                switch (result)
                {
                    // If the result is Failed, try again if attempts are remaining.
                    case FastMoveResult.Failed:
                        if (attempt == maxTries - 1)
                        {
                            GlobalLog.Error("[NpcHelper][TakeReward] Exceeded number of attempts with result Failed");
                            return false;
                        }

                        await Wait.Sleep(100);
                        continue;
                    // If any of these errors occur, log the error and return false.
                    case FastMoveResult.ProcessHookManagerNotEnabled:
                        GlobalLog.Error("[NpcHelper][TakeReward] ProcessHookManagerNotEnabled");
                        return false;
                    case FastMoveResult.CursorFull:
                        GlobalLog.Error("[NpcHelper][TakeReward] CursorFull");
                        return false;
                    case FastMoveResult.ItemNotFound:
                        GlobalLog.Error("[NpcHelper][TakeReward] ItemNotFound");
                        return false;
                    // For successful processing or unhandled cases.
                    case FastMoveResult.None:
                        GlobalLog.Debug($"[NpcHelper][TakeReward] Success take reward [{reward.Name}]");
                        return true;
                    case FastMoveResult.ItemTransparent:
                        GlobalLog.Error("[NpcHelper][TakeReward] ItemTransparent");
                        return false;
                    case FastMoveResult.Unsupported:
                        GlobalLog.Error("[NpcHelper][TakeReward] Unsupported");
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(result), result, null);
                }
            }

            // If the loop ends without returning, it means all attempts failed.
            return false;
        }

        public static async Task<bool> TakeReward(NetworkObject obj, string dialogName)
        {
            if (!await MoveToAndTalk(obj)) return false;
            if (!await SkipDialog(obj)) return false;

            if (!SelectDialog(dialogName)) return false;
            await Wait.Sleep(250);

            if (!RewardUi.IsOpened) return false;

            var rewardInvenoryControls = RewardUi.InventoryControls;
            if (rewardInvenoryControls.Count == 0) GlobalLog.Error("[NpcHelper] you see this error becaus I dont know");
            var rewardControl = rewardInvenoryControls[0];
            var reward = rewardControl.Inventory.Items[0];
            if (reward == null) return false;
            return await TryMoveReward(rewardControl, reward);
        }
    }
}