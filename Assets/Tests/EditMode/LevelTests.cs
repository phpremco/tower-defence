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
        private const string LevelPath = "Assets/_Project/Data/Levels/Level_C1_01.asset";

        // The first level with flyers and a second road (Crystal Plains level 3).
        private const string ForkPath = "Assets/_Project/Data/Levels/Level_C1_03.asset";

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
        public void Level3_WavesUseSeveralPathsIncludingFlyingRoutes()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ForkPath);
            var used = new System.Collections.Generic.HashSet<int>();
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                foreach (WaveDefinition wave in set.Waves)
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        used.Add(group.PathIndex);
                    }
                }
            }

            Assert.Greater(used.Count, 1);
            Assert.IsTrue(level.Paths.Length >= 3);
            Assert.IsTrue(level.Paths[1].Flying);
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
        public void GridCursor_StepsOneCellAndStaysInsideGrid()
        {
            var grid = new GridMap(new Vector2Int(4, 3), 1f);

            Assert.AreEqual(new Vector2Int(2, 1), GridCursor.Step(new Vector2Int(1, 1), Vector2.right, grid));
            Assert.AreEqual(new Vector2Int(1, 2), GridCursor.Step(new Vector2Int(1, 1), new Vector2(0.2f, 0.9f), grid));
            Assert.AreEqual(new Vector2Int(0, 1), GridCursor.Step(new Vector2Int(0, 1), Vector2.left, grid));
        }

        [Test]
        public void GridMap_WorldToCellRoundTrips()
        {
            var grid = new GridMap(new Vector2Int(16, 10), 1f);
            var cell = new Vector2Int(5, 7);

            Assert.AreEqual(cell, grid.WorldToCell(grid.CellToWorld(cell)));
        }

        [Test]
        public void BuildGrid_AllowsAnyCellExceptRoadBlockedAndOccupied()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            var grid = new BuildGrid(level);
            Vector2Int road = level.MainRoad[0];
            Vector2Int blocked = level.BlockedCells[0];
            Vector2Int free = FirstBuildable(grid, level);

            Assert.IsTrue(grid.IsRoad(road));
            Assert.IsFalse(grid.CanBuild(road));
            Assert.IsFalse(grid.CanBuild(blocked));
            Assert.IsTrue(grid.CanBuild(free));

            grid.Occupy(free);
            Assert.IsFalse(grid.CanBuild(free));
            grid.Release(free);
            Assert.IsTrue(grid.CanBuild(free));
        }

        // A cell the level data leaves open: off the road, no rock, no liquid, no mining node.
        private static Vector2Int FirstBuildable(BuildGrid grid, LevelDefinition level)
        {
            var closed = new HashSet<Vector2Int>(level.BlockedCells);
            closed.UnionWith(level.MineCells);
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.IsRoad(cell) && !closed.Contains(cell) && !level.Terrain.IsLiquid(cell))
                    {
                        return cell;
                    }
                }
            }

            Assert.Fail("the level has no open cell");
            return Vector2Int.zero;
        }
    }
}




