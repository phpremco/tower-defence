using System.Collections.Generic;
using Game.Balance;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The leak model (round 22b): what a wave over pressure one does to the base, on paper. Pure model, no assets: a wave
    /// held outright leaks nothing; a wave with no defence at all leaks everything for the leakers' measured lifetime; the stars follow the game's thresholds; and the plan evaluator lets a plan survive a leaking wave when the
    /// base can take it, but not when nothing stands.
    /// </summary>
    public sealed class LeakModelTests
    {
        private static WaveLoad Load() => new WaveLoad
        {
            StreamHealth = 1000f,
            GroundCount = 20,
            MeanHealth = 50f,
            GroundStrike = 4f,
            AirHealth = 100f,
            AirCount = 5,
            MeanAirHealth = 20f,
            AirBaseDamage = 2f,
            AirStrike = 1f,
        };

        [Test]
        public void HeldWave_LeaksNothing()
        {
            LeakLine leak = Leak.Of(Load(), BalanceRules.Default, 0.9f, 0.5f);
            Assert.AreEqual(0f, leak.GroundHealth);
            Assert.AreEqual(0f, leak.AirHealth);
            Assert.AreEqual(0f, leak.BaseDamage);
        }

        [Test]
        public void UndefendedWave_LeaksEverything_ForTheLeakersLifetime()
        {
            BalanceRules rules = BalanceRules.Default;
            LeakLine weak = Leak.Of(Load(), rules, float.PositiveInfinity, float.PositiveInfinity);
            Assert.AreEqual(1000f, weak.GroundHealth, 0.01f);
            Assert.AreEqual(100f, weak.AirHealth, 0.01f);
            // Five attackers at most hammer the base for the leakers' lifetime; the five flyers lay siege the same way.
            Assert.AreEqual((rules.AttackersPerGate * 4f + 5f * 1f) * rules.LeakFactor, weak.BaseDamage, 0.5f);
        }

        [Test]
        public void HalfLeak_ScalesWithTheShareOverOne_AndWithTheFactor()
        {
            BalanceRules rules = BalanceRules.Default.Copy();
            LeakLine one = Leak.Of(Load(), rules, 2f, 0f);
            Assert.AreEqual(500f, one.GroundHealth, 0.01f);
            rules.LeakFactor *= 0.5f;
            LeakLine half = Leak.Of(Load(), rules, 2f, 0f);
            Assert.AreEqual(one.BaseDamage * 0.5f, half.BaseDamage, 0.01f);
        }

        [Test]
        public void Stars_FollowTheBaseLeft()
        {
            float[] thresholds = { 0.5f, 0.9f };
            Assert.AreEqual(0, Leak.Stars(0f, 600f, thresholds));
            Assert.AreEqual(1, Leak.Stars(100f, 600f, thresholds));
            Assert.AreEqual(2, Leak.Stars(400f, 600f, thresholds));
            Assert.AreEqual(3, Leak.Stars(600f, 600f, thresholds));
            Assert.AreEqual(Leak.Score(true, 3, 600f, 100, 12, 12), 3000f + 600f + 10f, 0.01f);
            Assert.Less(Leak.Score(false, 0, 0f, 0, 12, 8), Leak.Score(false, 0, 0f, 0, 12, 11));
        }

        [Test]
        public void Evaluator_SurvivesALeakTheBaseCanTake_ButNotAnEmptyField()
        {
            LevelInput level = Level();
            var waves = new List<WaveSpec> { Wave(level, 1), Wave(level, 1) };
            var stats = new DifficultyStats();
            BalanceRules rules = BalanceRules.Default.HumanWithHero();
            PlanOutcome empty = PlanEvaluator.Evaluate(level, waves, stats, rules, new PlanSpec { Steps = new PlanStep[0] }, 1f);
            Assert.IsFalse(empty.Holds, "nothing stands: the base falls");
            Assert.Greater(empty.FallWave, 0);

            var plan = new PlanSpec { Steps = new[] { new PlanStep { Kind = PlanStep.Build, Item = "Gun", X = 0, Y = 0, Wave = 1 } } };
            PlanOutcome one = PlanEvaluator.Evaluate(level, waves, stats, rules, plan, 1f);
            Assert.Less(one.Margin, 1f, "one gun is over the cap: the wave leaks");
            Assert.IsTrue(one.Holds, "the base takes the leak");
            Assert.Less(one.BaseLeft, level.BaseHealth);
            Assert.Greater(one.Stars, 0);
            Assert.AreEqual(one.AnalyticScore, Leak.Score(true, one.Stars, one.BaseLeft, one.CoinsLeft, 2, 2), 0.01f);
        }

        // A level without a map (ideal spots), one gun, a base that takes a leak but not two undefended waves.
        private static LevelInput Level()
        {
            var level = new LevelInput { StartingMoney = 60, BaseHealth = 250f, GateHealth = 0f };
            level.Towers.Add(new TowerStats { Id = "Gun", Targets = Layer.Ground, Levels = new[] { new TowerLevelStats(50, 30f, 3f, 0.22f) } });
            return level;
        }

        private static WaveSpec Wave(LevelInput level, float multiplier)
        {
            var wave = new WaveSpec { HealthMultiplier = multiplier };
            wave.Groups.Add(new GroupSpec { Enemy = new EnemyStats { Id = "Ant", Health = 30f, Speed = 1.5f, Reward = 1, BlockadeDamage = 3f, AttackInterval = 1f }, Count = 40, Interval = 0.5f });
            return wave;
        }
    }
}
