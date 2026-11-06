using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// Tower parity: what one coin spent on a tower buys, compared across towers. Every buildable gun at every level is
    /// valued against four fixed reference streams (swarm, heavy, air, boss) with the ledger's own per-tower formula
    /// (DPS x overkill x splash x time in reach, see <c>Ledger.Options</c>), divided by the coins it takes to reach that
    /// level. Each case must stay within a band around the median of the towers that can hit that stream at the same level,
    /// and an upgrade may not buy much less per coin than a new build. Known outliers are ignored, not failed, until Remco
    /// has played them; the full table goes to Tools/PlayTest/tower_parity.txt. A model, not the game: no placement,
    /// no slow, no beam lock, no permanent upgrades.
    /// </summary>
    public sealed class TowerParityTests
    {
        private const string TowersFolder = "Assets/_Project/Data/Towers";
        private const string RecipesFolder = "Assets/_Project/Data/Recipes";
        private const string TablePath = "Tools/PlayTest/tower_parity.txt";

        /// <summary>Band around the median: a tower a quarter worse or a third better per coin is a real choice, beyond that it is a trap or a must-buy.</summary>
        private const float BandLow = 0.75f;
        private const float BandHigh = 1.35f;

        /// <summary>An upgrade may buy up to 15% less per coin than a new build: it saves a build spot and a placement.</summary>
        private const float UpgradeFloor = 0.85f;

        /// <summary>Values are shown per hundred coins so the table reads in whole numbers.</summary>
        private const float CoinUnit = 100f;

        /// <summary>
        /// A gun whose hits slow and whose own fire stays below this DPS is a slow mast: its worth is the time it buys every
        /// other tower, which a per-tower damage model cannot see, so it is left out (Cryo, 4 DPS).
        /// </summary>
        private const float ControlTowerMaxDps = 10f;

        private const string KnownPrefix = "Known outlier, awaiting Remco's playtest: ";

        /// <summary>
        /// One fixed stream of creatures a tower is valued against. Health is effective health (health plus armour, as the
        /// ledger counts it: armour soaks a share of each hit until it is gone, so it costs every gun the same points).
        /// Density is creatures per cell of road for the splash rule; window is how long the stream keeps the guns busy.
        /// </summary>
        private readonly struct ReferenceStream
        {
            public readonly string Name;
            public readonly int Layer;
            public readonly float Health;
            public readonly float Speed;
            public readonly float Density;
            public readonly float Window;

            public ReferenceStream(string name, int layer, float health, float speed, float density, float window)
            {
                Name = name;
                Layer = layer;
                Health = health;
                Speed = speed;
                Density = density;
                Window = window;
            }
        }

        private static readonly ReferenceStream[] Streams =
        {
            // Skitters, larvae and prowlers (11 to 62 health) after some wave growth, bunched about a cell apart: splash
            // reaches its target cap here, big shells waste most of each hit.
            new ReferenceStream("swarm", Layer.Ground, 40f, 1.4f, 1.2f, 10f),
            // Bulwarks, shardbacks and crawlers (220 to 380 effective health), slow and spread out: no overkill, little splash.
            new ReferenceStream("heavy", Layer.Ground, 450f, 0.7f, 0.15f, 12f),
            // A flock of bats and skyrays (31 to 80 health) at flying speed, tighter than walkers.
            new ReferenceStream("air", Layer.Air, 45f, 1.4f, 0.8f, 10f),
            // One continent boss (2200 to 3350 health), slow; a short gate hold, then it walks through every tower's reach.
            new ReferenceStream("boss", Layer.Ground, 2500f, 0.5f, 0f, 4f),
        };

        /// <summary>
        /// Cases outside the bands that are known and wait for a playtest before the data changes. Keyed by case name:
        /// "{tower asset} L{level} {stream}" for the median band, plus " upgrade" for the upgrade floor. Pre-filled from a hand
        /// calculation of the 2026-11-06 data (round 13b); the measured ratio is added to the ignore message at run time.
        /// </summary>
        private static readonly Dictionary<string, string> KnownOutliers = new Dictionary<string, string>
        {
            // Cannon (round 21a2): the slower reload (1.6/1.35/1.15 s) that keeps it from beating rate of fire on loose ants leaves it
            // just under the band against a lone boss; a boss is the Railgun's job, splash is the Cannon's.
            // Round 22: upgrade steps are priced by the upgrade-margin rule (BalanceSheet.Prices), which moves a few per-coin ratios to the
            // band's edge; the roles they serve are checked by TowerRolesTests and the yardstick, not by this parity band.
            { "Cannon L2 heavy", KnownPrefix + "the rule-priced Cannon L2 (350) sits at about 0.74x the median on armoured walkers" },
            { "CryoBeam L1 air", KnownPrefix + "the hybrid inherits the rule-priced Cryo and Laser steps, about 0.74x the median on flyers" },
            { "Laser L2 swarm", KnownPrefix + "the rule-priced Laser L2 (285) is about 1.37x the median on swarms: the swarm gun" },
            { "Laser L2 boss", KnownPrefix + "the Laser reaches farther since round 21a3 (4.8 at L2), about 1.38x the median on a lone boss" },
            { "Cannon L0 boss", KnownPrefix + "Cannon reloads slower since round 21a2, about 0.72x the median on a lone boss" },
            { "Cannon L1 boss", KnownPrefix + "Cannon reloads slower since round 21a2, about 0.74x the median on a lone boss" },
            { "Cannon L2 boss", KnownPrefix + "Cannon reloads slower since round 21a2, about 0.74x the median on a lone boss" },
            // Railgun: 80 to 180 damage slugs waste most of each hit on 40-health creatures, and the price climbs faster than the damage.
            { "Railgun L0 swarm", KnownPrefix + "Railgun slugs overkill small creatures, about 0.34x the median" },
            { "Railgun L1 swarm", KnownPrefix + "Railgun slugs overkill small creatures, about 0.23x the median" },
            { "Railgun L2 swarm", KnownPrefix + "Railgun slugs overkill small creatures, about 0.16x the median" },
            { "Railgun L1 swarm upgrade", KnownPrefix + "Railgun upgrades add overkill, not value, against swarms (about 0.68x)" },
            { "Railgun L2 swarm upgrade", KnownPrefix + "Railgun upgrades add overkill, not value, against swarms (about 0.46x)" },
            { "Railgun L2 heavy", KnownPrefix + "Railgun L2 costs 625 for 100 DPS, about 0.70x the median on armoured walkers" },
            { "Railgun L0 air", KnownPrefix + "Railgun slugs overkill bats, about 0.46x the median" },
            { "Railgun L1 air", KnownPrefix + "Railgun slugs overkill bats, about 0.33x the median" },
            { "Railgun L2 air", KnownPrefix + "Railgun slugs overkill bats, about 0.19x the median" },
            { "Railgun L1 air upgrade", KnownPrefix + "Railgun upgrades add overkill, not value, against flyers (about 0.68x)" },
            { "Railgun L2 air upgrade", KnownPrefix + "Railgun upgrades add overkill, not value, against flyers (about 0.46x)" },

            // Tesla: a cheap chain at the splash cap; it out-earns the Cannon against swarms at every level.
            { "Tesla L0 swarm", KnownPrefix + "Tesla splash at the cap on a fast cheap gun, about 1.81x the median (Cannon 1.48x)" },
            { "Tesla L1 swarm", KnownPrefix + "Tesla splash at the cap on a fast cheap gun, about 1.84x the median (Cannon 1.30x)" },
            { "Tesla L2 swarm", KnownPrefix + "Tesla splash at the cap on a fast cheap gun, about 1.94x the median (Cannon 0.91x)" },

            // Cannon: strong first build against swarms, then its bigger shells overkill them.
            { "Cannon L0 swarm", KnownPrefix + "Cannon splash at the cap for 100 coins, about 1.48x the median" },
            { "Cannon L2 swarm upgrade", KnownPrefix + "Cannon L2 85-damage shells overkill swarms, about 0.59x a new build" },

            // Flak (asset Rocket): air-only splash far above the air median, and its upgrades buy less than a new build.
            { "Rocket L0 air", KnownPrefix + "Flak splash at the cap for 120 coins, about 2.44x the air median" },
            { "Rocket L1 air", KnownPrefix + "Flak is far above the air median, about 2.09x" },
            { "Rocket L2 air", KnownPrefix + "Flak is far above the air median, about 1.80x" },
            { "Rocket L1 air upgrade", KnownPrefix + "Flak upgrade buys about 0.82x what a new Flak buys" },
            { "Rocket L2 air upgrade", KnownPrefix + "Flak upgrade buys about 0.82x what a new Flak buys" },

            // Laser: a cheap beam with high single-target DPS.
            { "Laser L0 boss", KnownPrefix + "Laser beam DPS for 130 coins, about 1.38x the boss median" },
            { "Laser L1 boss", KnownPrefix + "Laser beam DPS for 220 coins, about 1.35x the boss median (on the edge)" },

            // Hybrids, priced as both partners plus the recipe (combining at the cheapest level).
            { "Artillery L0 swarm", KnownPrefix + "Artillery 70-damage shells overkill swarms at a hybrid price, about 0.62x" },
            { "Artillery L1 swarm", KnownPrefix + "Artillery 130-damage shells overkill swarms at a hybrid price, about 0.46x" },
            { "Artillery L2 swarm", KnownPrefix + "Artillery 220-damage shells overkill swarms at a hybrid price, about 0.37x" },
            { "Artillery L1 swarm upgrade", KnownPrefix + "Artillery upgrades add overkill against swarms, about 0.74x a new build" },
            { "Artillery L2 swarm upgrade", KnownPrefix + "Artillery upgrades add overkill against swarms, about 0.58x a new build" },
            { "Artillery L1 air", KnownPrefix + "Artillery shells overkill bats, about 0.65x the air median" },
            { "Artillery L2 air", KnownPrefix + "Artillery shells overkill bats, about 0.44x the air median" },
            { "Artillery L1 air upgrade", KnownPrefix + "Artillery upgrades add overkill against flyers, about 0.74x a new build" },
            { "Artillery L2 air upgrade", KnownPrefix + "Artillery upgrades add overkill against flyers, about 0.58x a new build" },
            { "Artillery L0 boss", KnownPrefix + "Artillery L0 costs 370 for 44 DPS, about 0.66x the boss median" },
            { "StormRifle L0 swarm", KnownPrefix + "Storm Rifle splash at the cap on 70 DPS, about 1.89x the median" },
            { "StormRifle L1 swarm", KnownPrefix + "Storm Rifle splash at the cap on 111 DPS, about 1.99x the median" },
            { "StormRifle L2 swarm", KnownPrefix + "Storm Rifle splash at the cap on 187 DPS, about 2.24x the median" },
            { "StormRifle L0 air", KnownPrefix + "Storm Rifle splash against flocks, about 2.19x the air median" },
            { "StormRifle L1 air", KnownPrefix + "Storm Rifle splash against flocks, about 2.37x the air median" },
            { "StormRifle L2 air", KnownPrefix + "Storm Rifle splash against flocks, about 2.24x the air median" },
            { "CryoBeam L0 swarm", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.53x" },
            { "CryoBeam L1 swarm", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.66x" },
            { "CryoBeam L0 heavy", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.60x" },
            { "CryoBeam L1 heavy", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.69x" },
            { "CryoBeam L2 heavy", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.75x (on the edge)" },
            { "CryoBeam L0 air", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.64x" },
            { "CryoBeam L0 boss", KnownPrefix + "Cryo Beam pays a hybrid price for its slow, which this model does not value, about 0.72x" },
        };

        private static ParityTable _table;

        private static ParityTable Table => _table ?? (_table = ParityTable.Build());

        private static IEnumerable<TestCaseData> BandCases() => CasesOf(Table.BandCases);

        private static IEnumerable<TestCaseData> UpgradeCases() => CasesOf(Table.UpgradeCases);

        [Test]
        public void ParityTowers_AreFound()
        {
            Assert.IsNotEmpty(Table.Rows, $"No buildable gun found in {TowersFolder}; run Tools/Generate/Scenes and Starter Data first.");
        }

        [TestCaseSource(nameof(BandCases))]
        public void ValuePerCoin_StaysNearTheMedianOfItsLayer(string caseName)
        {
            Check(Table.BandCases[caseName], caseName);
        }

        [TestCaseSource(nameof(UpgradeCases))]
        public void Upgrade_BuysNearlyAsMuchPerCoinAsANewBuild(string caseName)
        {
            Check(Table.UpgradeCases[caseName], caseName);
        }

        [Test]
        public void KnownOutliers_NameRealCases()
        {
            var stale = new List<string>();
            foreach (string key in KnownOutliers.Keys)
            {
                if (!Table.BandCases.ContainsKey(key) && !Table.UpgradeCases.ContainsKey(key))
                {
                    stale.Add(key);
                }
            }

            Assert.IsEmpty(stale, "Known outliers that name no case (a typo or a removed tower): " + string.Join(", ", stale));
        }

        [Test]
        public void ParityTable_IsWritten()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TablePath) ?? ".");
            File.WriteAllText(TablePath, Table.Report());
            Assert.IsTrue(File.Exists(TablePath));
        }

        private static IEnumerable<TestCaseData> CasesOf(SortedDictionary<string, ParityCase> cases)
        {
            foreach (string name in cases.Keys)
            {
                yield return new TestCaseData(name).SetName(name);
            }
        }

        private static void Check(ParityCase parity, string caseName)
        {
            if (!parity.Inside && KnownOutliers.TryGetValue(caseName, out string reason))
            {
                Assert.Ignore($"{reason}; measured {parity.Detail}");
            }

            Assert.That(parity.Ratio, Is.InRange(parity.Low, parity.High), $"{caseName}: {parity.Detail}");
        }

        /// <summary>One check: a ratio, the band it must stay in, and how it was measured.</summary>
        private sealed class ParityCase
        {
            public float Ratio;
            public float Low;
            public float High;
            public string Detail = string.Empty;

            public bool Inside => Ratio >= Low && Ratio <= High;

            public string Flag(string name) => Inside ? "OK" : KnownOutliers.ContainsKey(name) ? "KNOWN" : "OUTLIER";
        }

        /// <summary>One tower at one level, valued against every stream. A stream the tower cannot hit holds NaN.</summary>
        private sealed class ParityRow
        {
            public string Tower = string.Empty;
            public int Level;
            public int Cost;
            public float Dps;
            public float Splash;
            public int Targets;
            public float Range;
            public float[] Value = new float[Streams.Length];
            public float[] Ratio = new float[Streams.Length];
            public float[] Upgrade = new float[Streams.Length];

            public string CaseName(int stream) => $"{Tower} L{Level} {Streams[stream].Name}";

            public string UpgradeName(int stream) => CaseName(stream) + " upgrade";
        }

        /// <summary>
        /// The per-tower half of the ledger (<c>Ledger.Options</c>), which is private there: DPS, overkill on small creatures,
        /// splash by stream density up to the rules' cap, and time in reach (the stream's window plus the walk through the
        /// tower's diameter). Divided by the window it reads as the damage per second the tower adds over the stream. The
        /// ledger's utilisation factor is left out: it scales every tower alike and cancels in every ratio.
        /// </summary>
        private static class ParityModel
        {
            public static float EffectiveDps(TowerLevelStats level, float splashRadius, ReferenceStream stream, BalanceRules rules)
            {
                float inReach = stream.Window + 2f * level.Range / stream.Speed;
                return level.Dps * Overkill(level.Damage, stream.Health) * Splash(splashRadius, stream.Density, rules) * inReach / stream.Window;
            }

            // Same as Ledger.Overkill: a hit bigger than the creature is partly wasted.
            private static float Overkill(float damage, float health) => health <= 0f || damage <= health ? 1f : health / damage;

            // Same as Ledger.Splash: one plus the creatures inside the blast's diameter, capped.
            private static float Splash(float radius, float perUnit, BalanceRules rules) =>
                radius <= 0f ? 1f : Math.Min(rules.MaxSplashTargets, 1f + 2f * radius * perUnit);
        }

        /// <summary>Every row, every case and the skipped towers, built once from the stored data.</summary>
        private sealed class ParityTable
        {
            public readonly List<ParityRow> Rows = new List<ParityRow>();
            public readonly List<string> Skipped = new List<string>();
            public readonly SortedDictionary<string, ParityCase> BandCases = new SortedDictionary<string, ParityCase>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, ParityCase> UpgradeCases = new SortedDictionary<string, ParityCase>(StringComparer.Ordinal);
            private readonly Dictionary<TowerDefinition, CombineRecipe> _recipes = new Dictionary<TowerDefinition, CombineRecipe>();

            public static ParityTable Build()
            {
                var table = new ParityTable();
                table.LoadRecipes();
                foreach (TowerDefinition tower in LoadAll<TowerDefinition>(TowersFolder))
                {
                    table.AddTower(tower);
                }

                table.Rows.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Tower, b.Tower));
                table.Compare();
                return table;
            }

            private void LoadRecipes()
            {
                foreach (CombineRecipe recipe in LoadAll<CombineRecipe>(RecipesFolder))
                {
                    if (recipe.Result != null && recipe.First != null && recipe.Second != null)
                    {
                        _recipes[recipe.Result] = recipe;
                    }
                }
            }

            private void AddTower(TowerDefinition tower)
            {
                TowerStats stats = BalanceLedger.Stats(tower);
                string skip = SkipReason(tower, stats);
                if (skip != null)
                {
                    Skipped.Add($"{tower.Id}: {skip}");
                    return;
                }

                BalanceRules rules = BalanceRules.Default;
                for (int level = 0; level < stats.Levels.Length; level++)
                {
                    TowerLevelStats levelStats = stats.Levels[level];
                    var row = new ParityRow
                    {
                        Tower = tower.Id,
                        Level = level,
                        Cost = CostToReach(tower, level, 0),
                        Dps = levelStats.Dps,
                        Splash = stats.Splash,
                        Targets = stats.Targets,
                        Range = levelStats.Range,
                    };
                    for (int s = 0; s < Streams.Length; s++)
                    {
                        row.Value[s] = Layer.Has(stats.Targets, Streams[s].Layer) && row.Cost > 0
                            ? ParityModel.EffectiveDps(levelStats, stats.Splash, Streams[s], rules) / row.Cost * CoinUnit
                            : float.NaN;
                    }

                    Rows.Add(row);
                }
            }

            private string SkipReason(TowerDefinition tower, TowerStats stats)
            {
                if (stats.BlocksGround)
                {
                    return "gate (blocks ground creatures, set by the level)";
                }

                if (stats.IsBaseDefence)
                {
                    return "base defence (bought with permanent upgrades, not coins)";
                }

                if (stats.IsSupport)
                {
                    return "support mast (no damage of its own)";
                }

                if (!stats.IsGun)
                {
                    return "no levels";
                }

                float slow = tower.GetLevel(0).SlowFactor > 0f ? tower.GetLevel(0).SlowFactor : tower.SlowFactor;
                if (slow < 1f && stats.Levels[0].Dps < ControlTowerMaxDps)
                {
                    return Invariant($"slow mast ({stats.Levels[0].Dps:0.0} DPS): its worth is the time it buys the other towers");
                }

                if (tower.BuildCost <= 0 && !_recipes.ContainsKey(tower))
                {
                    return "free first level and no recipe: not buildable";
                }

                return null;
            }

            // Coins to own a tower at a level: the sum of its level costs, or for a hybrid both partners at the combine level,
            // the recipe and the hybrid's own steps after it, combining at whichever level is cheapest (a hybrid keeps the
            // lower partner level, see PlacementController.OnCombine).
            private int CostToReach(TowerDefinition tower, int level, int depth)
            {
                if (!_recipes.TryGetValue(tower, out CombineRecipe recipe) || depth > _recipes.Count)
                {
                    return StepCost(tower, 0, level);
                }

                int best = int.MaxValue;
                for (int combineLevel = 0; combineLevel <= level; combineLevel++)
                {
                    int cost = CostToReach(recipe.First, combineLevel, depth + 1) + CostToReach(recipe.Second, combineLevel, depth + 1) + recipe.Cost + StepCost(tower, combineLevel + 1, level);
                    best = Math.Min(best, cost);
                }

                return best;
            }

            private static int StepCost(TowerDefinition tower, int from, int to)
            {
                int sum = 0;
                for (int i = from; i <= to; i++)
                {
                    sum += tower.GetLevel(i).Cost;
                }

                return sum;
            }

            private void Compare()
            {
                for (int s = 0; s < Streams.Length; s++)
                {
                    foreach (ParityRow row in Rows)
                    {
                        if (float.IsNaN(row.Value[s]))
                        {
                            row.Ratio[s] = float.NaN;
                            row.Upgrade[s] = float.NaN;
                            continue;
                        }

                        float median = MedianAt(s, row.Level);
                        row.Ratio[s] = row.Value[s] / median;
                        BandCases[row.CaseName(s)] = new ParityCase
                        {
                            Ratio = row.Ratio[s],
                            Low = BandLow,
                            High = BandHigh,
                            Detail = Invariant($"{row.Ratio[s]:0.00}x the median ({row.Value[s]:0.0} vs {median:0.0} per {CoinUnit:0} coins, cost {row.Cost})"),
                        };

                        ParityRow build = Find(row.Tower, 0);
                        row.Upgrade[s] = row.Value[s] / build.Value[s];
                        if (row.Level > 0)
                        {
                            UpgradeCases[row.UpgradeName(s)] = new ParityCase
                            {
                                Ratio = row.Upgrade[s],
                                Low = UpgradeFloor,
                                High = float.MaxValue,
                                Detail = Invariant($"{row.Upgrade[s]:0.00}x a new build ({row.Value[s]:0.0} vs {build.Value[s]:0.0} per {CoinUnit:0} coins)"),
                            };
                        }
                    }
                }
            }

            private float MedianAt(int stream, int level)
            {
                var values = new List<float>();
                foreach (ParityRow row in Rows)
                {
                    if (row.Level == level && !float.IsNaN(row.Value[stream]))
                    {
                        values.Add(row.Value[stream]);
                    }
                }

                values.Sort();
                int middle = values.Count / 2;
                return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2f;
            }

            private ParityRow Find(string tower, int level) => Rows.Find(r => r.Tower == tower && r.Level == level);

            public string Report()
            {
                var text = new StringBuilder();
                text.AppendLine("Tower parity (TowerParityTests): value per coin of every buildable gun and level against four reference streams.");
                text.AppendLine("value = DPS x overkill x splash x (window + 2 x range / speed) / window, per 100 coins to reach the level");
                text.AppendLine("(the ledger's per-tower formula; hybrids cost both partners + recipe, combined at the cheapest level).");
                text.AppendLine(Invariant($"ratio = value / median of the towers that hit that layer at the same level; band {BandLow:0.00} to {BandHigh:0.00}."));
                text.AppendLine(Invariant($"upg = value / value of a new build (L0) of the same tower; floor {UpgradeFloor:0.00}. Not modelled: slow, beam lock, placement, permanent upgrades."));
                text.AppendLine("Streams (health incl. armour, speed, creatures per cell, window s):");
                foreach (ReferenceStream stream in Streams)
                {
                    text.AppendLine(Invariant($"  {stream.Name,-6} {(stream.Layer == Layer.Air ? "air" : "ground"),-6} hp {stream.Health,5:0}  speed {stream.Speed:0.00}  density {stream.Density:0.00}  window {stream.Window:0}"));
                }

                text.AppendLine("Skipped: " + (Skipped.Count == 0 ? "none" : string.Join("; ", Skipped)));
                text.AppendLine();
                text.Append($"{"tower",-12}{"lvl",-4}{"cost",5}{"DPS",7}{"splash",7} {"lay",-4}{"range",6}");
                foreach (ReferenceStream stream in Streams)
                {
                    text.Append($" | {stream.Name + " val",10}{"ratio",6}{"upg",6}");
                }

                text.AppendLine("  flag");
                foreach (ParityRow row in Rows)
                {
                    AppendRow(text, row);
                }

                AppendStale(text);
                return text.ToString();
            }

            private void AppendRow(StringBuilder text, ParityRow row)
            {
                string layers = (Layer.Has(row.Targets, Layer.Ground) ? "G" : string.Empty) + (Layer.Has(row.Targets, Layer.Air) ? "A" : string.Empty);
                text.Append(Invariant($"{row.Tower,-12}{"L" + row.Level,-4}{row.Cost,5}{row.Dps,7:0.0}{row.Splash,7:0.0} {layers,-4}{row.Range,6:0.0}"));
                string flag = "OK";
                for (int s = 0; s < Streams.Length; s++)
                {
                    if (float.IsNaN(row.Value[s]))
                    {
                        text.Append($" | {"-",10}{"-",6}{"-",6}");
                        continue;
                    }

                    text.Append(Invariant($" | {row.Value[s],10:0.0}{row.Ratio[s],6:0.00}{row.Upgrade[s],6:0.00}"));
                    flag = Worse(flag, BandCases[row.CaseName(s)].Flag(row.CaseName(s)));
                    if (UpgradeCases.TryGetValue(row.UpgradeName(s), out ParityCase upgrade))
                    {
                        flag = Worse(flag, upgrade.Flag(row.UpgradeName(s)));
                    }
                }

                text.AppendLine("  " + flag);
            }

            // Known outliers that now sit inside their band: the list can lose them.
            private void AppendStale(StringBuilder text)
            {
                var back = new List<string>();
                foreach (string key in KnownOutliers.Keys)
                {
                    if ((BandCases.TryGetValue(key, out ParityCase parity) || UpgradeCases.TryGetValue(key, out parity)) && parity.Inside)
                    {
                        back.Add(key);
                    }
                }

                text.AppendLine();
                text.AppendLine("Known outliers back inside their band: " + (back.Count == 0 ? "none" : string.Join(", ", back)));
            }

            private static string Worse(string current, string next) =>
                current == "OUTLIER" || next == "OUTLIER" ? "OUTLIER" : current == "KNOWN" || next == "KNOWN" ? "KNOWN" : "OK";

            private static IEnumerable<T> LoadAll<T>(string folder) where T : UnityEngine.Object
            {
                foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset != null)
                    {
                        yield return asset;
                    }
                }
            }
        }
    }
}
