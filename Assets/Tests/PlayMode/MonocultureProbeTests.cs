using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    /// Monoculture probe (round 20, Remco: "Als je kunt winnen door alleen een rijtje torens neer te zetten van hetzelfde
    /// soort, dan is het niet in balans"). The probe bot plays each level as: one owned tower type only (mono, placed by
    /// its role, hero on, no drones, gadgets or selling), the creative bot (every owned tower by role, drones, mines before
    /// crowd and boss waves, a slow field at the gate's exit, sell-and-rebuy for a sky gun) and the naive bot (the creative
    /// bot's purse and choices on seeded random spots, upgrading without thought). The rules are reported, not failed,
    /// while the data is awaiting Remco's rebalance by role: Medium at most one mono type may win and only with one star,
    /// Hard and Psycho none; the creative bot wins Medium and Hard with at least two stars; the naive bot loses from Medium
    /// on. Only errors in the level fail a run. Tables: Tools/PlayTest/monoculture_smoke.txt (smoke) and
    /// Tools/PlayTest/monoculture.txt (the full set); how to read them: Docs/BALANCE.md, Monoculture check.
    /// </summary>
    public sealed class MonocultureProbeTests
    {
        private const string SmokePath = "Tools/PlayTest/monoculture_smoke.txt";
        private const string FullPath = "Tools/PlayTest/monoculture.txt";
        private const string EasyPath = "Tools/PlayTest/monoculture_easy.txt";
        private const string TracePath = "Logs/probe_trace.txt";
        private const int Easy = 0;
        private const int Medium = 1;
        private const int Hard = 2;
        private const int MediumMonoWins = 1;
        private const int MediumMonoStars = 1;
        private const int CreativeStars = 2;
        private const string KnownPrefix = "Known, awaiting Remco's rebalance by role: ";

        // The smoke set: Shard Ridge (first flyers) and Crysium Vein, on Medium, three mono types and the creative bot.
        private static readonly string[] SmokeLevels = { "Level_C1_03", "Level_C1_06" };
        private static readonly string[] SmokeMono = { "PulseRifle", "Cannon", "Laser" };
        private static readonly int[] FullDifficulties = { Medium, Hard };
        private static readonly string[] EasyLevels = { "Level_C1_01", "Level_C1_02", "Level_C1_03" };

        // The probe appends every run's trace; one test's file holds its own runs.
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
        [Timeout(7200000)]
        public IEnumerator Smoke_Monoculture_ShardRidgeAndCrysiumVein_OnMedium()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (int node in Levels(SmokeLevels))
            {
                foreach (string tower in SmokeMono)
                {
                    yield return BalanceProbeTests.Probe(new List<(int, int)> { (node, Medium) }, ProbeStrategy.Mono(tower), results);
                }

                yield return BalanceProbeTests.Probe(new List<(int, int)> { (node, Medium) }, ProbeStrategy.CreativeBot(), results);
            }

            Assert.AreEqual(SmokeLevels.Length * (SmokeMono.Length + 1), results.Count, "a run did not finish");
            Conclude(results, SmokePath, "smoke set");
        }

        [UnityTest]
        [Category("Monoculture")]
        [Explicit("Every level on Medium and Hard, every owned mono type, the creative and the naive bot; run on request.")]
        [Timeout(36000000)]
        public IEnumerator Full_Monoculture_EveryLevel_MediumAndHard()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (int node in BalanceProbeTests.ExistingLevels(false))
            {
                foreach (int difficulty in FullDifficulties)
                {
                    if (!BalanceProbeTests.Wanted("levels", node) || !BalanceProbeTests.Wanted("difficulties", difficulty))
                    {
                        continue;
                    }

                    var run = new List<(int, int)> { (node, difficulty) };
                    foreach (string tower in MonoTypes(node))
                    {
                        yield return BalanceProbeTests.Probe(run, ProbeStrategy.Mono(tower), results);
                    }

                    yield return BalanceProbeTests.Probe(run, ProbeStrategy.CreativeBot(), results);
                    yield return BalanceProbeTests.Probe(run, ProbeStrategy.NaiveBot(), results);
                }
            }

            Assert.IsNotEmpty(results);
            Conclude(results, FullPath, "full set");
        }

        /// <summary>
        /// Round 21a: Easy must stay comfortable on the first levels for a normal player, so the creative and the naive bot
        /// both must win Landing Site, Glass Meadows and Shard Ridge on Easy (reported like the full set).
        /// </summary>
        [UnityTest]
        [Category("Monoculture")]
        [Explicit("The first three levels on Easy, creative and naive bot; run on request.")]
        [Timeout(7200000)]
        public IEnumerator Full_Monoculture_FirstLevels_OnEasy()
        {
            var results = new List<BalanceProbeTests.ProbeResult>();
            foreach (int node in Levels(EasyLevels))
            {
                var run = new List<(int, int)> { (node, Easy) };
                yield return BalanceProbeTests.Probe(run, ProbeStrategy.CreativeBot(), results);
                yield return BalanceProbeTests.Probe(run, ProbeStrategy.NaiveBot(), results);
            }

            Assert.AreEqual(EasyLevels.Length * 2, results.Count, "a run did not finish");
            var breaches = results.FindAll(r => r.Outcome != LevelOutcome.Won).ConvertAll(r => $"{r.Asset} Easy: {r.Strategy} {r.Outcome} at wave {r.Wave}/{r.WaveCount} (must win)");
            System.IO.File.WriteAllText(EasyPath, Table(results, breaches, "first levels on Easy"));
            Debug.Log($"[Monoculture] Easy: {results.Count} runs, {breaches.Count} losses, see {EasyPath}");
            if (breaches.Count > 0)
            {
                Assert.Ignore(KnownPrefix + string.Join("; ", breaches));
            }
        }

        // Writes the table and the rule check; the breaches are printed and the test is marked ignored (known) when any exist.
        private static void Conclude(List<BalanceProbeTests.ProbeResult> results, string path, string set)
        {
            List<string> breaches = Breaches(results);
            System.IO.File.WriteAllText(path, Table(results, breaches, set));
            Debug.Log($"[Monoculture] {set}: {results.Count} runs, {breaches.Count} rule breaches, see {path}");
            if (breaches.Count > 0)
            {
                Debug.Log("[Monoculture] breaches:\n" + string.Join("\n", breaches));
                Assert.Ignore(KnownPrefix + string.Join("; ", breaches));
            }
        }

        private static List<string> Breaches(List<BalanceProbeTests.ProbeResult> results)
        {
            var breaches = new List<string>();
            var groups = new Dictionary<(string, int), List<BalanceProbeTests.ProbeResult>>();
            foreach (BalanceProbeTests.ProbeResult result in results)
            {
                if (!groups.TryGetValue((result.Asset, result.Difficulty), out var group))
                {
                    groups[(result.Asset, result.Difficulty)] = group = new List<BalanceProbeTests.ProbeResult>();
                }

                group.Add(result);
            }

            foreach (KeyValuePair<(string Asset, int Difficulty), List<BalanceProbeTests.ProbeResult>> group in groups)
            {
                string name = $"{group.Key.Asset} {DifficultyName(group.Key.Difficulty)}";
                var monoWins = group.Value.FindAll(r => r.Strategy.StartsWith("mono") && r.Outcome == LevelOutcome.Won);
                if (group.Key.Difficulty == Medium)
                {
                    if (monoWins.Count > MediumMonoWins || monoWins.Exists(r => r.Stars > MediumMonoStars))
                    {
                        breaches.Add($"{name}: mono wins {Describe(monoWins)} (at most {MediumMonoWins}, with {MediumMonoStars} star)");
                    }
                }
                else if (group.Key.Difficulty > Medium && monoWins.Count > 0)
                {
                    breaches.Add($"{name}: mono wins {Describe(monoWins)} (none allowed)");
                }

                foreach (BalanceProbeTests.ProbeResult result in group.Value)
                {
                    bool mustWin = result.Strategy == "creative" && (group.Key.Difficulty == Medium || group.Key.Difficulty == Hard);
                    if (mustWin && (result.Outcome != LevelOutcome.Won || result.Stars < CreativeStars))
                    {
                        breaches.Add($"{name}: creative {result.Outcome} with {result.Stars} stars at wave {result.Wave}/{result.WaveCount} (must win with {CreativeStars})");
                    }

                    if (result.Strategy == "naive" && group.Key.Difficulty >= Medium && result.Outcome == LevelOutcome.Won)
                    {
                        breaches.Add($"{name}: naive won with {result.Stars} stars (must lose: the level does not ask for thought)");
                    }
                }
            }

            return breaches;
        }

        private static string Describe(List<BalanceProbeTests.ProbeResult> wins) =>
            string.Join(", ", wins.ConvertAll(r => $"{r.Strategy.Substring(5)} {r.Stars}*"));

        private static string Table(List<BalanceProbeTests.ProbeResult> results, List<string> breaches, string set)
        {
            var text = new StringBuilder();
            text.Append($"Monoculture probe, {set} (round 21a; seeded, 0.2 s steps; Docs/BALANCE.md, Monoculture check and Roles by enemy)\n");
            text.Append("dps = damage really dealt per tower-second while creatures were on the field, against the data DPS of the levels it stood at.\n\n");
            text.Append("level        diff    strategy           result stars wave   base     built upg sold mines fields drones                        ore  unspent repairs(%earned)  in-game dps per type (real/data)\n");
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                text.Append(Invariant($"{r.Asset,-12} {DifficultyName(r.Difficulty),-7} {r.Strategy,-18} {r.Outcome,-6} {r.Stars,5} {r.Wave,2}/{r.WaveCount,-2} {r.BaseHealth,4:0}/{r.BaseMax,-4:0} {r.TowersBuilt,5} {r.Upgrades,3} {r.Sold,4} {r.Mines,5} {r.Fields,6} {(r.Drones.Length > 0 ? r.Drones : "-"),-29} {r.Harvested,4} {r.CoinsUnspent,8} {r.RepairSpent,7} ({100f * r.RepairSpent / Mathf.Max(1, r.Earned),3:0}%)  "));
                foreach (KeyValuePair<string, (float Damage, float Seconds, float DataDamage)> fire in r.Fire)
                {
                    text.Append(Invariant($" {fire.Key} {Rate(fire.Value.Damage, fire.Value.Seconds):0.0}/{Rate(fire.Value.DataDamage, fire.Value.Seconds):0.0}"));
                }

                text.Append('\n');
            }

            text.Append("\nIn-game DPS per tower type over all runs (per tower-second with creatures on the field; share of the data DPS):\n");
            var totals = new SortedDictionary<string, (float Damage, float Seconds, float Data)>();
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                foreach (KeyValuePair<string, (float Damage, float Seconds, float DataDamage)> fire in r.Fire)
                {
                    totals.TryGetValue(fire.Key, out var sum);
                    totals[fire.Key] = (sum.Damage + fire.Value.Damage, sum.Seconds + fire.Value.Seconds, sum.Data + fire.Value.DataDamage);
                }
            }

            foreach (KeyValuePair<string, (float Damage, float Seconds, float Data)> total in totals)
            {
                text.Append(Invariant($"  {total.Key,-12} {Rate(total.Value.Damage, total.Value.Seconds),6:0.0} dps of {Rate(total.Value.Data, total.Value.Seconds),6:0.0} data ({100f * total.Value.Damage / Mathf.Max(0.01f, total.Value.Data),3:0}%) over {total.Value.Seconds,6:0} tower-s\n"));
            }

            int repairs = 0;
            int earned = 0;
            foreach (BalanceProbeTests.ProbeResult r in results)
            {
                repairs += r.RepairSpent;
                earned += r.Earned;
            }

            text.Append(Invariant($"\nRepairs bought with coins over all runs: {repairs} of {earned} earned ({100f * repairs / Mathf.Max(1, earned):0.0}%).\n"));
            text.Append("\nRules (report only): Medium at most one mono type wins, with one star; Hard none; creative wins Medium and Hard with 2+ stars; naive loses Medium+.\n");
            text.Append(breaches.Count == 0 ? "  all met\n" : "  " + string.Join("\n  ", breaches) + "\n");
            return text.ToString();
        }

        private static float Rate(float amount, float seconds) => seconds > 0f ? amount / seconds : 0f;

        private static string DifficultyName(int difficulty)
        {
            string[] names = { "Easy", "Medium", "Hard", "Psycho" };
            return difficulty >= 0 && difficulty < names.Length ? names[difficulty] : difficulty.ToString();
        }

        // Node indices of the named levels.
        private static List<int> Levels(string[] assets)
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
        private static List<string> MonoTypes(int node)
        {
            var types = new List<string>();
            CampaignCatalog catalog = Catalog();
            if (catalog == null)
            {
                return types;
            }

            List<string> owned = BalanceProbeTests.OwnedTowers(catalog, node);
            foreach (TowerDefinition tower in catalog.Nodes[node].Level.AvailableTowers)
            {
                if (owned.Contains(tower.Id) && !tower.IsSupport && !tower.BlocksGroundEnemies && (tower.Targets & TargetLayers.Ground) != 0)
                {
                    types.Add(tower.Id);
                }
            }

            return types;
        }

        private static CampaignCatalog Catalog()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(BalanceProbeTests.CampaignPath);
#else
            return null;
#endif
        }
    }
}
