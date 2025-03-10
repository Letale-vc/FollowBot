using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using FollowBot.Helpers;
using log4net;

namespace FollowBot.Tasks
{
    internal class QuestInteractionTask : ITask
    {
        private readonly ILog _log = Logger.GetLoggerInstanceForType();

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.EnableQuestHelper) return false;

            var areaId = LokiPoe.CurrentWorldArea.Id;

            return await InteractWithTarget(areaId);
        }


        private async Task<bool> InteractWithTarget(string areaId)
        {
            var objects = LokiPoe.ObjectManager.Objects;
            foreach (var interaction in QuestInteractionList.Interactions)
            {
                if (areaId != interaction.ActId) continue;


                var target = objects.FirstOrDefault(x => x.Name == interaction.TargetName);

                if (target == null) continue;
                if (!target.IsTargetable) return false;
                if (interaction.IsNpc && target.Reaction != Reaction.Npc) continue;

                if (!interaction.TriggerAction(target)) continue;
                if (!LokiPoe.CurrentWorldArea.IsTown && LokiPoe.Me.Position.Distance(target.Position) > 50) continue;

                _log.Debug($"[{Name}: Find target [{interaction.TargetName}]");

                try
                {
                    await interaction.Action(target);
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"[{Name}] Error interacting with target {interaction.TargetName}: {ex}");
                }
            }

            return false;
        }


        #region No used

        public string Author => "Letale";
        public string Description => "Quest interact";
        public string Name => "QuestInteract";
        public string Version => "0.0.0.1";

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