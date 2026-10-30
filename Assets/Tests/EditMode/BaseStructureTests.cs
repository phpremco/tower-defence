using Game.Combat;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The base as a structure: it blocks the road while it stands, shares hits with its guard, and can be repaired.</summary>
    public sealed class BaseStructureTests
    {
        private sealed class HalfGuard : IDamageGuard
        {
            public float Taken { get; private set; }

            public float Absorb(float damage)
            {
                Taken += damage * 0.5f;
                return damage * 0.5f;
            }
        }

        [Test]
        public void Base_BlocksTheRoadWhileItStands_AndOpensItWhenItFalls()
        {
            var health = new Health(100f);
            var outpost = new BaseStructure(new Vector3(3f, 0f, 4f), health);
            var structures = new StructureRegistry();
            structures.Add(outpost);

            Assert.AreSame(outpost, structures.FindBlocker(new Vector3(3f, 0f, 3f), 1.5f));

            outpost.TakeDamage(100f);
            Assert.IsFalse(outpost.IsAlive);
            Assert.IsNull(structures.FindBlocker(new Vector3(3f, 0f, 3f), 1.5f), "a fallen base no longer holds anyone");
        }

        [Test]
        public void Base_SharesHitsWithItsGuard_AndCanBeRepaired()
        {
            var health = new Health(100f);
            var outpost = new BaseStructure(Vector3.zero, health) { Guard = new HalfGuard() };

            outpost.TakeDamage(40f);
            Assert.AreEqual(20f, ((HalfGuard)outpost.Guard).Taken, 0.001f);
            Assert.AreEqual(80f, health.Current, 0.001f);

            outpost.Repair(10f);
            Assert.AreEqual(90f, health.Current, 0.001f);

            outpost.Restore(0.5f);
            Assert.AreEqual(100f, health.Current, 0.001f, "the hero's repair caps at full health");
            Assert.AreEqual(1f, outpost.HealthFraction, 0.001f);
        }
    }
}
