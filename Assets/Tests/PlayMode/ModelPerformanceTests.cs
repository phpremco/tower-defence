using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// The imported creature models under load: the densest wave of Crysium Vein (C1_06) and of C1_05 on the hardest
    /// difficulty, released on its own spawn timing with the wave clock off, then measured over a stretch of frames on
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
                                  $"skinned meshes {result.Skinned} ({result.SkinnedVisible} drawn), enemies alive {result.Alive}";
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

            int spawned = 0;
            yield return Release(waves[densest], spawner, count => spawned = count);
            Time.timeScale = 1f;
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }

            using ProfilerRecorder loop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", MeasuredFrames);
            var samples = new List<float>(MeasuredFrames);
            for (int i = 0; i < MeasuredFrames; i++)
            {
                yield return null;
                if (loop.Valid && loop.LastValue > 0)
                {
                    samples.Add(loop.LastValue / 1e6f);
                }
            }

            samples.Sort();
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample;
            }

            int skinned = 0;
            int visible = 0;
            foreach (SkinnedMeshRenderer renderer in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                skinned++;
                visible += renderer.isVisible ? 1 : 0;
            }

            int alive = 0;
            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
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
            });
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
