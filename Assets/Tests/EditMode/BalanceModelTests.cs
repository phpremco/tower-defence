using System.Collections.Generic;
using Game.Balance;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>The pure balance model on small hand-made inputs: threat and rewards, buying, progression, the solver.</summary>
    public sealed class BalanceModelTests
    {
        private static readonly BalanceRules Rules = BalanceRules.Default;

        private static EnemyStats Walker(string id, float health, float speed = 1f, float armor = 0f) =>
            new EnemyStats { Id = id, Health = health, Armor = armor, Speed = speed, Reward = 2, AttackInterval = 1f, BlockadeDamage = 4f };

        private static TowerStats Rifle() => new TowerStats
        {
            Id = "Rifle",
            Targets = Layer.Ground | Layer.Air,
            Levels = new[] { new TowerLevelStats(50, 5f, 3.5f, 0.35f), new TowerLevelStats(60, 9f, 3.8f, 0.3f), new TowerLevelStats(90, 15f, 4.1f, 0.25f) },
        };

        private static LevelInput Level(params TowerStats[] towers)
        {
            var road = new RoadInput { PathIndex = 0, Waypoints = new[] { new Cell(0, 0), new Cell(30, 0) } };
            var level = new LevelInput
            {
                Key = "Test",
                StartingMoney = 200,
                Shape = LevelShape.Build(new[] { road }, new[] { new Cell(20, 0) }, new Cell[0], null),
                GateHealth = 500f,
                Hero = new HeroStats { BladeDamage = 18f, BladeInterval = 0.7f },
            };
            level.Towers.AddRange(towers);
            return level;
        }

        private static WaveSpec Wave(EnemyStats enemy, int count, float health = 1f)
        {
            var wave = new WaveSpec { HealthMultiplier = health };
            wave.Groups.Add(new GroupSpec { Enemy = enemy, Count = count, Interval = 1f });
            return wave;
        }

        private static DifficultyStats Medium => new DifficultyStats { Id = "Medium", Index = 1 };

        [Test]
        public void Mines_PayNothing_InALevelWithoutAHarvestDrone()
        {
            LevelInput level = Level(Rifle());
            level.MineCount = 3;
            level.MineStock = 140;
            level.HarvesterCost = 150;

            Assert.Greater(Ledger.MoneyAt(level, 10, 12, 0f, Rules), 200, "with a harvest drone the nodes pay");
            level.Harvest = false;
            Assert.AreEqual(200, Ledger.MoneyAt(level, 10, 12, 0f, Rules), "no harvest drone offered yet: the ore stays in the ground");
        }

        [Test]
        public void Reward_GrowsWithThreat_AndBossesPayMore()
        {
            EnemyStats small = Walker("Small", 20f);
            EnemyStats big = Walker("Big", 200f);
            EnemyStats boss = Walker("Boss", 200f);
            boss.IsBoss = true;

            Assert.Less(Threat.Reward(small, Rules), Threat.Reward(big, Rules));
            Assert.Greater(Threat.Reward(boss, Rules), Threat.Reward(big, Rules));
            Assert.GreaterOrEqual(Threat.Reward(Walker("Tiny", 1f), Rules), 1);
        }

        [Test]
        public void BossHealthShare_ScalesOnlyAContinentBoss()
        {
            LevelInput level = Level(Rifle());
            EnemyStats levelBoss = Walker("LevelBoss", 400f);
            levelBoss.IsBoss = true;
            EnemyStats continentBoss = Walker("ContinentBoss", 400f);
            continentBoss.IsBoss = true;
            continentBoss.IsContinentBoss = true;
            var hard = new DifficultyStats { Id = "Hard", Index = 2, BossHealth = 1.5f };

            float plainMedium = Ledger.EvaluateWave(level, Wave(levelBoss, 1), Medium, 400, Rules).Pressure;
            float plainHard = Ledger.EvaluateWave(level, Wave(levelBoss, 1), hard, 400, Rules).Pressure;
            float bossMedium = Ledger.EvaluateWave(level, Wave(continentBoss, 1), Medium, 400, Rules).Pressure;
            float bossHard = Ledger.EvaluateWave(level, Wave(continentBoss, 1), hard, 400, Rules).Pressure;

            Assert.AreEqual(plainMedium, plainHard, 1e-4f, "a level boss is sized by the wave solver, not by the boss share");
            Assert.AreEqual(plainMedium, bossMedium, 1e-4f, "a share of one changes nothing");
            Assert.Greater(bossHard, bossMedium * 1.3f, "the share must reach the continent boss's health");
        }

        [Test]
        public void Pressure_RisesWithHealth_AndFallsWithMoney()
        {
            LevelInput level = Level(Rifle());
            EnemyStats walker = Walker("Walker", 50f);
            float light = Ledger.EvaluateWave(level, Wave(walker, 6, 1f), Medium, 200, Rules).Pressure;
            float heavy = Ledger.EvaluateWave(level, Wave(walker, 6, 2f), Medium, 200, Rules).Pressure;
            float rich = Ledger.EvaluateWave(level, Wave(walker, 6, 2f), Medium, 800, Rules).Pressure;

            Assert.Greater(heavy, light);
            Assert.Less(rich, heavy);
        }

        [Test]
        public void WithoutAnAirGun_AnAirWaveCannotBeHeld()
        {
            var cannon = new TowerStats { Id = "Cannon", Targets = Layer.Ground, Splash = 1.2f, Levels = new[] { new TowerLevelStats(100, 25f, 4f, 1.4f) } };
            EnemyStats flyer = Walker("Flyer", 40f);
            flyer.Layer = Layer.Air;
            flyer.Flies = true;

            WaveLine line = Ledger.EvaluateWave(Level(cannon), Wave(flyer, 3), Medium, 500, Rules);
            Assert.IsTrue(float.IsInfinity(line.AirPressure));
        }

        [Test]
        public void Purchases_NeverUpgradeATowerThatWasNotBuilt()
        {
            // With money for exactly one level-three step and nothing else, buying must still start with a build.
            LevelInput level = Level(Rifle());
            WaveLine line = Ledger.EvaluateWave(level, Wave(Walker("Walker", 50f), 6), Medium, 90, Rules);
            Assert.AreEqual(1, line.Towers);
        }

        [Test]
        public void Progression_OwnsOnlyStartingAndEarnedTowers()
        {
            var campaign = new List<CampaignLevel>
            {
                new CampaignLevel { Key = "One", RewardTowerId = "Cannon", Research = 10 },
                new CampaignLevel { Key = "Two", RewardTowerId = "Laser", Research = 10 },
                new CampaignLevel { Key = "Three", RewardTowerId = "Rocket", Research = 10 },
            };
            var upgrades = new List<UpgradeStats>();

            PlayerState first = Progression.StateAt(campaign, 0, 1, new[] { "Rifle" }, upgrades, Rules);
            PlayerState third = Progression.StateAt(campaign, 2, 1, new[] { "Rifle" }, upgrades, Rules);
            PlayerState endGame = Progression.StateAt(campaign, 0, Progression.EndGameDifficulty, new[] { "Rifle" }, upgrades, Rules);

            CollectionAssert.AreEquivalent(new[] { "Rifle" }, first.OwnedTowers);
            CollectionAssert.AreEquivalent(new[] { "Rifle", "Cannon", "Laser" }, third.OwnedTowers);
            CollectionAssert.AreEquivalent(new[] { "Rifle", "Cannon" }, endGame.OwnedTowers, "Psycho on the first level has beaten it on the lower difficulties (its own reward, round 22f) but has not been further (round 22e)");
        }

        [Test]
        public void Progression_SpendsResearchOnOwnedTowersOnly()
        {
            var campaign = new List<CampaignLevel> { new CampaignLevel { Key = "One", Research = 100 }, new CampaignLevel { Key = "Two" } };
            var upgrades = new List<UpgradeStats>
            {
                new UpgradeStats { Id = "Rifle_Damage", Kind = UpgradeKind.TowerDamage, TowerId = "Rifle", BonusPerLevel = 0.1f, Costs = new[] { 5, 10, 15 } },
                new UpgradeStats { Id = "Laser_Damage", Kind = UpgradeKind.TowerDamage, TowerId = "Laser", BonusPerLevel = 0.1f, Costs = new[] { 1, 1, 1 } },
            };

            PlayerState state = Progression.StateAt(campaign, 1, 0, new[] { "Rifle" }, upgrades, Rules);
            Assert.AreEqual(1.3f, state.Boosts["Rifle"].Damage, 0.001f);
            Assert.IsFalse(state.Boosts.ContainsKey("Laser"));
        }

        [Test]
        public void Shape_FindsASecondFrontWhenRoadsMeetLate()
        {
            var main = new RoadInput { PathIndex = 0, Waypoints = new[] { new Cell(0, 10), new Cell(30, 10), new Cell(30, 0), new Cell(40, 0) } };
            var side = new RoadInput { PathIndex = 3, Waypoints = new[] { new Cell(0, 0), new Cell(30, 0), new Cell(40, 0) } };
            LevelShape late = LevelShape.Build(new[] { main, side }, new Cell[0], new Cell[0], null);
            LevelShape single = LevelShape.Build(new[] { main }, new Cell[0], new Cell[0], null);

            Assert.AreEqual(2, late.GroundFronts(Rules.TrunkReference));
            Assert.AreEqual(1, single.GroundFronts(Rules.TrunkReference));
        }

        [Test]
        public void Composer_IsDeterministic_AndHonoursBossesPerDifficulty()
        {
            var enemies = new Dictionary<string, EnemyStats> { ["Walker"] = Walker("Walker", 30f), ["King"] = Walker("King", 800f) };
            enemies["King"].IsBoss = true;
            var script = new WaveScriptSpec
            {
                Key = "Test",
                Waves = 10,
                Budget = 6f,
                FinalBudget = 60f,
                Troops = new[] { new TroopSpec { Enemy = "Walker", Weight = 1f, Interval = 1f } },
                Bosses = new[]
                {
                    new BossSpec { Wave = 10, Enemy = "King" },
                    new BossSpec { Wave = 5, Enemy = "King", MinDifficulty = 2, Routes = new[] { BossRoute.Entrance } },
                },
            };

            WaveSpec a = WaveComposer.Compose(script, enemies, 1, 7, 1.2f, -1, Rules);
            WaveSpec b = WaveComposer.Compose(script, enemies, 1, 7, 1.2f, -1, Rules);
            Assert.AreEqual(a.Groups.Count, b.Groups.Count);
            for (int i = 0; i < a.Groups.Count; i++)
            {
                Assert.AreEqual(a.Groups[i].Count, b.Groups[i].Count);
                Assert.AreEqual(a.Groups[i].Stagger, b.Groups[i].Stagger);
            }

            Assert.IsNull(WaveComposer.BossOn(script, 1, 5), "the extra boss waits for Hard");
            WaveSpec hard = WaveComposer.Compose(script, enemies, 2, 5, 1f, -1, Rules);
            Assert.IsTrue(hard.Groups.Exists(g => g.Enemy.IsBoss && g.Source == WaveLoad.EntranceSource));
        }

        [Test]
        public void Solver_LandsMediumWavesNearTheirTarget()
        {
            var enemies = new Dictionary<string, EnemyStats> { ["Walker"] = Walker("Walker", 30f), ["Brute"] = Walker("Brute", 120f, 0.8f) };
            var script = new WaveScriptSpec
            {
                Key = "Test",
                Waves = 12,
                Budget = 6f,
                FinalBudget = 80f,
                Troops = new[] { new TroopSpec { Enemy = "Walker", Weight = 2f, Interval = 0.8f }, new TroopSpec { Enemy = "Brute", FirstWave = 4, Weight = 1f, Interval = 2f } },
            };
            LevelInput level = Level(Rifle());
            float[] healths = WaveSolver.Solve(script, enemies, level, Medium, -1, Rules);

            var waves = new List<WaveSpec>();
            for (int i = 0; i < script.Waves; i++)
            {
                waves.Add(WaveComposer.Compose(script, enemies, 1, i + 1, healths[i], -1, Rules));
            }

            List<WaveLine> lines = Ledger.Evaluate(level, waves, Medium, Rules);
            for (int i = Rules.OpeningWaves; i < lines.Count; i++)
            {
                Assert.AreEqual(WaveSolver.Target(script, i + 1, Rules), lines[i].Pressure, 0.15f, $"wave {i + 1}");
            }
        }

        [Test]
        public void ThinnerIncome_LeavesLessMoney_AndMorePressure()
        {
            EnemyStats walker = Walker("Walker", 40f);
            var waves = new List<WaveSpec>();
            for (int i = 0; i < 8; i++)
            {
                waves.Add(Wave(walker, 6));
            }

            List<WaveLine> full = Ledger.Evaluate(Level(Rifle()), waves, Medium, Rules);
            LevelInput thin = Level(Rifle());
            thin.Income = 0.5f;
            List<WaveLine> half = Ledger.Evaluate(thin, waves, Medium, Rules);

            int last = waves.Count - 1;
            Assert.Less(half[last].Money, full[last].Money);
            Assert.AreEqual(full[last].Money - thin.StartingMoney, 2 * (half[last].Money - thin.StartingMoney), 2, "half the income, half the kill money");
            Assert.Greater(half[last].Pressure, full[last].Pressure);
        }

        [Test]
        public void HumanLens_FlagsAGenerousEasy_AndAnEasyAsHardAsMedium()
        {
            // The curve of 2026-11-04: Easy 1.4x, Medium 1.15x, Hard 0.95x, Psycho 0.8x.
            Assert.IsEmpty(BalanceChecks.CheckHuman("Fair", new[] { 0.71f, 0.87f, 1.05f, 1.25f }, Rules));
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Generous", new[] { 0.55f, 0.87f, 1.05f, 1.25f }, Rules), "1.8x the firepower on Easy");
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Flat", new[] { 0.86f, 0.87f, 1.05f, 1.25f }, Rules), "Easy as hard as Medium");
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Inverted", new[] { 0.71f, 0.87f, 0.8f, 1.25f }, Rules), "Hard easier than Medium");
        }

        [Test]
        public void HumanCurve_GetsHarderLevelByLevel_UpToItsCeiling()
        {
            for (int d = 0; d < 4; d++)
            {
                Assert.AreEqual(Rules.HumanSurplus[d], Rules.HumanSurplusAt(d, 0), 1e-4f, "a continent's first level asks the base curve");
                Assert.Less(Rules.HumanSurplusAt(d, 5), Rules.HumanSurplusAt(d, 4), "each later level a little harder");
                Assert.AreEqual(Rules.HumanSurplusAt(d, 60), Rules.HumanSurplusAt(d, 40), 1e-4f, "the ramp stops at its ceiling");
            }

            for (int d = 1; d < 4; d++)
            {
                Assert.Less(Rules.HumanSurplusAt(d, 0), Rules.HumanSurplusAt(d - 1, 0), "harder difficulties ask more");
            }
        }

        [Test]
        public void HumanTargetAndRamp_FlagALevelOffTheCurve_AndADip()
        {
            float[] onCurve = { 1f / Rules.HumanSurplusAt(0, 2), 1f / Rules.HumanSurplusAt(1, 2), 1f / Rules.HumanSurplusAt(2, 2), 1f / Rules.HumanSurplusAt(3, 2) };
            Assert.IsEmpty(BalanceChecks.CheckHumanTarget("OnCurve", 2, onCurve, Rules));
            float[] easyOff = (float[])onCurve.Clone();
            easyOff[0] *= 0.8f;
            Assert.IsNotEmpty(BalanceChecks.CheckHumanTarget("EasyOff", 2, easyOff, Rules), "Easy 25% too generous");

            var names = new[] { "L1", "L2", "L3" };
            Assert.IsEmpty(BalanceChecks.CheckRamp(names, new List<float[]> { new[] { 1.4f, 1.15f }, new[] { 1.38f, 1.14f }, new[] { 1.38f, 1.15f } }, Rules), "flat within the tolerance");
            List<string> dip = BalanceChecks.CheckRamp(names, new List<float[]> { new[] { 1.4f, 1.15f }, new[] { 1.38f, 1.13f }, new[] { 1.5f, 1.12f } }, Rules);
            Assert.AreEqual(1, dip.Count, "L3 Easy is easier than L2");
        }

        [Test]
        public void Beatable_FlagsAThinPlainMean_AndABossOverTheCap()
        {
            List<WaveLine> Lines(float plain, float boss)
            {
                var lines = new List<WaveLine>();
                for (int i = 1; i <= 10; i++)
                {
                    lines.Add(new WaveLine { Wave = i, Boss = i == 10, GroundPressure = i == 10 ? boss : plain });
                }

                return lines;
            }

            Assert.IsEmpty(BalanceChecks.CheckBeatable("Fine", "Medium", 1, Lines(0.8f, 1.1f), Rules));
            Assert.IsNotEmpty(BalanceChecks.CheckBeatable("Thin", "Medium", 1, Lines(1.05f, 1.1f), Rules), "plain waves need more than the player has");
            Assert.IsNotEmpty(BalanceChecks.CheckBeatable("BigBoss", "Medium", 1, Lines(0.8f, 1.5f), Rules), "the boss is not killable");
            Assert.IsEmpty(BalanceChecks.CheckBeatable("Psycho", "Psycho", 3, Lines(1.15f, 1.3f), Rules), "Psycho may ask more");
        }

        [Test]
        public void BuildWaves_CalibratesEveryDifficultyToTheHumanCurve_AndCapsItsBosses()
        {
            var enemies = new Dictionary<string, EnemyStats>
            {
                ["Walker"] = Walker("Walker", 30f),
                ["Brute"] = Walker("Brute", 120f, 0.8f),
                ["Boss"] = new EnemyStats { Id = "Boss", Health = 900f, Speed = 0.6f, Reward = 40, IsBoss = true, AttackInterval = 1f, BlockadeDamage = 20f },
            };
            var script = new WaveScriptSpec
            {
                Key = "Test",
                Waves = 12,
                Budget = 6f,
                FinalBudget = 80f,
                Troops = new[] { new TroopSpec { Enemy = "Walker", Weight = 2f, Interval = 0.8f }, new TroopSpec { Enemy = "Brute", FirstWave = 4, Weight = 1f, Interval = 2f } },
                Bosses = new[] { new BossSpec { Wave = 12, Enemy = "Boss" } },
            };
            LevelInput level = Level(Rifle());
            var model = new CampaignModel { Rules = Rules, Hero = level.Hero };
            model.Levels.Add(new CampaignLevel { Key = "Test", TowerIds = new List<string> { "Rifle" } });
            model.Facts.Add(new LevelFacts { Key = "Test", StartingMoney = level.StartingMoney, Shape = level.Shape, GateHealth = level.GateHealth });
            model.Towers["Rifle"] = Rifle();
            model.StartingTowers.Add("Rifle");
            foreach (KeyValuePair<string, EnemyStats> enemy in enemies)
            {
                model.Enemies[enemy.Key] = enemy.Value;
            }

            for (int d = 0; d < 4; d++)
            {
                model.Difficulties.Add(new DifficultyStats { Id = "D" + d, Index = d, Health = 0.7f + 0.3f * d, Count = 0.9f + 0.2f * d });
            }

            List<List<WaveSpec>> sets = model.BuildWaves(0, script, out _);
            for (int d = 0; d < 4; d++)
            {
                LevelInput input = model.InputFor(0, d);
                float mean = BalanceChecks.PlainMean(Ledger.Evaluate(input, sets[d], model.Difficulties[d], Rules.HumanTowersOnly()));
                Assert.AreEqual(Rules.HumanSurplusAt(d, 0), 1f / mean, Rules.HumanSurplusAt(d, 0) * Rules.HumanTargetTolerance, $"difficulty {d} on the curve");
                foreach (WaveLine line in Ledger.Evaluate(input, sets[d], model.Difficulties[d], Rules.HumanWithHero()))
                {
                    Assert.LessOrEqual(line.Pressure, Rules.HeroCapFor(d) * (1f + Rules.BeatableTolerance), $"difficulty {d} wave {line.Wave} under the cap");
                }
            }
        }
    }
}
