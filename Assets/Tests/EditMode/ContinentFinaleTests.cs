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

        [Test]
        public void EveryContinentBoss_AtItsFinale_IsKillableInsideTheHeroCap()
        {
            var failures = new List<string>();
            var table = new StringBuilder("Continent bosses at their finales (level 20, stand-in field, human lens)\n");
            foreach (BalanceSheet.ContinentFinale finale in BalanceSheet.Finales)
            {
                Setup setup = Prepare(finale);
                for (int d = 0; d < setup.Catalog.Difficulties.Count; d++)
                {
                    List<WaveSpec> waves = SpecsWithBoss(setup, d, out int boss);
                    var loads = new List<WaveLoad>();
                    FinaleLine line = FinaleModel.Judge(setup.Planned, waves, boss, d, setup.Extra, loads);
                    string name = setup.Catalog.Difficulties[d].name;
                    table.AppendLine($"{finale.Boss,-16} C{finale.Continent + 1} {name,-7} towers {line.Towers} research {line.ResearchEarned,4} money {line.Money,5} "
                        + $"scale {line.PlainScale:0.00} plain {line.PlainTowers:0.00}/{line.PlainHero:0.00} wave {line.BossTowers:0.00}/{line.BossHero:0.00} boss {line.BossAlone:0.00} cap {line.Cap:0.00} "
                        + $"mechanic {line.MechanicShare:0.00} -> {Verdict(line)}");
                    // Killable everywhere. "Too easy" is reported, not failed: the ledger counts a lone boss as easier to stop
                    // than the finale probe finds it (a boss at the base ends the level in seconds), so the health was set by
                    // the probe and the ledger only guards the top (Docs/BALANCE.md, "Continent bosses as played").
                    if (line.TooHard)
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

        [Test]
        public void EveryBossCounterplay_IsOwnedByItsFinale()
        {
            var failures = new List<string>();
            foreach (BalanceSheet.ContinentFinale finale in BalanceSheet.Finales)
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

        private static string Verdict(FinaleLine line) =>
            line.TooEasy ? "boss alone under a plain wave (see the probe)" : line.Verdict;

        private sealed class Setup
        {
            public CampaignCatalog Catalog;
            public CampaignModel Real;
            public CampaignModel Planned;
            public LevelDefinition StandIn;
            public EnemyStats Boss;
            public readonly List<TowerStats> Extra = new List<TowerStats>();
        }

        private static Setup Prepare(BalanceSheet.ContinentFinale finale)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{finale.Boss}.asset");
            Assert.IsNotNull(boss, $"run the Continent bosses generator step for {finale.Boss}");
            CampaignModel real = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default);
            var planned = new Dictionary<string, TowerStats>();
            var setup = new Setup { Catalog = catalog, Real = real, Boss = BalanceLedger.Stats(boss) };
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
