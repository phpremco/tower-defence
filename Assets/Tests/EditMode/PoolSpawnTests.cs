using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Pools as spawn points and boss entrances, checked against the generated forest level.</summary>
    public sealed class PoolSpawnTests
    {
        private const string ForestPath = "Assets/_Project/Data/Levels/Level_Forest.asset";

        private static LevelDefinition LoadForest()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ForestPath);
            Assert.IsNotNull(level, "Run Tools/Generate/Scenes and Starter Data first.");
            return level;
        }

        [Test]
        public void LiquidPools_GroupTheForestPondsAndPickCellsInsideTheGrid()
        {
            LevelDefinition level = LoadForest();
            var pools = new LiquidPools(level);

            Assert.Greater(pools.Count(TerrainKind.Water), 0, "the forest has ponds");
            Assert.AreEqual(0, pools.Count(TerrainKind.Lava), "but no lava");

            for (int i = 0; i < pools.Count(TerrainKind.Water); i++)
            {
                Vector2Int cell = pools.SurfaceCell(pools.Pool(TerrainKind.Water, i), 3);
                Assert.IsTrue(level.Grid.Contains(cell), $"pool {i} surfaces off the grid at {cell}");
                Assert.IsTrue(level.Terrain.IsLiquid(cell), $"pool {i} surfaces on dry land at {cell}");
            }
        }

        [Test]
        public void OffRoadRoute_JoinsTheNearestRoadAndEndsAtTheBase()
        {
            LevelDefinition level = LoadForest();
            Vector3 start = level.Grid.CellToWorld(level.BossEntrance.Cell);

            Vector3[] route = OffRoadRoute.Build(level, start, out float progress);

            Assert.AreEqual(start, route[0]);
            Vector2Int join = level.Grid.WorldToCell(route[1]);
            Assert.IsTrue(level.Road.IsRoad(join), $"the second point {route[1]} is not on the road");
            Vector3[] main = level.Road.Route(0);
            Assert.AreEqual(main[main.Length - 1], route[route.Length - 1], "every road ends at the base");
            Assert.Greater(progress, 0f, "joining halfway counts as progress along the road");
        }

        [Test]
        public void Forest_BossComesUpOutOfAPond()
        {
            LevelDefinition level = LoadForest();

            Assert.AreEqual(EntranceKind.Pool, level.BossEntrance.Kind);
            Assert.IsTrue(level.Terrain.IsLiquid(level.BossEntrance.Cell));
        }

        [Test]
        public void Validator_RejectsPoolSpawnsWithoutPoolsAndDryPoolEntrances()
        {
            var theme = ScriptableObject.CreateInstance<VisualTheme>();
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var enemy = ScriptableObject.CreateInstance<Game.Enemies.EnemyDefinition>();
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            var difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            try
            {
                var waveProps = new SerializedObject(wave);
                SerializedProperty groups = waveProps.FindProperty("_groups");
                groups.arraySize = 1;
                SerializedProperty group = groups.GetArrayElementAtIndex(0);
                group.FindPropertyRelative("Enemy").objectReferenceValue = enemy;
                group.FindPropertyRelative("Count").intValue = 1;
                group.FindPropertyRelative("Interval").floatValue = 1f;
                group.FindPropertyRelative("Source").enumValueIndex = (int)SpawnSource.Lava;
                waveProps.ApplyModifiedPropertiesWithoutUndo();

                var so = new SerializedObject(level);
                so.FindProperty("_theme").objectReferenceValue = theme;
                so.FindProperty("_gridSize").vector2IntValue = new Vector2Int(8, 4);
                SerializedProperty paths = so.FindProperty("_paths");
                paths.arraySize = 1;
                SerializedProperty path = paths.GetArrayElementAtIndex(0);
                path.FindPropertyRelative("Name").stringValue = "Road";
                path.FindPropertyRelative("Flying").boolValue = false;
                SerializedProperty waypoints = path.FindPropertyRelative("Waypoints");
                waypoints.arraySize = 2;
                waypoints.GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(0, 1);
                waypoints.GetArrayElementAtIndex(1).vector2IntValue = new Vector2Int(6, 1);
                SerializedProperty entrance = so.FindProperty("_bossEntrance");
                entrance.FindPropertyRelative("Kind").enumValueIndex = (int)EntranceKind.Pool;
                entrance.FindPropertyRelative("Cell").vector2IntValue = new Vector2Int(2, 2);
                SerializedProperty sets = so.FindProperty("_difficultyWaves");
                sets.arraySize = 1;
                SerializedProperty set = sets.GetArrayElementAtIndex(0);
                set.FindPropertyRelative("Difficulty").objectReferenceValue = difficulty;
                SerializedProperty waves = set.FindPropertyRelative("Waves");
                waves.arraySize = 1;
                waves.GetArrayElementAtIndex(0).objectReferenceValue = wave;
                so.ApplyModifiedPropertiesWithoutUndo();

                var errors = new List<string>();
                LevelValidator.Validate(level, errors);

                Assert.IsTrue(errors.Exists(e => e.Contains("Lava pool, but the level has none")), string.Join("\n", errors));
                Assert.IsTrue(errors.Exists(e => e.Contains("holds no liquid")), string.Join("\n", errors));
            }
            finally
            {
                Object.DestroyImmediate(difficulty);
                Object.DestroyImmediate(wave);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(level);
                Object.DestroyImmediate(theme);
            }
        }
    }
}
