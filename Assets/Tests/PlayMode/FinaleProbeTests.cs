using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Balance;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// The continent finales as they will be played (round 22f; Docs/BALANCE.md "The finale probe"): every continent boss on its
    /// arena field, per difficulty, against the finale player of <see cref="FinaleModel"/> (level 20 of the continent: the planned
    /// towers on the bar, that difficulty path's research spent, the finale's money) played by the creative reference bot with the
    /// arena waves at a boss health multiple that the test bisects to the largest the bot still beats. Writes
    /// Tools/PlayTest/finale_probe.txt, from which ContinentFinaleTests derives the boss health and the boss health share per
    /// difficulty and the apply stage writes them. Narrow it with probe_arg.txt: bosses=0,1 (index in FinalePlan.Finales),
    /// difficulties=1, seed=1, step=0.2.
    /// </summary>
    public sealed class FinaleProbeTests
    {
        private const string TowersFolder = "Assets/_Project/Data/Towers/";
        private const string EnemiesFolder = "Assets/_Project/Data/Enemies/";
        private const string AvailableTowersField = "_availableTowers";
        private const int FinaleSeed = 1;

        // The bisection on the boss's health multiple: from the data (1) outward by a step until the verdict flips, then halved
        // between the last win and the first loss; the bounds keep a boss the bot never beats or never loses to from running on.
        private const float ExpandStep = 1.4f;
        private const float MinMultiple = 0.25f;
        private const float MaxMultiple = 16f;
        private const int Refinements = 3;

        [TearDown]
        public void ResetOverride()
        {
            BalanceProbeTests.Override = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Category("Balance")]
        [Explicit("Every continent boss on every difficulty, about seven runs each: an hour or more. Narrow it with Tools/PlayTest/probe_arg.txt.")]
        [Timeout(36000000)]
        public IEnumerator Measure_ContinentFinales_AsPlayed()
        {
#if UNITY_EDITOR
            CampaignCatalog catalog = BalancePipelineTests.Catalog();
            Assert.IsNotNull(catalog, "run the generator first");
            var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>(BalanceProbeTests.UpgradesPath);
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(BalanceProbeTests.UnlocksPath);
            var calibration = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
            CampaignModel real = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default, calibration);
            var plannedStats = new Dictionary<string, TowerStats>();
            var plannedDefinitions = new Dictionary<string, TowerDefinition>();
            foreach (PlannedTower tower in FinalePlan.PlannedTowers)
            {
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerDefinition>(TowersFolder + tower.Id + ".asset");
                Assert.IsNotNull(definition, $"planned tower {tower.Id} has no data");
                plannedDefinitions[tower.Id] = definition;
                plannedStats[tower.Id] = BalanceLedger.Stats(definition);
            }

            CampaignModel planned = FinaleModel.Planned(real, FinalePlan.PlannedTowers, plannedStats, FinalePlan.CrystalPlainsResearchBudget);
            int finaleIndex = planned.Levels.Count - 1;
            int standInNode = StandInNode(catalog);
            LevelDefinition standIn = catalog.Nodes[standInNode].Level;
            BossArenaDefinition arena = catalog.BossArena;
            Assert.IsNotNull(arena, "the catalog has no boss arena");

            var cells = new List<FinaleProbeCell>();
            var table = new StringBuilder();
            var results = new List<BalanceProbeTests.ProbeResult>();
            for (int b = 0; b < FinalePlan.Finales.Length; b++)
            {
                ContinentFinale finale = FinalePlan.Finales[b];
                if (!BalanceProbeTests.Wanted("bosses", b))
                {
                    continue;
                }

                var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemiesFolder + finale.Boss + ".asset");
                Assert.IsNotNull(boss, $"no enemy asset for {finale.Boss}");
                var extraTowers = new List<TowerStats>();
                var extraDefinitions = new List<TowerDefinition>();
                foreach (PlannedTower tower in FinalePlan.PlannedTowers)
                {
                    if (tower.Continent <= finale.Continent)
                    {
                        extraDefinitions.Add(plannedDefinitions[tower.Id]);
                        if (tower.Continent > 0)
                        {
                            extraTowers.Add(plannedStats[tower.Id]);
                        }
                    }
                }

                LevelDefinition field = FieldWithTowers(arena.LevelFor(boss), extraDefinitions, finale.Boss);
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    if (!BalanceProbeTests.Wanted("difficulties", d))
                    {
                        continue;
                    }

                    List<WaveSpec> standInWaves = SpecsWithBoss(standIn, catalog.Difficulties[d], real, BalanceLedger.Stats(boss), out int bossWave);
                    FinaleLine line = FinaleModel.Judge(planned, standInWaves, bossWave, d, extraTowers);
                    PlayerState player = planned.PlayerAt(finaleIndex, d);
                    LevelInput input = planned.InputFor(finaleIndex, d);
                    input.Geometry = BalanceLedger.Geometry(field);
                    foreach (TowerStats tower in extraTowers)
                    {
                        if (!input.Towers.Exists(t => t.Id == tower.Id))
                        {
                            input.Towers.Add(tower);
                            input.Boosts[tower.Id] = TowerBoost.None;
                        }
                    }

                    var owned = new List<string>(player.OwnedTowers);
                    foreach (TowerStats tower in extraTowers)
                    {
                        if (!owned.Contains(tower.Id))
                        {
                            owned.Add(tower.Id);
                        }
                    }

                    var cell = new FinaleProbeCell
                    {
                        Boss = finale.Boss,
                        Difficulty = d,
                        ShareNow = catalog.Difficulties[d].BossHealthShare,
                        HealthNow = boss.MaxHealth,
                    };

                    // The bisection: one run per multiple; the overrides give the probe the arena field, its waves at that multiple,
                    // the finale player, their towers and the finale's purse, and a ledger view of those waves for the bot.
                    float won = -1f;
                    float lost = -1f;
                    float multiple = 1f;
                    var verdicts = new List<string>();
                    while (true)
                    {
                        WaveDefinition[] waves = Scaled(arena.WavesFor(boss), multiple);
                        BalanceProbeTests.Override = new BalanceProbeTests.ProbeOverride
                        {
                            Level = field,
                            Waves = waves,
                            StartingMoney = line.Money,
                            Player = player,
                            Owned = owned,
                            Ledger = BalanceProbeTests.LedgerFor(input, Specs(waves, real), planned.Difficulties[d]),
                        };
                        int before = results.Count;
                        yield return BalanceProbeTests.Probe(new List<(int, int)> { (standInNode, d) }, ProbeStrategy.CreativeBot(), results, Seed());
                        BalanceProbeTests.Override = null;
                        Assert.AreEqual(before + 1, results.Count, "the probe recorded no run");
                        BalanceProbeTests.ProbeResult result = results[results.Count - 1];
                        bool beaten = result.Outcome == LevelOutcome.Won;
                        cell.Runs++;
                        verdicts.Add(Invariant($"x{multiple:0.00} {(beaten ? "won" : "lost")} base {result.BaseHealth:0}/{result.BaseMax:0}"));
                        if (Math.Abs(multiple - 1f) < 0.001f)
                        {
                            cell.WonAsIs = beaten;
                            cell.BaseAsIs = result.BaseMax > 0f ? result.BaseHealth / result.BaseMax : 0f;
                        }

                        if (beaten)
                        {
                            won = multiple;
                        }
                        else
                        {
                            lost = multiple;
                        }

                        float next = Next(won, lost, cell.Runs);
                        if (next < 0f)
                        {
                            break;
                        }

                        multiple = next;
                    }

                    cell.Holds = won > 0f ? won : MinMultiple;
                    cells.Add(cell);
                    table.Append(Invariant($"  {finale.Boss,-16} {catalog.Difficulties[d].name,-7} money {line.Money,5} research {player.ResearchEarned,4} towers {owned.Count}: holds x{cell.Holds:0.00} ({string.Join(", ", verdicts)})\n"));
                }
            }

            Assert.IsNotEmpty(cells, "no finale cell played: check probe_arg.txt");
            string header = Invariant($"Finale probe as played {DateTime.Now:yyyy-MM-dd HH:mm} (round 22f): creative bot, seed {Seed()}, the finale player of FinaleModel on the boss's arena field. {BalancePipelineTests.CalibrationNote()}");
            System.IO.File.WriteAllText(FinaleProbeReport.Path, FinaleProbeReport.Write(header, cells) + "\n" + table);
            Debug.Log("[Balance] finale probe: " + cells.Count + " cells, see " + FinaleProbeReport.Path + "\n" + table);
