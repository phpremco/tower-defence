using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Balance;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Headroom (Docs/BALANCE_HEADROOM.md; stage 1 of Docs/BALANCE_METHOD_REVIEW.md): for a level, a difficulty, a fixed
    /// reference player and a seed, the largest uniform multiple of the creatures' health at which that player still wins,
    /// found by bisection in the real game. It is the finale probe's bisection on an ordinary level: the level is played as it
    /// is (its own waves, purse and expected player), only a runtime copy of the level carries the multiple in its enemy
    /// health scale. Nothing here reads the balance model's verdict, and no asset is written.
    ///
    /// The reference players: careless (the naive bot), sensible (the affordable buyer who stands the counter tower when the
    /// preview shows armour or flyers), skilled (the recorded build order of the cell's best replayed plan, as a fixed script),
    /// fresh (the sensible player on a plain save) and one row per owned tower type (mono).
    ///
    /// Tools/PlayTest/headroom_arg.txt gives the work: "units=node:difficulty:policy:seed,..." (policy: careless, sensible,
    /// skilled, fresh, mono for every row or mono-TowerId), "precision=0.03" (the bisection stops when first loss / last win is
    /// within it), "scan=0.7,0.85,1.15,1.3" (after the bisection, these multiples of the measured edge are played too: the
    /// monotonicity check) and "base=node:difficulty:factor,..." (the cell's health times that factor before the multiple: the
    /// rescale check). Every run and every edge is appended to Tools/PlayTest/headroom_records.txt.
    /// </summary>
    public sealed class HeadroomProbeTests
    {
        public const string ArgPath = "Tools/PlayTest/headroom_arg.txt";
        public const string RecordsPath = "Tools/PlayTest/headroom_records.txt";
        private const string PlanRecordsPath = "Tools/PlayTest/plan_records.txt";
        private const string TracePath = "Logs/probe_trace.txt";
        private const string HealthScaleField = "_enemyHealthMultiplier";
        private const string MonoPrefix = "mono-";

        // The bisection: from the data (1) outward by a step until the verdict flips, then the geometric middle of the last win
        // and the first loss until they are within the precision. The bounds stop a player who never wins or never loses.
        private const float ExpandStep = 1.5f;
        private const float MinMultiple = 0.1f;
        private const float MaxMultiple = 16f;
        private const float DefaultPrecision = 0.03f;

        private readonly struct Unit
        {
            public Unit(int node, int difficulty, string policy, int seed)
            {
                Node = node;
                Difficulty = difficulty;
                Policy = policy;
                Seed = seed;
            }

            public int Node { get; }

            public int Difficulty { get; }

            public string Policy { get; }

            public int Seed { get; }
        }

        [TearDown]
        public void ResetOverride()
        {
            BalanceProbeTests.Override = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Category("Balance")]
        [Explicit("One bisection per unit of Tools/PlayTest/headroom_arg.txt, about ten runs each; run it on the worker clones with Tools/PlayTest/headroom.sh.")]
        [Timeout(72000000)]
        public IEnumerator Measure_Headroom()
        {
            CampaignCatalog catalog = BalancePipelineTests.Catalog();
            Assert.IsNotNull(catalog, "run the generator first");
            List<Unit> units = Units();
            Assert.IsNotEmpty(units, "no units: write units=node:difficulty:policy:seed into " + ArgPath);
            float precision = Number("precision", DefaultPrecision);
            List<float> scan = Numbers("scan");
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (Unit unit in units)
            {
                LevelDefinition level = catalog.Nodes[unit.Node].Level;
                Assert.IsNotNull(level, $"node {unit.Node} has no level");
                ProbeStrategy strategy = StrategyFor(unit, level, out string note);
                if (strategy == null)
                {
                    Append(string.Join("|", "h", level.name, I(unit.Difficulty), unit.Policy, I(unit.Seed), "1", "0", "0", "0", "0", note));
                    continue;
                }

                float baseFactor = BaseFactor(unit.Node, unit.Difficulty);
                var lines = new List<string>();
                float started = Time.realtimeSinceStartup;
                float won = -1f;
                float lost = -1f;
                float multiple = 1f;
                int runs = 0;
                while (multiple > 0f)
                {
                    bool beaten = false;
                    yield return Play(catalog, unit, level, strategy, baseFactor, multiple, "bisect", results, lines, b => beaten = b);
                    runs++;
                    won = beaten ? Mathf.Max(won, multiple) : won;
                    lost = beaten ? lost : lost < 0f ? multiple : Mathf.Min(lost, multiple);
                    multiple = Next(won, lost, precision);
                }

                // The scan plays multiples on both sides of the edge the bisection found; one crossover means winning is monotone.
                float edge = won > 0f && lost > 0f ? Mathf.Sqrt(won * lost) : -1f;
                for (int i = 0; edge > 0f && i < scan.Count; i++)
                {
                    yield return Play(catalog, unit, level, strategy, baseFactor, edge * scan[i], "scan", results, lines, null);
                    runs++;
                }

                lines.Add(string.Join("|", "h", level.name, I(unit.Difficulty), unit.Policy, I(unit.Seed), F(baseFactor), F(Mathf.Max(0f, won)), F(Mathf.Max(0f, lost)), I(runs), F(Time.realtimeSinceStartup - started), note));
                Append(string.Join("\n", lines));
                if (System.IO.File.Exists(TracePath))
                {
                    // The probe's per-sample trace of every run is not read here and grows by megabytes per bisection.
                    System.IO.File.Delete(TracePath);
                }
            }

            Debug.Log($"[Headroom] {units.Count} units, see {RecordsPath}");
        }

        // One run at a multiple: the level's copy with its health scale times the multiple, the cell's expected (or plain) player.
        private static IEnumerator Play(CampaignCatalog catalog, Unit unit, LevelDefinition level, ProbeStrategy strategy, float baseFactor, float multiple, string phase, List<BalanceProbeTests.ProbeResult> results, List<string> lines, Action<bool> verdict)
        {
            LevelDefinition scaled = Scaled(level, baseFactor * multiple);
            bool fresh = unit.Policy == "fresh";
            BalanceProbeTests.Override = new BalanceProbeTests.ProbeOverride
            {
                Level = scaled,
                Waves = null,
                StartingMoney = -1,
                Player = ProbePlayer.StateFor(catalog, unit.Node, unit.Difficulty, fresh),
                Owned = BalanceProbeTests.OwnedTowers(catalog, unit.Node, unit.Difficulty),
            };
            int before = results.Count;
            float started = Time.realtimeSinceStartup;
            yield return BalanceProbeTests.Probe(new List<(int, int)> { (unit.Node, unit.Difficulty) }, strategy, results, unit.Seed);
            BalanceProbeTests.Override = null;
            UnityEngine.Object.Destroy(scaled);
            Assert.AreEqual(before + 1, results.Count, "the probe recorded no run");
            BalanceProbeTests.ProbeResult result = results[results.Count - 1];
            bool beaten = result.Outcome == LevelOutcome.Won;
            lines.Add(RunLine(unit, level, baseFactor, multiple, phase, result, Time.realtimeSinceStartup - started));
            // The run's records are written out; the list would otherwise keep every run of a long session in memory.
            results.Clear();
            verdict?.Invoke(beaten);
        }

        private static string RunLine(Unit unit, LevelDefinition level, float baseFactor, float multiple, string phase, BalanceProbeTests.ProbeResult result, float wallSeconds)
        {
            var reach = new List<string>();
            var waves = new List<int>(result.Reach.Keys);
            waves.Sort();
            foreach (int wave in waves)
            {
                reach.Add(I(wave) + "=" + result.Reach[wave].ToString("0.00", CultureInfo.InvariantCulture));
            }

            var baseLost = new List<string>();
            var gateLost = new List<string>();
            foreach (WaveRecord record in result.Records)
            {
                if (record.BaseLost > 0f)
                {
                    baseLost.Add(I(record.Wave) + "=" + record.BaseLost.ToString("0", CultureInfo.InvariantCulture));
                }

                if (record.GateLost > 0f)
                {
                    gateLost.Add(I(record.Wave) + "=" + record.GateLost.ToString("0", CultureInfo.InvariantCulture));
                }
            }

            string outcome = result.Outcome == LevelOutcome.Won ? "won" : result.Outcome == LevelOutcome.Running ? "stalled" : "lost";
            return string.Join("|", "run", level.name, I(unit.Difficulty), unit.Policy, I(unit.Seed), F(baseFactor), F(multiple), phase, outcome,
                I(result.Wave), I(result.WaveCount), F(result.BaseHealth), F(result.BaseMax), I(result.Stars), F(result.GameSeconds), F(wallSeconds),
                I(result.TowersBuilt), I(result.Upgrades), I(result.CoinsUnspent), string.Join(";", reach), string.Join(";", baseLost), string.Join(";", gateLost));
        }

        // The next multiple to play, or below zero when the edge is known to the precision (or lies outside the bounds).
        private static float Next(float won, float lost, float precision)
        {
            if (lost < 0f)
            {
                return won * ExpandStep <= MaxMultiple ? won * ExpandStep : -1f;
            }

            if (won < 0f)
            {
                return lost / ExpandStep >= MinMultiple ? lost / ExpandStep : -1f;
            }

            return lost / won > 1f + precision ? Mathf.Sqrt(won * lost) : -1f;
        }

        // A runtime copy of the level whose creatures carry the multiple; the asset is not touched and the copy keeps its name.
        internal static LevelDefinition Scaled(LevelDefinition level, float factor)
        {
            LevelDefinition copy = UnityEngine.Object.Instantiate(level);
            copy.name = level.name;
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(copy);
            UnityEditor.SerializedProperty scale = serialized.FindProperty(HealthScaleField);
            Assert.IsNotNull(scale, "LevelDefinition has no " + HealthScaleField);
            scale.floatValue = level.EnemyHealthScale * factor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            Assert.AreEqual(level.EnemyHealthScale * factor, copy.EnemyHealthScale, 0.0001f, "the level copy did not take the multiple");
            return copy;
        }

        private static ProbeStrategy StrategyFor(Unit unit, LevelDefinition level, out string note)
        {
            note = string.Empty;
            switch (unit.Policy)
            {
                case "careless":
                    return ProbeStrategy.NaiveBot();
                case "sensible":
                case "fresh":
                    return ProbeStrategy.SensibleBot();
                case "skilled":
                    PlanSpec plan = BestRecordedPlan(level.name, unit.Difficulty);
                    note = plan != null ? plan.Name : "no recorded plan for this cell";
                    return plan != null ? ProbeStrategy.Scripted(plan) : null;
                default:
                    Assert.IsTrue(unit.Policy.StartsWith(MonoPrefix, StringComparison.Ordinal), "unknown policy " + unit.Policy);
                    return ProbeStrategy.Mono(unit.Policy.Substring(MonoPrefix.Length));
            }
        }

        /// <summary>
        /// The skilled rung: the build order the best replayed plan of the cell really took (Tools/PlayTest/plan_records.txt), as a
        /// fixed script. Best is the strategy that won on the most seeds (then stars, then base health); of its wins the lowest
        /// seed's build order is taken. A script decides nothing, so this player reads no model either.
        /// </summary>
        internal static PlanSpec BestRecordedPlan(string levelName, int difficulty)
        {
            RunRecord best = BestRecordedRun(levelName, difficulty);
            if (best == null)
            {
                return null;
            }

            return new PlanSpec { Level = levelName, Difficulty = difficulty, Name = best.Strategy.Replace(StrategyPrefix, string.Empty) + " seed " + I(best.Seed), Steps = best.Moves.ToArray() };
        }

        /// <summary>
        /// The searched strategy behind the cell's best recorded plan (Tools/PlayTest/Plans), to be played adaptively: the
        /// pressure probe's skilled rung since round 76. Null when the cell has no record or its best plan is a fixed script.
        /// </summary>
        internal static StrategySpec BestRecordedStrategy(string levelName, int difficulty)
        {
            RunRecord best = BestRecordedRun(levelName, difficulty);
            if (best == null || !System.IO.Directory.Exists(PlansFolder))
            {
                return null;
            }

            var files = new List<string>(System.IO.Directory.GetFiles(PlansFolder, levelName + "_" + I(difficulty) + "_strategy*.json"));
            files.Sort(StringComparer.Ordinal);
            foreach (string file in files)
            {
                StrategySpec spec = JsonUtility.FromJson<StrategySpec>(System.IO.File.ReadAllText(file));
                if (spec != null && spec.Spots != null && StrategyPrefix + spec.Name == best.Strategy)
                {
                    return spec;
                }
            }

            return null;
        }

        private const string PlansFolder = "Tools/PlayTest/Plans";
        private const string StrategyPrefix = "strategy ";

        // The record the skilled rung is taken from: of the strategy that won on the most seeds (then stars, then base health),
        // the run of the lowest winning seed.
        private static RunRecord BestRecordedRun(string levelName, int difficulty)
        {
            if (!System.IO.File.Exists(PlanRecordsPath))
            {
                return null;
            }

            var byStrategy = new SortedDictionary<string, List<RunRecord>>(StringComparer.Ordinal);
            foreach (RunRecord run in CalibrationFit.Read(System.IO.File.ReadLines(PlanRecordsPath)))
            {
                if (run.Level != levelName || run.Difficulty != difficulty || run.FreshPlayer || run.Moves.Count == 0)
                {
                    continue;
                }

                if (!byStrategy.TryGetValue(run.Strategy, out List<RunRecord> list))
                {
                    byStrategy[run.Strategy] = list = new List<RunRecord>();
                }

                list.Add(run);
            }

            RunRecord best = null;
            float bestScore = float.MinValue;
            foreach (KeyValuePair<string, List<RunRecord>> pair in byStrategy)
            {
                // Each (strategy, seed) counts once, however often a merge repeated the record.
                var seeds = new SortedDictionary<int, RunRecord>();
                foreach (RunRecord run in pair.Value)
                {
                    seeds[run.Seed] = run;
                }

                float wins = 0f;
                float stars = 0f;
                float health = 0f;
                RunRecord pick = null;
                foreach (RunRecord run in seeds.Values)
                {
                    wins += run.Won ? 1f : 0f;
                    stars += run.Stars;
                    health += run.BaseHealth;
                    pick = pick == null || (run.Won && !pick.Won) ? run : pick;
                }

                float score = (wins / seeds.Count) * WinWeight + (stars / seeds.Count) * StarWeight + health / seeds.Count;
                if (pick != null && score > bestScore)
                {
                    bestScore = score;
                    best = pick;
                }
            }

            return best;
        }

        // Wins outrank stars and stars outrank base health in the pick of the best recorded plan (base health is in the thousands at most).
        private const float WinWeight = 1e8f;
        private const float StarWeight = 1e5f;

        private static List<Unit> Units()
        {
            var units = new List<Unit>();
            foreach (string item in Items("units"))
            {
                string[] parts = item.Split(':');
                Assert.AreEqual(4, parts.Length, "a unit is node:difficulty:policy:seed, got " + item);
                int node = int.Parse(parts[0], CultureInfo.InvariantCulture);
                int difficulty = int.Parse(parts[1], CultureInfo.InvariantCulture);
                int seed = int.Parse(parts[3], CultureInfo.InvariantCulture);
                if (parts[2] == "mono")
                {
                    foreach (string tower in BalancePipelineTests.MonoTypes(node, difficulty))
                    {
                        units.Add(new Unit(node, difficulty, MonoPrefix + tower, seed));
                    }
                }
                else
                {
                    units.Add(new Unit(node, difficulty, parts[2], seed));
                }
            }

            return units;
        }

        private static float BaseFactor(int node, int difficulty)
        {
            foreach (string item in Items("base"))
            {
                string[] parts = item.Split(':');
                if (parts.Length == 3 && int.Parse(parts[0], CultureInfo.InvariantCulture) == node && int.Parse(parts[1], CultureInfo.InvariantCulture) == difficulty)
                {
                    return float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                }
            }

            return 1f;
        }

        private static float Number(string key, float fallback)
        {
            List<float> values = Numbers(key);
            return values.Count > 0 ? values[0] : fallback;
        }

        private static List<float> Numbers(string key)
        {
            var values = new List<float>();
            foreach (string item in Items(key))
            {
                values.Add(float.Parse(item, NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            return values;
        }

        // The comma-separated items of the argument file's "key=" line; none without the file or the line.
        private static List<string> Items(string key)
        {
            var items = new List<string>();
            if (!System.IO.File.Exists(ArgPath))
            {
                return items;
            }

            foreach (string line in System.IO.File.ReadAllLines(ArgPath))
            {
                int split = line.IndexOf('=');
                if (split <= 0 || line.Substring(0, split).Trim() != key)
                {
                    continue;
                }

                foreach (string item in line.Substring(split + 1).Split(','))
                {
                    if (item.Trim().Length > 0)
                    {
                        items.Add(item.Trim());
                    }
                }
            }

            return items;
        }

        private static void Append(string text) => System.IO.File.AppendAllText(RecordsPath, text + "\n", new UTF8Encoding(false));

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
