using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// World labels and the HUD: a label clear of every panel stays put, one over the tower bar moves up just above
    /// it, one over a side panel moves sideways, a label in a gap too small for it is hidden, and nothing is ever
    /// pushed off screen. Sizes are the HUD's own at the 1920x1080 reference and a 720p-like short screen.
    /// </summary>
    public sealed class HudKeepOutTests
    {
        private static readonly Rect FullHd = new Rect(-960f, -540f, 1920f, 1080f);
        private static readonly Rect BottomBar = Rect.MinMaxRect(-960f, -540f, 960f, -540f + 156f);
        private static readonly Rect TopBar = Rect.MinMaxRect(-960f, 540f - 72f, 960f, 540f);
        private static readonly Rect HeroBar = Rect.MinMaxRect(-940f, -200f, -760f, 200f);
        private static readonly Vector2 Badge = new Vector2(230f, 54f);

        private static List<Rect> Hud() => new List<Rect> { TopBar, BottomBar, HeroBar };

        private static Rect BadgeAt(float x, float y) => new Rect(x - Badge.x * 0.5f, y, Badge.x, Badge.y);

        [Test]
        public void LabelClearOfThePanelsStaysWhereItIs()
        {
            Assert.IsTrue(KeepOutMath.Resolve(BadgeAt(0f, 0f), Hud(), FullHd, out Vector2 offset));
            Assert.AreEqual(Vector2.zero, offset);
        }

        [Test]
        public void LabelOverTheTowerBarMovesJustAboveIt()
        {
            Rect label = BadgeAt(200f, -500f);
            Assert.IsTrue(KeepOutMath.Resolve(label, Hud(), FullHd, out Vector2 offset));
            Assert.AreEqual(0f, offset.x, 0.001f, "A full-width bar is left upwards, never sideways.");
            Assert.AreEqual(BottomBar.yMax, label.yMin + offset.y, 1f, "The badge sits on the bar's upper edge.");
            Assert.IsFalse(new Rect(label.position + offset, label.size).Overlaps(BottomBar));
        }

        [Test]
        public void LabelOverTheTopBarMovesDown()
        {
            Rect label = BadgeAt(0f, 500f);
            Assert.IsTrue(KeepOutMath.Resolve(label, Hud(), FullHd, out Vector2 offset));
            Assert.Less(offset.y, 0f);
            Assert.AreEqual(TopBar.yMin, label.yMax + offset.y, 1f);
        }

        [Test]
        public void LabelOverASidePanelMovesSideways()
        {
            Rect label = BadgeAt(-800f, 0f);
            Assert.IsTrue(KeepOutMath.Resolve(label, Hud(), FullHd, out Vector2 offset));
            Assert.Greater(offset.x, 0f);
            Assert.AreEqual(0f, offset.y, 0.001f);
            Assert.AreEqual(HeroBar.xMax, label.xMin + offset.x, 1f);
        }

        [Test]
        public void StackedPanelsArePassedOneAfterTheOther()
        {
            // The money line sits right on the bar: a badge pushed off the bar lands on the money and moves on above it.
            Rect money = Rect.MinMaxRect(-940f, BottomBar.yMax, -600f, BottomBar.yMax + 44f);
            var zones = new List<Rect> { BottomBar, money };
            Rect label = BadgeAt(-800f, -520f);
            Assert.IsTrue(KeepOutMath.Resolve(label, zones, FullHd, out Vector2 offset));
            Rect placed = new Rect(label.position + offset, label.size);
            Assert.IsFalse(placed.Overlaps(BottomBar));
            Assert.IsFalse(placed.Overlaps(money));
        }

        [Test]
        public void LabelWithNoRoomLeftIsHidden()
        {
            // A short screen where the two bars leave less than a badge's height between them.
            Rect shortArea = new Rect(-640f, -60f, 1280f, 120f);
            var zones = new List<Rect> { Rect.MinMaxRect(-640f, -60f, 640f, -10f), Rect.MinMaxRect(-640f, 20f, 640f, 60f) };
            Assert.IsFalse(KeepOutMath.Resolve(BadgeAt(0f, -40f), zones, shortArea, out _));
        }

        [Test]
        public void LabelIsNeverPushedOffScreen()
        {
            Rect label = BadgeAt(940f, -530f);
            Assert.IsTrue(KeepOutMath.Resolve(label, Hud(), FullHd, out Vector2 offset));
            Rect placed = new Rect(label.position + offset, label.size);
            Assert.LessOrEqual(placed.xMax, FullHd.xMax + 1f);
            Assert.GreaterOrEqual(placed.yMin, BottomBar.yMax - 1f);
        }

        [Test]
        public void ClampIntoKeepsALabelOnScreen()
        {
            Rect placed = KeepOutMath.ClampInto(BadgeAt(1000f, 600f), FullHd);
            Assert.AreEqual(FullHd.xMax, placed.xMax, 0.001f);
            Assert.AreEqual(FullHd.yMax, placed.yMax, 0.001f);
        }
    }
}
