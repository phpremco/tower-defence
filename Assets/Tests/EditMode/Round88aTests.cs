using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88a, the findings of Remco's playtest of 2026-11-20 that are rules: no tower on a spawn point (he built one on a
    /// nest's opening), and an air-only gun reaches the ball of energy that walks the road (the Wisp is a target of the
    /// ground and of the air).
    /// </summary>
    public sealed class Round88aTests
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";

        private static IEnumerable<LevelDefinition> Levels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // ---- no tower on a spawn point ---------------------------------------------------------------------------

        [Test]
        public void NoCellOnASpawnPoint_IsABuildCell_OnAnyLevel()
        {
            int nests = 0;
            foreach (LevelDefinition level in Levels())
            {
                var build = new BuildGrid(level);
                GridMap grid = level.Grid;
                foreach (ElementNest nest in level.Nests)
                {
                    nests++;
                    // Independent of the rule's own code: the opening's slabs (2.8 cells across) lie over the nest's cell and
                    // its eight neighbours.
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            Vector2Int cell = nest.Cell + new Vector2Int(x, y);
                            Assert.IsFalse(build.CanBuild(cell), $"{level.name}: {cell} beside the {nest.Element} nest at {nest.Cell} takes a tower");
                        }
                    }
                }

                for (int p = 0; p < level.Paths.Length; p++)
                {
                    Vector2Int start = grid.WorldToCell(level.Road.Route(p)[0]);
                    Assert.IsFalse(build.CanBuild(start), $"{level.name}: the cell {start} under the gate of path {p} takes a tower");
                }

                var errors = new List<string>();
                LevelValidator.ValidateLayout(level, errors);
                CollectionAssert.IsEmpty(errors, $"{level.name} fails its validator");
            }

            Assert.Greater(nests, 0, "no level with a nest was found");
        }

        [Test]
        public void TheOpeningsArt_FitsTheRadiusTheRuleKeepsFree()
        {
            var seen = new HashSet<SpawnOriginDefinition>();
            foreach (LevelDefinition level in Levels())
            {
                SpawnOriginDefinition origins = level.SpawnOrigins;
                if (origins == null || !seen.Add(origins))
                {
                    continue;
                }

                foreach (TerrainKind terrain in System.Enum.GetValues(typeof(TerrainKind)))
                {
                    SpawnOriginArt art = origins.For(terrain);
                    if (art == null)
                    {
                        continue;
                    }

                    float radius = 0f;
                    foreach (SkinnedMeshRenderer skinned in art.Model.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        Bounds bounds = skinned.sharedMesh.bounds;
                        Matrix4x4 toRoot = art.Model.Model.transform.worldToLocalMatrix * skinned.transform.localToWorldMatrix;
                        for (int corner = 0; corner < 4; corner++)
                        {
                            Vector3 point = toRoot.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, 0f, (corner & 2) == 0 ? -1f : 1f)));
                            radius = Mathf.Max(radius, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z)) * art.Model.Scale);
                        }
                    }

                    TestContext.WriteLine($"{origins.name} {terrain}: half {radius:0.00} m across");
                    Assert.Greater(radius, 0f, $"{origins.name} {terrain}: the opening has no mesh");
                    Assert.LessOrEqual(radius, SpawnClearance.NestRadiusCells * level.CellSize + 0.05f, $"{origins.name} {terrain}: the opening is wider than the ground the rule keeps free");
                }
            }

            Assert.Greater(seen.Count, 0, "no level with living spawn origins was found");
        }

        [Test]
        public void TheValidator_RefusesAMiningNodeOnASpawnPoint()
        {
            LevelDefinition source = null;
            foreach (LevelDefinition level in Levels())
            {
                if (level.Nests.Length > 0)
                {
                    source = level;
                    break;
                }
            }

            Assert.IsNotNull(source, "no level with a nest");
            LevelDefinition copy = Object.Instantiate(source);
            try
            {
                Vector2Int beside = default;
                bool found = false;
                for (int x = -1; x <= 1 && !found; x++)
                {
                    for (int y = -1; y <= 1 && !found; y++)
                    {
                        Vector2Int cell = source.Nests[0].Cell + new Vector2Int(x, y);
                        if ((x != 0 || y != 0) && source.Grid.Contains(cell) && !source.Road.IsRoad(cell) && !source.Terrain.IsLiquid(cell))
                        {
                            beside = cell;
                            found = true;
                        }
                    }
                }

                Assert.IsTrue(found, "the nest has no dry neighbour off the road");
                var serialized = new SerializedObject(copy);
                SerializedProperty mines = serialized.FindProperty("_mineCells");
                mines.arraySize++;
                mines.GetArrayElementAtIndex(mines.arraySize - 1).vector2IntValue = beside;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var errors = new List<string>();
                LevelValidator.ValidateLayout(copy, errors);
                Assert.IsTrue(errors.Exists(e => e.Contains("mining node") && e.Contains(beside.ToString())), "no error for a mining node on the nest's opening: " + string.Join(" | ", errors));
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void SpawnClearance_KeepsTheRingRoundANest_AndOnlyTheCellUnderASmallPod()
        {
            var grid = new GridMap(new Vector2Int(9, 9), 1f);
            var nest = new SpawnClearance.Prop("nest", grid.CellToWorld(new Vector2Int(4, 4)), SpawnClearance.NestRadiusCells);
            var cells = new List<Vector2Int>();
            SpawnClearance.Collect(nest, grid, cells);
            Assert.AreEqual(9, cells.Count, "a nest keeps its own cell and its eight neighbours free");
            CollectionAssert.Contains(cells, new Vector2Int(5, 5));
            CollectionAssert.DoesNotContain(cells, new Vector2Int(6, 4));

            var pod = new SpawnClearance.Prop("pod", grid.CellToWorld(new Vector2Int(4, 4)), 0.5f);
            cells.Clear();
            SpawnClearance.Collect(pod, grid, cells);
            Assert.AreEqual(1, cells.Count, "a prop of one cell across keeps one cell free");
        }

        // ---- the ball of energy is a target of the ground and of the air -------------------------------------------

        [Test]
        public void AnAirOnlyGun_ReachesTheWispOnTheRoad_AndNeverAPlainWalker()
        {
            TargetLayers wisp = TargetLayers.Ground | TargetLayers.Air;
            Assert.IsTrue(TargetLayers.Air.CanHit(wisp), "an air-only gun must reach a creature of both layers");
            Assert.IsTrue(TargetLayers.Ground.CanHit(wisp), "a ground-only gun must reach a creature of both layers");
            Assert.IsTrue((TargetLayers.Ground | TargetLayers.Air).CanHit(wisp));
            Assert.IsFalse(TargetLayers.Air.CanHit(TargetLayers.Ground), "an air-only gun never hits a plain walker");
            Assert.IsFalse(TargetLayers.Ground.CanHit(TargetLayers.Air), "a ground-only gun never hits a flyer");
            Assert.IsFalse(TargetLayers.None.CanHit(wisp));
        }

        [Test]
        public void TheWispAndTheCoreWisp_AreOnBothLayers_AndEveryAirOnlyTowerOfTheGameReachesThem()
        {
            foreach (string id in new[] { "Wisp", "CoreWisp" })
            {
                var creature = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
                Assert.IsNotNull(creature, id);
                Assert.AreEqual(TargetLayers.Ground | TargetLayers.Air, creature.Layer, $"{id} must be a target of the ground and of the air");
                Assert.IsFalse(creature.Flies, $"{id} walks the road");
                int airOnly = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" }))
                {
                    var tower = AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                    if (tower.Targets == TargetLayers.None)
                    {
                        continue;
                    }

                    airOnly += tower.Targets == TargetLayers.Air ? 1 : 0;
                    Assert.IsTrue(tower.Targets.CanHit(creature.Layer), $"{tower.name} cannot aim at the {id}");
                }

                Assert.Greater(airOnly, 0, "the game has no air-only tower to hold the rule against");
            }
        }

        [Test]
        public void TheRegistry_HandsTheWispToAnAirOnlyGun_AndItsBurstSplashesIt()
        {
            var registry = new TargetRegistry();
            var walker = new StubTarget { Layer = TargetLayers.Ground, DistanceToGoal = 1f };
            var wisp = new StubTarget { Layer = TargetLayers.Ground | TargetLayers.Air, DistanceToGoal = 5f };
            registry.Add(walker);
            registry.Add(wisp);
            Assert.AreSame(wisp, registry.FindBest(Vector3.zero, 5f, TargetLayers.Air), "the air-only gun passes the walker over and takes the wisp");
            var found = new List<ITargetable>();
            registry.FindInRadius(Vector3.zero, 1f, TargetLayers.Air, found);
            CollectionAssert.AreEquivalent(new[] { wisp }, found, "a burst in the air splashes the wisp, not the walker");
        }

        private sealed class StubTarget : ITargetable
        {
            public TargetLayers Layer { get; set; }
            public float DistanceToGoal { get; set; }
            public Vector3 Position => Vector3.zero;
            public bool IsAlive => true;

            public void TakeDamage(float amount)
            {
            }

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }
    }
}
