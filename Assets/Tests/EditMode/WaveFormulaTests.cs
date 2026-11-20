using System;
using System.Collections.Generic;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 87: a wave's health is a formula (difficulty x level scalar x ramp x kind x peak), a level's boss a formula
    /// of its own (level index, difficulty, how late it comes) with the bar of the toughest walker before it as a floor only, and a
    /// level has two or three peak waves before its boss. Pure tests of <see cref="WaveFormula"/>, and the same held on
    /// every stored wave and boss.
    /// </summary>
    public sealed class WaveFormulaTests
    {
        /// <summary>The stored multipliers are rounded to three decimals, the boss scale too: one per cent covers both.</summary>
        private const float Slack = 0.01f;

        private const int FewestPeaks = 2;
        private const int MostPeaks = 3;
        private const int LevelsCompared = 6;

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        // ---- The formula, pure -----------------------------------------------------------------------------------

        [Test]
        public void AWave_WalksInOnItsFactors()
        {
            var rules = new BalanceRules
            {
                DifficultyScale = new[] { 2f, 3f },
                RampStart = 0.5f,
                RampEnd = 2f,
                RushFactor = 1.1f,
                HeavyFactor = 1.2f,
                AirFactor = 0.9f,
                AfterBossFactor = 0.8f,
                BossCrowdFactor = 0.7f,
                PeakFactor = 1.5f,
            };
            // The middle of a geometric ramp from 0.5 to 2 is one.
            Assert.AreEqual(3f * 1.25f, WaveFormula.WalkIn(rules, 1, 1.25f, 0.5f, WaveComposer.Kind.Plain, false, false, false), 0.0001f, "difficulty x level scalar");
            Assert.AreEqual(2f * 0.5f * 1.1f, WaveFormula.WalkIn(rules, 0, 1f, 0f, WaveComposer.Kind.Rush, false, false, false), 0.0001f, "the first wave of a rush");
            Assert.AreEqual(2f * 2f * 1.2f * 1.5f, WaveFormula.WalkIn(rules, 0, 1f, 1f, WaveComposer.Kind.Heavy, false, false, true), 0.0001f, "a heavy peak in the last wave");
            Assert.AreEqual(2f * 0.9f, WaveFormula.WalkIn(rules, 0, 1f, 0.5f, WaveComposer.Kind.Air, false, false, false), 0.0001f, "a raid");
            Assert.AreEqual(2f * 0.7f, WaveFormula.WalkIn(rules, 0, 1f, 0.5f, WaveComposer.Kind.Plain, true, false, false), 0.0001f, "the crowd of a boss wave");
            Assert.AreEqual(2f * 0.8f, WaveFormula.WalkIn(rules, 0, 1f, 0.5f, WaveComposer.Kind.Plain, false, true, false), 0.0001f, "the breather after a boss");
        }

        [Test]
        public void TheRamp_RisesWithEveryWave_FromItsStartToItsEnd()
        {
            BalanceRules rules = BalanceRules.Default;
            Assert.Less(rules.RampStart, rules.RampEnd, "a level gets tougher");
            Assert.AreEqual(rules.RampStart, WaveFormula.Ramp(rules, 0f), 0.0001f);
            Assert.AreEqual(rules.RampEnd, WaveFormula.Ramp(rules, 1f), 0.0001f);
            const int waves = 18;
            for (int number = 2; number <= waves; number++)
            {
                Assert.Greater(WaveFormula.Ramp(rules, WaveFormula.Phase(number, waves)), WaveFormula.Ramp(rules, WaveFormula.Phase(number - 1, waves)), $"wave {number}");
            }
        }

        [Test]
        public void APeak_LandsNearItsPhase_PrefersAMarkedNeighbour_AndNeverTouchesAnother()
        {
            var rules = new BalanceRules { PeakPhases = new[] { 0.4f, 0.7f }, LongLevelWaves = 16, LongLevelPeakPhase = 0.55f };
            // Twelve waves: the phases fall at 5.4 and 8.7.
            CollectionAssert.AreEqual(new[] { 5, 9 }, WaveFormula.Peaks(rules, 12, _ => true, _ => false), "the nearest wave");
            CollectionAssert.AreEqual(new[] { 6, 9 }, WaveFormula.Peaks(rules, 12, _ => true, number => number == 6), "a marked neighbour of the nearest wave takes the peak");
            CollectionAssert.AreEqual(new[] { 5, 9 }, WaveFormula.Peaks(rules, 12, _ => true, number => number == 7 || number == 11), "a marked wave two away does not");
            CollectionAssert.AreEqual(new[] { 4, 9 }, WaveFormula.Peaks(rules, 12, number => number != 5 && number != 6, _ => false), "a closed wave (boss, raid, breather) is passed over");
            // Sixteen waves: 7, 9.25 and 11.5; a level that long has three.
            Assert.AreEqual(3, WaveFormula.Peaks(rules, 16, _ => true, _ => false).Count);
            // Every wave marked and the phases close together: still never two peaks next to each other.
            var close = new BalanceRules { PeakPhases = new[] { 0.4f, 0.45f, 0.5f }, LongLevelWaves = 99 };
            List<int> peaks = WaveFormula.Peaks(close, 12, _ => true, _ => true);
            for (int i = 1; i < peaks.Count; i++)
            {
                Assert.Greater(peaks[i] - peaks[i - 1], 1, "two peaks side by side");
            }

            Assert.IsEmpty(WaveFormula.Peaks(rules, 12, _ => false, _ => true), "no open wave, no peak");
        }

        [Test]
        public void TheBossFormula_NeverFalls_WithTheDifficulty_TheLevelOrTheWave()
        {
            BalanceRules rules = BalanceRules.Default;
            for (int d = 1; d < rules.BossDifficulty.Length; d++)
            {
                Assert.GreaterOrEqual(rules.BossDifficulty[d], rules.BossDifficulty[d - 1], $"BossDifficulty falls at {DifficultyNames[d]}");
            }

            Assert.GreaterOrEqual(rules.BossLevelGrowth, 0f);
            Assert.That(rules.BossEarlyShare, Is.InRange(0f, 1f));
            Assert.That(rules.FlyingBossShare, Is.InRange(0f, 1f));
            Assert.AreEqual(rules.BossBase, WaveFormula.BossBar(rules, 0, WaveSolver.SolveDifficulty, 1f, false), 0.01f, "the base is Medium's last boss at a continent's first level");
            Assert.AreEqual(rules.BossBase * rules.BossEarlyShare, WaveFormula.BossBar(rules, 0, WaveSolver.SolveDifficulty, 0f, false), 0.01f);
            Assert.AreEqual(rules.BossBase * rules.FlyingBossShare, WaveFormula.BossBar(rules, 0, WaveSolver.SolveDifficulty, 1f, true), 0.01f);
            for (int d = 0; d < DifficultyNames.Length; d++)
            {
                for (int level = 0; level < LevelsCompared; level++)
                {
                    float bar = WaveFormula.BossBar(rules, level, d, 0.5f, false);
                    Assert.GreaterOrEqual(WaveFormula.BossBar(rules, level + 1, d, 0.5f, false), bar, "the next level");
                    Assert.GreaterOrEqual(WaveFormula.BossBar(rules, level, d, 0.6f, false), bar, "a later wave");
                    if (d > 0)
                    {
                        Assert.GreaterOrEqual(bar, WaveFormula.BossBar(rules, level, d - 1, 0.5f, false), "the difficulty below");
                    }
                }
            }
        }

        [Test]
        public void ABossFloor_IsItsMultipleOfTheToughestWalkersBar()
        {
            var plated = new EnemyStats { Health = 60f, Armor = 160f };
            var soft = new EnemyStats { Health = 300f };
            var flyer = new EnemyStats { Health = 4000f, Flies = true };
            var boss = new EnemyStats { Health = 500f, Armor = 500f, IsBoss = true };
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = plated, Count = 3 });
            wave.Groups.Add(new GroupSpec { Enemy = soft, Count = 3 });
            wave.Groups.Add(new GroupSpec { Enemy = flyer, Count = 1 });
            var met = new BossYardstick();
            Assert.IsFalse(met.HasMet, "nothing met yet");
            met.Meet(wave, 2f);

            // The bar is body plus armour: the soft walker's 600 against the plated one's 440; a flyer is not met.
            Assert.AreEqual(600f, met.ToughestBar, 0.01f);
            float multiple = met.Multiple(boss, 3f);
            Assert.AreEqual(1.8f, multiple, 0.0001f, "three bars of 600 over a book bar of 1000");
            Assert.AreEqual(3f, met.Over(boss, multiple), 0.001f);

            // The formula decides where it gives more than the floor; the floor where it gives less.
            var small = new BalanceRules { BossBase = 1f };
            var large = new BalanceRules { BossBase = 1000000f };
            Assert.AreEqual(multiple, WaveSolver.BossMultiple(boss, met, small, 0, 1, 1f, 1f), 0.001f, "the floor holds a small formula bar up");
            Assert.AreEqual(1000000f / 1000f, WaveSolver.BossMultiple(boss, met, large, 0, 1, 1f, 1f), 0.01f, "a formula bar over the floor stands as it is");
            Assert.AreEqual(1f / 1000f, WaveSolver.BossMultiple(boss, new BossYardstick(), small, 0, 1, 1f, 1f), 0.00001f, "no walker before it, no floor");
        }

        // ---- The stored waves ---------------------------------------------------------------------------------------

        [Test]
        public void EveryStoredWave_WalksInOnTheFormula()
        {
            int judged = 0;
            foreach (StoredCell cell in Cells())
            {
                for (int i = 0; i < cell.Stored.Count; i++)
                {
                    int number = i + 1;
                    WaveComposer.Kind kind = WaveComposer.KindOf(cell.Script, cell.Difficulty, number);
                    float expected = WaveFormula.WalkIn(cell.Rules, cell.Difficulty, cell.Script.TargetScaleFor(cell.Difficulty), WaveFormula.Phase(number, cell.Script.Waves), kind,
                        WaveComposer.BossOn(cell.Script, cell.Difficulty, number) != null, WaveComposer.BossOn(cell.Script, cell.Difficulty, number - 1) != null, cell.Peaks.Contains(number));
                    float stored = cell.Stored[i].HealthMultiplier;
                    // A raid may stand on its own floor instead (BalanceRules.RaidHealthFloor): never under the formula.
                    if (cell.Stored[i].AirRaid && stored > expected)
                    {
                        continue;
                    }

                    Assert.AreEqual(expected, stored, expected * Slack, $"{cell.Name} wave {number}");
                    judged++;
                }
            }

            Assert.Greater(judged, 0);
        }

        [Test]
        public void EveryCell_HasTwoOrThreePeaks_OnGroundWavesWithoutABoss_NeverSideBySide()
        {
            int cells = 0;
            foreach (StoredCell cell in Cells())
            {
                Assert.That(cell.Peaks.Count, Is.InRange(FewestPeaks, MostPeaks), $"{cell.Name}: peaks {string.Join(", ", cell.Peaks)}");
                for (int i = 0; i < cell.Peaks.Count; i++)
                {
                    int number = cell.Peaks[i];
                    Assert.IsNull(WaveComposer.BossOn(cell.Script, cell.Difficulty, number), $"{cell.Name}: peak on boss wave {number}");
                    Assert.IsFalse(cell.Stored[number - 1].AirRaid, $"{cell.Name}: peak on raid {number}");
                    Assert.AreNotEqual(WaveComposer.Kind.Air, WaveComposer.KindOf(cell.Script, cell.Difficulty, number), $"{cell.Name}: peak on air wave {number}");
                    if (i > 0)
                    {
                        Assert.Greater(number - cell.Peaks[i - 1], 1, $"{cell.Name}: peaks {cell.Peaks[i - 1]} and {number} side by side");
                    }
                }

                cells++;
            }

            Assert.Greater(cells, 0);
        }

        [Test]
        public void EveryStoredLevelBoss_StandsOnItsFormula_OrOnItsFloor()
        {
            var failures = new List<string>();
            int bosses = 0;
            foreach (StoredCell cell in Cells())
            {
                foreach (Boss boss in cell.Bosses())
                {
                    bosses++;
                    float over = cell.Rules.BossOverRegularFor(cell.Difficulty);
                    if (boss.Met && boss.Over < over * (1f - Slack))
                    {
                        failures.Add(Line($"{cell.Name} wave {boss.Wave} {boss.Stats.Id}: x{boss.Over:0.00} the bar of the toughest walker before it, under x{over:0.##}"));
                    }

                    if (Math.Abs(boss.Bar / boss.Expected - 1f) > Slack * 2f)
                    {
                        failures.Add(Line($"{cell.Name} wave {boss.Wave} {boss.Stats.Id}: a bar of {boss.Bar:0} where the formula gives {boss.Formula:0} and the floor {boss.Floor:0}: something else sized it"));
                    }
                }
            }

            Assert.Greater(bosses, 20, "the campaign's level bosses were not found");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>
        /// The critic's finding 4 (round 86: Hard bosses smaller than Medium ones on five of six levels): the same boss wave
        /// of a level never has a smaller bar on a harder difficulty. The formula alone cannot fail this
        /// (<see cref="TheBossFormula_NeverFalls_WithTheDifficulty_TheLevelOrTheWave"/>); the floor can, because it follows
        /// the walkers, and the message says where it binds.
        /// </summary>
        [Test]
        public void EveryStoredLevelBoss_GrowsWithTheDifficulty()
        {
            var failures = new List<string>();
            var easier = new Dictionary<string, (float Bar, string Text)>();
            int compared = 0;
            foreach (StoredCell cell in Cells())
            {
                foreach (Boss boss in cell.Bosses())
                {
                    string key = cell.Key + " wave " + boss.Wave;
                    string text = Line($"{DifficultyNames[cell.Difficulty]} {boss.Bar:0}{(boss.FloorBinds ? " (on its floor)" : string.Empty)}");
                    if (easier.TryGetValue(key, out (float Bar, string Text) below))
                    {
                        compared++;
                        if (boss.Bar < below.Bar * (1f - Slack))
                        {
                            failures.Add($"{key}: {below.Text} > {text}");
                        }
                    }

                    easier[key] = (boss.Bar, text);
                }
            }

            Assert.Greater(compared, 10);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>
        /// The last boss of a level never has a smaller bar than the last boss of the level before it on the same
        /// difficulty. A flying boss is compared as the walking boss it stands for (its bar over FlyingBossShare).
        /// </summary>
        [Test]
        public void TheLastBoss_GrowsWithTheLevel()
        {
            var failures = new List<string>();
            var before = new Dictionary<int, (float Bar, string Text)>();
            int compared = 0;
            foreach (StoredCell cell in Cells())
            {
                Boss last = null;
                foreach (Boss boss in cell.Bosses())
                {
                    last = boss;
                }

                if (last == null)
                {
                    continue;
                }

                float bar = last.Bar / (last.Stats.Flies ? cell.Rules.FlyingBossShare : 1f);
                string text = Line($"{cell.Key} {bar:0}{(last.Stats.Flies ? " (flies, as a walking boss)" : string.Empty)}{(last.FloorBinds ? " (on its floor)" : string.Empty)}");
                if (before.TryGetValue(cell.Difficulty, out (float Bar, string Text) earlier))
                {
                    compared++;
                    if (bar < earlier.Bar * (1f - Slack))
                    {
                        failures.Add($"{DifficultyNames[cell.Difficulty]}: {earlier.Text} > {text}");
                    }
                }

                before[cell.Difficulty] = (bar, text);
            }

            Assert.Greater(compared, 10);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>
        /// A level's scalar is a plain factor on every ordinary wave and moves no boss that stands on its formula: the
        /// closed loop cannot trade crowd for boss. (A boss on its floor follows the walkers; a raid may stand on its own floor.)
        /// </summary>
        [Test]
        public void ALevelsScalar_MovesEveryGroundWaveAlike_AndNoBossOnItsFormula()
        {
            const float soft = 0.7f;
            const float tough = 1.4f;
            int waves = 0;
            foreach (StoredCell cell in Cells())
            {
                float spawn = cell.Model.Difficulties[cell.Difficulty].Health * cell.Model.Facts[cell.Index].HealthScale;
                int level = cell.Model.ContinentIndexOf(cell.Index);
                WaveSolver.Solution low = WaveSolver.Solve(cell.Script, cell.Model.Enemies, cell.Difficulty, cell.Model.Facts[cell.Index].SecondRoad, cell.Rules, soft, level, spawn);
                WaveSolver.Solution high = WaveSolver.Solve(cell.Script, cell.Model.Enemies, cell.Difficulty, cell.Model.Facts[cell.Index].SecondRoad, cell.Rules, tough, level, spawn);
                for (int i = 0; i < low.WalkIn.Length; i++)
                {
                    if (!cell.Stored[i].AirRaid)
                    {
                        Assert.AreEqual(tough / soft, high.WalkIn[i] / low.WalkIn[i], tough / soft * Slack, $"{cell.Name} wave {i + 1}");
                        waves++;
                    }
                }

                foreach (Boss boss in cell.Bosses())
                {
                    float softBar = low.WalkIn[boss.Wave - 1] * low.BossScales[boss.Wave - 1];
                    float toughBar = high.WalkIn[boss.Wave - 1] * high.BossScales[boss.Wave - 1];
                    float formula = boss.Formula / (BossYardstick.Bar(boss.Stats) * spawn);
                    if (Math.Abs(toughBar / formula - 1f) <= Slack)
                    {
                        Assert.AreEqual(toughBar, softBar, toughBar * Slack * 2f, $"{cell.Name} wave {boss.Wave}: the boss moved with the level's scalar");
                    }
                }
            }

            Assert.Greater(waves, 0);
        }

        // ---- The stored campaign, cell by cell ----------------------------------------------------------------------

        private sealed class Boss
        {
            public int Wave;
            public EnemyStats Stats;

            /// <summary>The bar as spawned, what the formula gives, the floor's bar, and the larger of the two.</summary>
            public float Bar;
            public float Formula;
            public float Floor;
            public float Expected => Math.Max(Formula, Floor);
            public bool FloorBinds => Floor > Formula * (1f + Slack);

            /// <summary>True when a walker came before it; then how many times the bar of the toughest of them its bar is.</summary>
            public bool Met;
            public float Over;
        }

        private sealed class StoredCell
        {
            public string Key;
            public string Name;
            public int Index;
            public int Difficulty;
            public CampaignModel Model;
            public BalanceRules Rules;
            public WaveScriptSpec Script;
            public List<WaveSpec> Stored;
            public List<int> Peaks;

            /// <summary>The level bosses of the cell in wave order, each against the walkers that walked in before it.</summary>
            public IEnumerable<Boss> Bosses()
            {
                float spawn = Model.Difficulties[Difficulty].Health * Model.Facts[Index].HealthScale;
                var met = new BossYardstick();
                for (int i = 0; i < Stored.Count; i++)
                {
                    WaveSpec wave = Stored[i];
                    foreach (GroupSpec group in wave.Groups)
                    {
                        EnemyStats stats = group.Enemy;
                        if (stats == null || !stats.IsBoss || stats.IsContinentBoss)
                        {
                            continue;
                        }

                        float multiple = wave.HealthMultiplier * (wave.BossHealthScale > 0f ? wave.BossHealthScale : 1f);
                        float book = BossYardstick.Bar(stats) * spawn;
                        yield return new Boss
                        {
                            Wave = i + 1,
                            Stats = stats,
                            Bar = book * multiple,
                            Formula = WaveFormula.BossBar(Rules, Model.ContinentIndexOf(Index), Difficulty, WaveFormula.Phase(i + 1, Script.Waves), stats.Flies),
                            Floor = met.HasMet ? book * met.Multiple(stats, Rules.BossOverRegularFor(Difficulty)) : 0f,
                            Met = met.HasMet,
                            Over = met.Over(stats, multiple),
                        };
                    }

                    if (!wave.AirRaid)
                    {
                        met.Meet(wave, wave.HealthMultiplier);
                    }
                }
            }
        }

        private static IEnumerable<StoredCell> Cells()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            CampaignModel model = loaded.Model;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                // Sizing the waves leaves the flyer floors it found on the script (the Easy purse rule, the first-flyer
                // rule): kinds and peaks are read with them, as the stored waves were.
                model.BuildWaves(index, script, out _);
                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    var stored = new List<WaveSpec>();
                    foreach (WaveDefinition wave in node.Level.GetWaves(loaded.Catalog.Difficulties[d]))
                    {
                        stored.Add(BalanceLedger.Spec(wave, model.Enemies));
                    }

                    yield return new StoredCell
                    {
                        Key = key,
                        Name = key + " " + DifficultyNames[d],
                        Index = index,
                        Difficulty = d,
                        Model = model,
                        Rules = model.Rules,
                        Script = script,
                        Stored = stored,
                        Peaks = WaveSolver.PeaksOf(script, model.Enemies, d, model.Facts[index].SecondRoad, model.Rules),
                    };
                }
            }
        }

        private static string Line(FormattableString text) => FormattableString.Invariant(text);
    }
}
