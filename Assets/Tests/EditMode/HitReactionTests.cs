using Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Hit flinches are rationed: a cooldown under rapid fire, chip damage gathered, never over a mechanic.</summary>
    public sealed class HitReactionTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void RapidFire_FlinchesOncePerCooldown()
        {
            var reaction = new HitReaction(0.7f, 0f);
            int flinches = 0;
            // A laser hitting every frame for two seconds.
            for (float t = 0f; t < 2f; t += Frame)
            {
                flinches += reaction.Register(0.01f, false) ? 1 : 0;
                reaction.Tick(Frame);
            }

            Assert.AreEqual(3, flinches, "flinches at 0, 0.7 and 1.4 s only");
        }

        [Test]
        public void ChipDamage_IsGathered_UntilItCounts()
        {
            var reaction = new HitReaction(0f, 0.04f);
            Assert.IsFalse(reaction.Register(0.01f, false));
            Assert.IsFalse(reaction.Register(0.01f, false));
            Assert.IsFalse(reaction.Register(0.01f, false));
            Assert.IsTrue(reaction.Register(0.01f, false), "four chips of 1% make one flinch");
            Assert.IsFalse(reaction.Register(0.01f, false), "the gathered damage starts again after a flinch");
        }

        [Test]
        public void ABusyBody_NeverFlinches_AndFlinchesLaterFromTheGatheredDamage()
        {
            var reaction = new HitReaction(0.5f, 0.04f);
            Assert.IsFalse(reaction.Register(0.2f, true), "a mechanic's clip is never cut by a hit");
            Assert.IsTrue(reaction.Register(0f, false), "once free, the damage taken meanwhile still counts");
        }

        [Test]
        public void Bosses_FlinchLessOften_AndTheDataOverridesTheDefault()
        {
            var creature = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                Assert.AreEqual(HitReaction.CreatureCooldown, HitReaction.For(creature, 0f).Cooldown, 1e-5f);
                Assert.AreEqual(1.2f, HitReaction.For(creature, 1.2f).Cooldown, 1e-5f, "never shorter than the hit clip");
                Assert.Greater(HitReaction.BossCooldown, HitReaction.CreatureCooldown);
                Assert.Less(HitReaction.BossChipShare, HitReaction.CreatureChipShare);
                var data = new UnityEditor.SerializedObject(creature);
                data.FindProperty("_hitCooldown").floatValue = 0.3f;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(0.3f, HitReaction.For(creature, 0f).Cooldown, 1e-5f, "its own data wins over the default");
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void Reset_LetsAFreshCreatureFlinchAtOnce()
        {
            var reaction = new HitReaction(5f, 0f);
            Assert.IsTrue(reaction.Register(0.1f, false));
            Assert.IsFalse(reaction.Register(0.1f, false));
            reaction.Reset();
            Assert.IsTrue(reaction.Register(0.1f, false));
        }
    }
}
