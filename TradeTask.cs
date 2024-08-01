using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot
{
    class TradeTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => "Letale";

        public string Description => "Trade task";

        public string Name => "TradeTask";

        public string Version => "0.1";


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
        public async Task<bool> Run()
        {

            if (LokiPoe.InGameState.NotificationHud.IsOpened && LokiPoe.InstanceInfo.PartyStatus == DreamPoeBot.Loki.Game.GameData.PartyStatus.PartyMember)
            {
                await HandleTradeRequest();

            }
            else
            {
                return false;
            }


            var currentArea = World.CurrentArea;

            if (LokiPoe.InGameState.TradeUi.IsOpened)
            {

                if (!currentArea.IsHideoutArea)
                {
                    Log.InfoFormat("[TradeTask] Start Trade in map");
                    //  Map trade logic
                    while (TradeUi.IsOpened)
                    {
                        List<Item> allItems = TradeUi.TradeControl.InventoryControl_OtherOffer.Inventory.Items;

                        var transparentItems = allItems?.Where(
                                                      (Item item) => TradeUi.TradeControl.InventoryControl_OtherOffer.IsItemTransparent(item.LocalId));
                        foreach (Item item in transparentItems)
                        {
                            if (!(TradeUi.TradeControl.AcceptButtonText != "accept") || !(TradeUi.TradeControl.AcceptButtonText != "accept (0)"))
                            {
                                int itemId = item.LocalId;
                                TradeControlWrapper tradeControl2 = TradeUi.TradeControl;
                                tradeControl2?.InventoryControl_OtherOffer.ViewItemsInInventory((ShouldViewItemDelegate)((Inventory inventory, Item invenoryItem) => invenoryItem.LocalId == itemId), (Func<bool>)(() => TradeUi.IsOpened));
                                continue;
                            }
                            int rand = LokiPoe.Random.Next(2000, 3000);
                            await Coroutine.Sleep(rand);
                        }
                        if (TradeUi.TradeControl.AcceptButtonText == "accept" && TradeUi.TradeControl.OtherAcceptedTheOffert)
                        {
                            TradeUi.TradeControl.Accept(true);
                            Log.InfoFormat("[TradeTask] Accepting trade");
                            await Coroutines.CloseBlockingWindows();
                            await Coroutines.LatencyWait();
                        }

                    }
                }
                if (currentArea.IsHideoutArea)
                {
                    Log.InfoFormat("[TradeTask] Start Trade in Hideot");
                    // TODO: Add hideout trade logic
                    while (TradeUi.IsOpened)
                    {
                        if (TradeUi.TradeControl.MeAcceptedTheOffert)
                        {
                            Log.InfoFormat("[TradeTask] Wait accept trade");
                            continue;
                        }
                        var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
                        foreach (Item item in mainInventoryItems)
                        {
                            List<Item> playerInvenoryItems = TradeUi.TradeControl.InventoryControl_YourOffer.Inventory.Items;
                            //if (playerInvenoryItems.Any(e => e.LocalId == item.LocalId))
                            //{
                            //    continue;
                            //}
                            InventoryUi.InventoryControl_Main.FastMove(item.LocalId, true, false);

                            await Wait.SleepSafe(LokiPoe.Random.Next(30, 70));

                        }
                        var playerInvenoryTradeItems2 = TradeUi.TradeControl.InventoryControl_YourOffer.Inventory.Items;
                        if (playerInvenoryTradeItems2.Count == mainInventoryItems.Count && TradeUi.TradeControl.AcceptButtonText == "accept")
                        {
                            TradeUi.TradeControl.Accept(true);
                            Log.InfoFormat("[TradeTask] Accepting trade");
                            await Coroutines.LatencyWait();
                        }

                    }


                }

                return true;
            }
            return true;

        }
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
        private static async Task<bool> HandleTradeRequest()
        {
            if (LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count > 0)
            {
                FollowBot.Log.WarnFormat($"[FollowBot] Visible Notifications: {LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count}");
                LokiPoe.InGameState.ProcessNotificationEx isTradeRequestToBeAccepted = (x, y) =>
                {
                    var res = y == LokiPoe.InGameState.NotificationType.Trade && (string.IsNullOrEmpty(FollowBotSettings.Instance.InviteWhiteList) || FollowBotSettings.Instance.InviteWhiteList.Contains(x.CharacterName));
                    FollowBot.Log.WarnFormat($"[FollowBot] Detected {y} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting? {res}");
                    return res;
                };

                var anyVis = LokiPoe.InGameState.NotificationHud.NotificationList.Any(x => x.IsVisible);
                if (anyVis)
                {
                    return false;
                }
                var ret = LokiPoe.InGameState.NotificationHud.HandleNotificationEx(isTradeRequestToBeAccepted);
                FollowBot.Log.WarnFormat($"[HandleTradeRequest] Result: {ret}");
                await Coroutines.LatencyWait();
                if (ret == LokiPoe.InGameState.HandleNotificationResult.Accepted) return true;
            }
            return false;

        }

    }


}
