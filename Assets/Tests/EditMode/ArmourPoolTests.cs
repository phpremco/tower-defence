using Game.Balance;
using Game.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Round 24, Remco's armour design: armour is a pool in front of the body (the blue line). While it lasts a hit works on
    /// it at the weapon's armour factor and nothing hurts the body; once it is gone every weapon lands in full. The game's
    /// Health and the balance model's ArmourMix must agree on what a weapon pays for an armoured creature.
    /// </summary>
    public sealed class ArmourPoolTests
    {
        private const float LightFire = 0.3f;

        [Test]
        public void HeavyShell_StripsTheArmourPointForPoint_ThenHurtsTheBody()
        {
            var health = new Health(160f, 60f);
            float dealt = health.TakeDamage(25f);

            Assert.AreEqual(35f, health.Armor, 0.001f);
            Assert.AreEqual(160f, health.Current, 0.001f, "nothing reaches the body while armour is left");
            Assert.AreEqual(25f, dealt, 0.001f);

            dealt = health.TakeDamage(48f);
            Assert.AreEqual(0f, health.Armor, 0.001f);
            Assert.AreEqual(147f, health.Current, 0.001f, "35 finished the armour, the other 13 landed in full");
            Assert.AreEqual(48f, dealt, 0.001f);
        }

        [Test]
        public void LightFire_OnlyChipsTheArmour_AndIsWorthItsFraction()
        {
            var health = new Health(160f, 60f);
            float dealt = health.TakeDamage(5f, LightFire);

            Assert.AreEqual(58.5f, health.Armor, 0.001f, "a 5 pulse at 0.3 takes 1.5 off the armour");
            Assert.AreEqual(160f, health.Current, 0.001f);
            Assert.AreEqual(1.5f, dealt, 0.001f, "the hit was worth what it took off");
        }

        [Test]
        public void LightFire_OnTheLastOfTheArmour_PassesTheRestInFull()
        {
            var health = new Health(100f, 3f);
            // 3 armour at 0.3 needs a 10-point hit; of a 20-point hit half goes on the armour, half lands in full.
            float dealt = health.TakeDamage(20f, LightFire);

            Assert.AreEqual(0f, health.Armor, 0.001f);
            Assert.AreEqual(90f, health.Current, 0.001f);
            Assert.AreEqual(13f, dealt, 0.001f);
        }

        [Test]
        public void OnceTheArmourIsGone_EveryWeaponLandsInFull()
        {
            var health = new Health(100f, 10f);
            health.TakeDamage(10f);
            health.TakeDamage(5f, LightFire);

            Assert.AreEqual(95f, health.Current, 0.001f);
        }

        [Test]
        public void ArmourEvents_ChipAndBreak()
        {
            var health = new Health(100f, 10f);
            int chips = 0;
            int broken = 0;
            health.ArmorChanged += _ => chips++;
            health.ArmorBroken += () => broken++;

            health.TakeDamage(4f);
            health.TakeDamage(4f);
            Assert.AreEqual(2, chips);
            Assert.AreEqual(0, broken);

            health.TakeDamage(4f);
            Assert.AreEqual(1, broken, "the hit that takes the last of it breaks it once");
            health.TakeDamage(4f);
            Assert.AreEqual(1, broken);
            Assert.AreEqual(3, chips, "no armour event once it is gone");
        }

        [Test]
        public void AWeaponThatCannotWorkOnArmour_IsSwallowedWhileItLasts()
        {
            var health = new Health(100f, 10f);
            float dealt = health.TakeDamage(50f, 0f);

            Assert.AreEqual(0f, dealt, 0.001f);
            Assert.AreEqual(100f, health.Current, 0.001f);
            Assert.AreEqual(10f, health.Armor, 0.001f);
        }

        [Test]
        public void ArmourMix_IsTheShareOfAWeaponsDamageThatCounts_AgainstHealthPlusArmour()
        {
            var mix = new ArmourMix();
            mix.Add(160f, 60f);

            Assert.AreEqual(1f, mix.Share(1f), 0.0001f, "a heavy shell pays 220 for 220");
            Assert.AreEqual(220f / (160f + 60f / LightFire), mix.Share(LightFire), 0.0001f, "light fire pays 360 for 220");
            Assert.Greater(mix.Share(1.5f), 1f, "a slug that over-penetrates pays less than 220");
            Assert.AreEqual(0f, mix.Share(0f), "a weapon that cannot work on armour never gets past it");
        }

        [Test]
        public void ArmourMix_SoftWave_LetsEverythingCount()
        {
            var mix = new ArmourMix();
            mix.Add(300f, 0f);

            Assert.IsTrue(mix.IsSoft);
            Assert.AreEqual(1f, mix.Share(LightFire), 0.0001f);
        }

        [Test]
        public void ArmourMix_MatchesTheGame_WhenOneWeaponKillsTheCreature()
        {
            // The game: a light gun fires 5-point pulses until a 160 + 60 creature dies; the paper: the share of its damage that counts.
            var health = new Health(160f, 60f);
            float fired = 0f;
            while (!health.IsDead)
            {
                health.TakeDamage(5f, LightFire);
                fired += 5f;
            }

            var mix = new ArmourMix();
            mix.Add(160f, 60f);
            float paper = 220f / mix.Share(LightFire);
            Assert.AreEqual(paper, fired, 5f, "the damage the game needed is what the paper says, to within one pulse");
        }

        [Test]
        public void Threat_PricesArmourAboveHealth()
        {
            BalanceRules rules = BalanceRules.Default;
            var soft = new EnemyStats { Health = 220f };
            var armoured = new EnemyStats { Health = 160f, Armor = 60f };

            Assert.Greater(Threat.Of(armoured, rules), Threat.Of(soft, rules), "the same 220 points cost more when 60 of them are armour");
            float expected = (160f + 60f * rules.ArmorThreatShare) / rules.HealthPerThreat * (1f + armoured.BaseDamage * rules.BaseDamageThreat);
            Assert.AreEqual(expected, Threat.Of(armoured, rules), 0.0001f);
        }
    }
}
