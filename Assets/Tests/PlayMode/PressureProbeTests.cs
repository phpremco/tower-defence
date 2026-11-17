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
    /// Pressure (Docs/BALANCE_HEADROOM.md, stage 1b): the continuous replacement for the headroom probe's win/lose edge. A
    /// reference player plays a level at a few fixed multiples of the creatures' health, and every run is written out with
    /// what the report builds its pressure score from: per wave the health spawned, that health weighted by how far each
    /// creature got along its road, the health that reached the base, and the base and gate damage. Nothing is bisected, so
    /// nothing depends on winning being monotone. The level is played as it is, through the headroom probe's runtime copy.
    ///
    /// The reference players: skilled (since round 76 the cell's best searched strategy played adaptively, with a gun at a
    /// rift behind the defence gate a wave ahead, <see cref="ProbeStrategy.AdaptiveGuardingRifts"/>), script (the skilled rung
    /// of rounds 74 and 75: the build order of that strategy's best recorded run as a fixed script, with the rift reaction),
    /// typical (the build order the sensible bot took on seed 1 at the data, replayed as a fixed script on every seed and
    /// multiple), sensible and careless (the reactive bots of the headroom ladder), fresh (sensible on a plain save) and
    /// mono-TowerId.
    ///
    /// Tools/PlayTest/pressure_arg.txt gives the work: "units=node:difficulty:policy:seed,..." and "multiples=0.8,1,1.25";
    /// a "keeptrace=1" line keeps the probe's per-wave trace of every run (Logs/probe_trace.txt in the clone) for a diagnosis.
    /// Every run is appended to Tools/PlayTest/pressure_records.txt. Since round 74 a record carries the purse at each wave
    /// start ("wave=coins;...") and the idle-counter note (<see cref="BalanceProbeTests.ProbeResult.IdleCounter"/>); since
    /// round 76 it ends with the stamp of what it was measured with (<see cref="ProbeStamp"/>: bot version, data, game code).
    /// </summary>
    public sealed class PressureProbeTests
    {
        public const string ArgPath = "Tools/PlayTest/pressure_arg.txt";
        public const string RecordsPath = "Tools/PlayTest/pressure_records.txt";
        private const string TracePath = "Logs/probe_trace.txt";
        private const string MonoPrefix = "mono-";
        private const string TypicalPolicy = "typical";
        private const string ScriptPolicy = "script";

        // The typical script is the sensible bot's own build order on this seed, at the data.
        private const int TypicalSourceSeed = 1;

        private readonly Dictionary<(int Node, int Difficulty), PlanSpec> _typical = new Dictionary<(int, int), PlanSpec>();

        [TearDown]
        public void ResetOverride()
        {
            BalanceProbeTests.Override = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Category("Balance")]
        [Explicit("One run per unit and multiple of Tools/PlayTest/pressure_arg.txt; run it on the worker clones with Tools/PlayTest/pressure.sh.")]
        [Timeout(72000000)]
        public IEnumerator Measure_Pressure()
        {
            CampaignCatalog catalog = BalancePipelineTests.Catalog();
            Assert.IsNotNull(catalog, "run the generator first");
            List<string[]> units = Units();
            Assert.IsNotEmpty(units, "no units: write units=node:difficulty:policy:seed into " + ArgPath);
            List<float> multiples = Numbers("multiples");
            Assert.IsNotEmpty(multiples, "no multiples: write multiples=0.8,1,1.25 into " + ArgPath);
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (string[] unit in units)
            {
                int node = int.Parse(unit[0], CultureInfo.InvariantCulture);
                int difficulty = int.Parse(unit[1], CultureInfo.InvariantCulture);
                string policy = unit[2];
                int seed = int.Parse(unit[3], CultureInfo.InvariantCulture);
                LevelDefinition level = catalog.Nodes[node].Level;
                Assert.IsNotNull(level, $"node {node} has no level");
                if (policy == TypicalPolicy && !_typical.ContainsKey((node, difficulty)))
                {
                    // The reference run that the typical script is taken from; it is recorded as its own line.
                    yield return Play(catalog, node, difficulty, "sensible", TypicalSourceSeed, level, ProbeStrategy.SensibleBot(), 1f, results);
                    BalanceProbeTests.ProbeResult source = results[results.Count - 1];
                    _typical[(node, difficulty)] = new PlanSpec { Level = level.name, Difficulty = difficulty, Name = "typical " + level.name + " " + I(difficulty), Steps = source.Moves.ToArray() };
                    Append(Line("pref", level, difficulty, policy, TypicalSourceSeed, 1f, source, 0f, "source of the typical script"));
                    results.Clear();
                }

                ProbeStrategy strategy = StrategyFor(policy, node, difficulty, level, out string note);
                if (strategy == null)
                {
                    Append(string.Join("|", "pskip", level.name, I(difficulty), policy, I(seed), note));
                    continue;
                }

                var lines = new List<string>();
                foreach (float multiple in multiples)
                {
                    float started = Time.realtimeSinceStartup;
                    yield return Play(catalog, node, difficulty, policy, seed, level, strategy, multiple, results);
                    lines.Add(Line("prun", level, difficulty, policy, seed, multiple, results[results.Count - 1], Time.realtimeSinceStartup - started, note));
                    // The run is written out; the list would otherwise keep every run of a long session in memory.
                    results.Clear();
                }

                Append(string.Join("\n", lines));
                if (System.IO.File.Exists(TracePath) && Items("keeptrace").Count == 0)
                {
                    // The probe's per-sample trace of every run is not read here and grows by megabytes per unit.
                    System.IO.File.Delete(TracePath);
                }
            }

            Debug.Log($"[Pressure] {units.Count} units at {multiples.Count} multiples, see {RecordsPath}");
        }

        private static IEnumerator Play(CampaignCatalog catalog, int node, int difficulty, string policy, int seed, LevelDefinition level, ProbeStrategy strategy, float multiple, List<BalanceProbeTests.ProbeResult> results)
        {
            LevelDefinition scaled = HeadroomProbeTests.Scaled(level, multiple);
            BalanceProbeTests.Override = new BalanceProbeTests.ProbeOverride
            {
                Level = scaled,
                Waves = null,
                StartingMoney = -1,
                Player = ProbePlayer.StateFor(catalog, node, difficulty, policy == "fresh"),
                Owned = BalanceProbeTests.OwnedTowers(catalog, node, difficulty),
            };
            int before = results.Count;
            yield return BalanceProbeTests.Probe(new List<(int, int)> { (node, difficulty) }, strategy, results, seed);
            BalanceProbeTests.Override = null;
            Object.Destroy(scaled);
            Assert.AreEqual(before + 1, results.Count, "the probe recorded no run");
        }

        private ProbeStrategy StrategyFor(string policy, int node, int difficulty, LevelDefinition level, out string note)
        {
            note = string.Empty;
            switch (policy)
            {
                case "careless":
                    return ProbeStrategy.NaiveBot();
                case "sensible":
                case "fresh":
                    return ProbeStrategy.SensibleBot();
                case "skilled":
                    // Round 76: the best strategy itself, deciding as it goes; a frozen build order of one of its runs lost
                    // levels the strategy wins (Quartz Flats Hard, round 75).
                    StrategySpec spec = HeadroomProbeTests.BestRecordedStrategy(level.name, difficulty);
                    note = spec != null ? spec.Name + " adaptive" : "no recorded strategy for this cell";
                    return spec != null ? ProbeStrategy.AdaptiveGuardingRifts(spec) : null;
                case ScriptPolicy:
                    PlanSpec plan = HeadroomProbeTests.BestRecordedPlan(level.name, difficulty);
                    note = plan != null ? plan.Name : "no recorded plan for this cell";
                    // Round 74: the scripted player sees a marked rift behind the gate; the recorded plans of before did not.
                    return plan != null ? ProbeStrategy.ScriptedGuardingRifts(plan) : null;
                case TypicalPolicy:
                    PlanSpec typical = _typical[(node, difficulty)];
                    note = typical.Steps.Length + " steps";
                    return typical.Steps.Length > 0 ? ProbeStrategy.Scripted(typical) : null;
                default:
                    Assert.IsTrue(policy.StartsWith(MonoPrefix, System.StringComparison.Ordinal), "unknown policy " + policy);
                    return ProbeStrategy.Mono(policy.Substring(MonoPrefix.Length));
            }
        }

        private static string Line(string tag, LevelDefinition level, int difficulty, string policy, int seed, float multiple, BalanceProbeTests.ProbeResult result, float wallSeconds, string note)
        {
            var reach = new List<string>();
            var advance = new List<string>();
            for (int wave = 1; wave <= result.WaveCount; wave++)
            {
                if (result.Reach.TryGetValue(wave, out float furthest))
                {
                    reach.Add(I(wave) + "=" + furthest.ToString("0.000", CultureInfo.InvariantCulture));
                }

                if (result.Advance.TryGetValue(wave, out BotRun.WaveAdvance sum))
                {
                    advance.Add(I(wave) + "=" + F(sum.Health) + ":" + F(sum.Advanced) + ":" + F(sum.Leaked));
                }
            }

            var baseLost = new List<string>();
            var gateLost = new List<string>();
            foreach (WaveRecord record in result.Records)
            {
                if (record.BaseLost > 0f)
                {
                    baseLost.Add(I(record.Wave) + "=" + F(record.BaseLost));
                }

                if (record.GateLost > 0f)
                {
                    gateLost.Add(I(record.Wave) + "=" + F(record.GateLost));
                }
            }

            var moves = new List<string>();
            foreach (PlanStep step in result.Moves)
            {
                moves.Add(string.Join(":", step.Kind, step.Item, I(step.X), I(step.Y), I(step.Wave)));
            }

            var purse = new List<string>();
            for (int wave = 1; wave <= result.WaveCount; wave++)
            {
                if (result.Purse.TryGetValue(wave, out int coins))
                {
                    purse.Add(I(wave) + "=" + I(coins));
                }
            }

            string outcome = result.Outcome == LevelOutcome.Won ? "won" : result.Outcome == LevelOutcome.Running ? "stalled" : "lost";
            return string.Join("|", tag, level.name, I(difficulty), policy, I(seed), F(multiple), outcome,
                I(result.Wave), I(result.WaveCount), F(result.BaseHealth), F(result.BaseMax), I(result.Stars), F(result.GameSeconds), F(wallSeconds),
                I(result.TowersBuilt), I(result.Upgrades), I(result.CoinsUnspent), I(result.Earned), I(result.RepairSpent),
                string.Join(";", reach), string.Join(";", baseLost), string.Join(";", gateLost), string.Join(";", advance), string.Join(";", moves), note, string.Join(";", purse), result.IdleCounter, ProbeStamp.Line);
        }

        private static List<string[]> Units()
        {
            var units = new List<string[]>();
            foreach (string item in Items("units"))
            {
                string[] parts = item.Split(':');
                Assert.AreEqual(4, parts.Length, "a unit is node:difficulty:policy:seed, got " + item);
                units.Add(parts);
            }

            return units;
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
