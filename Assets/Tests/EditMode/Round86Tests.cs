using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Towers;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 86: a boss's phases on its whole bar and a shard's share of the armour; the beam's budget for the pulse that kills, settled as the pulse leaves. (The boss
    /// rule of that round is a formula with a floor since round 87: WaveFormulaTests.)
    /// </summary>
    public sealed class Round86Tests
    {
        // ---- Health: the whole bar, a shard's armour, the hit that kills ---------------------------------------

        [Test]
        public void TheBarFraction_CountsBodyAndArmour()
        {
            var health = new Health(800f, 200f);
            Assert.AreEqual(1f, health.BarFraction, 0.0001f);
            // A shell strips armour point for point: 200 armour and 50 body gone is a quarter of the bar.
            health.TakeDamage(250f, 1f);
            Assert.AreEqual(0.75f, health.BarFraction, 0.0001f, "a quarter of the bar is gone");
            Assert.AreEqual(750f / 800f, health.Fraction, 0.0001f, "while the body alone has lost a sixteenth: a phase read on it would wait");
        }

        [Test]
        public void TheHitToKill_IsTheBodyPlusTheArmourAtTheWeaponsPrice()
        {
            var plated = new Health(60f, 160f);
            Assert.AreEqual(220f, plated.HitToKill(1f), 0.001f, "a shell pays armour point for point");
            Assert.AreEqual(60f + 160f / 0.2f, plated.HitToKill(0.2f), 0.001f, "light fire pays five points for one");
            Assert.IsTrue(float.IsPositiveInfinity(plated.HitToKill(0f)), "a weapon that cannot touch armour never kills through it");

            // The hit of that size kills, and one a point smaller does not.
            var a = new Health(60f, 160f);
            a.TakeDamage(a.HitToKill(0.2f), 0.2f);
            Assert.IsTrue(a.IsDead);
            var b = new Health(60f, 160f);
            b.TakeDamage(b.HitToKill(0.2f) - 1f, 0.2f);
            Assert.IsFalse(b.IsDead);

            var soft = new Health(40f);
            soft.TakeDamage(15f);
            Assert.AreEqual(25f, soft.HitToKill(0.2f), 0.001f, "without armour it is what the body has left");
        }

        [Test]
        public void AShard_TakesItsShareOfBodyAndArmour_AndTheGiantFollowsBothPools()
        {
            var giant = new Health(900f, 300f);
            giant.TakeDamage(150f, 1f);
            var shards = new[] { new Health(1f), new Health(1f), new Health(1f) };
            foreach (Health shard in shards)
            {
                shard.Refill(giant.Current / shards.Length, giant.Armor / shards.Length);
            }

            Assert.AreEqual(300f, shards[0].Max, 0.001f);
            Assert.AreEqual(50f, shards[0].ArmorMax, 0.001f, "a third of the armour the giant had left");
            Assert.AreEqual(50f, shards[0].Armor, 0.001f);

            // One shard is shot through its plates and half its body; the giant's pools follow, pool by pool.
            shards[0].TakeDamage(200f, 1f);
            giant.LowerTo(shards[0].Current + shards[1].Current + shards[2].Current, shards[0].Armor + shards[1].Armor + shards[2].Armor);
            Assert.AreEqual(750f, giant.Current, 0.001f);
            Assert.AreEqual(100f, giant.Armor, 0.001f, "the armour of the two untouched shards is still on the giant");

            giant.LowerTo(2000f, 2000f);
            Assert.AreEqual(750f, giant.Current, 0.001f, "never up");
            giant.LowerTo(0f, 0f);
            Assert.IsTrue(giant.IsDead, "with every shard dead the giant is");
        }

        // ---- The beam's budget ---------------------------------------------------------------------------------

        [Test]
        public void ThePulseThatKills_CostsTheShareTheCreatureNeeded_NeverUnderTheSweep()
        {
            const float interval = 0.2f;
            const float sweep = 0.05f;
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 30f, 8f, sweep), 0.0001f, "a pulse that does not kill costs the interval");
            Assert.AreEqual(interval * 0.5f, BeamBudget.Seconds(interval, 4f, 8f, sweep), 0.0001f, "half a pulse needed, half an interval paid");
            Assert.AreEqual(sweep, BeamBudget.Seconds(interval, 0.8f, 8f, sweep), 0.0001f, "a tenth needed: the sweep to the next creature");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 8f, 8f, sweep), 0.0001f, "the whole pulse needed, the whole interval paid");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 4f, 8f, 0f), 0.0001f, "a beam that does not sweep pays in full");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, float.PositiveInfinity, 8f, sweep), 0.0001f, "a warded creature is not killed");
        }

        /// <summary>
        /// A stream of small creatures through the beam, one long frame at a time and many short ones: the same pulses, so
        /// the same seconds. Round 85 settled the kill a frame later and at twenty times speed (the probe's 0.2 s steps)
        /// a second pulse owed in the same frame went into the dying creature at a stale price.
        /// </summary>
        [Test]
        public void TheBeamsSeconds_DoNotDependOnTheFrameLength()
        {
            const float interval = 1f / 9f;
            const float sweep = 0.05f;
            const float damage = 18f;
            float[] bodies = { 22f, 22f, 51f, 12f, 22f, 51f, 12f, 12f, 22f, 51f, 22f, 12f };
            float coarse = SecondsToClear(bodies, damage, interval, sweep, 0.2f);
            float fine = SecondsToClear(bodies, damage, interval, sweep, 1f / 60f);
            Assert.AreEqual(fine, coarse, 0.2f + 0.001f, "within one coarse frame of each other");

            // And what it pays is its data: the health of the stream over its damage per second, plus sweeps for one-pulse bodies.
            float health = 0f;
            foreach (float body in bodies)
            {
                health += body;
            }

            Assert.GreaterOrEqual(fine, health / (damage / interval) - 1f / 60f, "no kill is free");
        }

        // The tower's loop without a scene (Tower.Update): pulses owed in a frame are fired in it, a pulse lands before
        // the next frame, the killing pulse is paid by BeamBudget and the beam lets go of the creature as the pulse leaves.
        private static float SecondsToClear(float[] bodies, float damage, float interval, float sweep, float frame)
        {
            var left = (float[])bodies.Clone();
            float cooldown = 0f;
            float clock = 0f;
            int target = 0;
            const int guard = 100000;
            for (int step = 0; step < guard && target < left.Length; step++)
            {
                clock += frame;
                cooldown -= frame;
                float underWay = 0f;
                for (int shots = 0; target < left.Length && Cadence.Due(cooldown, shots); shots++)
                {
                    float needed = BeamBudget.Needed(left[target], underWay);
                    cooldown = Cadence.Rearm(cooldown, BeamBudget.Seconds(interval, needed, damage, sweep));
                    if (BeamBudget.Kills(needed, damage))
                    {
                        left[target] = 0f;
                        target++;
                        underWay = 0f;
                    }
                    else
                    {
                        underWay += damage;
                    }
                }

                // The pulses under way land before the next frame.
                if (target < left.Length)
                {
                    left[target] -= underWay;
                }

                cooldown = Cadence.Idle(cooldown);
            }

            return clock;
        }
    }
}
