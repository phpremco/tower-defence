using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The pointer picks what it is drawn over: with the slanted orthographic camera a tower's body covers the cells
    /// behind it, so a ray through the body must select the tower, not the ground cell behind it.
    /// </summary>
    public sealed class TowerPickingTests
    {
        private const float Pitch = 44f;
        private const float TowerHeight = 1.4f;
        private const float TowerRadius = 0.35f;
        private static readonly Vector2Int TowerCell = new Vector2Int(5, 5);

        // The level camera: orthographic, so every ray shares one direction, looking down at the pitch along +z.
        private static Ray CameraRayThrough(Vector3 point)
        {
            Vector3 direction = Quaternion.Euler(Pitch, 0f, 0f) * Vector3.forward;
            return new Ray(point - direction * 50f, direction);
        }

        private static List<TowerPicking.Column> OneTower(Vector3 foot) => new List<TowerPicking.Column>
        {
            new TowerPicking.Column(TowerCell, foot, TowerRadius, TowerHeight),
        };

        [Test]
        public void ARayThroughTheTowerTop_SelectsTheTower_ThoughItsGroundPointIsCellsBehind()
        {
            var foot = new Vector3(5f, 0f, 5f);
            Ray ray = CameraRayThrough(foot + Vector3.up * (TowerHeight * 0.9f));

            var ground = new Plane(Vector3.up, Vector3.zero);
            ground.Raycast(ray, out float distance);
            float behind = ray.GetPoint(distance).z - foot.z;
            Assert.Greater(behind, 1f, "the ground under the tower's head lies more than a cell behind its foot");

            Assert.IsTrue(TowerPicking.TryPick(ray, OneTower(foot), out Vector2Int cell));
            Assert.AreEqual(TowerCell, cell);
        }

        [Test]
        public void ARayThroughTheShiftedFoot_SelectsTheTower()
        {
            // A tower stands back from the road inside its cell; the column stands where the tower is drawn.
            var foot = new Vector3(5.15f, 0f, 5f);
            Assert.IsTrue(TowerPicking.TryPick(CameraRayThrough(foot + Vector3.up * 0.05f), OneTower(foot), out Vector2Int cell));
            Assert.AreEqual(TowerCell, cell);
        }

        [Test]
        public void ARayBesideOrAboveTheTower_PicksNothing()
        {
            var foot = new Vector3(5f, 0f, 5f);
            List<TowerPicking.Column> columns = OneTower(foot);
            Assert.IsFalse(TowerPicking.TryPick(CameraRayThrough(foot + new Vector3(TowerRadius + 0.2f, 0.7f, 0f)), columns, out _), "beside it");
            Assert.IsFalse(TowerPicking.TryPick(CameraRayThrough(foot + Vector3.up * (TowerHeight + 0.4f)), columns, out _), "over its head");
            Assert.IsFalse(TowerPicking.TryPick(CameraRayThrough(foot + new Vector3(0f, 0f, -1f)), columns, out _), "on the ground a cell in front");
        }

        [Test]
        public void WhereTwoTowersOverlapOnScreen_TheNearerOneWins()
        {
            var near = new Vector3(5f, 0f, 5f);
            var far = new Vector3(5f, 0f, 6f);
            var columns = new List<TowerPicking.Column>
            {
                new TowerPicking.Column(new Vector2Int(5, 6), far, TowerRadius, TowerHeight),
                new TowerPicking.Column(TowerCell, near, TowerRadius, TowerHeight),
            };

            // Through the front tower's head, a ray goes on into the body of the tower behind it.
            Ray ray = CameraRayThrough(near + Vector3.up * (TowerHeight * 0.8f));
            Assert.IsTrue(TowerPicking.RayHitsColumn(ray, far, TowerRadius, TowerHeight, out _), "the ray also crosses the far tower");
            Assert.IsTrue(TowerPicking.TryPick(ray, columns, out Vector2Int cell));
            Assert.AreEqual(TowerCell, cell, "the tower in front covers the one behind");
        }

        [Test]
        public void TheGroundPoint_LandsOnARaisedPlateauAtItsHeight()
        {
            const float plateau = 0.55f;
            // Ground at plateau height for z >= 4, flat below.
            Vector3 target = new Vector3(3f, plateau, 6f);
            Ray ray = CameraRayThrough(target);

            Assert.IsTrue(TowerPicking.TryGround(ray, p => p.z >= 4f ? plateau : 0f, out Vector3 point));
            Assert.AreEqual(target.z, point.z, 1e-3f, "the ray meets the plateau top, not the flat grid under it");
            Assert.AreEqual(plateau, point.y, 1e-3f);
        }
    }
}
