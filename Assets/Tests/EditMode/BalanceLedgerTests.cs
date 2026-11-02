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
    /// expects there: owned towers only, the research of the levels before spent on upgrades.
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

            var report = new StringBuilder();
            var failures = new List<string>();
            int index = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

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
                }

                index++;
            }

            System.IO.File.WriteAllText(LedgerPath, report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + LedgerPath);
        }

        // GAME_DESIGN, Economy: every level on Easy pays about a fifth of all research upgrades, every level on Psycho
        // pays them all; power is never paid on Easy and Psycho pays every drone upgrade.
        [Test]
        public void MetaEconomy_FollowsTheDesign()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default);
            model.MetaTotals(out int researchCost, out int powerCost, out int[] research, out int[] power);

            Assert.AreEqual(0.2f, research[0] / (float)researchCost, 0.03f, "all of Easy pays a fifth of the research upgrades");
            Assert.AreEqual(1f, research[research.Length - 1] / (float)researchCost, 0.05f, "all of Psycho pays every research upgrade");
            Assert.AreEqual(0, power[0], "Easy pays no power");
            Assert.AreEqual(1f, power[power.Length - 1] / (float)powerCost, 0.05f, "all of Psycho pays every drone upgrade");
        }
    }
}
