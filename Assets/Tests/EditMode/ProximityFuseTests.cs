using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The Flak's proximity fuse and its air-only reach (S4 spec 2, curator decision 8).</summary>
    public sealed class ProximityFuseTests
    {
        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }
            public TargetLayers Layer { get; set; }
            public float PathProgress => 0f;
            public bool IsAlive => true;

            public void TakeDamage(float amount)
            {
            }

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }

        [Test]
        public void Shell_BurstsWithinTheFuseDistance()
        {
            var target = new Vector3(0f, 1.5f, 0f);
            Assert.IsTrue(ProximityFuse.ShouldBurst(new Vector3(0.5f, 1.5f, 0f), target, 0.6f, 0.1f));
            Assert.IsFalse(ProximityFuse.ShouldBurst(new Vector3(0.8f, 1.5f, 0f), target, 0.6f, 0.1f));
        }

        [Test]
        public void Shell_BurstsWhenItsStepWouldReachTheTarget()
        {
            var target = Vector3.zero;
            Assert.IsTrue(ProximityFuse.ShouldBurst(new Vector3(0.9f, 0f, 0f), target, 0.2f, 1f), "a fast shell never flies past its target");
            Assert.IsTrue(ProximityFuse.ShouldBurst(new Vector3(0.1f, 0f, 0f), target, 0f, 0.15f), "no fuse still hits on contact");
        }

        // At a high time scale one frame's step (here 4) is far longer than the splash (1.1): the burst must still land
        // within the fuse distance of the flyer, not where the shell happened to be when the fuse tripped.
        [Test]
        public void LongStep_BurstsWithinTheSplashOfTheTarget()
        {
            const float fuse = 0.6f;
            const float splash = 1.1f;
            const float step = 4f;
            var target = new Vector3(0f, 1.5f, 0f);
            var shell = new Vector3(3.5f, 1.5f, 0f);
            Assert.IsTrue(ProximityFuse.ShouldBurst(shell, target, fuse, step));

            Vector3 burst = ProximityFuse.BurstPoint(shell, target, fuse);
            Assert.AreEqual(fuse, Vector3.Distance(burst, target), 1e-4f);
            Assert.Less(Vector3.Distance(burst, target), splash);
            var inside = new Vector3(0.3f, 1.5f, 0f);
            Assert.AreEqual(inside, ProximityFuse.BurstPoint(inside, target, fuse), "inside the fuse it bursts where it is");
        }

        [Test]
        public void AirOnlyWeapons_NeverHitTheGround_OrAWispWalkingTheRoad()
        {
            var registry = new TargetRegistry();
            var flyer = new FakeTarget { Layer = TargetLayers.Air, Position = Vector3.zero };
            var walker = new FakeTarget { Layer = TargetLayers.Ground, Position = Vector3.zero };
            var wisp = new FakeTarget { Layer = TargetLayers.Ground | TargetLayers.Air, Position = Vector3.zero };
            registry.Add(flyer);
            registry.Add(walker);
            registry.Add(wisp);
            var found = new List<ITargetable>();

            registry.FindInRadius(Vector3.zero, 1.1f, TargetLayers.Air, found);
            CollectionAssert.AreEquivalent(new[] { flyer }, found, "the flak burst splashes the air layer only");

            registry.FindInRadius(Vector3.zero, 1.1f, TargetLayers.Ground | TargetLayers.Air, found);
            Assert.AreEqual(3, found.Count, "a gun that hits both still hits the wisp");

            Assert.AreSame(flyer, registry.FindBest(Vector3.zero, 5f, TargetLayers.Air));
        }
    }
}
