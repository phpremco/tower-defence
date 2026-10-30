using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class PathOffsetTests
    {
        [Test]
        public void Shift_StraightLine_StaysParallelAtDistance()
        {
            var points = new[] { new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(10f, 0f, 0f) };

            Vector3[] shifted = PathOffset.Shift(points, 0.3f);

            for (int i = 0; i < points.Length; i++)
            {
                Assert.AreEqual(points[i].x, shifted[i].x, 0.0001f);
                // Travelling along +x, the right-hand side is -z.
                Assert.AreEqual(-0.3f, shifted[i].z, 0.0001f);
            }
        }

        [Test]
        public void Shift_RightAngleCorner_LiesOnBothShiftedSegments()
        {
            var points = new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(4f, 0f, 4f) };

            Vector3[] shifted = PathOffset.Shift(points, 0.25f);

            // First leg runs along +x at z = -0.25, second leg runs along +z at x = 4.25; the corner is on both.
            Assert.AreEqual(-0.25f, shifted[0].z, 0.0001f);
            Assert.AreEqual(-0.25f, shifted[1].z, 0.0001f);
            Assert.AreEqual(4.25f, shifted[1].x, 0.0001f);
            Assert.AreEqual(4.25f, shifted[2].x, 0.0001f);
        }

        [Test]
        public void Shift_NegativeDistance_GoesToTheOtherSide()
        {
            var points = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 3f) };

            Vector3[] left = PathOffset.Shift(points, -0.5f);
            Vector3[] right = PathOffset.Shift(points, 0.5f);

            Assert.AreEqual(-0.5f, left[0].x, 0.0001f);
            Assert.AreEqual(0.5f, right[0].x, 0.0001f);
        }

        [Test]
        public void Shift_ZeroDistanceOrSinglePoint_ReturnsCopy()
        {
            var single = new[] { new Vector3(1f, 2f, 3f) };
            var pair = new[] { Vector3.zero, Vector3.forward };

            Assert.AreEqual(single[0], PathOffset.Shift(single, 1f)[0]);
            Assert.AreEqual(pair[1], PathOffset.Shift(pair, 0f)[1]);
        }

        [Test]
        public void Shift_KeepsHeightOfEveryPoint()
        {
            var points = new[] { new Vector3(0f, 1.6f, 0f), new Vector3(3f, 1.6f, 2f), new Vector3(6f, 1.6f, 0f) };

            Vector3[] shifted = PathOffset.Shift(points, 0.4f);

            foreach (Vector3 point in shifted)
            {
                Assert.AreEqual(1.6f, point.y, 0.0001f);
            }
        }
    }
}
