using System.Collections.Generic;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Checks the generated level 1 data and the grid helpers.</summary>
    public sealed class LevelTests
    {
        private const string LevelPath = "Assets/_Project/Data/Levels/Level_Landing.asset";

        [Test]
        public void Level1_PassesValidator()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            Assert.IsNotNull(level, "Run Tools/Generate/Scenes and Starter Data first.");

            var errors = new List<string>();
            bool valid = LevelValidator.Validate(level, errors);

            Assert.IsTrue(valid, string.Join("\n", errors));
        }

        [Test]
        public void Level1_HasWavesForEveryDifficulty()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);

            Assert.AreEqual(4, level.DifficultyWaves.Length);
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                Assert.IsNotNull(level.GetWaves(set.Difficulty));
            }
        }

        [Test]
        public void Level1_TowersHaveIncreasingUpgradeValues()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);

            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                for (int i = 1; i <= tower.MaxLevelIndex; i++)
                {
                    Assert.Greater(tower.GetLevel(i).Damage, tower.GetLevel(i - 1).Damage, tower.name);
                }
            }
        }

        [Test]
        public void LandPathCells_IncludesCellsBetweenWaypoints()
        {
            var path = new[] { new Vector2Int(0, 0), new Vector2Int(3, 0) };

            var cells = new List<Vector2Int>(LevelValidator.LandPathCells(path));

            CollectionAssert.AreEqual(
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) },
                cells);
        }

        [Test]
        public void GridMap_CentresGridOnOrigin()
        {
            var grid = new GridMap(new Vector2Int(4, 2), 1f);

            Assert.AreEqual(new Vector3(-1.5f, 0f, -0.5f), grid.CellToWorld(new Vector2Int(0, 0)));
            Assert.AreEqual(new Vector3(1.5f, 0f, 0.5f), grid.CellToWorld(new Vector2Int(3, 1)));
        }

        [Test]
        public void SlotCursor_MovesToClosestSlotInDirection()
        {
            var slots = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(0f, 0f, 3f) };

            Assert.AreEqual(1, SlotCursor.Next(slots, 0, Vector2.right));
            Assert.AreEqual(3, SlotCursor.Next(slots, 0, Vector2.up));
            Assert.AreEqual(0, SlotCursor.Next(slots, 0, Vector2.left));
        }
    }
}
