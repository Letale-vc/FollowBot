using DreamPoeBot.Loki.Game.GameData;
using System.Linq;

namespace FollowBot.Helpers
{
    public static class QuestHelper
    {
        public static bool CheckQuestStateId(string questId, int stateId)
        {
            var quest = Dat.QuestStates.FirstOrDefault(q => q.Quest.Id == questId);
            if (quest == null) return false;
            return quest.Id == stateId;
        }


        public static bool CheckQuestStateId(string questId, int[] stateIds)
        {
            var quest = Dat.QuestStates.FirstOrDefault(q => q.Quest.Id == questId);
            return quest != null && stateIds.Contains(quest.Id);
        }

        public static Bandits GetTypeBandit(string banditName)
        {
            switch (banditName)
            {
                case "Kraityn, Scarbearer": return Bandits.Kraityn;
                case "Oak, Skullbreaker": return Bandits.Oakm;
                case "Alira Darktongue": return Bandits.Alira;
                default: return Bandits.None;
            }
        }
    }

    public enum InteractBanditChoise
    {
        KillAllBandits,
        HelpAlira,
        HelpOak,
        HelpKraityn
    }

    public enum Bandits
    {
        None,
        Alira,
        Oakm,
        Kraityn
    }
}