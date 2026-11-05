using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The round-33 gate crossings as data (ArtSource README round 33): Gravemaw and the Cinder Matriarch carry their own
    /// Gate_Mount / Gate_Straddle_Loop / Gate_Dismount with weight frames and a per-frame gate burden that ends on the ground;
    /// Rotbloom cannot drape over a beam and batters gates with a clip it owns; the Sealed One bites like any creature.
    /// </summary>
    public sealed class BossGateCrossingTests
    {
        private const float FramesPerSecond = 30f;

        [TestCase("Gravemaw")]
        [TestCase("CinderMatriarch")]
        public void Climber_CarriesItsOwnGateCrossing_WithWeightsAndBurden(string boss)
        {
            EnemyDefinition enemy = Load(boss);
            Assert.AreEqual(GateBehaviour.Climb, enemy.Boss.Climb.GateBehaviour);
            ModelArtDefinition art = enemy.Art;
            ModelClip mount = Clip(art, ModelClipRole.GateMount);
            ModelClip loop = Clip(art, ModelClipRole.GateStraddle);
            ModelClip dismount = Clip(art, ModelClipRole.GateDismount);

            // Mount starts with the gate ahead and ends on the gate centre ON THE GROUND (not at the beam's height).
            Assert.Greater(mount.AnchorForward, 0f, "gate_forward_m");
            Assert.AreEqual(mount.AnchorForward, mount.EndOffset.x, 0.01f, "the mount ends over the gate centre");
            Assert.AreEqual(0f, mount.EndOffset.y, 0.001f, "the pivot stays on the ground");
            Assert.AreEqual(0f, loop.EndOffset.magnitude, 0.001f, "the straddle loop plays in place");
            Assert.Greater(dismount.EndOffset.x, 0f, "the dismount walks off the far side");

            foreach (ModelClip clip in new[] { mount, loop, dismount })
            {
                Assert.IsTrue(clip.HasBurden, $"{clip.Role} has its gate_burden series");
                Assert.Greater(clip.MomentSeconds.Length, 0, $"{clip.Role} has weight frames");
                Assert.AreEqual(clip.MomentSeconds[0], clip.CueSeconds, 0.001f, "the cue is the first weight frame");
                foreach (float moment in clip.MomentSeconds)
                {
                    Assert.That(moment, Is.InRange(0f, clip.Length), $"{clip.Role} weight inside the clip");
                }
            }

            Assert.AreEqual(0f, mount.BurdenAt(0f), 0.001f, "no sag before the body touches the beam");
            Assert.Greater(mount.BurdenAt(mount.MomentSeconds[0]), 0.1f, "the belly lands with weight");
            Assert.Greater(loop.BurdenAt(0f), 0.1f, "lying on the beam sags it");
            Assert.AreEqual(0f, dismount.BurdenAt(dismount.Length), 0.001f, "off the gate the burden is gone");
            Assert.AreEqual(2, loop.MomentSeconds.Length, "one blow per spade / scythe");
        }

        [Test]
        public void Rotbloom_BattersGates_WithAClipItOwns()
        {
            EnemyDefinition rotbloom = Load("Rotbloom");
            ClimbPower climb = rotbloom.Boss.Climb;
            Assert.AreEqual(GateBehaviour.Batter, climb.GateBehaviour);
            Assert.IsFalse(climb.ClimbsGates);
            ModelClip ram = Clip(rotbloom.Art, climb.RamClip);
            Assert.Greater(ram.EventSeconds, 0f, "the ram's blow lands on the clip's moment");
            Assert.Less(ram.EventSeconds, ram.Length);
        }

        [Test]
        public void SealedOne_BesiegesGates_AndTheOthersClimb()
        {
            Assert.AreEqual(GateBehaviour.Besiege, Load("SealedOne").Boss.Climb.GateBehaviour);
            Assert.AreEqual(GateBehaviour.Climb, Load("FrostWarden").Boss.Climb.GateBehaviour);
            Assert.AreEqual(GateBehaviour.Climb, Load("Shattered").Boss.Climb.GateBehaviour);
        }

        [Test]
        public void BurdenAt_InterpolatesBetweenFrames_AndHoldsAtTheEnds()
        {
            var clip = new ModelClip();
            SetField(clip, "_burden", new[] { 0f, 0.2f, 0.6f });
            Assert.AreEqual(0.1f, clip.BurdenAt(0.5f / FramesPerSecond), 0.0001f);
            Assert.AreEqual(0.4f, clip.BurdenAt(1.5f / FramesPerSecond), 0.0001f);
            Assert.AreEqual(0.6f, clip.BurdenAt(10f), 0.0001f);
            Assert.AreEqual(0f, clip.BurdenAt(-1f), 0.0001f);
            Assert.AreEqual(0f, new ModelClip().BurdenAt(1f), "no curve, no burden");
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(target, value);

        private static EnemyDefinition Load(string id)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            Assert.IsNotNull(enemy, id);
            Assert.IsNotNull(enemy.Boss, $"{id} has boss data");
            return enemy;
        }

        private static ModelClip Clip(ModelArtDefinition art, ModelClipRole role)
        {
            Assert.IsNotNull(art, "the boss wears its art");
            int index = art.IndexOf(role);
            Assert.GreaterOrEqual(index, 0, $"{art.name} has a {role} clip");
            return art.Clips[index];
        }
    }
}
