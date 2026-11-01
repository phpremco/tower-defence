using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The globe maths the planet map relies on: places on the sphere and the turn that shows them.</summary>
    public sealed class PlanetCoordinatesTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Direction_PointsEastNorthAndForward()
        {
            AssertClose(Vector3.forward, PlanetCoordinates.ToDirection(0f, 0f));
            AssertClose(Vector3.right, PlanetCoordinates.ToDirection(90f, 0f));
            AssertClose(Vector3.up, PlanetCoordinates.ToDirection(0f, 90f));
            AssertClose(Vector3.back, PlanetCoordinates.ToDirection(180f, 0f));
        }

        [Test]
        public void Uv_MapsTheWholeGlobeOntoTheTexture()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), PlanetCoordinates.ToUv(0f, 0f));
            Assert.AreEqual(new Vector2(0f, 0f), PlanetCoordinates.ToUv(-180f, -90f));
            Assert.AreEqual(new Vector2(1f, 1f), PlanetCoordinates.ToUv(180f, 90f));
        }

        [TestCase(0f, 0f)]
        [TestCase(90f, 0f)]
        [TestCase(-120f, 35f)]
        [TestCase(45f, -60f)]
        [TestCase(180f, 10f)]
        [TestCase(0f, 89f)]
        public void TurnToFront_BringsThePlaceToFaceTheCamera(float longitude, float latitude)
        {
            Vector3 place = PlanetCoordinates.ToDirection(longitude, latitude);
            Vector2 turn = PlanetCoordinates.TurnToFront(place);

            Vector3 turned = PlanetCoordinates.Rotation(turn.x, turn.y) * place;

            AssertClose(Vector3.back, turned);
        }

        [Test]
        public void Along_StaysOnTheSphereAndEndsAtBothPlaces()
        {
            Vector3 a = PlanetCoordinates.ToDirection(-60f, 10f);
            Vector3 b = PlanetCoordinates.ToDirection(60f, -20f);

            AssertClose(a, PlanetCoordinates.Along(a, b, 0f));
            AssertClose(b, PlanetCoordinates.Along(a, b, 1f));
            for (int i = 1; i < 10; i++)
            {
                Assert.AreEqual(1f, PlanetCoordinates.Along(a, b, i / 10f).magnitude, Tolerance);
            }
        }

        private static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, "z");
        }
    }
}
