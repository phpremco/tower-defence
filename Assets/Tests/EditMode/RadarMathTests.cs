using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The HUD radar's geometry: the field fits the scope by its diagonal, and world and radar points map both ways.</summary>
    public sealed class RadarMathTests
    {
        private const float Tolerance = 0.001f;
        private static readonly Vector2 Field = new Vector2(42f, 30f);
        private const float Radius = 112.5f;

        [Test]
        public void FitHalfExtents_PutsTheCornersOnTheRim()
        {
            Vector2 half = RadarMath.FitHalfExtents(Field, Radius);
            Assert.AreEqual(Radius, half.magnitude, Tolerance);
            Assert.AreEqual(Field.x / Field.y, half.x / half.y, Tolerance, "the field keeps its shape");
        }

        [Test]
        public void EveryCorner_StaysInsideTheScope_AtAnyTurn()
        {
            Vector2 half = RadarMath.FitHalfExtents(Field, Radius);
            for (float yaw = 0f; yaw < 360f; yaw += 15f)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, yaw);
                foreach (Vector2 corner in new[] { half, -half, new Vector2(half.x, -half.y), new Vector2(-half.x, half.y) })
                {
                    Assert.LessOrEqual(((Vector2)(turn * corner)).magnitude, Radius + Tolerance, $"corner {corner} at {yaw} degrees");
                }
            }
        }

        [Test]
        public void WorldToRadar_MapsCentreAndCorners()
        {
            Assert.AreEqual(Vector2.zero, RadarMath.WorldToRadar(Vector3.zero, Field, Radius));
            Vector2 corner = RadarMath.WorldToRadar(new Vector3(Field.x * 0.5f, 3f, Field.y * 0.5f), Field, Radius);
            Vector2 half = RadarMath.FitHalfExtents(Field, Radius);
            Assert.AreEqual(half.x, corner.x, Tolerance);
            Assert.AreEqual(half.y, corner.y, Tolerance, "world z is radar up; height plays no part");
        }

        [Test]
        public void RadarToWorld_IsTheInverse()
        {
            var world = new Vector3(-7.5f, 0f, 11.25f);
            Vector3 back = RadarMath.RadarToWorld(RadarMath.WorldToRadar(world, Field, Radius), Field, Radius);
            Assert.AreEqual(world.x, back.x, Tolerance);
            Assert.AreEqual(0f, back.y, Tolerance);
            Assert.AreEqual(world.z, back.z, Tolerance);
        }

        [Test]
        public void ViewFootprint_StretchesTheDepthByThePitch()
        {
            Vector2 flat = RadarMath.ViewFootprint(7f, 16f / 9f, 90f);
            Assert.AreEqual(7f * 2f * 16f / 9f, flat.x, Tolerance);
            Assert.AreEqual(14f, flat.y, Tolerance, "straight down the depth is the screen height");
            Vector2 slanted = RadarMath.ViewFootprint(7f, 16f / 9f, 50f);
            Assert.AreEqual(14f / Mathf.Sin(50f * Mathf.Deg2Rad), slanted.y, Tolerance);
        }

        [Test]
        public void Ping_IsFullUnderTheSweep_AndFadesBehindIt()
        {
            var up = new Vector2(0f, 10f);
            Assert.AreEqual(1f, RadarMath.Ping(up, 0f, 120f, 1f), Tolerance);
            float later = RadarMath.Ping(up, 90f, 120f, 1f);
            Assert.Less(later, 1f);
            Assert.Greater(later, RadarMath.Ping(up, 180f, 120f, 1f), "it keeps fading until the next pass");
            Assert.AreEqual(1f, RadarMath.Ping(new Vector2(10f, 0f), 90f, 120f, 1f), Tolerance, "the sweep turns clockwise from up");
        }
    }
}
