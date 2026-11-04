using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Economy;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Runs the balance model over every level, difficulty and wave as they are stored, writes the table to
    /// Tools/PlayTest/balance_ledger.txt and fails when a wave leaves its difficulty's band, jumps far above the waves
    /// before it, or late Easy asks nothing (<see cref="BalanceChecks"/>). The player is the one the progression model
    /// expects there: owned towers only, the research of the levels before spent on upgrades. Per level it also applies the
    /// human lens (<see cref="BalanceChecks.CheckHuman"/>): Easy may not hand a competent player far more firepower than
    /// it needs, and each difficulty must ask more than the one below.
    /// </summary>
    public sealed class BalanceLedgerTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string LedgerPath = "Tools/PlayTest/balance_ledger.txt";

        [Test]
        public void EveryWave_StaysInsideItsDifficultyBand()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules);
            BalanceRules human = rules.HumanTowersOnly();

            var report = new StringBuilder();
            var failures = new List<string>();
            int index = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                var plainMeans = new List<float>();
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    DifficultyProfile difficulty = catalog.Difficulties[d];
                    List<WaveLine> lines = BalanceLedger.Evaluate(model, index, node.Level, difficulty, d);
                    report.Append(node.Level.name).Append(' ').Append(difficulty.name).Append('\n');
                    foreach (WaveLine line in lines)
                    {
                        report.Append("  ").Append(line).Append('\n');
                    }

                    failures.AddRange(BalanceChecks.Check(node.Level.name, difficulty.name, d, lines, rules));
                    plainMeans.Add(BalanceChecks.PlainMean(BalanceLedger.Evaluate(model, index, node.Level, difficulty, d, null, human)));
                }

                report.Append(BalanceChecks.DescribeHuman(node.Level.name, plainMeans)).Append('\n');
                failures.AddRange(BalanceChecks.CheckHuman(node.Level.name, plainMeans, rules));

                index++;
            }

            System.IO.File.WriteAllText(LedgerPath, report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + LedgerPath);
        }

        // GAME_DESIGN, Economy, per continent: its levels on Easy pay about a fifth of the research upgrades tagged to it,
        // on Psycho all of them; power is never paid on Easy. Only some of a continent's twenty levels exist yet, so the
        // stored payouts are checked against the curve, and the curve over all twenty against the upgrades' cost.
        [Test]
        public void MetaEconomy_FollowsTheDesign()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules);

            for (int region = 0; region < catalog.Regions.Count; region++)
            {
                if (catalog.NodeCount(region) == 0)
                {
                    continue;
                }

                model.MetaTotals(region, out int researchCost, out int powerCost, out _, out int[] power);
                int easy = 0;
                for (int i = 0; i < rules.LevelsPerContinent; i++)
                {
                    easy += Game.Editor.BalanceSheet.Research(region, i);
                }

                string name = catalog.Regions[region].DisplayName;
                Assert.Greater(researchCost, 0, $"{name} has research upgrades tagged to it");
                Assert.AreEqual(0.2f, easy / (float)researchCost, 0.03f, $"{name}: all of Easy pays a fifth of its research upgrades");
                Assert.AreEqual(1f, easy * rules.ResearchMultipliers[3] / researchCost, 0.05f, $"{name}: all of Psycho pays every research upgrade");
                Assert.AreEqual(0, power[0], $"{name}: Easy pays no power");
                Assert.Greater(powerCost, 0, $"{name} has drone upgrades tagged to it");

                int first = catalog.FirstNode(region);
                for (int i = first; i < first + catalog.NodeCount(region); i++)
                {
                    LevelDefinition level = catalog.Nodes[i].Level;
                    if (level != null)
                    {
                        Assert.AreEqual(Game.Editor.BalanceSheet.Research(region, i - first), level.Reward.ResearchPoints, $"{level.name} pays the curve");
                    }
                }
            }
        }
    }
}
