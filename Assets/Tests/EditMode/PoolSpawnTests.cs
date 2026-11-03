using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Pools and nests as spawn points, checked against generated levels: the ponds of Crystal Plains level 5 and the rifts of level 4.</summary>
    public sealed class PoolSpawnTests
    {
        private const string PondsPath = "Assets/_Project/Data/Levels/Level_C1_05.asset";
        private const string RiftsPath = "Assets/_Project/Data/Levels/Level_C1_04.asset";

        private static LevelDefinition LoadPonds() => Load(PondsPath);

        private static LevelDefinition LoadRifts() => Load(RiftsPath);

        private static LevelDefinition Load(string path)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            Assert.IsNotNull(level, "Run Tools/Generate/Scenes and Starter Data first.");
            return level;
        }

        [Test]
        public void LiquidPools_GroupTheRiverPondsAndPickCellsInsideTheGrid()
        {
            LevelDefinition level = LoadPonds();
            var pools = new LiquidPools(level);

            Assert.Greater(pools.Count(TerrainKind.Water), 0, "the flats have ponds");
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
            LevelDefinition level = LoadRifts();
            Vector3 start = level.Grid.CellToWorld(level.Nests[0].Cell);

            Vector3[] route = OffRoadRoute.Build(level, start, out float progress);

            Assert.AreEqual(start, route[0]);
            Vector2Int join = level.Grid.WorldToCell(route[1]);
            Assert.IsTrue(level.Road.IsRoad(join), $"the second point {route[1]} is not on the road");
            Vector3[] main = level.Road.Route(0);
            Assert.AreEqual(main[main.Length - 1], route[route.Length - 1], "every road ends at the base");
            Assert.Greater(progress, 0f, "joining halfway counts as progress along the road");
        }

        [Test]
        public void Rifts_NestsLieOffTheRoadInTheRightTerrain_AndFeedTheWaves()
        {
            LevelDefinition level = LoadRifts();

            Assert.Greater(level.Nests.Length, 0, "the fault line has nests");
            bool stone = false;
            foreach (ElementNest nest in level.Nests)
            {
                Assert.IsFalse(level.Road.IsRoad(nest.Cell), $"nest at {nest.Cell} lies on the road");
                TerrainKind wanted = LevelValidator.NestTerrain(nest.Element);
                Assert.AreEqual(wanted == TerrainKind.Ground ? TerrainKind.Ground : wanted, TerrainMap.IsLiquid(wanted) ? level.Terrain.KindAt(nest.Cell) : TerrainKind.Ground, $"nest at {nest.Cell} sits in the wrong terrain");
                stone |= nest.Element == Game.Core.Element.Stone && level.Terrain.KindAt(nest.Cell) == TerrainKind.Rift;
            }

            Assert.IsTrue(stone, "stone bats rise from a rift on the fault line");

            bool nestSpawn = false;
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                foreach (WaveDefinition wave in set.Waves)
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        nestSpawn |= group.Source == SpawnSource.Nest && group.Enemy.Element != Game.Core.Element.None;
                    }
                }
            }

            Assert.IsTrue(nestSpawn, "some wave sends element creatures out of a nest");
        }

        [Test]
        public void Validator_RejectsPoolSpawnsWithoutPoolsAndDryPoolEntrances()
        {
            var theme = ScriptableObject.CreateInstance<VisualTheme>();
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var enemy = ScriptableObject.CreateInstance<Game.Enemies.EnemyDefinition>();
            var waves = ScriptableObject.CreateInstance<LevelWaves>();
            var difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            try
            {
                var waveProps = new SerializedObject(waves);
                SerializedProperty sets = waveProps.FindProperty("_difficultyWaves");
                sets.arraySize = 1;
                SerializedProperty set = sets.GetArrayElementAtIndex(0);
                set.FindPropertyRelative("Difficulty").objectReferenceValue = difficulty;
                SerializedProperty list = set.FindPropertyRelative("Waves");
                list.arraySize = 1;
                SerializedProperty groups = list.GetArrayElementAtIndex(0).FindPropertyRelative("_groups");
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
                so.FindProperty("_waves").objectReferenceValue = waves;
                so.ApplyModifiedPropertiesWithoutUndo();

                var errors = new List<string>();
                LevelValidator.Validate(level, errors);

                Assert.IsTrue(errors.Exists(e => e.Contains("Lava pool, but the level has none")), string.Join("\n", errors));
                Assert.IsTrue(errors.Exists(e => e.Contains("holds no liquid")), string.Join("\n", errors));
            }
            finally
            {
                Object.DestroyImmediate(difficulty);
                Object.DestroyImmediate(waves);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(level);
                Object.DestroyImmediate(theme);
            }
        }
    }
}
