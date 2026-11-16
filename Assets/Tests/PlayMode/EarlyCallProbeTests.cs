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
    /// Round 73: the best searched plan of Landing Site on Easy (strategy 1) played twice per seed, once waiting for every
    /// countdown (the balance baseline) and once calling every wave early as soon as the level accepts it. The early run must
    /// end (won or lost, no soft-lock, no errors) with every call accepted; the table compares the bonus with the level's income
    /// (Tools/PlayTest/r73_early_call.txt).
    /// </summary>
    public sealed class EarlyCallProbeTests
    {
        private const string ReportPath = "Tools/PlayTest/r73_early_call.txt";
        private const string PlanPath = "Tools/PlayTest/Plans/Level_C1_01_0_strategy1.json";
        private const int LandingSite = 0;
        private const int Easy = 0;
        private static readonly int[] Seeds = { 1, 2, 3 };

        [TearDown]
        public void Reset()
        {
            BalanceProbeTests.CallEarly = false;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Category("Balance")]
        [Explicit("Six probe runs of Landing Site Easy: minutes. Run on a worker clone.")]
        [Timeout(7200000)]
        public IEnumerator LandingSiteEasy_BestPlan_CallingEveryWaveEarly_EndsCleanly_AndPaysAModestBonus()
        {
            StrategySpec spec = JsonUtility.FromJson<StrategySpec>(System.IO.File.ReadAllText(PlanPath));
            Assert.IsNotNull(spec?.Spots, "the plan file is missing");
            ProbeStrategy strategy = ProbeStrategy.Adaptive(spec);
            var run = new List<(int, int)> { (LandingSite, Easy) };
            var waiting = new List<BalanceProbeTests.ProbeResult>();
            var calling = new List<BalanceProbeTests.ProbeResult>();
            foreach (int seed in Seeds)
            {
                BalanceProbeTests.CallEarly = false;
                yield return BalanceProbeTests.Probe(run, strategy, waiting, seed);
                BalanceProbeTests.CallEarly = true;
                yield return BalanceProbeTests.Probe(run, strategy, calling, seed);
            }

            BalanceProbeTests.CallEarly = false;
            CampaignCatalog catalog = BalancePipelineTests.Catalog();
            LevelDefinition level = catalog.Nodes[LandingSite].Level;
            int startingMoney = Mathf.RoundToInt(level.StartingMoney * catalog.Difficulties[Easy].StartingMoneyMultiplier);
            System.IO.File.WriteAllText(ReportPath, Report(waiting, calling, startingMoney));
            Debug.Log("[EarlyCall] " + System.IO.File.ReadAllText(ReportPath));

            foreach (BalanceProbeTests.ProbeResult result in calling)
            {
                Assert.AreNotEqual(LevelOutcome.Running, result.Outcome, $"seed {result.Seed}: the level did not end (soft-lock)");
                Assert.Greater(result.EarlyCalls, 0, $"seed {result.Seed}: no call was accepted");
                Assert.LessOrEqual(result.EarlyCalls, result.WaveCount - 1, "the first wave is build time");
            }

            foreach (BalanceProbeTests.ProbeResult result in waiting)
            {
                Assert.AreEqual(0, result.EarlyCalls, "the baseline never calls");
            }
        }

        private static string Report(List<BalanceProbeTests.ProbeResult> waiting, List<BalanceProbeTests.ProbeResult> calling, int startingMoney)
        {
            var text = new StringBuilder();
            text.Append(Invariant($"Early wave calls, Landing Site Easy, best plan (strategy 1), {System.DateTime.Now:yyyy-MM-dd HH:mm}. Starting money {startingMoney}.\n"));
            text.Append("Income = coins earned in the level from kills (raw bounties, before the Easy income share), start money excluded.\n\n");
            text.Append("seed  play     outcome  stars  base  wave   income  calls  bonus  bonus/income  game s\n");
            for (int i = 0; i < calling.Count; i++)
            {
                Row(text, waiting[i], "waits", startingMoney);
                Row(text, calling[i], "calls", startingMoney);
            }

            return text.ToString();
        }

        private static void Row(StringBuilder text, BalanceProbeTests.ProbeResult result, string play, int startingMoney)
        {
            int income = result.Earned - startingMoney;
            float share = income > 0 ? (float)result.EarlyBonus / income : 0f;
            text.Append(Invariant($"{result.Seed,4}  {play,-7}  {result.Outcome,-7}  {result.Stars,5}  {result.BaseHealth,4:0}  {result.Wave,2}/{result.WaveCount,-2}  {income,6}  {result.EarlyCalls,5}  {result.EarlyBonus,5}  {share,12:P1}  {result.GameSeconds,6:0}\n"));
        }
    }
}
