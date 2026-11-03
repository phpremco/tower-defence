using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class StoryTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        [Test]
        public void GeneratedCampaign_HasAnOpeningCutsceneAndABriefingPerLevel()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            Assert.IsNotNull(catalog.Story, "the campaign carries its story");
            Assert.IsNotNull(catalog.Story.OpeningCutscene, "the first launch opens with the cutscene");
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                Assert.IsFalse(string.IsNullOrWhiteSpace(node.Level.RegionName), $"{node.DisplayName} names its region");
                Assert.IsFalse(string.IsNullOrWhiteSpace(node.Level.Briefing), $"{node.DisplayName} has a briefing");
            }
        }

        [Test]
        public void LevelSession_AsksForABriefingOnlyWhenTold()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            DifficultyProfile difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();

            Assert.IsFalse(new LevelSession(level, difficulty).Briefing, "tests and tools skip the briefing");
            Assert.IsTrue(new LevelSession(level, difficulty, true).Briefing);

            Object.DestroyImmediate(level);
            Object.DestroyImmediate(difficulty);
        }

        [Test]
        public void PlanetPainter_HaloFadesFromTheCentreAndStaysInsideTheDisc()
        {
            const int size = 64;
            Texture2D halo = PlanetPainter.PaintHalo(size);

            Assert.Greater(halo.GetPixel(size / 2, size / 2).a, 0.9f, "the halo is strongest in the middle");
            Assert.Less(halo.GetPixel(size / 2, size - 4).a, halo.GetPixel(size / 2, size / 2 + 8).a, "and fades outwards");
            Assert.AreEqual(0f, halo.GetPixel(1, 1).a, "corners outside the disc are transparent");

            Object.DestroyImmediate(halo);
        }
    }
}
