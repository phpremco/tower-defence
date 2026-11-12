using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 48: the opening's sampled rigs blend a clip change and average a heading from the moment alone: the fade share,
    /// the window the heading is averaged over, and the pose snapshot a blend starts from.
    /// </summary>
    public sealed class CutsceneBlendTests
    {
        [Test]
        public void Fade_EasesFromZeroToOne_AndHasNoLengthWhenZero()
        {
            Assert.AreEqual(0f, CutsceneMath.Fade(0f, 0.25f), 1e-6f);
            Assert.AreEqual(0.5f, CutsceneMath.Fade(0.125f, 0.25f), 1e-6f);
            Assert.AreEqual(1f, CutsceneMath.Fade(0.25f, 0.25f), 1e-6f);
            Assert.AreEqual(1f, CutsceneMath.Fade(3f, 0.25f), 1e-6f);
            Assert.AreEqual(0f, CutsceneMath.Fade(-1f, 0.25f), 1e-6f, "before the change the old beat shows");
            Assert.AreEqual(1f, CutsceneMath.Fade(0f, 0f), 1e-6f, "no fade length cuts");
            Assert.Less(CutsceneMath.Fade(0.01f, 0.25f), 0.01f, "eased in: no jump at the start");
        }

        [Test]
        public void Window_SamplesAFixedGridAcrossTheWindow()
        {
            const int count = 8;
            for (int k = 0; k <= count; k++)
            {
                float at = CutsceneMath.WindowAt(k, count, 1.03f, 0.5f);
                Assert.AreEqual(0f, Mathf.Repeat(at + 1e-4f, 0.0625f), 2e-4f, "on the grid");
                Assert.GreaterOrEqual(at, 1.03f - 0.25f - 1e-5f);
                Assert.LessOrEqual(at, 1.03f + 0.25f + 0.0625f);
            }

            Assert.AreEqual(1f, CutsceneMath.WindowWeight(2f, 2f, 0.5f), 1e-6f, "the moment itself counts most");
            Assert.AreEqual(0.5f, CutsceneMath.WindowWeight(2.125f, 2f, 0.5f), 1e-6f);
            Assert.AreEqual(0f, CutsceneMath.WindowWeight(2.25f, 2f, 0.5f), 1e-6f, "nothing at the edge, so a sample enters without a jump");
            Assert.AreEqual(2f, CutsceneMath.WindowAt(3, count, 2f, 0f), 1e-6f, "no width: the moment only");
        }

        [Test]
        public void Window_TurnsAStepInHeadingIntoARamp()
        {
            // A heading that switches from +x to +z at t = 1: averaged over a window it comes round over the window, never in a step.
            const int count = 8;
            const float width = 0.5f;
            const float frame = 1f / 60f;
            float previous = Yaw(0.5f, count, width);
            for (float t = 0.5f + frame; t < 1.5f; t += frame)
            {
                float yaw = Yaw(t, count, width);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(previous, yaw)), 10f, $"no snap at {t}");
                previous = yaw;
            }

            Assert.AreEqual(90f, Yaw(0.6f, count, width), 1e-3f);
            Assert.AreEqual(0f, Yaw(1.4f, count, width), 1e-3f);
        }

        [Test]
        public void HeadingBlend_AveragesByAngle_SoOppositeHeadingsTurnInsteadOfCancelling()
        {
            var opposite = new HeadingBlend();
            opposite.Add(Vector3.forward, 1f);
            opposite.Add(Vector3.back * 0.999f + Vector3.right * 0.001f, 1f);
            Assert.IsTrue(opposite.HasHeading);
            Assert.AreEqual(90f, Mathf.Abs(Mathf.DeltaAngle(0f, opposite.Yaw)), 1f, "half way round, not a zero vector");

            var across = new HeadingBlend();
            across.Add(Quaternion.Euler(0f, 170f, 0f) * Vector3.forward, 1f);
            across.Add(Quaternion.Euler(0f, -170f, 0f) * Vector3.forward, 1f);
            Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(0f, across.Yaw)), 1e-3f, "the short way across 180 deg");

            var weighted = new HeadingBlend();
            weighted.Add(Vector3.forward * 3f, 1f);
            weighted.Add(Vector3.right, 1f);
            Assert.AreEqual(22.5f, weighted.Yaw, 1e-3f, "a longer (firmer) heading counts more");

            var none = new HeadingBlend();
            none.Add(Vector3.up, 1f);
            none.Add(Vector3.forward, 0f);
            Assert.IsFalse(none.HasHeading);
        }

        // Round 57: a builder's legs carry yaws unwrapped along its whole flight; averaged as given, a turn the track settled the
        // long way round stays that way (adding them as directions would pick the short way and flip where they cross 180).
        [Test]
        public void AddYaw_AveragesAnUnwrappedTrackAsGiven()
        {
            var track = new HeadingBlend();
            track.AddYaw(170f, 1f);
            track.AddYaw(370f, 1f);
            Assert.AreEqual(270f, track.Yaw, 1e-3f);

            var none = new HeadingBlend();
            none.AddYaw(40f, 0f);
            Assert.IsFalse(none.HasHeading, "a zero weight adds nothing");
        }

        // Round 57: the hauler's rotors spin up from rest instead of jumping to full speed at lift-off: the turn per frame grows
        // evenly over the ramp (never by more than the ramp's own step) and then holds at full speed.
        [Test]
        public void SpinUp_EasesTheRotorsInThenRunsAtFullSpeed()
        {
            const float ramp = 0.8f;
            const float frame = 1f / 60f;
            const float epsilon = 1e-5f;
            Assert.AreEqual(0f, CutsceneMath.SpinUp(0f, ramp));
            Assert.AreEqual(0f, CutsceneMath.SpinUp(-1f, ramp), "before lift-off the rotors stand still");
            float previous = 0f;
            float previousStep = 0f;
            for (int i = 1; i <= 120; i++)
            {
                float now = CutsceneMath.SpinUp(i * frame, ramp);
                float step = now - previous;
                Assert.GreaterOrEqual(step, previousStep - epsilon, $"slows down at frame {i}");
                Assert.LessOrEqual(step - previousStep, frame * frame / ramp + epsilon, $"jumps in speed at frame {i}");
                previous = now;
                previousStep = step;
            }

            Assert.AreEqual(frame, previousStep, epsilon, "full speed after the ramp");
            Assert.AreEqual(1.6f - ramp * 0.5f, CutsceneMath.SpinUp(1.6f, ramp), epsilon);
            Assert.AreEqual(1.6f, CutsceneMath.SpinUp(1.6f, 0f), epsilon, "no ramp: full speed at once");
        }

        [Test]
        public void PoseSnapshot_BlendsFromTheCapturedPose()
        {
            var root = new GameObject("Root").transform;
            var bone = new GameObject("Bone").transform;
            bone.SetParent(root, false);
            try
            {
                var snapshot = new PoseSnapshot(root);
                bone.localPosition = Vector3.zero;
                bone.localRotation = Quaternion.identity;
                snapshot.Capture();
                bone.localPosition = new Vector3(2f, 0f, 0f);
                bone.localRotation = Quaternion.Euler(0f, 90f, 0f);
                snapshot.BlendFromCaptured(0.5f);
                Assert.AreEqual(1f, bone.localPosition.x, 1e-5f);
                Assert.AreEqual(45f, Quaternion.Angle(Quaternion.identity, bone.localRotation), 1e-3f);

                snapshot.Capture();
                bone.localPosition = new Vector3(5f, 0f, 0f);
                snapshot.BlendFromCaptured(0f);
                Assert.AreEqual(1f, bone.localPosition.x, 1e-5f, "share 0 is the captured pose");
                bone.localPosition = new Vector3(5f, 0f, 0f);
                snapshot.BlendFromCaptured(1f);
                Assert.AreEqual(5f, bone.localPosition.x, 1e-5f, "share 1 leaves the new pose");
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        // The yaw of a heading that is +x before t = 1 and +z after, averaged over the window round t.
        private static float Yaw(float t, int count, float width)
        {
            Vector3 sum = Vector3.zero;
            for (int k = 0; k <= count; k++)
            {
                float at = CutsceneMath.WindowAt(k, count, t, width);
                sum += (at < 1f ? Vector3.right : Vector3.forward) * CutsceneMath.WindowWeight(at, t, width);
            }

            return Mathf.Atan2(sum.x, sum.z) * Mathf.Rad2Deg;
        }
    }
}
