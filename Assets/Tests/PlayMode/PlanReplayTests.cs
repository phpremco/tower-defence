using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// The tester's stage B (round 22): replays the searched plans of every cell (Tools/PlayTest/Plans/*.json, written by the
    /// plan search) as scripted runs over the seeds, records outcome, stars, base health and coins beside the plan's paper
    /// margin (Tools/PlayTest/plan_replay.txt), and writes the wave records to plan_records.txt so the fit learns from plans too.
    /// </summary>
    public sealed class PlanReplayTests
    {
        public const string ReplayPath = "Tools/PlayTest/plan_replay.txt";
        public const string RecordsPath = "Tools/PlayTest/plan_records.txt";
        private const string PlansFolder = "Tools/PlayTest/Plans";
        private const string TracePath = "Logs/probe_trace.txt";

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

        [UnityTest]
        [Category("Balance")]
        [Explicit("Replays the searched plans of every cell over the seeds: hours. Narrow it with Tools/PlayTest/probe_arg.txt.")]
        [Timeout(72000000)]
        public IEnumerator Replay_SearchedPlans_EveryCell()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            var margins = new Dictionary<string, float>();
            List<int> seeds = BalanceProbeTests.Seeds();
            CampaignCatalog catalog = BalancePipelineTests.Catalog();
            foreach (int node in BalanceProbeTests.ExistingLevels(false))
            {
                for (int difficulty = 0; difficulty < catalog.Difficulties.Count; difficulty++)
                {
                    if (!BalanceProbeTests.Wanted("levels", node) || !BalanceProbeTests.Wanted("difficulties", difficulty))
                    {
                        continue;
                    }

                    List<PlanSpec> plans = LoadPlans(catalog.Nodes[node].Level.name, difficulty);
                    var run = new List<(int, int)> { (node, difficulty) };
                    foreach (PlanSpec plan in plans)
                    {
                        foreach (int seed in seeds)
                        {
                            yield return BalanceProbeTests.Probe(run, ProbeStrategy.Scripted(plan), results, seed);
                        }
                    }
                }
            }

            Assert.IsNotEmpty(results, "no plans to replay: run the plan search first");
            BalancePipelineTests.WriteRecords(RecordsPath, results);
            System.IO.File.WriteAllText(ReplayPath, Table(results));
            Debug.Log($"[Balance] replay: {results.Count} runs, see {ReplayPath} and {RecordsPath}");
        }

        /// <summary>The searched plans of a cell as the plan search wrote them (Tools/PlayTest/Plans/LEVEL_DIFFICULTY_*.json), in file order.</summary>
        private static List<PlanSpec> LoadPlans(string levelName, int difficulty)
        {
            var plans = new List<PlanSpec>();
            if (!System.IO.Directory.Exists(PlansFolder))
            {
                return plans;
            }

            var files = new List<string>(System.IO.Directory.GetFiles(PlansFolder, $"{levelName}_{difficulty}_*.json"));
            files.Sort(System.StringComparer.Ordinal);
            foreach (string file in files)
            {
                PlanSpec plan = JsonUtility.FromJson<PlanSpec>(System.IO.File.ReadAllText(file));
                if (plan != null && plan.Steps != null)
                {
                    plans.Add(plan);
                }
            }

            return plans;
        }

        private static string Table(List<BalanceProbeTests.ProbeResult> results)
        {
            var text = new StringBuilder();
            text.Append(Invariant($"Plan replay (stage B) {System.DateTime.Now:yyyy-MM-dd HH:mm}. {BalancePipelineTests.CalibrationNote()}\n"));
            text.Append(BalancePipelineTests.Table(results, "searched plans"));
            return text.ToString();
        }
    }
}
