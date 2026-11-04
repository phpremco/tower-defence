using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Core;
using Game.Drones;
using Game.Enemies;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// The imported creature, tower and colony models under load: every buildable cell within reach of the road holds a
    /// tower at its top level (all tower types in turn, a full build-out), every drone kind flies as many as the level
    /// allows, every path object in the data is placed along the road (import stage 3a), then the densest wave of Crysium Vein (C1_06) and of
    /// C1_05 on the hardest difficulty is released on its own spawn timing with the wave clock off and measured over a stretch of frames on
    /// the High (PC) and the Low (Switch) model profile. Reports the main-thread player-loop time per frame (average and
    /// 95th percentile) and how many skinned meshes are drawn, and fails above the budget below. The editor adds its
    /// own overhead to every frame, so the budget is an editor budget: a player build runs well under it.
    /// </summary>
    public sealed class ModelPerformanceTests
    {
        // Editor budget for the 95th-percentile player loop with a full wave on the field (30 frames a second).
        private const float BudgetMilliseconds = 33f;
        private const int MeasuredFrames = 180;
        private const int SettleFrames = 30;
        private const float SpawnTimeScale = 4f;
        private const float SpawnTimeoutSeconds = 90f;
        private const string ReportPath = "Logs/model_perf.txt";

        // The build-out: towers on free cells this close to the road, up to this many (a dense late-level defence).
        private const int BuildOutReach = 2;
        private const int BuildOutLimit = 60;

        // Path objects: one of each kind every this many road cells, from this cell on, this many rounds.
        private const int GadgetSpacing = 4;
        private const int GadgetStart = 8;
        private const int GadgetRounds = 2;
        private const string PathObjectFolder = "Assets/_Project/Data/PathObjects";

        private static readonly string[] Levels = { "Level_C1_06", "Level_C1_05" };

        private string _savePath;
        private byte[] _saveBytes;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "Saves", "progress.json");
            _saveBytes = File.Exists(_savePath) ? File.ReadAllBytes(_savePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            QualitySettings.lodBias = 1f;
            if (_saveBytes != null)
            {
                File.WriteAllBytes(_savePath, _saveBytes);
            }
        }

        [UnityTest]
        public IEnumerator DensestWave_StaysInBudget_OnBothProfiles()
        {
            var lines = new List<string>();
            var failures = new List<string>();
            foreach (string level in Levels)
            {
                foreach (bool low in new[] { false, true })
                {
                    Measurement result = default;
                    yield return Measure(level, low, m => result = m);
                    string line = $"[Perf] {level} {(low ? "Low (Switch)" : "High (PC)")}: wave {result.Wave} ({result.Spawned} creatures), " +
                                  $"player loop avg {result.Average:0.0} ms, p95 {result.Percentile95:0.0} ms, " +
                                  $"skinned meshes {result.Skinned} ({result.SkinnedVisible} drawn), towers {result.Towers}, drones {result.Drones}, path objects {result.Gadgets}, enemies alive {result.Alive}, " +
                                  $"draw calls {result.DrawCalls:0}, batches {result.Batches:0}, set-pass calls {result.SetPass:0}, triangles {result.Triangles / 1000f:0}k";
                    lines.Add(line);
                    TestContext.Out.WriteLine(line);
                    if (result.Percentile95 > BudgetMilliseconds)
                    {
                        failures.Add(line);
                    }
                }
            }

            File.WriteAllLines(ReportPath, lines);
            Assert.IsEmpty(failures, "Over the editor frame budget of " + BudgetMilliseconds + " ms:\n" + string.Join("\n", failures));
            LogAssert.NoUnexpectedReceived();
        }

        private struct Measurement
        {
            public int Wave;
            public int Spawned;
            public float Average;
            public float Percentile95;
            public int Skinned;
            public int SkinnedVisible;
            public int Alive;
            public int Towers;
            public int Drones;
            public int Gadgets;
            public float DrawCalls;
            public float Batches;
            public float SetPass;
            public float Triangles;
        }

        private static IEnumerator Measure(string levelName, bool low, System.Action<Measurement> result)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            Services.Register(new ModelQuality(low));
            QualitySettings.lodBias = low ? 0.6f : 1f;

            var catalog = Services.Get<CampaignCatalog>();
            LevelDefinition level = null;
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Level != null && catalog.Nodes[i].Level.name == levelName)
                {
                    level = catalog.Nodes[i].Level;
                }
            }

            Assert.IsNotNull(level, $"no campaign level {levelName}");
            DifficultyProfile hardest = catalog.Difficulties[catalog.Difficulties.Count - 1];
            WaveDefinition[] waves = level.GetWaves(hardest);
            int densest = Densest(waves);
            Services.Register(new LevelSession(level, hardest));
            SceneManager.LoadScene(SceneNames.Level);
            LevelContext context = null;
            for (int i = 0; i < 300 && context == null; i++)
            {
                Services.TryGet(out context);
                yield return null;
            }

            Assert.IsNotNull(context, "the level never registered its context");
            var controller = Object.FindAnyObjectByType<LevelController>();
            var spawner = (EnemySpawner)typeof(LevelController).GetField("_spawner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
            controller.enabled = false;
            int towers = BuildOut(context);
            int drones = LaunchDrones(context);
            int gadgets = PlaceGadgets(context);

            int spawned = 0;
            yield return Release(waves[densest], spawner, count => spawned = count);
            Time.timeScale = 1f;
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }

            using ProfilerRecorder loop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", MeasuredFrames);
            // The render counters of the frame (the whole Game view: terrain, environment kit, units and effects).
            using ProfilerRecorder drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using ProfilerRecorder batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using ProfilerRecorder setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using ProfilerRecorder triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var samples = new List<float>(MeasuredFrames);
            var render = new Vector4();
            int renderFrames = 0;
            for (int i = 0; i < MeasuredFrames; i++)
            {
                yield return null;
                if (loop.Valid && loop.LastValue > 0)
                {
                    samples.Add(loop.LastValue / 1e6f);
                }

                if (drawCalls.Valid && drawCalls.LastValue > 0)
                {
                    render += new Vector4(drawCalls.LastValue, batches.LastValue, setPass.LastValue, triangles.LastValue);
                    renderFrames++;
                }
            }

            render /= Mathf.Max(1, renderFrames);

            samples.Sort();
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample;
            }

            int skinned = 0;
            int visible = 0;
            foreach (SkinnedMeshRenderer renderer in Object.FindObjectsByType<SkinnedMeshRenderer>())
            {
                skinned++;
                visible += renderer.isVisible ? 1 : 0;
            }

            int alive = 0;
            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>())
            {
                alive += enemy.IsAlive ? 1 : 0;
            }

            result(new Measurement
            {
                Wave = densest + 1,
                Spawned = spawned,
                Average = samples.Count > 0 ? sum / samples.Count : 0f,
                Percentile95 = samples.Count > 0 ? samples[Mathf.Min(samples.Count - 1, Mathf.FloorToInt(samples.Count * 0.95f))] : 0f,
                Skinned = skinned,
                SkinnedVisible = visible,
                Alive = alive,
                Towers = towers,
                Drones = drones,
                Gadgets = gadgets,
                DrawCalls = render.x,
                Batches = render.y,
                SetPass = render.z,
                Triangles = render.w,
            });
        }

        // Fills the cells beside the road with every tower type in turn at its top level, as a player late in a level.
        private static int BuildOut(LevelContext context)
        {
            var grid = new BuildGrid(context.Level);
            var types = new List<TowerDefinition>();
            foreach (TowerDefinition tower in context.AllTowers)
            {
                if (!tower.BlocksGroundEnemies && !tower.IsBaseDefence)
                {
                    types.Add(tower);
                }
            }

            int built = 0;
            foreach (Vector2Int road in context.Level.MainRoad)
            {
                for (int dx = -BuildOutReach; dx <= BuildOutReach && built < BuildOutLimit; dx++)
                {
                    for (int dy = -BuildOutReach; dy <= BuildOutReach && built < BuildOutLimit; dy++)
                    {
                        Vector2Int cell = road + new Vector2Int(dx, dy);
                        if (!grid.CanBuild(cell))
                        {
                            continue;
                        }

                        grid.Occupy(cell);
                        TowerDefinition type = types[built % types.Count];
                        var go = new GameObject("PerfTower_" + type.name);
                        go.transform.position = context.Level.Grid.CellToWorld(cell);
                        var tower = go.AddComponent<Tower>();
                        go.AddComponent<TowerView>();
                        tower.Initialize(type, context.Targets, context.Projectiles, context.ModifiersFor(type));
                        tower.Register(context.Structures);
                        tower.SetLevel(type.MaxLevelIndex);
                        built++;
                    }
                }
            }

            return built;
        }

        // As many drones of every kind as the level lets fly, paid for so the purse is not the limit.
        private static int LaunchDrones(LevelContext context)
        {
            int launched = 0;
            foreach (DroneDefinition drone in context.Level.Drones)
            {
                for (int i = context.ActiveDrones(drone); i < context.MaxDrones(drone); i++)
                {
                    context.Wallet.Add(drone.Cost);
                    context.DroneRequested.Raise(drone);
                }

                launched += context.ActiveDrones(drone);
            }

            return launched;
        }

        // Every path object in the data along the road (the beside kinds a few cells off it), as the placement does it:
        // rollers get the road ahead, and a used-up object is removed.
        private static int PlaceGadgets(LevelContext context)
        {
            var definitions = new List<PathObjectDefinition>();
#if UNITY_EDITOR
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:PathObjectDefinition", new[] { PathObjectFolder }))
            {
                definitions.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<PathObjectDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
            }
#endif
            var cells = new List<Vector2Int>(LevelValidator.LandPathCells(context.Level.MainRoad));
            int placed = 0;
            for (int round = 0; round < GadgetRounds; round++)
            {
                foreach (PathObjectDefinition definition in definitions)
                {
                    int index = GadgetStart + placed * GadgetSpacing;
                    if (index + 1 >= cells.Count)
                    {
                        return placed;
                    }

                    Vector2Int cell = cells[index];
                    if (definition.Placement == PathObjectPlacement.Beside)
                    {
                        Vector2Int step = cells[index + 1] - cell;
                        cell += new Vector2Int(-step.y, step.x) * (context.Level.Road.Width + 1);
                    }

                    var go = new GameObject("PerfGadget_" + definition.name);
                    go.transform.position = context.Level.Grid.CellToWorld(cell);
                    Vector3[] route = new Vector3[0];
                    int next = 0;
                    if (definition.Kind == PathObjectKind.Roller && context.Level.Road.TryLocate(cell, out Vector3[] found, out int nextIndex))
                    {
                        route = found;
                        next = nextIndex;
                    }

                    var pathObject = go.AddComponent<PathObject>();
                    pathObject.Initialize(definition, context.Targets, route, next);
                    go.AddComponent<PathObjectView>().Initialize(pathObject);
                    pathObject.Finished += finished => Object.Destroy(finished.gameObject);
                    placed++;
                }
            }

            return placed;
        }

        // The wave with the most creatures in it.
        private static int Densest(WaveDefinition[] waves)
        {
            int best = 0;
            int bestCount = -1;
            for (int w = 0; w < waves.Length; w++)
            {
                int count = 0;
                foreach (SpawnGroup group in waves[w].Groups)
                {
                    count += group.Count;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = w;
                }
            }

            return best;
        }

        // Every group of the wave on its own delay and interval, sped up, so the field fills as it would in play.
        private static IEnumerator Release(WaveDefinition wave, EnemySpawner spawner, System.Action<int> spawned)
        {
            var due = new List<(float Time, SpawnGroup Group)>();
            foreach (SpawnGroup group in wave.Groups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    due.Add((group.Delay + group.Stagger + group.Interval * i, group));
                }
            }

            due.Sort((a, b) => a.Time.CompareTo(b.Time));
            Time.timeScale = SpawnTimeScale;
            float clock = 0f;
            float started = Time.realtimeSinceStartup;
            int next = 0;
            while (next < due.Count && Time.realtimeSinceStartup - started < SpawnTimeoutSeconds)
            {
                clock += Time.deltaTime;
                while (next < due.Count && due[next].Time <= clock)
                {
                    SpawnGroup group = due[next].Group;
                    spawner.Spawn(group.Enemy, group.PathIndex, group.Source);
                    next++;
                }

                yield return null;
            }

            spawned(next);
        }
    }
}
