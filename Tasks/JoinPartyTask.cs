using System;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using FollowBot.Helpers;
using log4net;

namespace FollowBot.Tasks
{
    internal class JoinPartyTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();

        public async Task<bool> Run()
        {
            var partyStatus = LokiPoe.Me.PartyStatus;
            switch (partyStatus)
            {
                case PartyStatus.PartyMember:
                    return false;
                case PartyStatus.PartyLeader:
                    await PartyHelper.LeaveParty();
                    return true;
                case PartyStatus.None:
                case PartyStatus.Invited:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var invite = LokiPoe.InstanceInfo.PendingPartyInvites;
            if (invite.Count == 0) return false;
            Log.DebugFormat("[JoinPartyTask] Found party invite count: {0}", invite.Count);
            await PartyHelper.HandlePartyInviteNew();
            return partyStatus != PartyStatus.None;
        }

        #region skip

        public string Name => "JoinPartyTask";
        public string Description => "This task will ask for party.";
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

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        #endregion
    }
}