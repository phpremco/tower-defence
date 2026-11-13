using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Core;
using Game.Economy;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// The six continent bosses at their finales as they will be played (Docs/BALANCE.md, "Continent bosses as played"):
    /// level 20 of their continent with the roadmap's loadout and the research of that difficulty's path, on the last real
    /// level as stand-in field. Each must be killable inside the hero cap, must keep its
    /// mechanic inside the ledger's cap, and its counterplay must rest on tools the player owns by then.
    /// </summary>
    public sealed class ContinentFinaleTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string TableFile = "Tools/PlayTest/finale_balance.txt";
        private const string ShareFile = "Tools/PlayTest/finale_boss_share.txt";
        private const int MediumIndex = 1;
        // How far Hard's and Psycho's boss may sit from Medium's share of the cap (pressure is not exactly linear in health).
        private const float ShareTolerance = 0.12f;

        [Test]
        public void EveryContinentBoss_AtItsFinale_IsKillableInsideTheHeroCap()
        {
            var failures = new List<string>();
            var table = new StringBuilder("Continent bosses at their finales (level 20, stand-in field, human lens)\n");
            List<FinaleProbeCell> probe = ProbeReport();
            foreach (ContinentFinale finale in BalanceSheet.Finales)
            {
                Setup setup = Prepare(finale);
                for (int d = 0; d < setup.Catalog.Difficulties.Count; d++)
                {
                    List<WaveSpec> waves = SpecsWithBoss(setup, d, out int boss);
                    var loads = new List<WaveLoad>();
                    FinaleLine line = FinaleModel.Judge(setup.Planned, waves, boss, d, setup.Extra, loads);
                    string name = setup.Catalog.Difficulties[d].name;
                    // The finale probe (round 22f) is the judge where it played: a boss the bot beat as the data has it is not too hard.
                    bool beatenInPlay = BeatenInPlay(probe, finale.Boss, d, setup.BossHealth, setup.Catalog.Difficulties[d].BossHealthShare * setup.Boss.HealthOn(name));
                    table.AppendLine($"{finale.Boss,-16} C{finale.Continent + 1} {name,-7} towers {line.Towers} research {line.ResearchEarned,4} money {line.Money,5} "
                        + $"scale {line.PlainScale:0.00} plain {line.PlainTowers:0.00}/{line.PlainHero:0.00} wave {line.BossTowers:0.00}/{line.BossHero:0.00} boss {line.BossAlone:0.00} cap {line.Cap:0.00} "
                        + $"mechanic {line.MechanicShare:0.00} -> {Verdict(line)}{(line.TooHard && beatenInPlay ? $" (the finale probe beat it, holds x{FinaleProbeReport.Find(probe, finale.Boss, d).Holds:0.00})" : string.Empty)}");
                    // Killable everywhere. "Too easy" is reported, not failed: the ledger counts a lone boss as easier to stop
                    // than the finale probe finds it (a boss at the base ends the level in seconds), so the health was set by
                    // the probe and the ledger only guards the top (Docs/BALANCE.md, "Continent bosses as played").
                    if (line.TooHard && !beatenInPlay)
                    {
                        failures.Add($"{finale.Boss} {name}: the boss alone asks {line.BossAlone:0.00} with the hero against plain {line.PlainHero:0.00} and cap {line.Cap:0.00} ({line.Verdict})");
                    }

                    string mechanic = BalanceChecks.CheckBossMechanic(finale.Boss, waves[boss].Groups.Find(g => g.Enemy.IsBoss).Enemy.MechanicLossSeconds, loads[boss].BossFight, setup.Planned.Rules.HumanWithHero());
                    if (mechanic != null)
                    {
                        failures.Add(mechanic);
                    }
                }
            }

            System.IO.File.WriteAllText(TableFile, table.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\n" + table);
        }

        /// <summary>
        /// Remco 2026-11-05: boss health per difficulty. With one health everywhere the boss alone asked about 0.6 of the Hard
        /// and Psycho caps against about 0.96 of Medium's. The difficulty's boss share must bring Hard and Psycho to Medium's
        /// share of their cap (the boss carries its part of the climax there too); Easy stays at or under Medium's (forgiving).
        /// The share each difficulty would need is derived from the ledger and written beside the table.
        /// </summary>
        [Test]
        public void BossHealthShare_GivesEveryContinentBossMediumsShareOfTheCap()
        {
            var failures = new List<string>();
            var table = new StringBuilder("\nBoss health share per difficulty (boss alone / cap; share needed for Medium's ratio, linear)\n");
            List<FinaleProbeCell> probe = ProbeReport();
            foreach (ContinentFinale finale in BalanceSheet.Finales)
            {
                Setup setup = Prepare(finale);
                int count = setup.Catalog.Difficulties.Count;
                var ratios = new float[count];
                for (int d = 0; d < count; d++)
                {
                    List<WaveSpec> waves = SpecsWithBoss(setup, d, out int boss);
                    FinaleLine line = FinaleModel.Judge(setup.Planned, waves, boss, d, setup.Extra);
                    ratios[d] = line.BossAlone / line.Cap;
                }

                float medium = ratios[MediumIndex];
                var row = new StringBuilder($"{finale.Boss,-16}");
                for (int d = 0; d < count; d++)
                {
                    DifficultyProfile profile = setup.Catalog.Difficulties[d];
                    float needed = profile.BossHealthShare * medium / ratios[d];
                    row.Append($" {profile.name} {ratios[d]:0.00} (share {profile.BossHealthShare:0.00}, needs {needed:0.00})");
                    float relative = ratios[d] / medium;
                    // Where the finale probe played the difficulty, its derived share judges instead of the ledger's ratio (round 22f).
                    float probed = probe != null ? FinaleProbeReport.DerivedShare(probe, d) : -1f;
                    float hardShare = setup.Catalog.Difficulties[FinaleProbeReport.Hard].BossHealthShare;
                    bool shared = BalanceRules.Default.PsychoBossShareAtLeastHard;
                    if (shared && d == FinaleProbeReport.Psycho && profile.BossHealthShare < hardShare - 0.001f)
                    {
                        failures.Add($"{profile.name}: the boss health share {profile.BossHealthShare:0.00} is below Hard's {hardShare:0.00} (Psycho at least Hard, round 30)");
                    }

                    float designer = BalanceSheet.DesignerHardBossShare;
                    if (d > MediumIndex && designer > 0f)
                    {
                        // Remco 2026-11-10 (round 31, question 3): Hard carries the designer's share (1.25), lighter than the probe's
                        // Psycho ceiling (1.53 at round 31), and Psycho at least Hard's (checked above). The probe's figure is
                        // printed as advice, not asserted, until the designer hands the share back to it (designer share 0).
                        float advice = probed > 0f && shared ? FinaleProbeReport.HardPsychoShare(probe, BalanceRules.Default) : probed;
                        row.Append($" [designer {designer:0.00}, probe would ask {advice:0.00}]");
                        if (d == FinaleProbeReport.Hard && System.Math.Abs(profile.BossHealthShare - designer) > 0.001f)
                        {
                            failures.Add($"{profile.name}: the boss health share is {profile.BossHealthShare:0.00}, the designer's is {designer:0.00} (BalanceSheet.DesignerHardBossShare)");
                        }
                    }
                    else if (d > MediumIndex && probed > 0f)
                    {
                        // Round 30: under the Psycho-at-least-Hard rule both carry one share, Hard's derivation pulled down to the
                        // Psycho ceiling (the share at which the Psycho finale player still beats every boss); else each its own.
                        float wanted = shared ? FinaleProbeReport.HardPsychoShare(probe, BalanceRules.Default) : probed;
                        row.Append(shared ? $" [probe needs {probed:0.00}, shared {wanted:0.00}]" : $" [probe needs {probed:0.00}]");
                        if (wanted > 0f && System.Math.Abs(profile.BossHealthShare / wanted - 1f) > ShareTolerance)
                        {
                            failures.Add($"{profile.name}: the boss health share is {profile.BossHealthShare:0.00}, the finale probe asks {wanted:0.00} (run the finale stage)");
                        }
                    }
                    else if (d > MediumIndex && System.Math.Abs(relative - 1f) > ShareTolerance)
                    {
                        failures.Add($"{finale.Boss} {profile.name}: the boss alone asks {relative:0.00}x Medium's share of the cap (share {profile.BossHealthShare:0.00}, needs about {needed:0.00})");
                    }

                    if (d < MediumIndex && relative > 1f + ShareTolerance)
                    {
                        failures.Add($"{finale.Boss} {profile.name}: the boss asks more of Easy's cap than of Medium's ({relative:0.00}x)");
                    }
                }

                table.AppendLine(row.ToString());
            }

            System.IO.File.WriteAllText(ShareFile, table.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + table);
        }

        // Round 58: a boss's own difficulty factors name real difficulty profiles, and the Shattered's touches Easy only (its new
        // entrance made only Easy easier); the ledger reads the factor where the spawner does.
        [Test]
        public void BossDifficultyHealth_NamesRealProfiles_AndTheShatteredOnlyEasy()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var names = new HashSet<string>();
            foreach (DifficultyProfile profile in catalog.Difficulties)
            {
                names.Add(profile.name);
            }

            foreach (ContinentFinale finale in BalanceSheet.Finales)
            {
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{finale.Boss}.asset");
                foreach (BossDifficultyHealth entry in boss.Boss.DifficultyHealth)
                {
                    Assert.IsTrue(names.Contains(entry.Difficulty), $"{finale.Boss}: no difficulty profile named {entry.Difficulty}");
                }
            }

            var shattered = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Shattered.asset");
            EnemyStats stats = BalanceLedger.Stats(shattered);
            for (int d = 0; d < catalog.Difficulties.Count; d++)
            {
                DifficultyProfile profile = catalog.Difficulties[d];
                DifficultyStats difficulty = BalanceLedger.Stats(profile, d);
                bool easy = profile.name == "Easy";
                Assert.AreEqual(easy, shattered.Boss.HealthOn(profile.name) > 1f, $"Shattered on {profile.name}");
                Assert.AreEqual(profile.BossHealthShare * shattered.Boss.HealthOn(profile.name), difficulty.ShareFor(stats), 1e-5f, $"the ledger's share on {profile.name}");
            }
        }

        [Test]
        public void EveryBossCounterplay_IsOwnedByItsFinale()
        {
            var failures = new List<string>();
            foreach (ContinentFinale finale in BalanceSheet.Finales)
            {
                foreach ((string what, int continent, bool built) in finale.Counters)
                {
                    if (continent > finale.Continent)
                    {
                        failures.Add($"{finale.Boss} (continent {finale.Continent + 1}) relies on {what}, which arrives in continent {continent + 1}");
                    }

                    if (!built)
                    {
                        failures.Add($"{finale.Boss} relies on {what}, which has no data yet");
                    }
                }
            }

            foreach (PlannedTower tower in BalanceSheet.PlannedTowers)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{tower.Id}.asset"), $"planned tower {tower.Id} has no data");
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void PlannedCampaign_KeepsTheRealLevels_AndPaysTheContinentsResearch()
        {
            Setup setup = Prepare(BalanceSheet.Finales[0]);
            CampaignModel real = setup.Real;
            Assert.AreEqual(real.Rules.LevelsPerContinent, setup.Planned.Levels.Count);
            for (int i = 0; i < real.Levels.Count; i++)
            {
                Assert.AreSame(real.Levels[i], setup.Planned.Levels[i], "the real levels stay as they are");
            }

            int easyTotal = 0;
            for (int i = 0; i < setup.Planned.Levels.Count; i++)
            {
                easyTotal += setup.Planned.Levels[i].Research;
            }

            // Easy everywhere pays the Easy share of the continent's budget (rounding and the per-level floor aside).
            Assert.AreEqual(real.Rules.EasyResearchShare * BalanceSheet.Continents[0].ResearchBudget, easyTotal, 12f);
            PlayerState finale = setup.Planned.PlayerAt(setup.Planned.Levels.Count - 1, 1);
            foreach (PlannedTower tower in BalanceSheet.PlannedTowers)
            {
                Assert.AreEqual(tower.Continent == 0, finale.OwnedTowers.Contains(tower.Id), $"{tower.Id} at the Crystal Plains finale");
            }
        }

        /// <summary>
        /// The finale probe's derived boss health (Medium's holding multiple with the hold share) against the data, per boss
        /// (round 22f); ignored until the probe has run. The finale stage writes the derived numbers.
        /// </summary>
        [Test]
        public void FinaleProbe_DerivedBossHealth_MatchesTheData()
        {
            List<FinaleProbeCell> probe = ProbeReport();
            if (probe == null)
            {
                Assert.Ignore("no finale probe report yet: run FinaleProbeTests (PlayMode, explicit) first");
            }

            var failures = new List<string>();
            var table = new StringBuilder("\nBoss health from the finale probe (data / derived)\n");
            BalanceRules rules = BalanceRules.Default;
            foreach (ContinentFinale finale in BalanceSheet.Finales)
            {
                float derived = FinaleProbeReport.DerivedHealth(probe, finale.Boss, rules);
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{finale.Boss}.asset");
                if (derived <= 0f || boss == null)
                {
                    table.AppendLine($"{finale.Boss,-16} not probed on Medium");
                    continue;
                }

                table.AppendLine($"{finale.Boss,-16} {boss.MaxHealth:0} / {derived:0}");
                if (System.Math.Abs(boss.MaxHealth / derived - 1f) > HealthTolerance)
                {
                    failures.Add($"{finale.Boss}: health {boss.MaxHealth:0} against the probe's {derived:0} (run the finale stage)");
                }
            }

            System.IO.File.AppendAllText(ShareFile, table.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + table);
        }

        // 0.1 until round 29; the one-seed probe gives holds that differ by up to 15 % between runs on identical data (probes 6 and 8:
        // Matriarch Easy 1.40 / 1.52, Shattered Easy 1.66 / 1.40), so the band is the probe's own spread.
        private const float HealthTolerance = 0.2f;

        private static List<FinaleProbeCell> ProbeReport() =>
            System.IO.File.Exists(FinaleProbeReport.Path) ? FinaleProbeReport.Read(System.IO.File.ReadAllLines(FinaleProbeReport.Path)) : null;

        // The probe judged the boss as the data stood when it ran (its holding multiple is of that health and share). A health
        // or share moved since is held against the health the probe beat: Gravemaw Medium (round 31) lost at 55050 holding
        // x0.845, so the probe beat it up to about 46500, and the data now carries 39540 (FinaleProbeReport.HoldsAsIs reads
        // the multiple alone and would call that a loss).
        private static bool BeatenInPlay(List<FinaleProbeCell> probe, string boss, int difficulty, float health, float share)
        {
            FinaleProbeCell cell = probe != null ? FinaleProbeReport.Find(probe, boss, difficulty) : null;
            return cell != null && cell.HealthNow > 0f && cell.Holds * cell.HealthNow * cell.ShareNow >= health * share * (1f - HealthSlack);
        }

        // Rounding of the health and share the probe report prints.
        private const float HealthSlack = 0.001f;

        private static string Verdict(FinaleLine line) =>
            line.TooEasy ? "boss alone under a plain wave (see the probe)" : line.Verdict;

        private sealed class Setup
        {
            public CampaignCatalog Catalog;
            public CampaignModel Real;
            public CampaignModel Planned;
            public LevelDefinition StandIn;
            public EnemyStats Boss;
            public float BossHealth;
            public readonly List<TowerStats> Extra = new List<TowerStats>();
        }

        private static Setup Prepare(ContinentFinale finale)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{finale.Boss}.asset");
            Assert.IsNotNull(boss, $"run the Continent bosses generator step for {finale.Boss}");
            CampaignModel real = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
            var planned = new Dictionary<string, TowerStats>();
            var setup = new Setup { Catalog = catalog, Real = real, Boss = BalanceLedger.Stats(boss), BossHealth = boss.MaxHealth };
            foreach (PlannedTower tower in BalanceSheet.PlannedTowers)
            {
                TowerStats stats = BalanceLedger.Stats(AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{tower.Id}.asset"));
                planned[tower.Id] = stats;
                if (tower.Continent > 0 && tower.Continent <= finale.Continent)
                {
                    setup.Extra.Add(stats);
                }
            }

            setup.Planned = FinaleModel.Planned(real, BalanceSheet.PlannedTowers, planned, BalanceSheet.Continents[0].ResearchBudget);
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    setup.StandIn = node.Level;
                }
            }

            return setup;
        }

        // The stand-in level's waves on a difficulty, its last boss wave carrying the continent boss (one of it).
        private static List<WaveSpec> SpecsWithBoss(Setup setup, int difficulty, out int finale)
        {
            var waves = new List<WaveSpec>();
            finale = -1;
            foreach (WaveDefinition wave in setup.StandIn.GetWaves(setup.Catalog.Difficulties[difficulty]))
            {
                WaveSpec spec = BalanceLedger.Spec(wave, setup.Real.Enemies);
                waves.Add(spec);
                if (spec.Groups.Exists(g => g.Enemy.IsBoss))
                {
                    finale = waves.Count - 1;
                }
            }

            Assert.GreaterOrEqual(finale, 0, $"{setup.StandIn.name} has no boss wave");
            List<GroupSpec> groups = waves[finale].Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Enemy.IsBoss)
                {
                    GroupSpec group = groups[i];
                    group.Enemy = setup.Boss;
                    group.Count = 1;
                    groups[i] = group;
                }
            }

            return waves;
        }
    }
}