#else
            yield break;
#endif
        }

        // The next multiple to try: expand from the data until both a win and a loss are known, then halve between them a few times.
        private static float Next(float won, float lost, int runs)
        {
            if (won < 0f && lost < 0f)
            {
                return 1f;
            }

            if (lost < 0f)
            {
                return won * ExpandStep <= MaxMultiple ? won * ExpandStep : -1f;
            }

            if (won < 0f)
            {
                return lost / ExpandStep >= MinMultiple ? lost / ExpandStep : -1f;
            }

            int refinements = runs - RunsToBracket(won, lost);
            return refinements < Refinements ? (float)Math.Sqrt(won * lost) : -1f;
        }

        // How many runs the expansion took to bracket the edge (geometric steps from 1 to the farther bound).
        private static int RunsToBracket(float won, float lost)
        {
            float far = Math.Max(won, lost) >= 1f ? Math.Max(won, lost) : Math.Min(won, lost);
            int steps = (int)Math.Round(Math.Abs(Math.Log(far) / Math.Log(ExpandStep)));
            return steps + 1;
        }

        private static int Seed() => (int)Setting("seed", FinaleSeed);

        private static float Setting(string key, float fallback)
        {
            const string filter = "Tools/PlayTest/probe_arg.txt";
            if (!System.IO.File.Exists(filter))
            {
                return fallback;
            }

            foreach (string line in System.IO.File.ReadAllLines(filter))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static int StandInNode(CampaignCatalog catalog)
        {
            int node = -1;
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                node = catalog.Nodes[i].Level != null ? i : node;
            }

            return node;
        }

        // The arena waves with every wave's health multiple replaced by the trial's (the arena's own showcase multiple is not the finale's).
        private static WaveDefinition[] Scaled(WaveDefinition[] waves, float multiple)
        {
            var scaled = new WaveDefinition[waves.Length];
            for (int i = 0; i < waves.Length; i++)
            {
                scaled[i] = new WaveDefinition(waves[i].Groups, waves[i].SpawnJitter, waves[i].StartDelay, multiple);
            }

            return scaled;
        }

        private static List<WaveSpec> Specs(WaveDefinition[] waves, CampaignModel model)
        {
            var specs = new List<WaveSpec>(waves.Length);
            foreach (WaveDefinition wave in waves)
            {
                specs.Add(BalanceLedger.Spec(wave, model.Enemies));
            }

            return specs;
        }

        // The stand-in level's waves on a difficulty, its last boss wave carrying the continent boss (as ContinentFinaleTests does on paper).
        private static List<WaveSpec> SpecsWithBoss(LevelDefinition standIn, DifficultyProfile profile, CampaignModel model, EnemyStats boss, out int finale)
        {
            var waves = new List<WaveSpec>();
            finale = -1;
            foreach (WaveDefinition wave in standIn.GetWaves(profile))
            {
                WaveSpec spec = BalanceLedger.Spec(wave, model.Enemies);
                waves.Add(spec);
                if (spec.Groups.Exists(g => g.Enemy.IsBoss))
                {
                    finale = waves.Count - 1;
                }
            }

            Assert.GreaterOrEqual(finale, 0, $"{standIn.name} has no boss wave");
            List<GroupSpec> groups = waves[finale].Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Enemy.IsBoss)
                {
                    GroupSpec group = groups[i];
                    group.Enemy = boss;
                    group.Count = 1;
                    groups[i] = group;
                }
            }

            return waves;
        }

        // A runtime copy of the arena field whose bar also sells the planned towers; nothing is written to the asset.
        private static LevelDefinition FieldWithTowers(LevelDefinition level, List<TowerDefinition> extra, string boss)
        {
            LevelDefinition copy = UnityEngine.Object.Instantiate(level);
            copy.name = level.name + "_" + boss;
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(copy);
            UnityEditor.SerializedProperty towers = serialized.FindProperty(AvailableTowersField);
            foreach (TowerDefinition tower in extra)
            {
                if (Array.IndexOf(copy.AvailableTowers, tower) >= 0)
                {
                    continue;
                }

                towers.InsertArrayElementAtIndex(towers.arraySize);
                towers.GetArrayElementAtIndex(towers.arraySize - 1).objectReferenceValue = tower;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            return copy;
        }
    }
}
