using System.Collections.Generic;
using Game.Balance;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The siege model (round 22c): what a wave's creatures the guns do not kill on the walk do to the gates and the base, on
    /// paper. Pure model, no assets: a wave killed on the walk touches nothing; an undefended leak breaks the gates first and
    /// the base after them; a guard at the queue clears a leak at a cost that scales with the siege factor; the hero is
    /// explicit (he kills at his gate, falls when his guard share of the hammer has taken his health, and is down for his
    /// revive time); the stars follow the game's thresholds; and the plan evaluator lets a plan survive a leak the guards
    /// can clear, but not an empty field.
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
            AirStrike = 1f,
            Window = 30f,
        };

        private static LevelInput Gated(int gates, bool hero)
        {
            var level = new LevelInput { BaseHealth = 600f, GateHealth = 500f };
            for (int i = 0; i < gates; i++)
            {
                level.Shape.Gates.Add(new LevelShape.Gate { OnTrunk = true, Progress = 0.3f + 0.2f * i });
            }

            level.Hero = hero ? new HeroStats { BladeDamage = 18f, BladeInterval = 0.7f, GunDamage = 4f, GunInterval = 0.2f, Magazine = 8, ReloadSeconds = 1.2f, MaxHealth = 150f, GuardShare = 0.4f, ReviveSeconds = 120f } : null;
            return level;
        }

        [Test]
        public void WaveKilledOnTheWalk_TouchesNothing()
        {
            LevelInput level = Gated(2, true);
            SiegeState state = SiegeState.Start(level, 2);
            var guards = new SiegeGuards { WalkStream = 1200f, WalkAir = 150f };
            SiegeLine line = Leak.Resolve(level, Load(), BalanceRules.Default, guards, state, 40f);
            Assert.AreEqual(0f, line.GroundLeak);
            Assert.AreEqual(0f, line.AirLeak);
            Assert.AreEqual(0f, line.GateDamage);
            Assert.AreEqual(0f, line.BaseDamage);
            Assert.AreEqual(600f, state.BaseLeft);
            Assert.AreEqual(1000f, state.GatesLeft);
        }

        [Test]
        public void UndefendedLeak_BreaksTheGatesFirst_ThenTheBase()
        {
            LevelInput level = Gated(2, false);
            SiegeState state = SiegeState.Start(level, 2);
            SiegeLine line = Leak.Resolve(level, Load(), BalanceRules.Default, new SiegeGuards(), state, 40f);
            Assert.AreEqual(1000f, line.GroundLeak, 0.01f);
            Assert.AreEqual(1000f, line.GateDamage, 0.01f, "both trunk gates fall");
            Assert.AreEqual(600f, line.BaseDamage, 0.01f, "nothing guards the doorstep: the base falls");
            Assert.AreEqual(0f, state.GatesLeft);
            Assert.AreEqual(0f, state.BaseLeft);
        }

        [Test]
        public void GuardedQueue_ClearsTheLeak_AtACostThatScalesWithTheFactor()
        {
            BalanceRules rules = BalanceRules.Default.Copy();
            LevelInput level = Gated(1, false);
            var guards = new SiegeGuards { QueueStream = 50f, DoorAir = 20f };
            SiegeState one = SiegeState.Start(level, 1);
            SiegeLine first = Leak.Resolve(level, Load(), rules, guards, one, 40f);
            // Twenty ants, five at the gate at once at 4 a second: 20 damage a second for the 20 seconds the guard needs.
            Assert.AreEqual(400f, first.GateDamage, 0.5f);
            Assert.AreEqual(0f, first.RestLeak, "the guard clears the queue before the gate falls");
            Assert.AreEqual(100f, first.AirLeak, 0.01f);
            Assert.Greater(first.BaseDamage, 0f, "the flyers besiege the base until the doorstep guard has them");
            Assert.Greater(one.BaseLeft, 0f);

            rules.SiegeFactor = 0.5f;
            SiegeState half = SiegeState.Start(level, 1);
            SiegeLine second = Leak.Resolve(level, Load(), rules, guards, half, 40f);
            Assert.AreEqual(first.GateDamage * 0.5f, second.GateDamage, 0.5f);
        }

        [Test]
        public void Hero_KillsAtHisGate_AndFallsUnderALongSiege()
        {
            LevelInput level = Gated(1, true);
            WaveLoad load = Load();
            SiegeState withHero = SiegeState.Start(level, 1);
            SiegeLine line = Leak.Resolve(level, load, BalanceRules.Default, new SiegeGuards { QueueStream = 30f }, withHero, 40f);
            SiegeState alone = SiegeState.Start(Gated(1, false), 1);
            SiegeLine without = Leak.Resolve(Gated(1, false), load, BalanceRules.Default, new SiegeGuards { QueueStream = 30f }, alone, 40f);
            Assert.Less(line.GateDamage, without.GateDamage, "the hero's blade shortens the siege of his gate");

            // A siege the guard cannot end: the hero takes his share of the hammer, goes down and is out for his revive time.
            SiegeState pressed = SiegeState.Start(level, 1);
            SiegeLine fell = Leak.Resolve(level, load, BalanceRules.Default, new SiegeGuards(), pressed, 40f);
            Assert.IsTrue(fell.HeroFell);
            Assert.IsFalse(pressed.HeroUp);
            Assert.AreEqual(120f - 20f, pressed.HeroDownLeft, 0.01f);
            Leak.Resolve(level, Load(), BalanceRules.Default, new SiegeGuards { WalkStream = 2000f, WalkAir = 200f }, pressed, 60f);
            Assert.AreEqual(40f, pressed.HeroDownLeft, 0.01f, "the clock runs on through a quiet wave");
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
        public void Evaluator_SurvivesALeakTheGuardsCanClear_ButNotAnEmptyField()
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
            Assert.IsTrue(one.Holds, "the gun at the doorstep clears the leak while the base takes it");
            Assert.Less(one.BaseLeft, level.BaseHealth);
            Assert.Greater(one.Stars, 0);
            Assert.AreEqual(one.AnalyticScore, Leak.Score(true, one.Stars, one.BaseLeft, one.CoinsLeft, 2, 2), 0.01f);
        }

        // A level without a map (ideal spots: the gun reaches the doorstep), one gun, a base that takes a leak but not two undefended waves.
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
