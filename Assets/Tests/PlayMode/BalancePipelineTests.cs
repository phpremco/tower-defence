using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Core;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// The balance pipeline's PlayMode stages (round 22, Docs/BALANCE.md "The balance pipeline"). Stage "measure": the obvious
    /// family (a row of each owned type, the careless near-gate mix, the ledger-less affordable buyer) and the creative reference
    /// bot play every level and difficulty over several seeds at a fixed step; the table of win rates goes to
    /// Tools/PlayTest/yardstick.txt and every run's wave records (what stood, what each type dealt, what the wave brought) to
    /// Tools/PlayTest/calibration_records.txt, which the EditMode fit (CalibrationFitTests) turns into the calibration. The
    /// step check replays a few runs at a finer step so the fit can tell whether the step is fine enough. Only errors in the
    /// level fail a run; verdicts live in the EditMode acceptance tests.
    /// </summary>
    public sealed class BalancePipelineTests
    {
        public const string YardstickPath = "Tools/PlayTest/yardstick.txt";
        public const string RecordsPath = "Tools/PlayTest/calibration_records.txt";
        public const string StepRecordsPath = "Tools/PlayTest/calibration_records_step.txt";
        private const string TracePath = "Logs/probe_trace.txt";
        private const int Medium = 1;
        private const float FineStep = 0.05f;
        private const string StepCheckLevel = "Level_C1_03";

        // The fresh-save sanity cell: the campaign's first level on Easy, played without any permanent upgrade.
        private const int FreshSanityNode = 0;
        private const int FreshSanityDifficulty = 0;

        [SetUp]
        public void ClearTrace()
        {
            if (System.IO.File.Exists(TracePath))
            {
                System.IO.File.Delete(TracePath);
            }
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        /// <summary>Two runs on Shard Ridge Medium: the pipeline's pieces (records, obvious bots) work.</summary>
        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator Smoke_Yardstick_ShardRidgeMedium()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (int node in Levels(new[] { StepCheckLevel }))
            {
                var run = new List<(int, int)> { (node, Medium) };
                yield return BalanceProbeTests.Probe(run, ProbeStrategy.Mono("Cannon"), results, 1);
                yield return BalanceProbeTests.Probe(run, ProbeStrategy.NaiveBot(), results, 1);
            }

            Assert.AreEqual(2, results.Count, "a run did not finish");
            foreach (BalanceProbeTests.ProbeResult result in results)
            {
                Assert.IsNotEmpty(result.Records, result.Strategy + " recorded no waves");
                Assert.Greater(result.Records[0].Health, 0f, result.Strategy + " recorded no wave health");
            }

            System.IO.File.WriteAllText("Tools/PlayTest/yardstick_smoke.txt", Table(results, "smoke"));
        }

        /// <summary>
        /// Stage "measure": the obvious family and the creative reference on every existing level and difficulty (the filter
        /// file narrows levels, difficulties and seeds), all seeds. Writes yardstick.txt and the calibration records.
        /// </summary>
        [UnityTest]
        [Category("Balance")]
        [Explicit("The yardstick sweep: hours. Narrow it with Tools/PlayTest/probe_arg.txt (levels=, difficulties=, seeds=).")]
        [Timeout(72000000)]
        public IEnumerator Measure_Yardstick_ObviousFamily()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            List<int> seeds = BalanceProbeTests.Seeds();
            foreach (int node in BalanceProbeTests.ExistingLevels(false))
            {
                for (int difficulty = 0; difficulty < 4; difficulty++)
                {
                    if (!BalanceProbeTests.WantedCell(node, difficulty))
                    {
                        continue;
                    }

                    var run = new List<(int, int)> { (node, difficulty) };
                    foreach (int seed in seeds)
                    {
                        if (!BalanceProbeTests.WantedSeed(node, difficulty, seed))
                        {
                            continue;
                        }

                        foreach (string tower in MonoTypes(node, difficulty))
                        {
                            yield return BalanceProbeTests.Probe(run, ProbeStrategy.Mono(tower), results, seed);
                        }

                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.NaiveBot(), results, seed);
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.AffordableBot, results, seed);
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.CreativeBot(), results, seed);
                    }

                    // One fresh-save sanity cell (the curator, round 22d): a new player with no upgrades can start the campaign.
                    if (node == FreshSanityNode && difficulty == FreshSanityDifficulty)
                    {
                        foreach (int seed in seeds)
                        {
                            if (!BalanceProbeTests.WantedSeed(node, difficulty, seed))
                            {
                                continue;
                            }

                            yield return BalanceProbeTests.Probe(run, ProbeStrategy.AffordableBot, results, seed, true);
                            yield return BalanceProbeTests.Probe(run, ProbeStrategy.NaiveBot(), results, seed, true);
                        }
                    }
                }
            }

            Assert.IsNotEmpty(results);
            System.IO.File.WriteAllText(YardstickPath, Table(results, "obvious family"));
            WriteRecords(RecordsPath, results);
            Debug.Log($"[Balance] yardstick: {results.Count} runs, see {YardstickPath} and {RecordsPath}");
        }

        /// <summary>
        /// The step check: three runs on Shard Ridge Medium at the usual step and at a finer one; the EditMode fit compares the
        /// efficiencies of both sets and says whether the usual step is a valid calibration step.
        /// </summary>
        [UnityTest]
        [Category("Balance")]
        [Explicit("Step check (about 15 minutes); the fit reads calibration_records_step.txt.")]
        [Timeout(7200000)]
        public IEnumerator Measure_StepCheck_FineAgainstUsual()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (int node in Levels(new[] { StepCheckLevel }))
            {
                var run = new List<(int, int)> { (node, Medium) };
                foreach (float step in new[] { 0f, FineStep })
                {
                    BalanceProbeTests.StepOverride = step > 0f ? step : (float?)null;
                    foreach (string tower in MonoTypes(node, Medium))
                    {
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.Mono(tower), results, 1);
                    }

                    yield return BalanceProbeTests.Probe(run, ProbeStrategy.AffordableBot, results, 1);
                }
            }

            BalanceProbeTests.StepOverride = null;
            Assert.IsNotEmpty(results);
            WriteRecords(StepRecordsPath, results);
            Debug.Log($"[Balance] step check: {results.Count} runs, see {StepRecordsPath}");
        }

        internal static void WriteRecords(string path, List<BalanceProbeTests.ProbeResult> results)
        {
            var records = new List<RunRecord>();
            foreach (BalanceProbeTests.ProbeResult result in results)
            {
                records.Add(result.ToRecord());
            }

            System.IO.File.WriteAllText(path, CalibrationFit.Write(records));
        }

        /// <summary>Win rates per cell and strategy, with the seeds and the step in the header; the EditMode acceptance reads the records.</summary>
        internal static string Table(List<BalanceProbeTests.ProbeResult> results, string set)
        {
            var seeds = new SortedSet<int>();
            var steps = new SortedSet<float>();
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                seeds.Add(r.Seed);
                steps.Add(r.Step);
            }

            var text = new StringBuilder();
            text.Append(Invariant($"Yardstick, {set}: seeds {string.Join(",", seeds)}, step {string.Join(",", steps)} s, {System.DateTime.Now:yyyy-MM-dd HH:mm}. {CalibrationNote()}\n"));
            text.Append("Per cell and strategy: wins over seeds, stars (mean of the wins), base left (mean), waves reached (losses), coins unspent (mean).\n\n");
            var groups = new SortedDictionary<string, List<BalanceProbeTests.ProbeResult>>();
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                string key = $"{r.Asset} {Difficulty(r.Difficulty),-7} {r.Strategy}{(r.FreshPlayer ? " (fresh save)" : string.Empty)}";
                if (!groups.TryGetValue(key, out List<BalanceProbeTests.ProbeResult> group))
                {
                    groups[key] = group = new List<BalanceProbeTests.ProbeResult>();
                }

                group.Add(r);
            }

            text.Append("level        diff    strategy          wins  stars   base  reached   coins\n");
            foreach (KeyValuePair<string, List<BalanceProbeTests.ProbeResult>> group in groups)
            {
                int wins = 0;
                float stars = 0f;
                float baseLeft = 0f;
                float coins = 0f;
                var reached = new List<string>();
                foreach (BalanceProbeTests.ProbeResult r in group.Value)
                {
                    bool won = r.Outcome == LevelOutcome.Won;
                    wins += won ? 1 : 0;
                    stars += won ? r.Stars : 0;
                    baseLeft += r.BaseHealth;
                    coins += r.CoinsUnspent;
                    if (!won)
                    {
                        reached.Add(r.Wave + "/" + r.WaveCount);
                    }
                }

                int n = group.Value.Count;
                string[] parts = group.Key.Split(new[] { ' ' }, 3);
                text.Append(Invariant($"{parts[0],-12} {parts[1],-7} {parts[2].Trim(),-17} {wins}/{n}  {(wins > 0 ? stars / wins : 0f),5:0.0}  {baseLeft / n,5:0}  {(reached.Count > 0 ? string.Join(" ", reached) : "-"),-8} {coins / n,6:0}\n"));
            }

            text.Append("\nRuns:\n");
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                text.Append(Invariant($"  {r.Asset,-12} {Difficulty(r.Difficulty),-7} seed {r.Seed} {r.Strategy,-17} {r.Outcome,-6} {r.Stars}* {r.Wave,2}/{r.WaveCount,-2} base {r.BaseHealth,4:0} built {r.TowersBuilt,2} upg {r.Upgrades,2} sold {r.Sold} mines {r.Mines} fields {r.Fields} drones {(r.Drones.Length > 0 ? r.Drones : "-")} coins {r.CoinsUnspent,4} repairs {r.RepairSpent}\n"));
            }

            return text.ToString();
        }

        internal static string CalibrationNote()
        {
#if UNITY_EDITOR
            var calibration = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(BalanceProbeTests.CampaignPath);
            var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
            string hash = InputHash.Of(BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default));
            return calibration != null ? calibration.Describe(hash) : "no calibration asset yet (inputs " + hash + ")";
