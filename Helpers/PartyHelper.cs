using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;

namespace FollowBot.Helpers
{
    public static class PartyHelper
    {
        public static List<string> PartyPortal = new List<string>
        {
            "portal pls",
            "Portal ples",
            "new portal pls",
            "can i have a portal?",
            "new gate pls",
            "bro give me a port",
            "door pls",
            "port",
            "Port pls",
            "1 to teleport",
            "tp"
        };

        public static async Task<bool> HandlePartyInvite()
        {
            if (LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count <=
                0) return false;

            FollowBot.Log.WarnFormat(
                $"[FollowBot] Visible Notifications: {LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count}");

            var anyVis = LokiPoe.InGameState.NotificationHud.NotificationList.Any(x => x.IsVisible);
            if (anyVis) await Wait.Sleep(500);
            var ret = LokiPoe.InGameState.NotificationHud.HandleNotificationEx(IsPartyRequestToBeAccepted);
            FollowBot.Log.WarnFormat($"[HandlePartyInvite] Result: {ret}");
            await Coroutines.LatencyWait();
            return ret == LokiPoe.InGameState.HandleNotificationResult.Accepted;

            bool IsPartyRequestToBeAccepted(LokiPoe.InGameState.NotificationData x,
                LokiPoe.InGameState.NotificationType y)
            {
                var res = y == LokiPoe.InGameState.NotificationType.Party &&
                          (string.IsNullOrEmpty(FollowBotSettings.Instance.InviteWhiteList) ||
                           FollowBotSettings.Instance.InviteWhiteList.Contains(x.CharacterName));
                FollowBot.Log.WarnFormat(
                    $"[FollowBot] Detected {y.ToString()} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting? {res}");
                return res;
            }
        }

        public static async Task<bool> OpenSocialPanel()
        {
            var key = LokiPoe.Input.Binding.open_social_panel;
            LokiPoe.Input.SimulateKeyEvent(key);
            await Wait.LatencySleep();
            LokiPoe.Input.SimulateKeyEvent(key, false);
            return LokiPoe.InGameState.SocialUi.IsOpened;
        }

        public static async Task<bool> OpenCreatPartyPanel()
        {
            if (!await OpenSocialPanel())
                return false;
            var result = LokiPoe.InGameState.SocialUi.SwitchToPartyTab();
            return result == SwitchToTabResult.None;
        }

        public static async Task<bool> HandlePartyInviteNew()
        {
            if (!await OpenCreatPartyPanel())
                return false;
            var whiteListName = FollowBotSettings.Instance.InviteWhiteList.Trim();
            var resAccepPartyInv = LokiPoe.InGameState.SocialUi.HandlePendingPartyInviteNew(whiteListName);
            await Wait.LatencySleep();
            return resAccepPartyInv == LokiPoe.InGameState.HandlePendingPartyInviteResut.Accepted;
        }

        public static async Task<bool> LeaveParty()
        {
            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
                LokiPoe.InGameState.ChatPanel.ToggleChat();

            if (!LokiPoe.InGameState.ChatPanel.IsOpened) return false;
            LokiPoe.InGameState.ChatPanel.Chat("/kick " + LokiPoe.Me.Name);
            await Coroutines.LatencyWait();

            if (LokiPoe.InGameState.ChatPanel.IsOpened)
                LokiPoe.InGameState.ChatPanel.ToggleChat();

            return true;
        }

        public static async Task<bool> GoToPartyHideOut(string name)
        {
            await Coroutines.CloseBlockingWindows();
            await Coroutines.LatencyWait();
            LokiPoe.InGameState.PartyHud.OpenContextMenu(name);
            var ret = LokiPoe.InGameState.ContextMenu.VisitHideout();
            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            return ret == LokiPoe.InGameState.ContextMenuResult.None;
        }

        public static async Task<bool> FastGotoPartyZone(string name)
        {
            await Coroutines.CloseBlockingWindows();
            await Coroutines.LatencyWait();
            var ret = LokiPoe.InGameState.PartyHud.FastGoToZone(name);
            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            if (ret != LokiPoe.InGameState.FastGoToZoneResult.None)
            {
                GlobalLog.Error($"[FastGotoPartyZone] Returned Error: {ret}");
                return false;
            }

            if (LokiPoe.InGameState.GlobalWarningDialog.IsOpened)
                LokiPoe.InGameState.GlobalWarningDialog.ConfirmDialog();
            return true;
        }
    }
}