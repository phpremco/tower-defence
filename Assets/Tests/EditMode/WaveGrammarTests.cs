using System.Collections.Generic;
using System.Linq;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// The wave grammar (round 79, Docs/WAVE_GRAMMAR.md): the timeline's cuts, the picking rules for patterns and surprises,
    /// how the composer lays a wave out by them, and the validator's bounds on the stored waves of the first continent.
    /// </summary>
    public sealed class WaveGrammarTests
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const int Difficulties = 4;
        private const int SecondRoad = 3;
        private const int NestSource = WaveScripts.Nest;

        private static readonly BalanceRules Rules = BalanceRules.Default;

        // The rules with any number of creatures counting as a surprise: for the tests of where and when a surprise falls,
        // on scripts too small to fill a group early.
        private static readonly BalanceRules AnyGroup = new BalanceRules { Grammar = new WaveGrammarRules { MinSurpriseGroup = 1 } };

        private static Dictionary<string, EnemyStats> Enemies() => new Dictionary<string, EnemyStats>
        {
            ["Ant"] = new EnemyStats { Id = "Ant", Health = 11f, Speed = 1.6f, Reward = 1 },
            ["Tank"] = new EnemyStats { Id = "Tank", Health = 60f, Armor = 160f, Speed = 0.6f, Reward = 15 },
            ["Bat"] = new EnemyStats { Id = "Bat", Health = 40f, Speed = 1.3f, Flies = true, Layer = Layer.Air, Reward = 3 },
            ["Queen"] = new EnemyStats { Id = "Queen", Health = 900f, Speed = 0.55f, IsBoss = true, Reward = 100 },
        };

        // Twelve waves, ants and tanks at the gate, tanks out of a rift from wave 3, a boss on waves 6 and 12.
        private static WaveScriptSpec Script(string rhythm = "P") => new WaveScriptSpec
        {
            Key = "Grammar",
            Waves = 12,
            Budget = 10f,
            FinalBudget = 160f,
            Rhythm = rhythm,
            Troops = new[]
            {
                new TroopSpec { Enemy = "Ant", Weight = 3f, Interval = 0.5f },
                new TroopSpec { Enemy = "Tank", FirstWave = 2, Weight = 1f, Interval = 2.5f },
                new TroopSpec { Enemy = "Tank", FirstWave = 3, Weight = 1f, Interval = 2.5f, Source = NestSource, Delay = 7f, Nest = 1 },
            },
            Bosses = new[] { new BossSpec { Wave = 6, Enemy = "Queen" }, new BossSpec { Wave = 12, Enemy = "Queen" } },
        };

        private static List<float> Times(IEnumerable<GroupSpec> groups)
        {
            var times = new List<float>();
            foreach (GroupSpec group in groups)
            {
                for (int k = 0; k < group.Count; k++)
                {
                    times.Add(WaveTimeline.Start(group) + k * group.Interval);
                }
            }

            times.Sort();
            return times;
        }

        [TestCase(9f, 1)]
        [TestCase(14f, 1)]
        [TestCase(15.1f, 2)]
        [TestCase(20f, 2)]
        [TestCase(24f, 2)]
        [TestCase(26f, 3)]
        [TestCase(100f, 4)]
        public void Beats_AreAboutABeatLong_NeverMuchShorter_NeverMoreThanTheMost(float span, int expected)
        {
            Assert.AreEqual(expected, WaveTimeline.Beats(span, 10f, 0.75f, 4));
        }

        [Test]
        public void Spread_KeepsEveryCreature_AndPutsTheBreatherBetweenTheClusters()
        {
            var groups = new List<GroupSpec>
            {
                new GroupSpec { Count = 21, Interval = 1f },
                new GroupSpec { Count = 6, Interval = 0.2f, Stagger = 9.5f },
                new GroupSpec { Count = 2, Interval = 2f, Delay = 14f },
            };
            List<float> before = Times(groups);

            float[] starts = WaveTimeline.Spread(groups, 2, 5f);

            List<float> after = Times(groups);
            Assert.AreEqual(before.Count, after.Count, "no creature is lost or made by a cut");
            Assert.AreEqual(2, starts.Length);
            // The first cluster ends with the pack's third creature at 9.9 s; the second starts a breather later.
            Assert.AreEqual(14.9f, starts[1], 1e-3f, "the second cluster starts a breather after the last creature of the first");
            for (int i = 0; i < before.Count; i++)
            {
                // A creature of the first half keeps its moment; one of the second half keeps its place in its cluster.
                Assert.AreEqual(before[i] + (before[i] < 10f ? 0f : 4.9f), after[i], 1e-3f, $"spawn {i}");
            }

            foreach (GroupSpec group in groups)
            {
                Assert.IsTrue(group.Delay == 0f || System.Math.Abs(group.Delay - 14.9f) < 1e-3f, "every piece is re-based on its cluster's start");
            }
        }

        [Test]
        public void Spread_DoesNotAddABreatherToASilenceThatIsAlreadyThere()
        {
            // Tanks first, their escort twelve seconds later: cut in two, the silence between them is the breather, not both.
            var groups = new List<GroupSpec>
            {
                new GroupSpec { Count = 3, Interval = 2f },
                new GroupSpec { Count = 10, Interval = 0.5f, Delay = 16f },
            };

            float[] starts = WaveTimeline.Spread(groups, 2, 6f);

            List<float> times = Times(groups);
            Assert.AreEqual(4f, times[2], 1e-3f);
            Assert.AreEqual(10f, times[3], 1e-3f, "six seconds after the last tank");
            Assert.AreEqual(10f, starts[1], 1e-3f);
            Assert.AreEqual(13, times.Count);
        }

        [Test]
        public void Spread_IntoOneCluster_ChangesNothing()
        {
            var groups = new List<GroupSpec> { new GroupSpec { Count = 8, Interval = 1f, Delay = 2.5f } };
            WaveTimeline.Spread(groups, 1, 5f);
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(2.5f, groups[0].Delay);
        }

        [Test]
        public void ActiveSeconds_LeaveOutTheIdlePartOfABreather()
        {
            Assert.AreEqual(22f, WaveTimeline.ActiveSeconds(new List<(float, float)> { (15f, 25f), (0f, 10f) }, 2f), 1e-4f, "5 s of silence, 2 s of it still used");
            Assert.AreEqual(25f, WaveTimeline.ActiveSeconds(new List<(float, float)> { (0f, 14f), (12f, 25f) }, 2f), 1e-4f, "overlapping stretches leave no pause");
            Assert.AreEqual(0f, WaveTimeline.ActiveSeconds(new List<(float, float)>(), 2f));
        }

        [Test]
        public void Pattern_FollowsTheRhythm_AndEverySecondLatePlainWaveIsACombo()
        {
            WaveScriptSpec script = Script("PRHPPPPPPPPP");
            script.Bosses = new BossSpec[0];
            WaveGrammarRules grammar = Rules.Grammar;
            Assert.AreEqual(WavePattern.SpreadClusters, WaveGrammar.PatternOf(script, 1, 1, grammar));
            Assert.AreEqual(WavePattern.Swarm, WaveGrammar.PatternOf(script, 1, 2, grammar));
            Assert.AreEqual(WavePattern.TankEscort, WaveGrammar.PatternOf(script, 1, 3, grammar));
            int first = Enumerable.Range(1, script.Waves).First(w => WaveGrammar.Phase(script, w) >= grammar.ComboFrom);
            for (int w = 4; w <= script.Waves; w++)
            {
                bool combo = w >= first && (w - first) % 2 == 1;
                Assert.AreEqual(combo ? WavePattern.ComboSwarm : WavePattern.SpreadClusters, WaveGrammar.PatternOf(script, 1, w, grammar), $"wave {w}");
            }
        }

        [Test]
        public void Surprises_KeepTheirSpacing_AndStayOffTheOpening_TheBossWaves_TheRaids_TheSwarmsAndTheLastWave()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            var scripts = new List<WaveScriptSpec> { Script() };
            scripts.AddRange(LevelSpecs.All.Select(spec => spec.Waves));
            int seen = 0;
            foreach (WaveScriptSpec script in scripts)
            {
                foreach (int road in new[] { -1, SecondRoad })
                {
                    for (int d = 0; d < Difficulties; d++)
                    {
                        List<int> waves = WaveGrammar.SurpriseWaves(script, CrystalOr(enemies, script), d, road, Rules);
                        seen += waves.Count;
                        for (int i = 0; i < waves.Count; i++)
                        {
                            string where = $"{script.Key} difficulty {d} wave {waves[i]}";
                            // The rules are the default plan's. A surprise the level wrote may fall on any wave (round 81).
                            if (script.SurpriseFor(waves[i], d) != null)
                            {
                                continue;
                            }

                            Assert.Greater(waves[i], Rules.Grammar.SurpriseFreeWavesFor(d), where + ": inside the surprise-free opening");
                            Assert.Less(waves[i], script.Waves, where + ": the last wave is the finale");
                            Assert.IsNull(WaveComposer.BossOn(script, d, waves[i]), where + ": a boss wave");
                            Assert.AreNotEqual(WaveComposer.Kind.Air, WaveComposer.KindOf(script, d, waves[i]), where + ": an air raid");
                            Assert.AreNotEqual(WaveComposer.Kind.Rush, WaveComposer.KindOf(script, d, waves[i]), where + ": a swarm");
                            if (i > 0)
                            {
                                Assert.GreaterOrEqual(waves[i] - waves[i - 1], Rules.Grammar.SurpriseSpacingFor(d), where + ": too soon after the last surprise");
                            }
                        }
                    }
                }
            }

            Assert.Greater(seen, 0);
        }

        [Test]
        public void EveryLevelOfTheFirstContinent_HasASurprise_WhereItsShareFillsAGroup()
        {
            // With the creatures as they are in the data. Glass Meadows has one rift troop, the Cragback, and no wave's share
            // buys a group of those: the default plan gives it no surprise (round 81); since round 82 the level writes one
            // small surprise from Medium on. Landing Site has the same troop and writes its own surprises (the rear rift's
            // lesson). Quartz Flats writes its surprises on every difficulty (rounds 82 and 83: nothing walks out of a rift alone).
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var heavyRiftOnly = new HashSet<string> { "C1_02" };
            const int medium = 1;
            foreach (CampaignLevelSpec spec in LevelSpecs.Crystal)
            {
                int index = loaded.Model.IndexOf(spec.Key);
                Assert.GreaterOrEqual(index, 0, spec.Key);
                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, spec.Waves, out _);
                for (int d = 0; d < Difficulties; d++)
                {
                    int surprises = sets[d].Count(wave => wave.Surprise != SurpriseKind.None);
                    bool writesItsOwn = spec.Key == "C1_01" || (spec.Key == "C1_02" && d >= medium) || spec.Key == "C1_05";
                    if (heavyRiftOnly.Contains(spec.Key))
                    {
                        Assert.AreEqual(d >= medium ? 1 : 0, surprises, $"{spec.Key} difficulty {d}: the level's one written surprise from Medium on, never a default");
                        Assert.AreEqual(surprises, sets[d].Count(wave => wave.Surprise == SurpriseKind.Authored), $"{spec.Key} difficulty {d}");
                    }
                    else
                    {
                        Assert.Greater(surprises, 0, $"{spec.Key} difficulty {d}");
                        Assert.AreEqual(writesItsOwn, sets[d].Any(wave => wave.Surprise == SurpriseKind.Authored), $"{spec.Key} difficulty {d}: a written surprise where the level writes its own");
                    }
                }
            }
        }

        [Test]
        public void ASurprise_IsARealGroup_AndAWaveWhoseShareBuysFewerCarriesNone()
        {
            // Remco, round 81: one stray creature behind a gate is no surprise. The fixture's rift troop is a tank: its share
            // of an early wave buys one or two, so the first surprise waits for the wave whose share fills the group.
            Dictionary<string, EnemyStats> enemies = Enemies();
            var scripts = new List<WaveScriptSpec> { Script() };
            scripts.AddRange(LevelSpecs.All.Select(spec => spec.Waves));
            int group = Rules.Grammar.MinSurpriseGroup;
            int seen = 0;
            foreach (WaveScriptSpec script in scripts)
            {
                Dictionary<string, EnemyStats> roster = CrystalOr(enemies, script);
                foreach (int road in new[] { -1, SecondRoad })
                {
                    for (int d = 0; d < Difficulties; d++)
                    {
                        List<int> loose = WaveGrammar.SurpriseWaves(script, roster, d, road, AnyGroup);
                        List<int> waves = WaveGrammar.SurpriseWaves(script, roster, d, road, Rules);
                        foreach (int number in waves)
                        {
                            WaveSpec wave = WaveComposer.Compose(script, roster, d, number, 1f, road, Rules);
                            int heads = wave.Surprise == SurpriseKind.Rift
                                ? wave.Groups.Where(g => g.Source == NestSource).Sum(g => g.Count)
                                : WaveComposer.SurpriseHeads(script, roster, d, number, road, Rules, wave.Surprise);
                            // What the level wrote itself may be as small as it likes; the default plan sends a group.
                            Assert.GreaterOrEqual(heads, wave.Surprise == SurpriseKind.Authored ? 1 : group, $"{script.Key} difficulty {d} wave {number}: {heads} is a stray, not a surprise");
                            seen++;
                        }

                        if (script.Surprises.Length > 0)
                        {
                            continue;
                        }

                        // The group rule only ever takes a surprise away or moves it to a later wave.
                        Assert.LessOrEqual(waves.Count, loose.Count, $"{script.Key} difficulty {d}");
                        if (waves.Count > 0)
                        {
                            Assert.GreaterOrEqual(waves[0], loose[0], $"{script.Key} difficulty {d}");
                        }
                    }
                }
            }

            Assert.Greater(seen, 0);
            WaveScriptSpec tanks = Script();
            int first = WaveGrammar.SurpriseWaves(tanks, enemies, 1, -1, AnyGroup)[0];
            Assert.Less(WaveComposer.SurpriseHeads(tanks, enemies, 1, first, -1, Rules, SurpriseKind.Rift), group, "the share of the first open wave buys less than a group of tanks");
            Assert.AreEqual(SurpriseKind.None, WaveComposer.Compose(tanks, enemies, 1, first, 1f, -1, Rules).Surprise);
            Assert.IsEmpty(WaveComposer.Compose(tanks, enemies, 1, first, 1f, -1, Rules).Groups.Where(g => g.Source == NestSource), "and the wave sends nothing out of the rift");
        }

        [Test]
        public void TheLevelsOwnSurprise_ReplacesTheDefault_OnItsWaveAndDifficulties_AndTheFrontPaysForIt()
        {
            // Remco, round 81: what comes out of a surprise point is the level's to write, per wave: three creatures out of
            // one point on wave 6, a swarm out of two points on wave 9. Small is fine when it is written.
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script();
            script.Surprises = new[]
            {
                WaveScripts.Surprise(5, WaveScripts.FromNest(1, "Tank", 1)),
                WaveScripts.Surprise(9, 2, 3, WaveScripts.FromNest(1, "Ant", 12, WavePattern.Swarm), WaveScripts.FromNest(2, "Tank", 2), WaveScripts.DownPath(SecondRoad, "Ant", 6, WavePattern.SpreadClusters, 2f)),
                WaveScripts.Surprise(10),
            };

            for (int d = 0; d < Difficulties; d++)
            {
                WaveSpec five = WaveComposer.Compose(script, enemies, d, 5, 1f, -1, Rules);
                Assert.AreEqual(SurpriseKind.Authored, five.Surprise, $"difficulty {d}");
                List<GroupSpec> rift = five.Groups.Where(g => g.Source == NestSource).ToList();
                Assert.AreEqual(1, rift.Count);
                Assert.AreEqual(1, rift[0].Count, "one big creature is fine when the level writes it");
                Assert.AreEqual(1, rift[0].Nest);
                Assert.GreaterOrEqual(WaveTimeline.Start(rift[0]), Rules.Grammar.SurpriseMinDelay, "mid-wave, as every surprise");
                // The front gives up what the surprise costs: the wave is no bigger for it.
                float Threat(WaveSpec wave) => wave.Groups.Where(g => g.Source == 0).Sum(g => g.Count * Game.Balance.Threat.Of(g.Enemy, Rules));
                Assert.Less(Threat(five), Threat(WaveComposer.Compose(NoRift(), enemies, d, 5, 1f, -1, Rules)), $"difficulty {d}: the front pays");

                WaveSpec nine = WaveComposer.Compose(script, enemies, d, 9, 1f, SecondRoad, Rules);
                if (d < 2)
                {
                    Assert.AreNotEqual(SurpriseKind.Authored, nine.Surprise, $"difficulty {d}: outside the entry's difficulties the default plan decides");
                }
                else
                {
                    Assert.AreEqual(SurpriseKind.Authored, nine.Surprise);
                    Assert.AreEqual(12, nine.Groups.Where(g => g.Source == NestSource && g.Nest == 1).Sum(g => g.Count), "a swarm out of the first point");
                    Assert.AreEqual(2, nine.Groups.Where(g => g.Source == NestSource && g.Nest == 2).Sum(g => g.Count), "and two tanks out of the second, in the same wave");
                    List<GroupSpec> road = nine.Groups.Where(g => g.Source == 0 && g.Path == SecondRoad && g.Enemy.Id == "Ant" && WaveTimeline.Start(g) >= Rules.Grammar.SurpriseMinDelay).ToList();
                    Assert.GreaterOrEqual(road.Sum(g => g.Count), 6, "and a file down the surprise path");
                    GroupSpec swarm = nine.Groups.First(g => g.Source == NestSource && g.Nest == 1);
                    Assert.Less(swarm.Interval, 0.5f, "a swarm comes closer together than the creature's own spacing");
                }

                Assert.AreEqual(SurpriseKind.None, WaveComposer.Compose(script, enemies, d, 10, 1f, -1, AnyGroup).Surprise, "a surprise without entries keeps the wave free, of the default too");
                Assert.IsEmpty(WaveComposer.Compose(script, enemies, d, 10, 1f, -1, AnyGroup).Groups.Where(g => g.Source == NestSource));
            }
        }

        // The fixture without its rift troop: what the front is with the whole budget.
        private static WaveScriptSpec NoRift()
        {
            WaveScriptSpec script = Script();
            script.Troops = script.Troops.Where(t => t.Source == 0).ToArray();
            return script;
        }

        [Test]
        public void TheSurpriseSchedule_IsCheckedAgainstTheLevelsSpawnPoints()
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelFolder + "/Level_C1_05.asset");
            Assert.IsNotNull(level, "Run Tools/Generate/Scenes and Starter Data first.");
            Game.Enemies.EnemyDefinition ById(string id) => AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            int road = System.Array.FindIndex(level.Paths, path => !path.Flying);
            Assert.AreEqual(SpawnRole.Main, level.RoleOfPath(road));
            Assert.AreEqual(SpawnRole.Surprise, level.RoleOfNest(1), "a nest is a surprise point unless the level says otherwise");

            var errors = new List<string>();
            // Any combination of the level's surprise points in one wave is fine.
            var fine = new[] { WaveScripts.Surprise(6, WaveScripts.FromNest(1, "Cragback", 1), WaveScripts.FromNest(2, "Shalebat", 5, WavePattern.Swarm)), WaveScripts.Surprise(8) };
            SurpriseSchedule.Check(level, fine, 17, ById, errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));

            var wrong = new[]
            {
                WaveScripts.Surprise(30, WaveScripts.FromNest(1, "Cragback", 1)),
                WaveScripts.Surprise(6, WaveScripts.FromNest(9, "Cragback", 1), WaveScripts.FromNest(1, "Skitter", 3), WaveScripts.FromNest(1, "Cragback", 0),
                    WaveScripts.DownPath(road, "Skitter", 3), WaveScripts.FromNest(1, "Nobody", 1)),
            };
            SurpriseSchedule.Check(level, wrong, 17, ById, errors);
            Assert.AreEqual(6, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("waves 1 to 17", errors[0]);
            StringAssert.Contains("nest 9 does not exist", errors[1]);
            StringAssert.Contains("Skitter cannot come out of", errors[2]);
            StringAssert.Contains("empty group", errors[3]);
            StringAssert.Contains("is a main path", errors[4]);
            StringAssert.Contains("unknown creature", errors[5]);
        }

        [Test]
        public void EveryLevelsOwnSurprises_FitItsSpawnPoints()
        {
            Game.Enemies.EnemyDefinition ById(string id) => AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            foreach (CampaignLevelSpec spec in LevelSpecs.All)
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{LevelFolder}/Level_{spec.Key}.asset");
                if (level == null)
                {
                    continue;
                }

                var errors = new List<string>();
                SurpriseSchedule.Check(level, spec.Waves.Surprises, spec.Waves.Waves, ById, errors);
                Assert.IsEmpty(errors, string.Join("\n", errors));
            }
        }

        [Test]
        public void ARiftSurprise_OpensMidWave_WithItsShareOfTheBudget_AndNoOtherWaveUsesTheRift()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script();
            for (int d = 0; d < Difficulties; d++)
            {
                List<int> surprises = WaveGrammar.SurpriseWaves(script, enemies, d, -1, AnyGroup);
                for (int number = 1; number <= script.Waves; number++)
                {
                    WaveSpec wave = WaveComposer.Compose(script, enemies, d, number, 1f, -1, AnyGroup);
                    List<GroupSpec> rift = wave.Groups.Where(g => g.Source == NestSource).ToList();
                    if (!surprises.Contains(number))
                    {
                        Assert.AreEqual(SurpriseKind.None, wave.Surprise, $"difficulty {d} wave {number}");
                        Assert.IsEmpty(rift, $"difficulty {d} wave {number}: the rift opens on surprise waves only");
                        continue;
                    }

                    Assert.AreEqual(SurpriseKind.Rift, wave.Surprise, $"difficulty {d} wave {number}");
                    Assert.IsNotEmpty(rift);
                    float front = wave.Groups.Where(g => g.Source == 0 && !g.Enemy.IsBoss).Max(WaveTimeline.End);
                    foreach (GroupSpec group in rift)
                    {
                        Assert.GreaterOrEqual(WaveTimeline.Start(group), AnyGroup.Grammar.SurpriseMinDelay, "never in the first seconds of a wave");
                        Assert.GreaterOrEqual(WaveTimeline.Start(group), front * AnyGroup.Grammar.SurpriseAt, "the front is under way when the rift opens");
                        Assert.AreEqual(1, group.Nest, "through the troop's authored nest");
                    }
                }
            }
        }

        // Ants at the gate, bats out of a rift from wave 3, one air raid in wave 9 on every difficulty.
        private static WaveScriptSpec RiftBatScript() => new WaveScriptSpec
        {
            Key = "GrammarBats",
            Waves = 12,
            Budget = 10f,
            FinalBudget = 160f,
            Rhythm = "P",
            Flyer = "Bat",
            AirWaves = new[] { 9 },
            AirWavesPerDifficulty = new[] { 1, 1, 1, 1 },
            Troops = new[]
            {
                new TroopSpec { Enemy = "Ant", Weight = 3f, Interval = 0.5f },
                new TroopSpec { Enemy = "Bat", FirstWave = 3, Weight = 1f, Interval = 1.5f, Source = NestSource, Delay = 7f },
            },
        };

        private static int FirstRiftFlyerWave(WaveScriptSpec script, Dictionary<string, EnemyStats> enemies, int difficulty)
        {
            for (int number = 1; number <= script.Waves; number++)
            {
                if (WaveComposer.Compose(script, enemies, difficulty, number, 1f, -1, AnyGroup).Groups.Any(g => g.Source == NestSource && g.Enemy.Flies))
                {
                    return number;
                }
            }

            return 0;
        }

        [Test]
        public void ARiftsFlyers_DoNotWaitForTheFirstRaid_OnlyForTheFlyingTroopFloor()
        {
            // Remco, round 80: being caught out by flyers from a rift is part of the game. Only the first-flyer rule of Easy and
            // Medium (the flying troop floor the solve sets) holds them back.
            Dictionary<string, EnemyStats> enemies = Enemies();
            for (int d = 0; d < Difficulties; d++)
            {
                WaveScriptSpec script = RiftBatScript();
                int free = AnyGroup.Grammar.SurpriseFreeWavesFor(d);
                int first = FirstRiftFlyerWave(script, enemies, d);
                Assert.AreEqual(9, WaveComposer.FirstAirWave(script, d));
                Assert.AreEqual(free + 1, first, $"difficulty {d}: the first wave that may carry a surprise sends the rift's bats");
                Assert.Less(first, 9, $"difficulty {d}: before the first raid");

                WaveScriptSpec held = RiftBatScript();
                held.FlyingTroopFloor = new[] { 7 };
                Assert.GreaterOrEqual(FirstRiftFlyerWave(held, enemies, d), 7, $"difficulty {d}: the flying troop floor holds a rift's flyers too");
            }
        }

        [Test]
        public void ASurprise_MakesNoOtherWaveTougher()
        {
            // Round 83: every wave is solved to its own target, so a surprise takes from its own wave and nothing is spread
            // over the others (rounds 80 and 80b needed a second solve and a relief bound for that, because one common factor
            // held the level's mean). Round 84: the waves after a surprise wave are held against the wave it would have been
            // without its surprise (the band and the median of WaveSolver.Solve read the calm solve there), so a swarm on
            // the upper edge of its band no longer follows a surprise wave's fewer, tougher creatures (Crysium Vein Psycho
            // wave 16 stood x1.59 above the calm level) and the swarm is judged like every other wave.
            // What is left is what the purse moves: a surprise wave's kills pay at other moments and leave other creatures
            // on the road, so the player's purchase for the waves after it shifts a little. Measured on all six levels
            // (Tools/PlayTest/Scripts/SurpriseLiftLab.cs): a plain wave x1.09 at most, a raid x1.03: bound x1.15, as it was.
            // A swarm x1.23 (Glass Meadows Psycho wave 11: its pressure rises slowly with its health, so what wave 10
            // leaves on the road shows large in the multiplier): bound x1.25; round 83b left swarms out.
            // A boss wave is read on the health of the whole wave, boss included (round 83b read the crowd alone, which
            // is what is left beside the boss and says little: 5 % more wave reads as 39 % more crowd, and it missed the
            // boss): x1.29 at most (Shard Ridge Psycho wave 10, the boss sized against a defence the flank waves before it
            // changed): bound x1.3.
            // Round 85 widened the swarm bound to x1.35 and the boss wave's to x1.4: a boss was sized on the toughest creature
            // met, and a surprise's creatures counted there at their own, higher multiplier (Shard Ridge Hard wave 16 stood
            // x1.35 over the level without surprises), while a boss wave's crowd was filled up to a paper target around it.
            // Round 86 took both channels out (the yardstick of a boss reads a surprise wave as the calm wave it would have
            // been, a boss wave's crowd comes on the edge of its band) and the bounds are round 84's again. Measured
            // (SurpriseLiftLab.cs, 2026-11-19): a plain wave x1.05 at most, a raid x1.09, a swarm x1.12 (Shard Ridge Easy
            // wave 10), a boss wave x1.07 in health (Crysium Vein Psycho wave 12).
            const float purse = 1.15f;
            const float swarm = 1.25f;
            const float bossWave = 1.3f;
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            CampaignModel model = loaded.Model;
            int surprises = 0;
            int judged = 0;
            foreach (string key in new[] { "C1_01", "C1_02", "C1_03", "C1_04", "C1_05", "C1_06" })
            {
                int index = model.IndexOf(key);
                Assert.GreaterOrEqual(index, 0);
                List<List<WaveSpec>> sets = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
                List<List<WaveSpec>> calm;
                model.Rules.Grammar.Surprises = false;
                try
                {
                    calm = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
                }
                finally
                {
                    model.Rules.Grammar.Surprises = true;
                }

                for (int d = 0; d < sets.Count; d++)
                {
                    for (int i = 0; i < sets[d].Count; i++)
                    {
                        // Not judged: the surprise wave itself and a wave that is a raid in one solve and not in the other (the
                        // flyer floors read the purse of the whole level).
                        if (sets[d][i].Surprise != SurpriseKind.None || sets[d][i].AirRaid != calm[d][i].AirRaid)
                        {
                            surprises += sets[d][i].Surprise != SurpriseKind.None ? 1 : 0;
                            continue;
                        }

                        judged++;
                        if (sets[d][i].Groups.Exists(g => g.Enemy != null && g.Enemy.IsBoss))
                        {
                            Assert.LessOrEqual(WaveHealth(sets[d][i]), WaveHealth(calm[d][i]) * bossWave, $"{key} difficulty {d} wave {i + 1} (boss wave)");
                            continue;
                        }

                        float bound = sets[d][i].Pattern == WavePattern.Swarm ? swarm : purse;
                        Assert.LessOrEqual(sets[d][i].HealthMultiplier, calm[d][i].HealthMultiplier * bound, $"{key} difficulty {d} wave {i + 1}");
                    }
                }
            }

            Assert.Greater(surprises, 0);
            Assert.Greater(judged, surprises, "most waves carry no surprise and are judged");
        }

        // The health of every creature of a wave as it walks in, a boss with its own scale.
        private static float WaveHealth(WaveSpec wave)
        {
            float health = 0f;
            foreach (GroupSpec group in wave.Groups)
            {
                if (group.Enemy != null)
                {
                    health += group.Count * group.Enemy.Health * wave.HealthMultiplier * (group.Enemy.IsBoss ? wave.BossHealthScale : 1f);
                }
            }

            return health;
        }

        [Test]
        public void AFlank_ComesDownTheSecondRoadAlone_MidWave()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script();
            script.Troops = script.Troops.Where(t => t.Source == 0).ToArray();
            List<int> surprises = WaveGrammar.SurpriseWaves(script, enemies, 1, SecondRoad, AnyGroup);
            Assert.IsNotEmpty(surprises);
            Assert.IsEmpty(WaveGrammar.SurpriseWaves(script, enemies, 1, -1, AnyGroup), "no rift troops and no second road: nothing to surprise with");
            foreach (int number in surprises)
            {
                Assert.GreaterOrEqual(WaveGrammar.Phase(script, number), AnyGroup.Grammar.FlankFrom);
                WaveSpec wave = WaveComposer.Compose(script, enemies, 1, number, 1f, SecondRoad, AnyGroup);
                Assert.AreEqual(SurpriseKind.Flank, wave.Surprise);
                // The flank is the tail of the group list: everything from the first group that starts at the surprise's moment.
                List<GroupSpec> late = wave.Groups.Where(g => g.Delay >= AnyGroup.Grammar.SurpriseMinDelay && g.Path == SecondRoad).ToList();
                Assert.IsNotEmpty(late, $"wave {number}: a group down the second road after the opening");
            }
        }

        [Test]
        public void ABossWave_ComesInClusters_AndTheBossWalksInBehindTheLast()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script();
            for (int d = 0; d < Difficulties; d++)
            {
                foreach (int number in new[] { 6, 12 })
                {
                    WaveSpec wave = WaveComposer.Compose(script, enemies, d, number, 1f, -1, Rules);
                    Assert.AreEqual(number == script.Waves ? Rules.Grammar.FinaleBeats : Rules.Grammar.BossBeats, wave.Beats, $"difficulty {d} wave {number}");
                    GroupSpec boss = wave.Groups.First(g => g.Enemy.IsBoss);
                    // The crowd on the ground: the flyers that escort a last boss come out after it by design.
                    List<float> crowd = Times(wave.Groups.Where(g => !g.Enemy.IsBoss && !g.Enemy.Flies));
                    // Round 88c: the boss closes its wave, out behind the last creature of its last cluster (it used to come
                    // out with the first of them, and walked under its own plated crowd).
                    float tail = crowd.Max();
                    Assert.GreaterOrEqual(boss.Delay - tail, Rules.Grammar.BossEntranceGap - 1e-3f, "the boss comes out a gap after the last creature of its crowd");
                    Assert.LessOrEqual(boss.Delay - tail, Rules.Grammar.BossEntranceGap + (tail * (Rules.Grammar.BossTailStretch - 1f)) + 1e-3f, "and no later than the stretched cluster asks");
                    Assert.AreEqual(SurpriseKind.None, wave.Surprise, "a boss wave carries no surprise");
                }
            }
        }

        [Test]
        public void ATankEscort_SendsTheTanksFirst_AndTheEscortCatchesThemAtTheFirstGuns()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script("H");
            script.Troops = script.Troops.Where(t => t.Source == 0).ToArray();
            WaveSpec wave = WaveComposer.Compose(script, enemies, 1, 9, 1f, -1, Rules);
            Assert.AreEqual(WavePattern.TankEscort, wave.Pattern);
            float tank = wave.Groups.Where(g => g.Enemy.Id == "Tank").Min(WaveTimeline.Start);
            float ant = wave.Groups.Where(g => g.Enemy.Id == "Ant").Min(WaveTimeline.Start);
            WaveGrammarRules grammar = Rules.Grammar;
            float lag = System.Math.Min(grammar.MaxEscortLag, grammar.EscortCatchCells * (1f / 0.6f - 1f / 1.6f));
            Assert.AreEqual(0f, tank, 1e-3f, "the tanks open the wave");
            Assert.AreEqual(lag, ant, 1e-3f, "the ants start as much later as they need to catch the tanks after the grammar's cells");
            // The first ant and the first tank are level after EscortCatchCells of road (or the lag is capped and they meet later).
            Assert.LessOrEqual(grammar.EscortCatchCells / 0.6f, lag + grammar.EscortCatchCells / 1.6f + 1e-3f + (lag >= grammar.MaxEscortLag ? float.MaxValue : 0f));
        }

        [Test]
        public void TheGrammar_MovesCreaturesInTime_NeverTheirNumber()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            var uncut = new BalanceRules { Grammar = new WaveGrammarRules { MaxBeats = 1, BossBeats = 1, FinaleBeats = 1 } };
            WaveScriptSpec script = Script("PRHP");
            for (int d = 0; d < Difficulties; d++)
            {
                for (int number = 1; number <= script.Waves; number++)
                {
                    WaveSpec cut = WaveComposer.Compose(script, enemies, d, number, 1f, SecondRoad, Rules);
                    WaveSpec whole = WaveComposer.Compose(script, enemies, d, number, 1f, SecondRoad, uncut);
                    foreach (string id in enemies.Keys)
                    {
                        Assert.AreEqual(whole.Groups.Where(g => g.Enemy.Id == id).Sum(g => g.Count), cut.Groups.Where(g => g.Enemy.Id == id).Sum(g => g.Count), $"difficulty {d} wave {number}: {id}");
                    }

                    Assert.AreEqual(whole.HealthMultiplier, cut.HealthMultiplier, "the cuts do not touch the creatures' health");
                }
            }
        }

        [Test]
        public void TheLedger_GivesABreatherNoFirepower()
        {
            // The same forty ants in one stream and in two clusters with eight idle seconds between them: the window the towers
            // get (and so the health the solver may ask) is the same but for the credit.
            var ant = new EnemyStats { Id = "Ant", Health = 11f, Speed = 1.6f, Reward = 1 };
            var whole = new WaveSpec();
            whole.Groups.Add(new GroupSpec { Enemy = ant, Count = 40, Interval = 0.5f });
            var cut = new WaveSpec();
            cut.Groups.Add(new GroupSpec { Enemy = ant, Count = 20, Interval = 0.5f });
            cut.Groups.Add(new GroupSpec { Enemy = ant, Count = 20, Interval = 0.5f, Delay = 20f });
            var level = new LevelInput();
            var medium = new DifficultyStats();

            float spanWhole = WaveLoad.Of(level, whole, medium, Rules).Span;
            float spanCut = WaveLoad.Of(level, cut, medium, Rules).Span;

            Assert.AreEqual(19.5f, spanWhole, 1e-3f);
            Assert.AreEqual(9.5f + Rules.Grammar.PauseCredit + 9.5f, spanCut, 1e-3f, "10.5 s of silence count for the credit only");
        }

        [Test]
        public void TheStoredWaves_KeepTheGrammarsBounds()
        {
            int checkedWaves = 0;
            int storedSurprises = 0;
            foreach (LevelDefinition level in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelDefinition>))
            {
                for (int d = 0; d < level.DifficultyWaves.Length; d++)
                {
                    DifficultyWaves set = level.DifficultyWaves[d];
                    for (int i = 0; i < set.Waves.Length; i++)
                    {
                        var errors = new List<string>();
                        WaveShape.Check($"{level.name} {set.Difficulty.name} {i + 1}", set.Waves[i], set.Difficulty, errors);
                        Assert.IsEmpty(errors, string.Join("\n", errors));
                        checkedWaves++;
                        // What the default plan sends out of the nests is a group; only a surprise the level wrote may be smaller.
                        int heads = WaveShape.RiftHeads(set.Waves[i], set.Difficulty);
                        storedSurprises += heads > 0 ? 1 : 0;
                        WaveScriptSpec script = WaveScripts.ByKey(level.name.Replace("Level_", string.Empty));
                        // A level without a script of its own (a boss arena) plays a campaign level's waves, judged there.
                        bool written = script == null || script.SurpriseFor(i + 1, d) != null;
                        Assert.IsTrue(heads == 0 || written || heads >= Rules.Grammar.MinSurpriseGroup, $"{level.name} {set.Difficulty.name} wave {i + 1}: {heads} out of the nests is a stray, not a surprise");
                    }
                }
            }

            Assert.Greater(checkedWaves, 0);
            Assert.Greater(storedSurprises, 0, "the first continent opens a rift somewhere");
        }

        [Test]
        public void TheValidator_NamesAWaveThatDrags_AndTakesAnyCombinationOfSpawnPoints()
        {
            var difficulty = UnityEngine.ScriptableObject.CreateInstance<DifficultyProfile>();
            var walker = UnityEngine.ScriptableObject.CreateInstance<Game.Enemies.EnemyDefinition>();
            try
            {
                var quiet = new WaveDefinition(new[]
                {
                    new SpawnGroup { Enemy = walker, Count = 2, Interval = 1f },
                    new SpawnGroup { Enemy = walker, Count = 2, Interval = 1f, Delay = WaveGrammarRules.MaxPauseSeconds + 2f },
                });
                // Remco, round 81: any main paths with any surprise points in one wave; one creature is fine when it is written.
                var twoRifts = new WaveDefinition(new[]
                {
                    new SpawnGroup { Enemy = walker, Count = 6, Interval = 1f },
                    new SpawnGroup { Enemy = walker, Count = 6, Interval = 1f, PathIndex = SecondRoad },
                    new SpawnGroup { Enemy = walker, Count = 2, Interval = 1f, Delay = 12f, Source = SpawnSource.Nest, Nest = 1 },
                    new SpawnGroup { Enemy = walker, Count = 1, Interval = 1f, Delay = 12f, Source = SpawnSource.Nest, Nest = 2 },
                });

                var errors = new List<string>();
                WaveShape.Check("quiet", quiet, difficulty, errors);
                Assert.AreEqual(1, errors.Count, "a silence longer than the longest breather");
                errors.Clear();
                WaveShape.Check("two rifts", twoRifts, difficulty, errors);
                Assert.IsEmpty(errors, "two roads and two nests in one wave");
                Assert.AreEqual(3, WaveShape.RiftHeads(twoRifts, difficulty));
                Assert.AreEqual(13f, WaveShape.Span(twoRifts, difficulty), 1e-3f);
                Assert.AreEqual(7f, WaveShape.LongestPause(twoRifts, difficulty), 1e-3f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(difficulty);
                UnityEngine.Object.DestroyImmediate(walker);
            }
        }

        // The creatures of the first continent's scripts, as far as the grammar reads them (which fly, which are bosses).
        private static Dictionary<string, EnemyStats> CrystalEnemies()
        {
            var enemies = new Dictionary<string, EnemyStats>();
            foreach (string id in new[] { "Skitter", "Cragback", "Wisp", "Roller", "Prowler" })
            {
                enemies[id] = new EnemyStats { Id = id, Health = 20f };
            }

            foreach (string id in new[] { "Shalebat", "Skyray" })
            {
                enemies[id] = new EnemyStats { Id = id, Health = 40f, Flies = true, Layer = Layer.Air };
            }

            foreach (string id in new[] { "BroodMother", "CoreWisp", "StormMatriarch", "PackAlpha" })
            {
                enemies[id] = new EnemyStats { Id = id, Health = 600f, IsBoss = true };
            }

            return enemies;
        }

        private static Dictionary<string, EnemyStats> CrystalOr(Dictionary<string, EnemyStats> own, WaveScriptSpec script) => script.Key == "Grammar" ? own : CrystalEnemies();

        // The gate script without its rift troop: its surprise is a flank, as on a level that forks without rifts.
        private static WaveScriptSpec ForkScript()
        {
            WaveScriptSpec script = Script("PRPH");
            script.Troops = script.Troops.Where(t => t.Source == 0).ToArray();
            return script;
        }

        private static List<int> FrontPaths(WaveSpec wave) =>
            wave.Groups.Where(g => !g.Enemy.IsBoss && !g.Enemy.Flies && g.Source == 0 && (wave.Surprise != SurpriseKind.Flank || g.Delay < Rules.Grammar.SurpriseMinDelay || g.Path != SecondRoad))
                .Select(g => g.Path).Distinct().OrderBy(p => p).ToList();

        [Test]
        public void ALevelWithOneRoad_UsesIt_OnEveryWave()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = Script("PRPH");
            for (int d = 0; d < Difficulties; d++)
            {
                for (int number = 1; number <= script.Waves; number++)
                {
                    Assert.AreEqual(MainRoads.First, WaveGrammar.RoadsOn(script, enemies, d, number, -1, Rules), $"difficulty {d} wave {number}");
                    WaveSpec wave = WaveComposer.Compose(script, enemies, d, number, 1f, -1, Rules);
                    Assert.IsTrue(wave.Groups.Where(g => !g.Enemy.Flies).All(g => g.Path == WaveComposer.Road), $"difficulty {d} wave {number}");
                }
            }
        }

        [Test]
        public void OnALevelThatForks_AWaveTakesASubsetOfTheMainRoads_MoreCombinationsOnTheHarderDifficulties()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = ForkScript();
            int before = -1;
            for (int d = 0; d < Difficulties; d++)
            {
                int both = 0;
                var singles = new List<MainRoads>();
                for (int number = 1; number <= script.Waves; number++)
                {
                    MainRoads roads = WaveGrammar.RoadsOn(script, enemies, d, number, SecondRoad, Rules);
                    WaveSpec wave = WaveComposer.Compose(script, enemies, d, number, 1f, SecondRoad, Rules);
                    Assert.AreEqual(roads, wave.Roads);
                    Assert.AreNotEqual(MainRoads.None, roads);
                    bool boss = WaveComposer.BossOn(script, d, number) != null;
                    bool flank = wave.Surprise == SurpriseKind.Flank;
                    if (boss || number == script.Waves)
                    {
                        Assert.AreEqual(MainRoads.Both, roads, $"difficulty {d} wave {number}: a boss wave and the last wave come down every road");
                    }
                    else if (flank)
                    {
                        Assert.AreEqual(MainRoads.First, roads, $"difficulty {d} wave {number}: the front of a flank wave keeps to the first road");
                    }
                    else
                    {
                        both += roads == MainRoads.Both ? 1 : 0;
                        if (roads != MainRoads.Both)
                        {
                            singles.Add(roads);
                        }
                    }

                    // The front walks in on exactly the roads the grammar chose (a wave too small for two clusters may fill one).
                    List<int> paths = FrontPaths(wave);
                    var allowed = new List<int>();
                    if ((roads & MainRoads.First) != 0)
                    {
                        allowed.Add(WaveComposer.Road);
                    }

                    if ((roads & MainRoads.Second) != 0)
                    {
                        allowed.Add(SecondRoad);
                    }

                    Assert.IsNotEmpty(paths, $"difficulty {d} wave {number}");
                    Assert.IsTrue(paths.All(allowed.Contains), $"difficulty {d} wave {number}: roads {roads}, walked {string.Join(",", paths)}");
                    if (roads != MainRoads.Both)
                    {
                        Assert.AreEqual(allowed, paths, $"difficulty {d} wave {number}");
                    }
                }

                Assert.AreEqual(MainRoads.First, WaveGrammar.RoadsOn(script, enemies, d, 1, SecondRoad, Rules), "the first wave of a level is one road");
                // The rule alone (no flank to take a wave's front back to one road): harder is never fewer.
                var calm = new BalanceRules { Grammar = new WaveGrammarRules { Surprises = false } };
                int difficulty = d;
                int byRule = Enumerable.Range(1, script.Waves).Count(n => WaveGrammar.RoadsOn(script, enemies, difficulty, n, SecondRoad, calm) == MainRoads.Both);
                Assert.GreaterOrEqual(byRule, before, $"difficulty {d}: no fewer waves on both roads than the difficulty below");
                Assert.GreaterOrEqual(byRule, both);
                before = byRule;
                // The single-road waves take turns: both roads are used alone on a difficulty that has two such waves.
                if (singles.Count >= 2)
                {
                    Assert.Contains(MainRoads.First, singles);
                    Assert.Contains(MainRoads.Second, singles);
                }
            }

            Assert.Greater(before, 0, "Psycho sends waves down both roads");
        }

        [Test]
        public void TheLevelsOwnRoads_ReplaceTheRule_OnTheirWaveAndDifficulties()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = ForkScript();
            MainRoads byRule = WaveGrammar.RoadsOn(script, enemies, 0, 3, SecondRoad, Rules);
            MainRoads other = byRule == MainRoads.Second ? MainRoads.First : MainRoads.Second;
            script.RoadPlan = new[] { new RoadSpec { Wave = 3, MinDifficulty = 0, MaxDifficulty = 1, Roads = other } };
            Assert.AreEqual(other, WaveGrammar.RoadsOn(script, enemies, 0, 3, SecondRoad, Rules));
            Assert.AreEqual(other, WaveGrammar.RoadsOn(script, enemies, 1, 3, SecondRoad, Rules));
            WaveScriptSpec plain = ForkScript();
            Assert.AreEqual(WaveGrammar.RoadsOn(plain, enemies, 2, 3, SecondRoad, Rules), WaveGrammar.RoadsOn(script, enemies, 2, 3, SecondRoad, Rules), "Hard keeps the rule");
            WaveSpec wave = WaveComposer.Compose(script, enemies, 0, 3, 1f, SecondRoad, Rules);
            Assert.AreEqual(new List<int> { other == MainRoads.Second ? SecondRoad : WaveComposer.Road }, FrontPaths(wave));
            // The kill money is the script's whatever the roads.
            Assert.AreEqual(WaveComposer.Compose(plain, enemies, 0, 3, 1f, SecondRoad, Rules).Groups.Sum(g => g.Count), wave.Groups.Sum(g => g.Count));
        }

        [Test]
        public void EveryWaveOnEveryRoad_IsTheRuleAtAShareOfOne()
        {
            Dictionary<string, EnemyStats> enemies = Enemies();
            WaveScriptSpec script = ForkScript();
            var every = new BalanceRules { Grammar = new WaveGrammarRules { MultiRoadShare = new[] { 1f } } };
            for (int number = 1; number <= script.Waves; number++)
            {
                bool flank = WaveGrammar.SurpriseOn(script, enemies, 1, number, SecondRoad, every) == SurpriseKind.Flank;
                Assert.AreEqual(flank ? MainRoads.First : MainRoads.Both, WaveGrammar.RoadsOn(script, enemies, 1, number, SecondRoad, every), $"wave {number}");
            }
        }

        [Test]
        public void TheRoadPlan_IsCheckedAgainstTheLevelsRoads()
        {
            LevelDefinition forked = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{LevelFolder}/Level_C1_03.asset");
            LevelDefinition single = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{LevelFolder}/Level_C1_05.asset");
            Assert.IsNotNull(forked);
            Assert.IsNotNull(single);
            var fine = new[] { new RoadSpec { Wave = 3, Roads = MainRoads.Second }, new RoadSpec { Wave = 16, Roads = MainRoads.Both } };
            var errors = new List<string>();
            RoadSchedule.Check(forked, fine, 16, errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            RoadSchedule.Check(single, fine, 17, errors);
            Assert.AreEqual(2, errors.Count, "a level with one road has no second road to name");
            errors.Clear();
            RoadSchedule.Check(forked, new[] { new RoadSpec { Wave = 17, Roads = MainRoads.First }, new RoadSpec { Wave = 2, Roads = MainRoads.None }, new RoadSpec { Wave = 2, MinDifficulty = 2, MaxDifficulty = 1 } }, 16, errors);
            Assert.AreEqual(3, errors.Count, string.Join("\n", errors));
            foreach (CampaignLevelSpec spec in LevelSpecs.Crystal)
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{LevelFolder}/Level_{spec.Key}.asset");
                errors.Clear();
                RoadSchedule.Check(level, spec.Waves.RoadPlan, spec.Waves.Waves, errors);
                Assert.IsEmpty(errors, string.Join("\n", errors));
            }
        }

        // The most a stored ground wave's multiplier may stand above the median of its level and difficulty (round 82).
        private const float MostOverTheMedian = 2.5f;

        [Test]
        public void NoStoredGroundWave_IsFarTougherThanItsLevel()
        {
            // Round 82: a swarm solved on its own difficulty came out at three times its level's median (Quartz Flats Hard
            // wave 15, x6.07: more health than the finale, and the wave the cell was lost on). The trend band of the solver
            // now holds every ground wave, the lift of a capped group included; this is its gate on the stored waves.
            int checkedWaves = 0;
            foreach (LevelDefinition level in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelDefinition>))
            {
                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    List<float> ground = set.Waves.Where(w => !w.AirRaid).Select(w => w.HealthMultiplier).OrderBy(m => m).ToList();
                    if (ground.Count == 0)
                    {
                        continue;
                    }

                    float median = ground.Count % 2 == 1 ? ground[ground.Count / 2] : (ground[ground.Count / 2 - 1] + ground[ground.Count / 2]) * 0.5f;
                    for (int i = 0; i < set.Waves.Length; i++)
                    {
                        if (!set.Waves[i].AirRaid)
                        {
                            Assert.LessOrEqual(set.Waves[i].HealthMultiplier, median * MostOverTheMedian, $"{level.name} {set.Difficulty.name} wave {i + 1}: x{set.Waves[i].HealthMultiplier:0.00} against a median of x{median:0.00}");
                            checkedWaves++;
                        }
                    }
                }
            }

            Assert.Greater(checkedWaves, 0);
        }
    }
}
