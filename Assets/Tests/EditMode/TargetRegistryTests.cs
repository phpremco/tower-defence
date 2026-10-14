using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class TargetRegistryTests
    {
        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }
            public TargetLayers Layer { get; set; } = TargetLayers.Ground;
            public float PathProgress { get; set; }
            public bool IsAlive { get; set; } = true;
            public void TakeDamage(float amount) { }
        }

        [Test]
        public void FindBest_PrefersTargetFurthestAlongPath()
        {
            var registry = new TargetRegistry();
            var behind = new FakeTarget { Position = new Vector3(1f, 0f, 0f), PathProgress = 2f };
            var ahead = new FakeTarget { Position = new Vector3(2f, 0f, 0f), PathProgress = 9f };
            registry.Add(behind);
            registry.Add(ahead);

            Assert.AreSame(ahead, registry.FindBest(Vector3.zero, 5f, TargetLayers.Ground));
        }

        [Test]
        public void FindBest_IgnoresOutOfRangeDeadAndWrongLayer()
        {
            var registry = new TargetRegistry();
            registry.Add(new FakeTarget { Position = new Vector3(10f, 0f, 0f), PathProgress = 1f });
            registry.Add(new FakeTarget { Position = new Vector3(1f, 0f, 0f), PathProgress = 2f, IsAlive = false });
            registry.Add(new FakeTarget { Position = new Vector3(1f, 0f, 0f), PathProgress = 3f, Layer = TargetLayers.Air });

            Assert.IsNull(registry.FindBest(Vector3.zero, 5f, TargetLayers.Ground));
        }

        [Test]
        public void FindBest_HitsAirWhenMaskIncludesAir()
        {
            var registry = new TargetRegistry();
            var air = new FakeTarget { Position = new Vector3(1f, 1.6f, 0f), Layer = TargetLayers.Air };
            registry.Add(air);

            Assert.AreSame(air, registry.FindBest(Vector3.zero, 5f, TargetLayers.Ground | TargetLayers.Air));
        }

        [Test]
        public void FindInRadius_ReturnsOnlyTargetsInsideRadius()
        {
            var registry = new TargetRegistry();
            var near = new FakeTarget { Position = new Vector3(1f, 0f, 0f) };
            registry.Add(near);
            registry.Add(new FakeTarget { Position = new Vector3(5f, 0f, 0f) });
            var results = new List<ITargetable>();

            registry.FindInRadius(Vector3.zero, 2f, TargetLayers.Ground, results);

            Assert.AreEqual(1, results.Count);
            Assert.AreSame(near, results[0]);
        }
    }
}