#else
            return string.Empty;
#endif
        }

        internal static string Difficulty(int difficulty)
        {
            string[] names = { "Easy", "Medium", "Hard", "Psycho" };
            return difficulty >= 0 && difficulty < names.Length ? names[difficulty] : difficulty.ToString();
        }

        // Node indices of the named levels.
        internal static List<int> Levels(string[] assets)
        {
            var nodes = new List<int>();
            CampaignCatalog catalog = Catalog();
            for (int i = 0; catalog != null && i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Level != null && System.Array.IndexOf(assets, catalog.Nodes[i].Level.name) >= 0)
                {
                    nodes.Add(i);
                }
            }

            return nodes;
        }

        // The owned guns that can hold a ground wave alone at this point of the campaign (sky-only guns and masts cannot).
        internal static List<string> MonoTypes(int node, int difficulty)
        {
            var types = new List<string>();
            CampaignCatalog catalog = Catalog();
            if (catalog == null)
            {
                return types;
            }

            List<string> owned = BalanceProbeTests.OwnedTowers(catalog, node, difficulty);
            foreach (TowerDefinition tower in catalog.Nodes[node].Level.AvailableTowers)
            {
                if (owned.Contains(tower.Id) && !tower.IsSupport && !tower.BlocksGroundEnemies && (tower.Targets & TargetLayers.Ground) != 0)
                {
                    types.Add(tower.Id);
                }
            }

            return types;
        }

        internal static CampaignCatalog Catalog()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(BalanceProbeTests.CampaignPath);
#else
            return null;
#endif
        }
    }
}
