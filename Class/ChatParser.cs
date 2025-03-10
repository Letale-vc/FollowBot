using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using FollowBot.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatPanel = DreamPoeBot.Loki.Game.LokiPoe.InGameState.ChatPanel;

namespace FollowBot.Class
{
    public class ChatParser
    {
        private volatile string _lastMd5;
        private volatile Dictionary<string, bool> _treatedMd5List = new Dictionary<string, bool>();
        public bool shouldCleanMessages;

        public ChatParser()
        {
            _treatedMd5List.Clear();
            shouldCleanMessages = true;
            _lastMd5 = "";
        }

        public static LokiPoe.InGameState.ChatResult SendChatMsg(string msg, bool closeChatUi = true)
        {
            if (string.IsNullOrEmpty(msg)) return LokiPoe.InGameState.ChatResult.None;
            if (!chatPanel.IsOpened) chatPanel.ToggleChat();

            if (!chatPanel.IsOpened) return LokiPoe.InGameState.ChatResult.UiNotOpen;

            var result = chatPanel.Chat(msg);

            if (!closeChatUi) return result;
            if (chatPanel.IsOpened)
                chatPanel.ToggleChat();

            return result;
        }

        private void CleanMessage()
        {
            if (!LokiPoe.IsInGame) return;
            SendChatMsg("/cls");
            var msgs = chatPanel.Messages.ToList();
            if (msgs.Count <= 0) return;

            for (var i = 0; i < msgs.Count; i++)
            {
                var chatEntry = msgs[i];
                _lastMd5 = chatEntry.MD5;
                if (_treatedMd5List.TryGetValue(chatEntry.MD5, out var alreadyTreated))
                {
                    if (alreadyTreated) continue;

                    _treatedMd5List[chatEntry.MD5] = true;
                }
                else
                {
                    _treatedMd5List.Add(chatEntry.MD5, true);
                }
            }
        }

        public void Update()
        {
            if (!LokiPoe.IsInGame) return;
            if (shouldCleanMessages)
            {
                CleanMessage();
                shouldCleanMessages = false;
            }

            var msgs = chatPanel.Messages;

            if (msgs.Count <= 0) return;

            var lastMd5 = msgs.Last().MD5;

            if (lastMd5 == _lastMd5) return;

            _lastMd5 = lastMd5;

            foreach (var chatEntry in msgs)
            {
                if (_treatedMd5List.TryGetValue(chatEntry.MD5, out var alreadyTreated))
                {
                    if (alreadyTreated) continue;

                    _treatedMd5List[chatEntry.MD5] = true;
                }
                else
                {
                    _treatedMd5List.Add(chatEntry.MD5, true);
                }

                try
                {
                    ProcessNewMessage(chatEntry);
                }
                catch (Exception)
                {
                    // Suppressing all Exception without warming. This is a bad practice, under mormal circustance you want to know what went wrong.
                    //GlobalLog.Error($"{e}");
                }
            }
        }

        private void ProcessNewMessage(chatPanel.ChatEntry newmessage)
        {
            if (newmessage == null) return;

            switch (newmessage.MessageType)
            {
                case chatPanel.MessageType.Local:
                case chatPanel.MessageType.Global:
                    break;
                case chatPanel.MessageType.Party:
                case chatPanel.MessageType.Whisper:
                    ProcessPartyMessage(newmessage);
                    break;
                case chatPanel.MessageType.Trade:
                case chatPanel.MessageType.Guild:
                case chatPanel.MessageType.System:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static Task ProcessPartyMessage(chatPanel.ChatEntry newmessage)
        {
            if (FollowBot.LeaderPartyEntry == null || FollowBot.LeaderPartyEntry.PlayerEntry == null)
                return Task.CompletedTask;
            var leadername = FollowBot.LeaderPartyEntry.PlayerEntry.Name;
            if (string.IsNullOrEmpty(leadername))
                return Task.CompletedTask;
            if (newmessage.RemoteName != leadername)
                return Task.CompletedTask;
            var start = newmessage.Message.IndexOf($"{leadername}:", StringComparison.InvariantCulture) +
                        $"{leadername}:".Length + 1;
            var end = newmessage.Message.Length - start;
            var command = newmessage.Message.Substring(start, end);

            GlobalLog.Warn($"Recieved Message: {newmessage.Message}, Command: {command}");

            if (command == FollowBotSettings.Instance.OpenTownPortalChatCommand)
                DefenseAndFlaskTask.ShouldOpenPortal = true;

            if (command == FollowBotSettings.Instance.TeleportToLeaderChatCommand)
                DefenseAndFlaskTask.ShouldTeleport = true;

            if (command == FollowBotSettings.Instance.StartFollowChatCommand)
                FollowBotSettings.Instance.ShouldFollow = true;
            if (command == FollowBotSettings.Instance.StopFollowChatCommand)
                FollowBotSettings.Instance.ShouldFollow = false;

            if (command == FollowBotSettings.Instance.StartAttackChatCommand)
                FollowBotSettings.Instance.ShouldKill = true;
            if (command == FollowBotSettings.Instance.StopAttackChatCommand)
                FollowBotSettings.Instance.ShouldKill = false;

            if (command == FollowBotSettings.Instance.StartLootChatCommand)
                FollowBotSettings.Instance.ShouldLoot = true;
            if (command == FollowBotSettings.Instance.StopLootChatCommand)
                FollowBotSettings.Instance.ShouldLoot = false;

            if (command == FollowBotSettings.Instance.StartAutoTeleportChatCommand)
                FollowBotSettings.Instance.DontPortOutofMap = false;
            if (command == FollowBotSettings.Instance.StopAutoTeleportChatCommand)
                FollowBotSettings.Instance.DontPortOutofMap = true;

            if (command == FollowBotSettings.Instance.StartSentinelChatCommand)
                FollowBotSettings.Instance.UseStalkerSentinel = true;
            if (command == FollowBotSettings.Instance.StopSentinelChatCommand)
                FollowBotSettings.Instance.UseStalkerSentinel = false;
            return Task.CompletedTask;
        }
    }
}