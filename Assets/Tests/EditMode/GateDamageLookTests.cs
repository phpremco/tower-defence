using Game.Towers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>The damaged look of a gate with its band between going in and coming out (round 23c, the repair flicker).</summary>
    public sealed class GateDamageLookTests
    {
        private const float Line = 0.5f;
        private const float Margin = 0.15f;

        [Test]
        public void WholeGate_TurnsDamaged_BelowTheLine()
        {
            var look = new GateDamageLook(Line, Margin);

            Assert.IsFalse(look.Update(0.6f, false));
            Assert.IsFalse(look.IsDamaged);
            Assert.IsTrue(look.Update(0.49f, false));
            Assert.IsTrue(look.IsDamaged);
        }

        [Test]
        public void TrickledRepair_AgainstHits_AroundTheLine_ChangesTheLookOnce()
        {
            // A repair drone heals a little every frame while a creature hits the gate every few frames: the health
            // hovers just around the line, which flipped the old single-threshold look on every hit and every tick.
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.48f, false);
            float health = 0.48f;
            int changes = 0;
            for (int frame = 1; frame <= 200; frame++)
            {
                health += 0.004f;
                if (frame % 4 == 0)
                {
                    health -= 0.015f;
                }

                if (look.Update(health, false))
                {
                    changes++;
                }
            }

            Assert.Less(health, Line + Margin, "the trickle never got clear of the band in this run");
            Assert.AreEqual(0, changes, "the look holds while the health hovers around the line");
            Assert.IsTrue(look.IsDamaged);
        }

        [Test]
        public void DamagedGate_LooksWholeAgain_OnlyAboveTheBand()
        {
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.45f, false);

            Assert.IsFalse(look.Update(0.52f, false), "a small heal past the line is inside the band");
            Assert.IsFalse(look.Update(0.6f, false));
            Assert.IsTrue(look.IsDamaged);
            Assert.IsTrue(look.Update(Line + Margin, false));
            Assert.IsFalse(look.IsDamaged);
            Assert.IsFalse(look.Update(0.55f, false), "back inside the band from above keeps the whole look");
        }

        [Test]
        public void OneRepair_ThatLiftsItPastTheLine_LooksWholeAtOnce()
        {
            // The hero's repair restores a share in one go: a deliberate fix, so the gate shows it even inside the band.
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.22f, false);

            Assert.IsTrue(look.Update(0.57f, false));
            Assert.IsFalse(look.IsDamaged);
        }

        [Test]
        public void OneRepair_ThatStaysBelowTheLine_KeepsTheDamage()
        {
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.1f, false);

            Assert.IsFalse(look.Update(0.45f, false));
            Assert.IsTrue(look.IsDamaged);
        }

        [Test]
        public void FullRepair_OfADamagedGate_LooksWhole()
        {
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.2f, false);

            Assert.IsTrue(look.Update(1f, false));
            Assert.IsFalse(look.IsDamaged);
        }

        [Test]
        public void Wreck_IsNotDamaged_AndARebuiltGate_StartsWhole()
        {
            var look = new GateDamageLook(Line, Margin);
            look.Update(0.2f, false);

            look.Update(0f, true);
            Assert.IsFalse(look.IsDamaged, "a wreck has its own look");
            Assert.IsFalse(look.Update(1f, false));
            Assert.IsFalse(look.IsDamaged);
        }

        [Test]
        public void Band_NeverReachesPastFullHealth()
        {
            var look = new GateDamageLook(0.9f, 0.5f);
            look.Update(0.5f, false);

            Assert.AreEqual(1f, look.RecoverAt);
            Assert.IsTrue(look.Update(1f, false), "a full repair always looks whole");
        }
    }
}
