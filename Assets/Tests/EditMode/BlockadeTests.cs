using Game.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class BlockadeTests
    {
        private sealed class FakeStructure : IStructure
        {
            public Vector3 Position { get; set; }

            public bool IsAlive { get; set; } = true;

            public bool BlocksGround { get; set; }
            public bool IsBase => false;

            public float HealthFraction => 1f;

            public void Repair(float amount)
            {
            }

            public void TakeDamage(float amount)
            {
            }
        }

        [Test]
        public void FindBlocker_ReturnsOnlyBlockingStructuresInReach()
        {
            var registry = new StructureRegistry();
            var tower = new FakeStructure { Position = Vector3.zero };
            var wall = new FakeStructure { Position = new Vector3(1f, 0f, 0f), BlocksGround = true };
            registry.Add(tower);
            registry.Add(wall);

            Assert.AreSame(wall, registry.FindBlocker(new Vector3(0.5f, 0f, 0f), 0.85f));
            Assert.IsNull(registry.FindBlocker(new Vector3(5f, 0f, 0f), 0.85f));
        }

        [Test]
        public void FindBlocker_IgnoresDestroyedBlockade()
        {
            var registry = new StructureRegistry();
            var wall = new FakeStructure { BlocksGround = true };
            registry.Add(wall);
            wall.BlocksGround = false;

            Assert.IsNull(registry.FindBlocker(Vector3.zero, 1f));
        }

        [Test]
        public void HealthExtend_RaisesMaxAndCurrent()
        {
            var health = new Health(100f);
            health.TakeDamage(40f);
            health.Extend(50f);

            Assert.AreEqual(150f, health.Max, 0.001f);
            Assert.AreEqual(110f, health.Current, 0.001f);
        }
    }
}
