using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The hold-to-overview camera: it frames the whole field, eases evenly, picks ground points and keeps them on the field.</summary>
    public sealed class OverviewFramingTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Overview_FramesTheWholeField()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                // Zoom one is the fitted size: every ground corner of the field shows inside the band the HUD leaves.
                Assert.AreEqual(1f, OverviewFraming.Zoom);
                Assert.AreEqual(Vector3.zero, OverviewFraming.Focus);
                Vector2 half = level.WorldSize * 0.5f;
                foreach (var corner in new[] { new Vector3(half.x, 0f, half.y), new Vector3(-half.x, 0f, half.y), new Vector3(half.x, 0f, -half.y), new Vector3(-half.x, 0f, -half.y) })
                {
                    Assert.IsTrue(FixedCameraRig.InView(level, corner, 16f / 9f, 0f), $"corner {corner} out of the overview");
                }
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void Blend_RunsFromThePlayViewToTheOverview()
        {
            var play = new Vector3(8f, 0f, -5f);
            OverviewFraming.Blend(play, 3f, 0f, out Vector3 focus, out float zoom);
            Assert.AreEqual(play, focus);
            Assert.AreEqual(3f, zoom, Tolerance);
            OverviewFraming.Blend(play, 3f, 1f, out focus, out zoom);
            Assert.AreEqual(OverviewFraming.Focus, focus);
            Assert.AreEqual(OverviewFraming.Zoom, zoom, Tolerance);
        }

        [Test]
        public void Blend_HalfwayIsTheGeometricMeanOfTheZooms()
        {
            OverviewFraming.Blend(Vector3.zero, 4f, 0.5f, out _, out float zoom);
            Assert.AreEqual(Mathf.Sqrt(4f * OverviewFraming.Zoom), zoom, Tolerance, "the zoom changes evenly in scale");
        }

        [Test]
        public void GroundPoint_FindsWhereARayMeetsTheGround()
        {
            var ray = new Ray(new Vector3(2f, 10f, -3f), new Vector3(0f, -1f, 1f));
            Assert.IsTrue(OverviewFraming.GroundPoint(ray, out Vector3 point));
            Assert.AreEqual(2f, point.x, Tolerance);
            Assert.AreEqual(7f, point.z, Tolerance);
            Assert.IsFalse(OverviewFraming.GroundPoint(new Ray(Vector3.up, Vector3.up), out _), "a ray going up never lands");
        }

        [Test]
        public void KeepOnField_PullsAPointInsideTheMargin()
        {
            Vector3 kept = OverviewFraming.KeepOnField(new Vector3(40f, 3f, -40f), new Vector2(20f, 15f), 1.5f);
            Assert.AreEqual(18.5f, kept.x, Tolerance);
            Assert.AreEqual(0f, kept.y, Tolerance);
            Assert.AreEqual(-13.5f, kept.z, Tolerance);
        }
    }
}
