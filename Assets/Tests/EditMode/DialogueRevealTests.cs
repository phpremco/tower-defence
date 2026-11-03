using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class DialogueRevealTests
    {
        [Test]
        public void RevealClock_ShowsCharactersAtItsSpeed_AndStopsAtTheEnd()
        {
            var clock = new RevealClock(10f);
            clock.Restart(25);

            Assert.AreEqual(0, clock.Visible);
            clock.Advance(0.55f);
            Assert.AreEqual(5, clock.Visible);
            Assert.IsFalse(clock.IsComplete);
            clock.Advance(10f);
            Assert.AreEqual(25, clock.Visible);
            Assert.IsTrue(clock.IsComplete);
        }

        [Test]
        public void RevealClock_FinishShowsEverything_AndRestartHidesItAgain()
        {
            var clock = new RevealClock(45f);
            clock.Restart(100);
            clock.Finish();
            Assert.AreEqual(100, clock.Visible);

            clock.Restart(7);
            Assert.AreEqual(0, clock.Visible);
            clock.Advance(-1f);
            Assert.AreEqual(0, clock.Visible, "negative time never rewinds");
        }

        [Test]
        public void Glitch_IsDeterministic_AndOffAtZeroIntensity()
        {
            for (int i = 0; i < 200; i++)
            {
                Assert.IsFalse(DialogueGlitch.IsScrambled(0f, 7, i, 3));
                Assert.AreEqual(DialogueGlitch.IsScrambled(1f, 7, i, 3), DialogueGlitch.IsScrambled(1f, 7, i, 3));
                Assert.AreEqual(DialogueGlitch.Glyph(7, i, 3), DialogueGlitch.Glyph(7, i, 3));
            }

            Assert.AreEqual(Vector2.zero, DialogueGlitch.Jitter(0f, 7, 3));
        }

        [Test]
        public void Glitch_AtFullStrength_ScramblesSomeButKeepsMostReadable()
        {
            const int characters = 2000;
            int scrambled = 0;
            for (int i = 0; i < characters; i++)
            {
                scrambled += DialogueGlitch.IsScrambled(1f, 11, i, 5) ? 1 : 0;
            }

            Assert.Greater(scrambled, characters / 10);
            Assert.Less(scrambled, characters / 2);
        }

        [Test]
        public void Glitch_JitterStaysWithinIntensity_AndChangesPerTick()
        {
            bool changed = false;
            Vector2 first = DialogueGlitch.Jitter(0.5f, 3, 0);
            for (int tick = 0; tick < 100; tick++)
            {
                Vector2 jitter = DialogueGlitch.Jitter(0.5f, 3, tick);
                Assert.LessOrEqual(Mathf.Abs(jitter.x), 0.5f);
                Assert.LessOrEqual(Mathf.Abs(jitter.y), 0.5f);
                changed |= jitter != first;
            }

            Assert.IsTrue(changed);
        }
    }
}
