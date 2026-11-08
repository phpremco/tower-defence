using Game.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>The blue protection line over a creature's health bar (round 23a).</summary>
    public sealed class ProtectionBarTests
    {
        [Test]
        public void PlainCreature_HasNoLine()
        {
            var health = new Health(100f);

            Assert.IsFalse(ProtectionBar.Shows(health));
            Assert.AreEqual(0f, ProtectionBar.Line(health));
        }

        [Test]
        public void Armour_IsAPool_TheLineShrinksAsItIsChipped()
        {
            var health = new Health(100f, 40f, 0.5f);
            Assert.IsTrue(ProtectionBar.Shows(health));
            Assert.AreEqual(1f, ProtectionBar.Line(health));

            health.TakeDamage(40f);

            Assert.AreEqual(0.5f, ProtectionBar.Line(health), 0.0001f);
        }

        [Test]
        public void Plating_NeverWearsOff_TheLineStaysFull()
        {
            var health = new Health(100f, 0f, 0f, 10f, 0.2f);
            health.TakeDamage(30f);
            health.TakeDamage(30f);

            Assert.IsTrue(ProtectionBar.Shows(health));
            Assert.AreEqual(1f, ProtectionBar.Line(health));
        }

        [Test]
        public void Plating_ReportsTheShareItStopped_AndNothingForContinuousDamage()
        {
            var health = new Health(100f, 0f, 0f, 10f, 0.2f);
            float stopped = -1f;
            health.PlatingStopped += share => stopped = share;

            health.TakeDamage(40f);
            Assert.AreEqual(0.25f, stopped, 0.0001f, "a 40 hit loses 10 to the plates");
            Assert.AreEqual(70f, health.Current, 0.0001f);

            health.TakeDamage(5f);
            Assert.AreEqual(0.8f, stopped, 0.0001f, "a light hit keeps only the floor share");

            stopped = -1f;
            health.TakeDamage(5f, true);
            Assert.AreEqual(-1f, stopped, "a burn has no single hit for the plates to stop");
        }
    }
}
