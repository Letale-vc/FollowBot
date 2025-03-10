using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FollowBot.Helpers
{
    public class QuestInteractionInfo
    {
        public string ActId { get; set; }
        public string TargetName { get; set; }
        public Func<NetworkObject, bool> TriggerAction { get; set; }
        public Func<NetworkObject, Task> Action { get; set; }
        public bool IsNpc { get; set; }
    }

    public static class QuestInteractionList
    {
        public static List<QuestInteractionInfo> Interactions { get; } = new List<QuestInteractionInfo>
        {
            // Objects
            new QuestInteractionInfo
            {
                ActId = "1_1_3",
                TargetName = "Strange Glyph Wall",
                TriggerAction =
                    _ => Inventories.HasItem(new[] { "Haliotis Glyph", "Roseus Glyph", "Ammonite Glyph" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_12",
                TargetName = "Tree Roots",
                TriggerAction = _ => Inventories.HasItem(new[] { "Maligaro's Spike" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_9",
                TargetName = "Thaumetic Seal",
                TriggerAction = _ => Inventories.HasItem(new[] { "Thaumetic Emblem" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_11",
                TargetName = "Ancient Seal",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_2",
                TargetName = "Sewer Grating",
                TriggerAction = _ => Inventories.HasItem(new[] { "Sewer Keys" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_10_1",
                TargetName = "Undying Blockage",
                TriggerAction = _ => Inventories.HasItem(new[] { "Infernal Talc" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_15",
                TargetName = "Locked Door",
                TriggerAction = _ => Inventories.HasItem(new[] { "Tower Key" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_4_town",
                TargetName = "Deshret's Seal",
                TriggerAction = _ => Inventories.HasItem(new[] { "Deshret's Banner" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_4_3_2",
                TargetName = "Deshret's Spirit",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_5_3",
                TargetName = "Templar Courts Entrance",
                TriggerAction = _ => Inventories.HasItem(new[] { "Eyes of Zeal" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "2_6_4",
                TargetName = "Fortress Gate",
                TriggerAction = _ => Inventories.HasItem(new[] { "Eye of Conquest" }),
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            // TODO: May bee add
            // new QuestInteractionInfo
            // {
            //     ActId = "2_6_14",
            //     TargetName = "Ignition Switch",
            //     TriggerAction = _ => Inventories.HasItem(new[] { "The Black Flag" }),
            //     Action = PlayerAction.Interact,
            //     IsNpc = false
            // },
            new QuestInteractionInfo
            {
                ActId = "2_6_14",
                TargetName = "The Beacon",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "2_7_5_2",
                TargetName = "Secret Passage",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "2_7_9",
                TargetName = "Firefly",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_3_1",
                TargetName = "Tolman",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_2",
                TargetName = "Stash",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_7",
                TargetName = "Blackguard Chest",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_9",
                TargetName = "Supply Container",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_9",
                TargetName = "Theurgic Precipitate Machine",
                TriggerAction = _ => true,
                Action = PlayerAction.Interact,
                IsNpc = false
            },
            // NPCs
            new QuestInteractionInfo
            {
                ActId = "1_2_town",
                TargetName = "Eramir",
                TriggerAction = _ =>
                    Inventories.HasItem(new[]
                        { "Alira's Amulet", "Kraityn's Amulet", "Oak's Amulet" }),
                Action = async obj => await NpcHelper.TakeReward(obj, "Take the Apex"),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_1_9",
                TargetName = "Captain Fairgraves",
                TriggerAction = _ => Inventories.HasItem("Allflame"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_town",
                TargetName = "Clarissa",
                TriggerAction = _ => Inventories.HasItem("Tolman's Bracelet"),
                Action = async obj => await NpcHelper.TakeReward(obj, "Take Sewer Keys"),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_1",
                TargetName = "Clarissa",
                TriggerAction = obj => obj.Components.NpcComponent.HasIconOverHead,
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_4",
                TargetName = "Kraityn, Scarbearer",
                TriggerAction = obj => obj.Components.NpcComponent.HasIconOverHead,
                Action = async obj => await NpcHelper.BanditInteract(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_12",
                TargetName = "Oak, Skullbreaker",
                TriggerAction = obj => obj.Components.NpcComponent.HasIconOverHead,
                Action = async obj => await NpcHelper.BanditInteract(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_2_9",
                TargetName = "Alira Darktongue",
                TriggerAction = obj => obj.Components.NpcComponent.HasIconOverHead,
                Action = async obj => await NpcHelper.BanditInteract(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_3_8_2",
                TargetName = "Lady Dialla",
                TriggerAction = _ => Inventories.HasItem(new[] { "Ribbon Spool", "Thaumetic Sulphite" }),
                Action = async obj => await NpcHelper.TakeReward(obj, "Take Infernal Talc"),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_4_3_3",
                TargetName = "Lady Dialla",
                TriggerAction = _ => Inventories.HasItem(new[] { "The Eye of Fury", "The Eye of Desire" }),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_4_6_2",
                TargetName = "Piety",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a4q1", 15),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "1_4_6_3",
                TargetName = "Piety",
                TriggerAction = _ =>
                    Inventories.HasItem(new[] { "Malachai's Heart", "Malachai's Entrails", "Malachai's Lungs" }),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_7_5_1",
                TargetName = "Silk",
                TriggerAction = _ => Inventories.HasItem("Black Venom"),
                Action = async obj => await NpcHelper.TakeReward(obj, "Black Death Reward"),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_7_11",
                TargetName = "Yeena",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a7q7", 3),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_8_8",
                TargetName = "Clarissa",
                TriggerAction = _ => Inventories.HasItem("Ankh of Eternity"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_8_8",
                TargetName = "Clarissa",
                TriggerAction = obj => obj.Components.NpcComponent.HasIconOverHead,
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_town",
                TargetName = "Petarus and Vanja",
                TriggerAction = _ => Inventories.HasItem("Storm Blade"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_town",
                TargetName = "Sin",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a9q1", 27),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_town",
                TargetName = "Sin",
                TriggerAction = _ => Inventories.HasItem("Basilisk Acid"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_8",
                TargetName = "Sin",
                TriggerAction = _ => Inventories.HasItem("Basilisk Acid"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_town",
                TargetName = "Petarus and Vanja",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a9q5", 7),
                Action = async obj => await NpcHelper.TakeReward(obj, "Take Bottled Storm"),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_9_8",
                TargetName = "Sin",
                TriggerAction = _ => Inventories.HasItem("Trarthan Powder"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_10_1",
                TargetName = "Bannon",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a10q1", 4),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_10_town",
                TargetName = "Bannon",
                TriggerAction = _ => Inventories.HasItem("The Staff of Purity"),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            },
            new QuestInteractionInfo
            {
                ActId = "2_10_2",
                TargetName = "Innocence",
                TriggerAction = _ => QuestHelper.CheckQuestStateId("a10q3", 10),
                Action = async obj => await NpcHelper.TalkAndSkipDialog(obj),
                IsNpc = true
            }
        };
    }
}