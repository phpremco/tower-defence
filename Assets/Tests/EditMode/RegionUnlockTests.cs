using System.Collections.Generic;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>A continent opens once the last level of the one before it is beaten, on any difficulty.</summary>
    public sealed class RegionUnlockTests
    {
        private const int PerRegion = 20;
        private const int SitesPerRow = 5;
        private const float SiteStepDegrees = 10f;
        private const float FakeRegionRadius = 40f;
        private const float RegionBandDegrees = 120f;

        private sealed class MemorySave : ISaveService
        {
            public void Save<T>(string key, T data)
            {
            }

            public bool TryLoad<T>(string key, out T data)
            {
                data = default;
                return false;
            }
        }

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        private static float RegionLongitude(int region) => -120f + region * RegionBandDegrees;

        private CampaignCatalog FakeCampaign(int regions)
        {
            var catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
            _made.Add(catalog);
            var so = new SerializedObject(catalog);
            SerializedProperty regionArray = so.FindProperty("_regions");
            regionArray.arraySize = regions;
            for (int r = 0; r < regions; r++)
            {
                // Each fake region is a cap wide enough for its sites, a third of the way round the planet from the next.
                SerializedProperty region = regionArray.GetArrayElementAtIndex(r);
                region.FindPropertyRelative("Longitude").floatValue = RegionLongitude(r);
                region.FindPropertyRelative("Latitude").floatValue = 0f;
                region.FindPropertyRelative("RadiusDegrees").floatValue = FakeRegionRadius;
            }

            SerializedProperty nodes = so.FindProperty("_nodes");
            nodes.arraySize = regions * PerRegion;
            for (int i = 0; i < regions * PerRegion; i++)
            {
                var level = ScriptableObject.CreateInstance<LevelDefinition>();
                level.name = $"Fake_{i}";
                _made.Add(level);
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                node.FindPropertyRelative("Region").intValue = i / PerRegion;
                node.FindPropertyRelative("Level").objectReferenceValue = level;
                // Sites spread on a grid well over the minimum spacing, so the catalogue's own checks stay quiet.
                int site = i % PerRegion;
                node.FindPropertyRelative("Longitude").floatValue = RegionLongitude(i / PerRegion) + (site % SitesPerRow - (SitesPerRow - 1) * 0.5f) * SiteStepDegrees;
                node.FindPropertyRelative("Latitude").floatValue = (site / SitesPerRow - 1.5f) * SiteStepDegrees;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        [Test]
        public void NextRegion_OpensAfterTheTwentiethLevelOfThePreviousOne()
        {
            CampaignCatalog catalog = FakeCampaign(3);
            var progress = new ProgressService(new MemorySave(), new string[0]);

            Assert.IsTrue(CampaignRules.IsRegionOpen(catalog, 0, progress));
            Assert.IsFalse(CampaignRules.IsRegionOpen(catalog, 1, progress));

            for (int i = 0; i < PerRegion - 1; i++)
            {
                progress.MarkCompleted(catalog.Nodes[i].Level.Id, "Easy");
            }

            Assert.IsFalse(CampaignRules.IsRegionOpen(catalog, 1, progress), "nineteen of twenty is not enough");
            progress.MarkCompleted(catalog.Nodes[PerRegion - 1].Level.Id, "Psycho");
            Assert.IsTrue(CampaignRules.IsRegionOpen(catalog, 1, progress), "the twentieth on any difficulty opens the next continent");
            Assert.IsFalse(CampaignRules.IsRegionOpen(catalog, 2, progress));
        }
    }
}
