using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Guards the wave storage format: one <see cref="LevelWaves"/> asset per level holds every wave of every difficulty,
    /// and nothing else lies in the waves folder, so the file count grows with levels and never with waves.
    /// </summary>
    public sealed class WaveStorageTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string WavesFolder = "Assets/_Project/Data/Waves";

        [Test]
        public void EveryCampaignLevel_KeepsItsWavesInOneAssetForAllDifficulties()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var expected = new HashSet<string>();
            foreach (MapNode node in catalog.Nodes)
            {
                LevelDefinition level = node.Level;
                if (level == null)
                {
                    continue;
                }

                Assert.IsNotNull(level.Waves, $"{level.name} has no wave asset.");
                string path = AssetDatabase.GetAssetPath(level.Waves);
                Assert.IsTrue(path.StartsWith(WavesFolder + "/"), $"{level.name} keeps its waves outside {WavesFolder}: {path}");
                Assert.IsTrue(expected.Add(path), $"{level.name} shares its wave asset with another level.");
                Assert.AreEqual(catalog.Difficulties.Count, level.DifficultyWaves.Length, $"{level.name} lacks waves for a difficulty.");
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    Assert.AreSame(catalog.Difficulties[d], level.DifficultyWaves[d].Difficulty, $"{level.name} lists its difficulties out of order.");
                    Assert.Greater(level.DifficultyWaves[d].Waves.Length, 0, $"{level.name} {catalog.Difficulties[d].name} has no waves.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { WavesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.IsTrue(expected.Contains(path), $"{path} belongs to no campaign level.");
            }
        }
    }
}
