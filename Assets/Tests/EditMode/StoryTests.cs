using System.Collections.Generic;
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
        public void GeneratedCampaign_HasAnIntroAndABriefingPerLevel()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            Assert.IsNotNull(catalog.Story, "the campaign carries its story");
            Assert.GreaterOrEqual(catalog.Story.IntroBeats.Count, 3, "enough beats for space, approach and landing");
            foreach (StoryBeat beat in catalog.Story.IntroBeats)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(beat.Text));
                Assert.GreaterOrEqual(beat.Seconds, 1f);
            }

            Assert.AreEqual(StoryScene.Landing, catalog.Story.IntroBeats[catalog.Story.IntroBeats.Count - 1].Scene, "the intro ends with the landing");
            foreach (MapNode node in catalog.Nodes)
            {
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
        public void PlanetPainter_TintsRegionsAndStaysInsideTheDisc()
        {
            const int size = 128;
            var regions = new List<PlanetPainter.Region>
            {
                new PlanetPainter.Region { Position = new Vector2(0.5f, 0.5f), Colour = Color.red, Radius = 0.3f },
            };
            Texture2D plain = PlanetPainter.PaintSurface(size, new List<PlanetPainter.Region>(), Vector2.one);
            Texture2D tinted = PlanetPainter.PaintSurface(size, regions, Vector2.one);
            Texture2D clouds = PlanetPainter.PaintClouds(size);

            Color centre = tinted.GetPixel(size / 2, size / 2);
            Color untinted = plain.GetPixel(size / 2, size / 2);
            Assert.Greater(centre.r - centre.g, untinted.r - untinted.g, "the region colour shows at its centre");
            Assert.AreEqual(0f, tinted.GetPixel(2, 2).a, "corners outside the disc are transparent");
            Assert.AreEqual(0f, clouds.GetPixel(2, 2).a, "clouds stay inside the disc");
            Assert.Greater(centre.a, 0.99f, "the disc itself is opaque");

            Object.DestroyImmediate(plain);
            Object.DestroyImmediate(tinted);
            Object.DestroyImmediate(clouds);
        }
    }
}
