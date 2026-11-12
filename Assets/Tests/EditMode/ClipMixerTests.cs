using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 47: the blending behind every animated model. A blend must never jump (the pose would pop), the clips on top must
    /// keep their clocks exactly (attack hits, power releases and footfalls are timed on them), and a raised game speed must
    /// stretch only the fades, not the clips.
    /// </summary>
    public sealed class ClipMixerTests
    {
        private const int Idle = 0;
        private const int Walk = 1;
        private const int Jog = 2;
        private const int Hit = 3;
        private const int Strike = 4;
        private const float Step = 1f / 60f;
        // The most any slot's weight may change in one 60 fps frame: a 0.08 s eased fade moves at most ~0.31 per frame.
        private const float MaxWeightStep = 0.35f;

        private static ClipMixer Create(float strikeBlend = 0f)
        {
            return new ClipMixer(
                new[] { 2f, 1f, 0.6f, 0.5f, 1.2f },
                new[] { 1f, 1f, 1f, 1f, 1f },
                new[] { 0f, 0f, 0f, 0f, strikeBlend },
                new[] { true, true, true, false, false });
        }

        // Advances and returns the largest weight change of any slot over the frames.
        private static float Run(ClipMixer mixer, int frames, float stretch = 1f)
        {
            float[] before = new float[mixer.SlotCount];
            float largest = 0f;
            for (int f = 0; f < frames; f++)
            {
                for (int s = 0; s < mixer.SlotCount; s++)
                {
                    before[s] = mixer.WeightOf(s);
                }

                mixer.Advance(Step, stretch);
                for (int s = 0; s < mixer.SlotCount; s++)
                {
                    largest = Mathf.Max(largest, Mathf.Abs(mixer.WeightOf(s) - before[s]));
                }
            }

            return largest;
        }

        private static float Total(ClipMixer mixer)
        {
            float total = 0f;
            for (int s = 0; s < mixer.SlotCount; s++)
            {
                total += mixer.WeightOf(s);
            }

            return total;
        }

        private static float ClipWeight(ClipMixer mixer, int clip)
        {
            float total = 0f;
            for (int s = 0; s < mixer.SlotCount; s++)
            {
                if (mixer.ClipOf(s) == clip)
                {
                    total += mixer.WeightOf(s);
                }
            }

            return total;
        }

        [Test]
        public void EchoSlots_OnlyForClipsThatDoNotLoop()
        {
            ClipMixer mixer = Create();
            Assert.AreEqual(7, mixer.SlotCount);
            Assert.AreEqual(Hit, mixer.ClipOf(5));
            Assert.AreEqual(Strike, mixer.ClipOf(6));
        }

        [Test]
        public void FirstCycle_ShowsAtOnce()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.Advance(Step, 1f);
            Assert.AreEqual(1f, ClipWeight(mixer, Idle), 1e-5f);
        }

        [Test]
        public void CycleChange_CrossFadesWithoutAJump()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            Run(mixer, 5);
            mixer.Loop(Walk, 1.5f, 0f);
            float largest = Run(mixer, 30);
            Assert.Less(largest, MaxWeightStep / 2f);
            Assert.AreEqual(1f, ClipWeight(mixer, Walk), 1e-5f);
            Assert.AreEqual(1f, Total(mixer), 1e-4f);
        }

        [Test]
        public void CycleChangedAgainMidFade_KeepsTheHalfShownCycle()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            Run(mixer, 5);
            mixer.Loop(Walk, 1f, 0f);
            Run(mixer, 6);
            float walkBefore = ClipWeight(mixer, Walk);
            mixer.Loop(Jog, 1f, 0f);
            mixer.Advance(Step, 1f);
            Assert.Greater(walkBefore, 0.2f);
            Assert.Greater(ClipWeight(mixer, Walk), walkBefore - MaxWeightStep / 2f, "the half-shown walk must fade, not vanish");
            Assert.AreEqual(1f, Total(mixer), 1e-4f);
            Assert.Less(Run(mixer, 30), MaxWeightStep / 2f);
        }

        [Test]
        public void CycleTakenBackMidFade_ResumesItsOwnClock()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Walk, 1f, 0f);
            Run(mixer, 30);
            mixer.Loop(Idle, 1f, 0f);
            Run(mixer, 3);
            mixer.Loop(Walk, 1f, 0f);
            mixer.Advance(Step, 1f);
            Assert.AreEqual(34 * Step, mixer.TimeOf(Walk), 1e-4f, "the walk was still showing, so it carries on instead of restarting");
        }

        [Test]
        public void SameClipRestarting_CrossFadesOnItsEcho()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.PlayOnce(Hit, 0f, false, 1f);
            Run(mixer, 12);
            mixer.PlayOnce(Hit, 0f, false, 1f);
            float largest = Run(mixer, 1);
            Assert.Less(largest, MaxWeightStep);
            Assert.Greater(mixer.WeightOf(Hit) + mixer.WeightOf(5), 0.85f, "the old flinch still shows while the new one fades in");
            Assert.AreEqual(Step, mixer.OnceTime, 1e-5f, "the new flinch starts from its first frame");
            Assert.Less(Run(mixer, 40), MaxWeightStep);
        }

        [Test]
        public void SameClipAskedAgainWhileFadingIn_RestartsInPlace()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.PlayOnce(Hit, 0f, false, 1f);
            Run(mixer, 2);
            float weight = mixer.WeightOf(Hit);
            mixer.PlayOnce(Hit, 0f, false, 1f);
            mixer.Advance(Step, 1f);
            Assert.AreEqual(0f, mixer.WeightOf(5), "no second run on the echo");
            Assert.Greater(mixer.WeightOf(Hit), weight, "the fade-in carries on");
            Assert.AreEqual(Step, mixer.OnceTime, 1e-5f, "the clock restarted");
        }

        [Test]
        public void ClipReplacingTheOneOnTop_NeverShowsTheCycleBetween()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.PlayOnce(Hit, 0f, false, 1f);
            Run(mixer, 10);
            mixer.PlayOnce(Strike, 0f, false, 1f);
            for (int f = 0; f < 10; f++)
            {
                mixer.Advance(Step, 1f);
                Assert.Less(ClipWeight(mixer, Idle), 0.2f, "frame " + f);
            }
        }

        [Test]
        public void ClipOnTop_KeepsItsClockAndEndsWithTheClip()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.PlayOnce(Strike, 0.25f, false, 1.5f);
            for (int f = 0; f < 10; f++)
            {
                mixer.Advance(Step, 3f);
            }

            Assert.AreEqual(0.25f + 10 * Step * 1.5f, mixer.OnceTime, 1e-4f, "a raised game speed stretches the fades, never the clip");
            float previous = mixer.OnceTime;
            for (int f = 0; f < 120 && mixer.Once == Strike; f++)
            {
                previous = mixer.OnceTime;
                mixer.Advance(Step, 1f);
            }

            Assert.AreEqual(-1, mixer.Once);
            Assert.Less(previous, 1.2f, "still on top while the clip runs");
            Assert.GreaterOrEqual(previous + Step * 1.5f, 1.2f - 1e-4f, "gone on the frame the clip reaches its end");
        }

        [Test]
        public void RaisedGameSpeed_StretchesTheBlend()
        {
            ClipMixer normal = Create();
            ClipMixer fast = Create();
            foreach (ClipMixer mixer in new[] { normal, fast })
            {
                mixer.Loop(Idle, 1f, 0f);
                mixer.Advance(Step, 1f);
                mixer.Loop(Walk, 1f, 0f);
            }

            normal.Advance(Step * 3f, 1f);
            fast.Advance(Step * 3f, 3f);
            Assert.Less(ClipWeight(fast, Walk), ClipWeight(normal, Walk));
            Assert.AreEqual(normal.TimeOf(Walk), fast.TimeOf(Walk), 1e-6f);
        }

        [Test]
        public void ClipBlendSecondsFromData_SetsTheFade()
        {
            ClipMixer quick = Create();
            ClipMixer slow = Create(0.4f);
            foreach (ClipMixer mixer in new[] { quick, slow })
            {
                mixer.Loop(Idle, 1f, 0f);
                mixer.PlayOnce(Strike, 0f, false, 1f);
                Run(mixer, 6);
            }

            Assert.AreEqual(1f, ClipWeight(quick, Strike), 1e-4f);
            Assert.Less(ClipWeight(slow, Strike), 0.6f);
        }

        [Test]
        public void HeldClip_StaysOnItsLastFrame()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Idle, 1f, 0f);
            mixer.PlayOnce(Hit, 0f, true, 1f);
            Run(mixer, 120);
            Assert.AreEqual(Hit, mixer.Once);
            Assert.AreEqual(0.5f, mixer.OnceTime, 1e-5f);
            Assert.AreEqual(1f, ClipWeight(mixer, Hit), 1e-4f);
        }

        [Test]
        public void CopyFrom_TakesOverThePose()
        {
            ClipMixer body = Create();
            body.Loop(Walk, 1.3f, 0.2f);
            body.PlayOnce(Hit, 0f, false, 1f);
            Run(body, 4);
            ClipMixer corpse = Create();
            corpse.Loop(Idle, 1f, 0f);
            corpse.CopyFrom(body);
            for (int s = 0; s < body.SlotCount; s++)
            {
                Assert.AreEqual(body.WeightOf(s), corpse.WeightOf(s), 1e-6f);
                Assert.AreEqual(body.TimeOf(s), corpse.TimeOf(s), 1e-6f);
            }

            corpse.PlayOnce(Strike, 0f, true, 1f);
            Assert.Less(Run(corpse, 30), MaxWeightStep);
        }

        [Test]
        public void Restart_ClearsEverything()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Walk, 1f, 0f);
            mixer.PlayOnce(Hit, 0f, true, 1f);
            Run(mixer, 10);
            mixer.Restart();
            mixer.Advance(Step, 1f);
            Assert.AreEqual(-1, mixer.Once);
            Assert.AreEqual(-1, mixer.Cycle);
            Assert.AreEqual(0f, Total(mixer), 1e-6f);
            mixer.Loop(Idle, 1f, 0f);
            mixer.Advance(Step, 1f);
            Assert.AreEqual(1f, ClipWeight(mixer, Idle), 1e-5f, "the first cycle after a restart shows at once");
        }

        // Round 50: the ray boss keeps its tail on aim while the beam burns by holding its power clip where it is.
        [Test]
        public void OnceRateZero_HoldsTheClipOnItsFrame_AndOneLetsItPlayOut()
        {
            ClipMixer mixer = Create();
            mixer.Loop(Walk, 1f, 0f);
            mixer.PlayOnce(Strike, 0f, false, 1f);
            Run(mixer, 30);
            float held = mixer.OnceTime;
            mixer.SetOnceRate(0f);
            Run(mixer, 240);
            Assert.AreEqual(Strike, mixer.Once, "a held clip stays on top");
            Assert.AreEqual(held, mixer.OnceTime, 1e-5f, "a held clip keeps its frame");
            Assert.AreEqual(1f, ClipWeight(mixer, Strike), 1e-4f, "a held clip keeps its full weight");
            mixer.SetOnceRate(1f);
            Run(mixer, 120);
            Assert.AreEqual(-1, mixer.Once, "released, the clip plays out and fades back to the cycle");
        }
    }
}
