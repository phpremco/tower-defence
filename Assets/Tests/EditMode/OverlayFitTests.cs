using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Overlay panels laid out at the 1920x1080 reference size stay inside the safe area (round 23c).</summary>
    public sealed class OverlayFitTests
    {
        private static readonly Vector2 ResultPanel = new Vector2(1180f, 860f);
        private const float Margin = 24f;

        [Test]
        public void SixteenByNine_KeepsThePanelAtFullSize()
        {
            Assert.AreEqual(1f, FitInsideParent.FitScale(ResultPanel, new Vector2(1920f, 1080f), Margin));
        }

        [Test]
        public void NarrowWindow_ShrinksThePanelInsideItsWidth()
        {
            // A docked Game view in portrait leaves the safe area narrower than the panel in reference units.
            var space = new Vector2(1000f, 1600f);
            float scale = FitInsideParent.FitScale(ResultPanel, space, Margin);

            Assert.Less(scale, 1f);
            Assert.LessOrEqual(ResultPanel.x * scale, space.x - 2f * Margin + 0.01f);
        }

        [Test]
        public void ShortWindow_ShrinksThePanelInsideItsHeight()
        {
            var space = new Vector2(2600f, 700f);
            float scale = FitInsideParent.FitScale(ResultPanel, space, Margin);

            Assert.LessOrEqual(ResultPanel.y * scale, space.y - 2f * Margin + 0.01f);
        }

        [Test]
        public void NeverScalesUp_NorBelowZero()
        {
            Assert.AreEqual(1f, FitInsideParent.FitScale(ResultPanel, new Vector2(4000f, 3000f), Margin));
            Assert.AreEqual(0f, FitInsideParent.FitScale(ResultPanel, new Vector2(10f, 10f), Margin));
        }
    }
}
