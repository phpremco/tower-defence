using System.Collections;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Round 73: calling the next wave early in a real level (Landing Site, Easy). The opening countdown is build time; a call
    /// between waves starts the next wave on top of the creatures already on the field and pays the bonus; a second press
    /// right after does nothing; a held call (a boss intro) is refused; and a base that falls with two waves on the field
    /// loses the level for good. Run on a worker clone: it plays the campaign level with the clone's own save.
    /// </summary>
    public sealed class WaveCallPlayTests
    {
        private const int LandingSite = 0;
        private const int Easy = 0;
        private const float StepSeconds = 0.2f;
        private const float TimeoutGameSeconds = 400f;

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator CallingEarly_OverlapsTheWaves_PaysTheBonus_AndALostBaseStaysLost()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[LandingSite].Level, catalog.Difficulties[Easy], seed: 1));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
            Assert.IsTrue(Services.TryGet(out LevelContext context), "the level did not build");
            WaveCallDefinition rules = catalog.WaveCall;
            Assert.IsNotNull(rules, "the campaign carries the wave call rules");
            Time.captureDeltaTime = StepSeconds;

            yield return null;
            Assert.AreEqual(WaveCallBlock.BeforeFirstWave, context.WaveCallBlock, "the opening countdown is build time");
            int money = context.Wallet.Money;
            context.CallWaveRequested.Raise();
            Assert.AreEqual(0, context.WaveNumber, "no call before the first wave");
            Assert.AreEqual(money, context.Wallet.Money);

            // Wave 1 runs its course until it has fully spawned and the countdown to wave 2 runs, its creatures on the field.
            float waited = 0f;
            while (!(context.WaveNumber == 1 && context.NextWaveCountdown > 0f) && waited < TimeoutGameSeconds)
            {
                waited += StepSeconds;
                yield return null;
            }

            Assert.AreEqual(1, context.WaveNumber, "wave 1 never finished spawning");
            Assert.AreEqual(WaveCallBlock.None, context.WaveCallBlock, "a call is open between waves");
            int alive = context.Targets.Count;
            Assert.Greater(alive, 0, "wave 1's creatures are still on the field");
            float countdown = context.NextWaveCountdown;
            int bonus = rules.BonusFor(countdown);
            Assert.Greater(bonus, 0);
            money = context.Wallet.Money;
            int paid = -1;
            System.Action<int> onCalled = value => paid = value;
            context.WaveCalledEarly.Subscribe(onCalled);
            context.CallWaveRequested.Raise();
            context.WaveCalledEarly.Unsubscribe(onCalled);

            Assert.AreEqual(2, context.WaveNumber, "wave 2 starts at once");
            Assert.AreEqual(bonus, paid);
            Assert.AreEqual(money + bonus, context.Wallet.Money, "the bonus is paid for the seconds saved");
            Assert.GreaterOrEqual(context.Targets.Count, alive, "wave 1 stays on the field: the waves overlap");
            Assert.AreEqual(WaveCallBlock.WaveSpawning, context.WaveCallBlock);
            context.CallWaveRequested.Raise();
            Assert.AreEqual(2, context.WaveNumber, "a second press right after does not dump the next wave");

            // A held call (what the boss intro does) is refused even between waves.
            var owner = new object();
            context.HoldWaveCall(owner);
            waited = 0f;
            while (!(context.WaveNumber == 2 && context.NextWaveCountdown > 0f) && waited < TimeoutGameSeconds)
            {
                waited += StepSeconds;
                yield return null;
            }

            yield return null;
            Assert.AreEqual(WaveCallBlock.BeatPlaying, context.WaveCallBlock);
            context.CallWaveRequested.Raise();
            Assert.AreEqual(2, context.WaveNumber, "no call over a boss intro");
            context.ReleaseWaveCall(owner);
            yield return null;
            Assert.AreEqual(WaveCallBlock.None, context.WaveCallBlock);
            context.CallWaveRequested.Raise();
            Assert.AreEqual(3, context.WaveNumber);

            // The base falls with waves 1-3 on the field: lost, and it stays lost while the field empties.
            context.BaseHealth.TakeDamage(context.BaseHealth.Current + context.BaseHealth.Max);
            yield return null;
            Assert.AreEqual(LevelOutcome.Lost, context.Outcome);
            Assert.AreEqual(WaveCallBlock.LevelOver, context.WaveCallBlock);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.AreEqual(LevelOutcome.Lost, context.Outcome);
        }
    }
}
