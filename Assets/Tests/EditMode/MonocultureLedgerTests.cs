using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Economy;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// Monoculture pre-screen (round 20, Remco: "Als je kunt winnen door alleen een rijtje torens neer te zetten van
    /// hetzelfde soort, dan is het niet in balans"): the ledger in the human lens with the hero, once with the whole
    /// loadout the player owns at each level and once per owned gun with only that gun to buy. A gun alone that passes the
    /// same bar the full loadout must pass (every wave inside the hero cap) would let a row of one type win. Report only:
    /// the table goes to Tools/PlayTest/monoculture_ledger.txt and the rule is printed beside it, not asserted, until Remco
    /// rebalances by role. The PlayMode probe (MonocultureProbeTests) plays the same question for real.
    /// </summary>
    public sealed class MonocultureLedgerTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string TablePath = "Tools/PlayTest/monoculture_ledger.txt";
        private const int Medium = 1;

        [Test]
        public void Monoculture_LedgerPreScreen_IsReported()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
            BalanceRules judge = rules.HumanWithHero();

            var report = new StringBuilder();
            report.Append("Monoculture pre-screen (ledger, human lens with the hero). Per level and difficulty: the full owned loadout, then each\n");
            report.Append("owned gun alone. surplus = 1 / mean pressure of the plain waves; worst = highest wave pressure (boss waves included);\n");
            report.Append("holds = every wave inside the hero cap (the bar the full loadout must pass). Rule (report only): Medium at most one gun\n");
            report.Append("alone holds, Hard and Psycho none. Air-only guns cannot hold a ground wave and are listed for completeness.\n\n");
            var breaches = new List<string>();
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
                    List<WaveSpec> waves = Waves(model, node.Level, difficulty);
                    LevelInput full = model.InputFor(index, d);
                    float cap = rules.HeroCapFor(d) * (1f + rules.BeatableTolerance);
                    report.Append(Invariant($"{node.Level.name} {difficulty.name} (hero cap {cap:0.00})\n"));
                    report.Append(Line("all owned", Ledger.Evaluate(full, waves, model.Difficulties[d], judge), cap, out _));
                    int holding = 0;
                    var holders = new List<string>();
                    foreach (TowerStats tower in full.Towers)
                    {
                        if (!tower.IsGun)
                        {
                            continue;
                        }

                        LevelInput alone = Only(full, tower);
                        report.Append(Line(tower.Id + " only", Ledger.Evaluate(alone, waves, model.Difficulties[d], judge), cap, out bool holds));
                        if (holds)
                        {
                            holding++;
                            holders.Add(tower.Id);
                        }
                    }

                    int allowed = d < Medium ? int.MaxValue : d == Medium ? 1 : 0;
                    if (holding > allowed)
                    {
                        breaches.Add($"{node.Level.name} {difficulty.name}: {holding} guns hold alone ({string.Join(", ", holders)}), allowed {allowed}");
                    }
                }

                report.Append('\n');
                index++;
            }

            report.Append("Rule breaches (known, awaiting Remco's rebalance by role):\n");
            foreach (string breach in breaches)
            {
                report.Append("  ").Append(breach).Append('\n');
            }

            if (breaches.Count == 0)
            {
                report.Append("  none\n");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TablePath));
            File.WriteAllText(TablePath, report.ToString());
            Debug.Log($"[Monoculture] ledger pre-screen: {breaches.Count} rule breaches, see {TablePath}");
        }

        private static List<WaveSpec> Waves(CampaignModel model, LevelDefinition level, DifficultyProfile difficulty)
        {
            var waves = new List<WaveSpec>();
            foreach (WaveDefinition wave in level.GetWaves(difficulty))
            {
                waves.Add(BalanceLedger.Spec(wave, model.Enemies));
            }

            return waves;
        }

        // The same level input with one gun for sale (gates, masts and the base turret kept: they are not bought by the ledger).
        private static LevelInput Only(LevelInput full, TowerStats gun)
        {
            var alone = (LevelInput)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(full, null);
            alone.Towers = full.Towers.FindAll(t => t == gun || !t.IsGun);
            return alone;
        }

        private static string Line(string name, List<WaveLine> lines, float cap, out bool holds)
        {
            float worst = 0f;
            int over = 0;
            foreach (WaveLine line in lines)
            {
                worst = Mathf.Max(worst, line.Pressure);
                over += line.Pressure > cap ? 1 : 0;
            }

            float mean = BalanceChecks.PlainMean(lines);
            holds = over == 0 && lines.Count > 0;
            string surplus = mean > 0f && !float.IsInfinity(mean) ? Invariant($"{1f / mean,5:0.00}x") : "  0.00x";
            string worstText = float.IsInfinity(worst) ? "  inf" : Invariant($"{worst,5:0.00}");
            return Invariant($"  {name,-18} surplus {surplus} worst {worstText} waves over cap {over,2}/{lines.Count,-2} {(holds ? "HOLDS" : "falls")}\n");
        }
    }
}
