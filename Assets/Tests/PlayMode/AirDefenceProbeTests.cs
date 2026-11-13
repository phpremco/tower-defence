using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// Round 61 (Remco: "you don't need air defence on Easy"): the same ledger bot plays a cell three times, without any gun
    /// made for the sky, without one but patching the base with the guns it has once flyers are over it, and with sky guns
    /// standing before the first raid, and the table says who cleared the sky: per
    /// run the flyers shot down, the hits they landed on the base, the base left, and per source (tower type, base turret,
    /// hero, drone) the share of the flyers' health it took. A report, not a verdict: only errors in the level fail it; the
    /// rule it serves is checked in EditMode (AirDefenceTests).
    /// </summary>
    public sealed class AirDefenceProbeTests
    {
        public const string ReportPath = "Tools/PlayTest/air_defence.txt";
        private const string TracePath = "Logs/probe_trace.txt";

        // Shard Ridge and Fault Line, the first two levels that sell the sky gun; Easy and Medium.
        private static readonly int[] DefaultLevels = { 2, 3 };
        private static readonly int[] DefaultDifficulties = { 0, 1 };

        // One sky gun under each flight route.
        private const int SkyGuns = 2;

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
        [Explicit("About half a minute a run; narrow it with Tools/PlayTest/probe_arg.txt (levels=, difficulties=, seeds=).")]
        [Timeout(7200000)]
        public IEnumerator Report_SkyGunsAgainstNone_OnTheFirstAirLevels()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            List<int> seeds = BalanceProbeTests.Seeds();
            foreach (int node in BalanceProbeTests.ExistingLevels(false))
            {
                foreach (int difficulty in DefaultDifficulties)
                {
                    if (System.Array.IndexOf(DefaultLevels, node) < 0 || !BalanceProbeTests.WantedCell(node, difficulty))
                    {
                        continue;
                    }

                    var run = new List<(int, int)> { (node, difficulty) };
                    foreach (int seed in seeds)
                    {
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.NoSkyGun, results, seed);
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.NoSkyGunGuardsBase, results, seed);
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.WithSkyGuns(SkyGuns), results, seed);
                    }
                }
            }

            Assert.IsNotEmpty(results, "no run played (does the filter file leave a cell?)");
            System.IO.File.WriteAllText(ReportPath, Table(results));
            Debug.Log($"[Balance] air defence: {results.Count} runs, see {ReportPath}");
        }

        private static string Table(List<BalanceProbeTests.ProbeResult> results)
        {
            var text = new StringBuilder();
            text.Append(Invariant($"Air defence, sky guns against none, {System.DateTime.Now:yyyy-MM-dd HH:mm}. {BalancePipelineTests.CalibrationNote()}\n"));
            text.Append("Per run: outcome, wave reached, stars, base left, flyers shot down of those that came, hits flyers landed on the base, towers standing by type,\n");
            text.Append("then who took the flyers' health (share of what all flyers brought; the rest reached the end alive), and the raids one by one.\n\n");
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                text.Append(Invariant($"{r.Asset} {DifficultyName(r.Difficulty),-6} seed {r.Seed} {r.Strategy,-14}: {(r.Outcome == LevelOutcome.Won ? "won " : "LOST")} wave {r.Wave}/{r.WaveCount}, {r.Stars} stars, base {r.BaseHealth:0}/{r.BaseMax:0}, flyers down {r.FlyersKilled}/{r.FlyersSpawned}, flyer base hits {r.FlyerBaseHits}, coins left {r.CoinsUnspent}, built {Built(r)}\n"));
                var bySource = new SortedDictionary<string, float>();
                var byWave = new SortedDictionary<int, float>();
                float brought = 0f;
                foreach (KeyValuePair<int, float> wave in r.AirHealth)
                {
                    brought += wave.Value;
                }

                foreach (KeyValuePair<(int Wave, string Source), float> dealt in r.AirDealt)
                {
                    bySource[dealt.Key.Source] = (bySource.TryGetValue(dealt.Key.Source, out float sum) ? sum : 0f) + dealt.Value;
                    byWave[dealt.Key.Wave] = (byWave.TryGetValue(dealt.Key.Wave, out float taken) ? taken : 0f) + dealt.Value;
                }

                text.Append(Invariant($"    flyer health {brought:0}:"));
                float all = 0f;
                foreach (KeyValuePair<string, float> source in bySource)
                {
                    all += source.Value;
                    text.Append(Invariant($" {source.Key} {source.Value:0} ({Share(source.Value, brought):0}%)"));
                }

                text.Append(Invariant($"; not shot {Mathf.Max(0f, brought - all):0} ({Share(Mathf.Max(0f, brought - all), brought):0}%)\n    raids:"));
                var waves = new List<int>(r.AirHealth.Keys);
                waves.Sort();
                foreach (int wave in waves)
                {
                    text.Append(Invariant($" w{wave} {(byWave.TryGetValue(wave, out float taken) ? taken : 0f):0}/{r.AirHealth[wave]:0}"));
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        private static float Share(float part, float whole) => whole > 0f ? 100f * part / whole : 0f;

        private static string Built(BalanceProbeTests.ProbeResult r)
        {
            var counts = new SortedDictionary<string, int>();
            foreach (Game.Balance.PlanStep move in r.Moves)
            {
                if (move.Kind == Game.Balance.PlanStep.Build)
                {
                    counts[move.Item] = (counts.TryGetValue(move.Item, out int have) ? have : 0) + 1;
                }
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                parts.Add(pair.Key + " x" + pair.Value);
            }

            return (parts.Count > 0 ? string.Join(", ", parts) : "nothing") + Invariant($", {r.Upgrades} upgrades");
        }

        private static string DifficultyName(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return "Easy";
                case 1: return "Medium";
                case 2: return "Hard";
                default: return "Psycho";
            }
        }
    }
}
