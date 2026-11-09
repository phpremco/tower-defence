using Game.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>The blue protection line over a creature's health bar (round 23a; round 24: the armour pool only).</summary>
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
            var health = new Health(100f, 40f);
            Assert.IsTrue(ProtectionBar.Shows(health));
            Assert.AreEqual(1f, ProtectionBar.Line(health));

            health.TakeDamage(20f);

            Assert.AreEqual(0.5f, ProtectionBar.Line(health), 0.0001f);
        }

        [Test]
        public void LightFire_ChipsTheLineSlowly_AndTheGreenLineWaits()
        {
            var health = new Health(100f, 40f);
            health.TakeDamage(20f, 0.3f);

            Assert.AreEqual(34f / 40f, ProtectionBar.Line(health), 0.0001f, "a 20 pulse at 0.3 takes 6 off the armour");
            Assert.AreEqual(1f, health.Fraction, 0.0001f, "nothing reaches the body while armour is left");
        }

        [Test]
        public void BrokenArmour_StillShowsTheBars_WithAnEmptyLine()
        {
            var health = new Health(100f, 40f);
            health.TakeDamage(60f);

            Assert.IsTrue(ProtectionBar.Shows(health), "an armoured creature shows its bars all its life");
            Assert.AreEqual(0f, ProtectionBar.Line(health), 0.0001f);
            Assert.AreEqual(80f, health.Current, 0.0001f, "the 20 the armour did not need hurt the body");
        }
    }
}
