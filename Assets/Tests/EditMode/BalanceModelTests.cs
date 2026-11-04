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
            Assert.IsTrue(endGame.OwnedTowers.Contains("Rocket"), "Psycho expects the whole campaign behind the player");
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
            Assert.IsEmpty(BalanceChecks.CheckHuman("Fair", new[] { 0.55f, 0.67f, 1.06f, 1.24f }, Rules));
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Generous", new[] { 0.41f, 0.67f, 1.06f, 1.24f }, Rules), "2.4x the firepower on Easy");
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Flat", new[] { 0.66f, 0.67f, 1.06f, 1.24f }, Rules), "Easy as hard as Medium");
            Assert.IsNotEmpty(BalanceChecks.CheckHuman("Inverted", new[] { 0.55f, 0.67f, 0.6f, 1.24f }, Rules), "Hard easier than Medium");
        }
    }
}
