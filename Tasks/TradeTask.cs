using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Tasks
{
    internal class TradeTask : ITask
    {
        private TradeControlWrapper _tradeControl;

        public async Task<bool> Run()
        {
            if (LokiPoe.Me.PartyStatus != PartyStatus.PartyMember)
                return false;

            if (!FindTradeRequest()) return false;

            await Wait.SleepSafe(500);
            if (!await HandleTradeRequest())
            {
                Log.DebugFormat("[TradeTask] HandleTradeRequest is false");
                return false;
            }

            await Wait.SleepSafe(200);

            if (!TradeUi.IsOpened)
            {
                Log.DebugFormat("[TradeTask] TradeUi is not opened");
                return false;
            }

            _tradeControl = TradeUi.TradeControl;

            if (_tradeControl == null)
            {
                Log.Debug("[TradeTask] TradeControl is null");
                return false;
            }

            var currentArea = LokiPoe.LocalData.WorldArea;

            if (currentArea.IsCombatArea)
            {
                Log.DebugFormat("[TradeTask] Start take items");
                if (!await ProcessTakeItem())
                {
                    Log.DebugFormat("[TradeTask] HandleGiveItem is false");
                    CancelTrade();
                    await Coroutines.CloseBlockingWindows();
                    return false;
                }
            }
            else if (currentArea.IsHideoutArea)
            {
                Log.DebugFormat("[TradeTask] Start Trade in Hideout");

                if (!await ProcessGiveItem())
                {
                    Log.DebugFormat("[TradeTask] HandleGiveItem is false");
                    CancelTrade();
                    await Coroutines.CloseBlockingWindows();
                    return false;
                }
            }
            else
            {
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            await WaitForAcceptButtonState();

            var buttonPressResult = await AcceptTrade();
            var waitResult = false;

            if (buttonPressResult)
                waitResult = await Wait.For(() => !TradeUi.IsOpened, "Closed trade", 100, 5000);


            await Coroutines.CloseBlockingWindows();
            return buttonPressResult && waitResult;
        }

        private async Task<bool> ProcessGiveItem()
        {
            var inventoryItems = Inventories.InventoryItems.Where(x => x.Rarity != Rarity.Quest)
                .OrderByDescending(x => x.Size.Y).ToList();
            Log.DebugFormat($"[TradeTask] Main Inventory Items: {inventoryItems.Count}");

            foreach (var item in inventoryItems)
                if (!await TryMoveItemToTrade(item))
                    return false;

            return true;
        }


        private async Task<bool> TryMoveItemToTrade(Item item, int attempts = 3)
        {
            for (var i = 0; i <= attempts; i++)
            {
                InventoryUi.InventoryControl_Main.FastMove(item.LocalId);
                var waitResult = await Wait.For(() =>
                    {
                        var yourOfferInventoryItems = _tradeControl.InventoryControl_YourOffer.Inventory.Items;
                        return yourOfferInventoryItems.Any(x => x.Id == item.Id);
                    }, $"Try move item to trade. Attempt: {i}/{attempts}", 100, 500);

                if (i == attempts && !waitResult) return false;
                if (waitResult) return true;
            }

            return false;
        }

        private async Task WaitForTransparentItems()
        {
            var items = _tradeControl.InventoryControl_OtherOffer.Inventory.Items;
            var transparentItems = items.Where(
                item => _tradeControl.InventoryControl_OtherOffer.IsItemTransparent(item.LocalId)).Select(item => item.LocalId).ToList();

            if (transparentItems.Count == 0) return;

            Log.DebugFormat($"[TradeTask] Find {transparentItems.Count()} transparent items.");

            foreach (var itemId in transparentItems)
            {
                _tradeControl.InventoryControl_OtherOffer.ViewItemsInInventory(
                    (_, invenoryItem) => invenoryItem.LocalId == itemId, () => TradeUi.IsOpened);
                await Wait.SleepSafe(100);
            }

            await Wait.LatencySleep();
        }

        private async Task<bool> ProcessTakeItem()
        {
            while (TradeUi.IsOpened && !BotManager.IsStopping)
            {
                if (_tradeControl.OtherAcceptedTheOffert && string.IsNullOrEmpty(_tradeControl.ConfirmLabelText))
                    return true;
                await WaitForTransparentItems();

                if (string.IsNullOrEmpty(_tradeControl.ConfirmLabelText) && _tradeControl.OtherAcceptedTheOffert)
                    Log.DebugFormat($"{Name} Unable to accept trade because: [{_tradeControl.ConfirmLabelText}]");
            }
            return false;
        }

        private async Task WaitForAcceptButtonState()
        {
            if (!string.Equals(_tradeControl.AcceptButtonText, "accept"))
                while (!BotManager.IsStopping && TradeUi.IsOpened &&
                       !string.Equals(_tradeControl.AcceptButtonText, "accept"))
                {
                    Log.DebugFormat("[TradeTask] Waiting button state");
                    await Wait.Sleep(100);
                }
        }

        private async Task<bool> AcceptTrade()
        {
            var tradeControl = TradeUi.TradeControl;
            if (tradeControl == null)
            {
                Log.DebugFormat("[TradeTask] TradeControl is null");
                return false;
            }

            var tradeResult = _tradeControl.Accept();

            await Wait.SleepSafe(200);

            if (tradeResult != TradeResult.None)
            {
                Log.DebugFormat($"[TradeTask] Result trade: {tradeResult}");
                return false;
            }

            Log.DebugFormat("[TradeTask] Result trade: Accepting trade");
            return true;
        }

        private void CancelTrade()
        {
            var tradeControl = TradeUi.TradeControl;
            if (tradeControl == null)
            {
                Log.DebugFormat("[TradeTask] TradeControl is null");
                return;
            }

            var tradeResult = tradeControl.Cancel();
            if (tradeResult != TradeResult.None)
            {
                Log.DebugFormat($"[TradeTask] Result trade: {tradeResult}");
                return;
            }

            Log.DebugFormat("[TradeTask] Result trade: Accepting trade");
        }

        private static async Task<bool> HandleTradeRequest()
        {
            var hasValidNotification =
                NotificationHud.NotificationList.Any(x =>
                    x.IsVisible && x.NotificationTypeEnum == NotificationType.Trade);
            if (!hasValidNotification) return false;
            var result = NotificationHud.HandleNotificationEx(IsTradeRequestToBeAccepted);
            FollowBot.Log.WarnFormat($"[HandleTradeRequest] Result: {result}");
            await Coroutines.ReactionWait();

            return result == HandleNotificationResult.Accepted;

            bool IsTradeRequestToBeAccepted(NotificationData x, NotificationType y)
            {
                var res = y == NotificationType.Trade &&
                          (FollowBot.Leader?.Name == x.CharacterName ||
                           FollowBotSettings.Instance.InviteWhiteList.ToLower().Contains(x.CharacterName.ToLower()));
                FollowBot.Log.WarnFormat(
                    $"[FollowBot] Detected {y} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting?: {res}");
                return res;
            }
        }

        private static bool FindTradeRequest()
        {
            return NotificationHud.NotificationList.Any(x =>
                x.IsVisible && x.NotificationTypeEnum == NotificationType.Trade &&
                (FollowBot.Leader?.Name == x.CharName ||
                 FollowBotSettings.Instance.InviteWhiteList.ToLower().Contains(x.CharName.ToLower())));
        }

        #region skip

        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => string.Empty;

        public string Description => string.Empty;

        public string Name => "TradeTask";

        public string Version => string.Empty;

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

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

        #endregion
    }
}