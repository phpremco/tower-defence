using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 73: calling the next wave early. The bonus for the seconds saved, the cooldown between calls, the moments a call is
    /// refused (a boss intro, a story box, the briefing, the opening build time, a wave still coming out), and the wave
    /// timeline with waves on top of each other: the level is won only when every wave has started and spawned and nothing lives.
    /// </summary>
    public sealed class WaveCallTests
    {
        private const float Delay = 8f;
        private const float FirstDelay = 10f;
        private static readonly WaveCallSituation Live = new WaveCallSituation(true, false, false, false);

        private WaveCallDefinition _rules;

        [SetUp]
        public void CreateRules() => _rules = WaveCallDefinition.Create(1f, 8, 4f, false);

        [TearDown]
        public void DestroyRules() => Object.DestroyImmediate(_rules);

        private static WaveClock ThreeWaves() => new WaveClock(new[] { FirstDelay, Delay, Delay });

        // The clock after the first wave has started and fully spawned: the countdown to wave 2 runs.
        private static WaveClock BetweenFirstAndSecond()
        {
            WaveClock clock = ThreeWaves();
            clock.BeginWave();
            clock.EndSpawning();
            return clock;
        }

        [TestCase(0f, 0)]
        [TestCase(-1f, 0)]
        [TestCase(0.9f, 0)]
        [TestCase(5.3f, 5)]
        [TestCase(8f, 8)]
        [TestCase(30f, 8)]
        public void Bonus_PaysCoinsPerSecondSaved_RoundedDown_AndCapped(float seconds, int expected)
        {
            Assert.AreEqual(expected, _rules.BonusFor(seconds));
        }

        [Test]
        public void Bonus_FollowsTheRate()
        {
            WaveCallDefinition half = WaveCallDefinition.Create(0.5f, 100, 0f, false);
            Assert.AreEqual(3, half.BonusFor(7.9f));
            Object.DestroyImmediate(half);
        }

        [Test]
        public void Accept_PaysTheBonus_CountsTheCall_AndStartsTheCooldown()
        {
            var caller = new WaveCaller(_rules);
            WaveClock clock = BetweenFirstAndSecond();
            Assert.AreEqual(WaveCallBlock.None, caller.Check(Live, clock));

            Assert.AreEqual(6, caller.Accept(6.5f));
            Assert.AreEqual(1, caller.Calls);
            Assert.AreEqual(6, caller.BonusPaid);
            Assert.AreEqual(4f, caller.CooldownLeft, 1e-5f);
        }

        [Test]
        public void Cooldown_RefusesCalls_UntilItHasRunOut()
        {
            var caller = new WaveCaller(_rules);
            WaveClock clock = BetweenFirstAndSecond();
            caller.Accept(Delay);

            Assert.AreEqual(WaveCallBlock.Cooldown, caller.Check(Live, clock), "mashing the button does not send another wave");
            caller.Tick(3.9f);
            Assert.AreEqual(WaveCallBlock.Cooldown, caller.Check(Live, clock));
            caller.Tick(0.2f);
            Assert.AreEqual(0f, caller.CooldownLeft);
            Assert.AreEqual(WaveCallBlock.None, caller.Check(Live, clock));
        }

        [Test]
        public void NotCallable_DuringABossIntro_AStoryBox_TheBriefing_OrAfterTheEnd()
        {
            var caller = new WaveCaller(_rules);
            WaveClock clock = BetweenFirstAndSecond();

            Assert.AreEqual(WaveCallBlock.BeatPlaying, caller.Check(new WaveCallSituation(true, false, false, true), clock));
            Assert.AreEqual(WaveCallBlock.InputBlocked, caller.Check(new WaveCallSituation(true, true, false, false), clock));
            Assert.AreEqual(WaveCallBlock.NotDeployed, caller.Check(new WaveCallSituation(true, false, true, false), clock));
            Assert.AreEqual(WaveCallBlock.LevelOver, caller.Check(new WaveCallSituation(false, false, false, false), clock));
        }

        [Test]
        public void Situation_ReadsTheBossIntroHold_AndTheClockHold_OffTheContext()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var context = new LevelContext(level, null, null, null, null, null, null, null, null, null, 3, false);
            var intro = new object();
            var other = new object();
            try
            {
                Assert.IsFalse(WaveCallSituation.Of(context).BeatPlaying);
                context.HoldWaveCall(intro);
                context.HoldWaveCall(other);
                context.ReleaseWaveCall(intro);
                Assert.IsTrue(WaveCallSituation.Of(context).BeatPlaying, "another holder still holds the call");
                context.ReleaseWaveCall(other);
                Assert.IsFalse(WaveCallSituation.Of(context).BeatPlaying);

                context.Held = true;
                Assert.IsTrue(WaveCallSituation.Of(context).InputBlocked, "a dialogue, the codex or the recon holds the clock");
                context.Held = false;
                context.BriefingPending = true;
                Assert.IsTrue(WaveCallSituation.Of(context).NotDeployed);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void FirstWave_IsBuildTime_UnlessTheDataAllowsTheCall()
        {
            WaveClock clock = ThreeWaves();
            Assert.AreEqual(WaveCallBlock.BeforeFirstWave, new WaveCaller(_rules).Check(Live, clock));

            WaveCallDefinition open = WaveCallDefinition.Create(1f, 8, 4f, true);
            Assert.AreEqual(WaveCallBlock.None, new WaveCaller(open).Check(Live, clock));
            Object.DestroyImmediate(open);
        }

        [Test]
        public void NotCallable_WhileAWaveSpawns_OrAfterTheLastWaveStarted()
        {
            var caller = new WaveCaller(_rules);
            WaveClock clock = ThreeWaves();
            clock.BeginWave();
            Assert.AreEqual(WaveCallBlock.WaveSpawning, caller.Check(Live, clock), "a wave is never cut off");
            clock.EndSpawning();
            clock.BeginWave();
            clock.EndSpawning();
            clock.BeginWave();
            clock.EndSpawning();
            Assert.AreEqual(WaveCallBlock.NoWaveLeft, caller.Check(Live, clock));
        }

        [Test]
        public void Clock_CountsDownBetweenWaves_OnlyAfterAWaveHasSpawned()
        {
            WaveClock clock = ThreeWaves();
            Assert.IsFalse(clock.Tick(FirstDelay - 0.5f));
            Assert.IsTrue(clock.Tick(0.5f), "the first wave is due when its countdown runs out");
            Assert.AreEqual(0, clock.BeginWave());
            Assert.IsFalse(clock.Tick(100f), "no countdown while the wave spawns");
            clock.EndSpawning();
            Assert.AreEqual(Delay, clock.Countdown);
            Assert.IsTrue(clock.CountingDown);
        }

        [Test]
        public void OverlappingWaves_TheLevelIsWonOnlyWhenTheLastWaveHasSpawned_AndNothingLives()
        {
            WaveClock clock = BetweenFirstAndSecond();
            const int firstWaveAlive = 5;

            // Wave 2 called early on top of wave 1's survivors.
            Assert.AreEqual(1, clock.BeginWave());
            Assert.IsFalse(clock.IsCleared(firstWaveAlive));
            Assert.IsFalse(clock.IsCleared(0), "wave 2 still spawning");
            clock.EndSpawning();
            Assert.IsFalse(clock.IsCleared(0), "wave 3 has not started, even with the field empty");

            // Wave 3 called at once as well: all three waves are on the field together.
            Assert.AreEqual(2, clock.BeginWave());
            Assert.IsTrue(clock.AllStarted);
            Assert.IsFalse(clock.IsCleared(0), "the last wave is still coming out");
            clock.EndSpawning();
            Assert.AreEqual(0f, clock.Countdown, "no countdown after the last wave");
            Assert.IsFalse(clock.IsCleared(firstWaveAlive + 7), "survivors of every wave keep the level going");
            Assert.IsTrue(clock.IsCleared(0));
        }

        [Test]
        public void Clock_WithoutEarlyCalls_RunsTheSameAsBefore()
        {
            // The waiting player's timeline (the balance baseline): each countdown starts when the previous wave has spawned.
            WaveClock clock = ThreeWaves();
            float elapsed = 0f;
            const float step = 0.2f;
            int started = 0;
            while (!clock.AllStarted)
            {
                elapsed += step;
                if (clock.Tick(step))
                {
                    clock.BeginWave();
                    started++;
                    clock.EndSpawning();
                }
            }

            Assert.AreEqual(3, started);
            // Each start lands on the first step at or past its countdown, so the timeline may run up to a step late per wave.
            Assert.AreEqual(FirstDelay + Delay + Delay, elapsed, step * (started + 1));
        }
    }
}
