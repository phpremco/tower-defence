using Game.Core;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The camera pan speed: a tap moves a fixed number of screens per second at any zoom, holding speeds
    /// up to a top speed that crosses even a big field in a few seconds, the ramp only ever rises, and the player's
    /// factor scales it all and stays inside its limits.
    /// </summary>
    public sealed class PanSpeedCurveTests
    {
        private const float PlayView = 14f;
        private const float BigField = 140f;
        private const float SmallField = 30f;

        [Test]
        public void StartSpeedIsAboutAScreenPerSecond()
        {
            Assert.AreEqual(PanSpeedCurve.ScreensPerSecond * PlayView, PanSpeedCurve.Speed(PlayView, BigField, 0f, 1f), 0.001f);
            Assert.AreEqual(2f * PanSpeedCurve.Speed(PlayView, BigField, 0f, 1f), PanSpeedCurve.Speed(2f * PlayView, BigField, 0f, 1f), 0.001f, "Zoomed out twice as far pans twice as fast.");
        }

        [Test]
        public void HeldPanCrossesABigFieldInAFewSeconds()
        {
            float top = PanSpeedCurve.Speed(PlayView, BigField, PanSpeedCurve.RampSeconds, 1f);
            Assert.AreEqual(BigField / PanSpeedCurve.FieldCrossSeconds, top, 0.01f);
            Assert.LessOrEqual(BigField / top, PanSpeedCurve.FieldCrossSeconds + 0.01f);
        }

        [Test]
        public void SmallFieldStillGetsTheMinimumBoost()
        {
            float top = PanSpeedCurve.TopSpeed(PlayView, SmallField, 1f);
            Assert.AreEqual(PanSpeedCurve.StartSpeed(PlayView, 1f) * PanSpeedCurve.MinBoost, top, 0.001f);
        }

        [Test]
        public void RampOnlyRisesAndSettles()
        {
            float previous = 0f;
            for (float held = 0f; held <= PanSpeedCurve.RampSeconds * 2f; held += 0.05f)
            {
                float speed = PanSpeedCurve.Speed(PlayView, BigField, held, 1f);
                Assert.GreaterOrEqual(speed, previous - 0.0001f);
                previous = speed;
            }

            Assert.AreEqual(PanSpeedCurve.TopSpeed(PlayView, BigField, 1f), previous, 0.001f);
        }

        [Test]
        public void PlayerFactorScalesTheWholeCurve()
        {
            Assert.AreEqual(2f * PanSpeedCurve.Speed(PlayView, BigField, 0.4f, 1f), PanSpeedCurve.Speed(PlayView, BigField, 0.4f, 2f), 0.001f);
        }

        [Test]
        public void CameraControlsKeepTheFactorInRange()
        {
            var controls = new CameraControls();
            controls.Set(10f, false);
            Assert.AreEqual(CameraControls.MaxSpeedScale, controls.SpeedScale);
            Assert.IsFalse(controls.EdgeScrolling);
            controls.Set(0f, true);
            Assert.AreEqual(CameraControls.MinSpeedScale, controls.SpeedScale);
        }
    }
}
