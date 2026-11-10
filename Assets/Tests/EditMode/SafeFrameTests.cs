using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Round 33: the HUD frame inside the safe area and the title-safe margin.</summary>
    public sealed class SafeFrameTests
    {
        private const float Tolerance = 1e-4f;
        private const float Width = 1920f;
        private const float Height = 1080f;

        [Test]
        public void NoMarginAndAFullSafeArea_IsTheWholeScreen()
        {
            SafeFrame.Anchors(new Rect(0f, 0f, Width, Height), Width, Height, 0f, out Vector2 min, out Vector2 max);
            AssertVector(Vector2.zero, min);
            AssertVector(Vector2.one, max);
        }

        [Test]
        public void FivePercent_MovesEveryEdgeInByFivePercent()
        {
            SafeFrame.Anchors(new Rect(0f, 0f, Width, Height), Width, Height, 5f, out Vector2 min, out Vector2 max);
            AssertVector(new Vector2(0.05f, 0.05f), min);
            AssertVector(new Vector2(0.95f, 0.95f), max);
        }

        [Test]
        public void TheSafeAreaWins_WhereItReachesFurtherInThanTheMargin()
        {
            // A notch eats 10 % on the left; the 5 % margin rules the other three edges.
            SafeFrame.Anchors(new Rect(Width * 0.1f, 0f, Width * 0.9f, Height), Width, Height, 5f, out Vector2 min, out Vector2 max);
            AssertVector(new Vector2(0.1f, 0.05f), min);
            AssertVector(new Vector2(0.95f, 0.95f), max);
        }

        [Test]
        public void TheMargin_IsClampedToTheOptionRange()
        {
            SafeFrame.Anchors(new Rect(0f, 0f, Width, Height), Width, Height, 40f, out Vector2 min, out _);
            Assert.AreEqual(SafeFrame.MaxMarginPercent / 100f, min.x, Tolerance);
            SafeFrame.Anchors(new Rect(0f, 0f, Width, Height), Width, Height, -3f, out min, out _);
            Assert.AreEqual(0f, min.x, Tolerance);
        }

        [Test]
        public void TheChoice_OverridesThePlatformDefault_UnsetKeepsIt()
        {
            Assert.AreEqual(5f, SafeFrame.Effective(-1, 5f), Tolerance, "an unset option keeps the platform's margin");
            Assert.AreEqual(0f, SafeFrame.Effective(0, 5f), Tolerance, "the player may choose no margin on a TV that shows everything");
            Assert.AreEqual(3f, SafeFrame.Effective(3, 0f), Tolerance);
            Assert.AreEqual(SafeFrame.MaxMarginPercent, SafeFrame.Effective(50, 0f), Tolerance);
        }

        [Test]
        public void CoverAnchors_MapTheFrameBackToTheScreenEdges()
        {
            var frameMin = new Vector2(0.05f, 0.1f);
            var frameMax = new Vector2(0.95f, 0.9f);
            SafeFrame.CoverAnchors(frameMin, frameMax, out Vector2 min, out Vector2 max);

            // A point at the cover's anchors, taken through the frame, lands on the screen's corners.
            Vector2 size = frameMax - frameMin;
            AssertVector(Vector2.zero, frameMin + Vector2.Scale(min, size));
            AssertVector(Vector2.one, frameMin + Vector2.Scale(max, size));
        }

        [Test]
        public void ZeroSizedScreen_FallsBackToTheWholeCanvas()
        {
            SafeFrame.Anchors(Rect.zero, 0f, 0f, 5f, out Vector2 min, out Vector2 max);
            AssertVector(Vector2.zero, min);
            AssertVector(Vector2.one, max);
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, Tolerance, $"y of {actual}");
        }
    }
}
