using System.Collections.Generic;
using Game.Drones;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Drones arrive one kind at a time in the campaign (2026-11-05): a kind is offered from its introduction level on and
    /// never taken away again, and the level that offers it first explains it in a dialogue that lights up its card.
    /// </summary>
    public sealed class DroneIntroTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        private static List<LevelDefinition> CampaignLevels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "no campaign catalog");
            var levels = new List<LevelDefinition>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    levels.Add(node.Level);
                }
            }

            return levels;
        }

        [Test]
        public void TheFirstLevel_OffersNoDrones()
        {
            List<LevelDefinition> levels = CampaignLevels();
            Assert.IsNotEmpty(levels);
            Assert.IsEmpty(levels[0].Drones, $"{levels[0].name} teaches turrets, the gate and the hero; drones come later");
        }

        [Test]
        public void AnOfferedDrone_StaysOfferedInEveryLaterLevel()
        {
            var offered = new HashSet<DroneDefinition>();
            foreach (LevelDefinition level in CampaignLevels())
            {
                var here = new HashSet<DroneDefinition>(level.Drones);
                foreach (DroneDefinition earlier in offered)
                {
                    Assert.IsTrue(here.Contains(earlier), $"{level.name} no longer offers the {earlier.DisplayName}");
                }

                offered.UnionWith(here);
            }

            Assert.AreEqual(3, offered.Count, "the first continent introduces all three drone kinds");
        }

        [Test]
        public void EveryDrone_IsIntroducedByADialogue_WhereItIsFirstOffered()
        {
            var offered = new HashSet<DroneDefinition>();
            foreach (LevelDefinition level in CampaignLevels())
            {
                foreach (DroneDefinition drone in level.Drones)
                {
                    if (offered.Contains(drone))
                    {
                        continue;
                    }

                    Assert.IsTrue(IntroducedIn(level, drone), $"{level.name} offers the {drone.DisplayName} for the first time but no dialogue introduces it");
                }

                offered.UnionWith(level.Drones);
            }
        }

        [Test]
        public void ADroneIntroduction_PlaysWhenTheLevelStarts_AndOnlyWhereTheDroneIsOffered()
        {
            foreach (LevelDefinition level in CampaignLevels())
            {
                var here = new HashSet<DroneDefinition>(level.Drones);
                foreach (StoryTrigger trigger in level.StoryTriggers)
                {
                    if (trigger.Dialogue == null || trigger.Dialogue.IntroducedDrones.Count == 0)
                    {
                        continue;
                    }

                    Assert.AreEqual(StoryTriggerKind.LevelStart, trigger.Kind, $"{level.name}: {trigger.Dialogue.name} should explain its drone before the first wave");
                    foreach (DroneDefinition drone in trigger.Dialogue.IntroducedDrones)
                    {
                        Assert.IsTrue(here.Contains(drone), $"{level.name}: {trigger.Dialogue.name} introduces the {drone.DisplayName}, which the level does not offer");
                    }
                }
            }
        }

        private static bool IntroducedIn(LevelDefinition level, DroneDefinition drone)
        {
            foreach (StoryTrigger trigger in level.StoryTriggers)
            {
                if (trigger.Dialogue != null && trigger.Dialogue.Introduces(drone))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
