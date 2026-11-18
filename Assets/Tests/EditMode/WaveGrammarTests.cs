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
            // buys a group of those: the default plan gives it no surprise (round 81). Landing Site has the same troop and
            // writes its own surprises (the rear rift's lesson).
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var heavyRiftOnly = new HashSet<string> { "C1_02" };
            foreach (CampaignLevelSpec spec in LevelSpecs.Crystal)
            {
                int index = loaded.Model.IndexOf(spec.Key);
                Assert.GreaterOrEqual(index, 0, spec.Key);
                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, spec.Waves, out _);
                for (int d = 0; d < Difficulties; d++)
                {
                    int surprises = sets[d].Count(wave => wave.Surprise != SurpriseKind.None);
                    if (heavyRiftOnly.Contains(spec.Key))
                    {
                        Assert.AreEqual(0, surprises, $"{spec.Key} difficulty {d}: a lone Cragback is no surprise");
                    }
                    else
                    {
                        Assert.Greater(surprises, 0, $"{spec.Key} difficulty {d}");
                        Assert.AreEqual(spec.Key == "C1_01", sets[d].Any(wave => wave.Surprise == SurpriseKind.Authored), $"{spec.Key} difficulty {d}: only Landing Site writes its own");
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
        public void ASurprise_MakesNoOtherWaveTougher_AndTheLevelStaysInsideItsBand()
        {
            // BalanceRules.SurprisesLiftNoWave (found on Glass Meadows, read on Shard Ridge since round 81 took the lone
            // Cragbacks of Glass Meadows away): a wave without a surprise is never
            // tougher than the rule leaves it without (some are softer: they take the multiplier of the level solved without
            // surprises), and the curve comes first: the relief leaves the level at most
            // BalanceRules.SurpriseReliefMost under its human curve (round 80b).
            const string key = "C1_03";
            const float rounding = 0.01f;
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            CampaignModel model = loaded.Model;
            int index = model.IndexOf(key);
            Assert.GreaterOrEqual(index, 0);
            Assert.IsTrue(model.Rules.SurprisesLiftNoWave);
            List<List<WaveSpec>> sets = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
            List<List<WaveSpec>> lifted;
            model.Rules.SurprisesLiftNoWave = false;
            try
            {
                lifted = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
            }
            finally
            {
                model.Rules.SurprisesLiftNoWave = true;
            }

            int surprises = 0;
            int lowered = 0;
            for (int d = 0; d < sets.Count; d++)
            {
                for (int i = 0; i < sets[d].Count; i++)
                {
                    if (sets[d][i].Surprise != SurpriseKind.None)
                    {
                        surprises++;
                        continue;
                    }

                    Assert.LessOrEqual(sets[d][i].HealthMultiplier, lifted[d][i].HealthMultiplier * 1.002f, $"{key} difficulty {d} wave {i + 1}");
                    lowered += sets[d][i].HealthMultiplier < lifted[d][i].HealthMultiplier * 0.998f ? 1 : 0;
                }

                float mean = BalanceChecks.PlainMean(Ledger.Evaluate(model.InputFor(index, d), sets[d], model.Difficulties[d], model.Rules.HumanTowersOnly()));
                Assert.GreaterOrEqual(mean, model.HumanTarget(index, d) / (1f + model.Rules.SurpriseReliefMost + rounding), $"{key} difficulty {d}: the relief leaves the level inside its band");
            }

            Assert.Greater(surprises, 0);
            Assert.Greater(lowered, 0, "the rule lowers a wave somewhere on the level");
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
        public void ABossWave_ComesInClusters_AndTheBossWalksInWithTheLast()
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
                    List<float> crowd = Times(wave.Groups.Where(g => !g.Enemy.IsBoss));
                    float lastCluster = crowd.Where(t => t >= boss.Delay - 1e-3f).DefaultIfEmpty(-1f).Min();
                    Assert.AreEqual(boss.Delay, lastCluster, 1.5f, "the boss comes with the first creatures of the last cluster");
                    float before = crowd.Where(t => t < boss.Delay - 1e-3f).Max();
                    Assert.GreaterOrEqual(boss.Delay - before, Rules.Grammar.BossBreatherFor(d) - 1e-3f, "a breather before the boss");
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
                        bool written = script != null && script.SurpriseFor(i + 1, d) != null;
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
    }
}
