using System;
using Game.Combat;
using Game.Editor;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The effects painted in Blender (README round 20) as data: every weapon family has a recipe from muzzle to impact
    /// on sheets with both profile materials, the atlas layouts match the report, blasts leave a fireball and smoke,
    /// a boss goes down in a chain, and only big blasts kick the camera, briefly.
    /// </summary>
    public sealed class VfxArtTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string Particle = "Game/VfxParticle";
        private const string Smoke = "Game/VfxSmoke";
        private const string Distortion = "Game/VfxDistortion";
        private const float KickSeconds = 0.6f;

        // Everyday hits never kick the camera (Remco: shaking only for a moment, on big impacts).
        private static readonly string[] QuietImpacts = { "Hit", "Frost", "Arc", "Rail", "Shell", "Rocket", "FlakBurst", "Death" };

        private static VfxLibrary Library()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            Assert.IsNotNull(library, "Run Tools/Generate/Blender VFX first.");
            return library;
        }

        [Test]
        public void EveryProjectileStyle_HasAWeaponRecipe_WithAPaintedLook()
        {
            VfxLibrary library = Library();
            foreach (ProjectileStyle style in Enum.GetValues(typeof(ProjectileStyle)))
            {
                WeaponVfxDefinition weapon = library.WeaponFor(style);
                Assert.IsNotNull(weapon, $"{style} has no weapon recipe");
                Assert.IsNotNull(weapon.Impact, $"{style} has no impact");
                Assert.AreSame(weapon.Impact, library.For(style));
                bool beam = style == ProjectileStyle.Laser || style == ProjectileStyle.Lightning;
                Assert.AreEqual(beam, weapon.Beam != null, $"{style}: only beam weapons draw a beam");
                Assert.IsTrue(weapon.Projectile != null || weapon.Trail != null || weapon.Beam != null || weapon.Exhaust != null, $"{style} flies without a painted part");
                Assert.IsTrue(weapon.Impact.Flash != null || weapon.Impact.Fireball != null, $"{style} hits without a painted flash or fireball");
                if (style != ProjectileStyle.Lightning)
                {
                    Assert.IsNotNull(weapon.Muzzle, $"{style} has no muzzle flash");
                    Assert.Less(weapon.MuzzleRow, weapon.Muzzle.Rows, $"{style} points past the muzzle atlas");
                }
            }
        }

        [Test]
        public void EverySheet_HasHighAndLowMaterials_OnTheEffectShaders()
        {
            foreach (BlenderVfxCatalog.Sheet entry in BlenderVfxCatalog.Sheets)
            {
                var sheet = AssetDatabase.LoadAssetAtPath<VfxSheet>($"{BlenderVfxBuilder.SheetFolder}/{entry.Name}.asset");
                Assert.IsNotNull(sheet, $"no sheet {entry.Name}");
                Material high = sheet.MaterialFor(false);
                Material low = sheet.MaterialFor(true);
                Assert.IsNotNull(high, $"{entry.Name} has no material");
                Assert.AreNotSame(high, low, $"{entry.Name} has no Low material");
                string shader = entry.Kind == BlenderVfxCatalog.Kind.SixWay ? Smoke : entry.Kind == BlenderVfxCatalog.Kind.Distortion ? Distortion : Particle;
                Assert.AreEqual(shader, high.shader.name, entry.Name);
                Assert.AreEqual(shader, low.shader.name, entry.Name);
                Assert.IsFalse(low.IsKeywordEnabled("_SOFT_PARTICLES"), $"{entry.Name}: the Low profile skips the depth fade");
                Assert.IsFalse(low.IsKeywordEnabled("_NOISE"), $"{entry.Name}: the Low profile skips the noise layer");
                string texture = entry.SixWay ? "_PosMap" : "_MainTex";
                Assert.IsNotNull(high.GetTexture(texture), $"{entry.Name} wears no texture");
                StringAssert.Contains("/Low/", AssetDatabase.GetAssetPath(low.GetTexture(texture)), $"{entry.Name}: the Low material wears the half-size texture");
                if (entry.SixWay)
                {
                    Assert.IsNotNull(high.GetTexture("_NegMap"), $"{entry.Name} misses its Neg map");
                    Assert.IsTrue(high.IsKeywordEnabled("_HEAT_STREAM"), $"{entry.Name} ignores the heat curve");
                }
            }
        }

        [Test]
        public void SheetLayouts_FollowTheReport()
        {
            VfxSheet large = Sheet("Explosion_Large");
            Assert.AreEqual(8, large.Columns);
            Assert.AreEqual(8, large.Rows);
            Assert.AreEqual(64, large.Frames);
            Assert.AreEqual(48f, large.Fps, 0.01f);
            Assert.AreEqual(1.33f, large.Lifetime, 0.01f);
            Assert.Less(large.Pivot.y, 0.2f, "the blast rises from the bottom of its cell");

            VfxSheet muzzle = Sheet("Muzzle_Side");
            Assert.AreEqual(4, muzzle.Columns);
            Assert.AreEqual(4, muzzle.Rows);
            Assert.AreEqual(0.5f, muzzle.CellAspect, 0.01f, "the side flash cells are twice as long as high");

            VfxSheet streaks = Sheet("Spark_Streaks");
            Assert.AreEqual(1, streaks.Columns);
            Assert.AreEqual(4, streaks.Rows);
        }

        [Test]
        public void Blasts_LeaveAFireball_ThatCoolsAndThenSmokes()
        {
            foreach (string name in new[] { "Shell", "Rocket", "Mortar", "Mine", "Barrel", "BossDeath" })
            {
                ImpactVfxDefinition vfx = Impact(name);
                Assert.IsNotNull(vfx.Fireball, $"{name} has no fireball");
                Assert.IsNotNull(vfx.SmokeSheet, $"{name} leaves no smoke");
                Assert.Greater(vfx.Heat.Evaluate(0f), 1f, $"{name} does not start white-hot");
                Assert.Less(vfx.Heat.Evaluate(0.35f), 1f, $"{name} is still white-hot at a third of its life");
                Assert.Greater(vfx.SmokeDelay, 0f, $"{name}: the smoke should follow the fire");
            }
        }

        [Test]
        public void BossDeath_IsAChainOfBigBlasts_WithHangingSmoke()
        {
            VfxLibrary library = Library();
            ImpactVfxDefinition boss = library.BossDeath;
            Assert.AreNotSame(library.Death, boss, "a boss dies like any creature");
            Assert.GreaterOrEqual(boss.Chain, 3);
            Assert.AreEqual("Explosion_Boss", boss.Fireball.name);
            Assert.IsNotNull(boss.LingerSmoke);
            Assert.Less(boss.ChainSeconds, 1f, "the chain drags on");
        }

        [Test]
        public void OnlyBigBlasts_KickTheCamera_AndAKickIsShort()
        {
            foreach (string name in QuietImpacts)
            {
                ImpactVfxDefinition vfx = Impact(name);
                Assert.AreEqual(0f, vfx.ShakeFor(2f), $"{name} shakes the camera");
            }

            Assert.Greater(Impact("Mortar").ShakeFor(1.8f), 0f, "heavy artillery should kick");
            Assert.Greater(Library().BossDeath.ShakeFor(1.5f), 0f, "a boss going down should kick");
            Assert.Less(ShakeCurve.Fade(1f, KickSeconds), ShakeCurve.Floor, "a full kick still runs after the kick time");
        }

        [Test]
        public void LowProfile_PlaysNoDistortion()
        {
            Assert.IsFalse(new SwitchPlatformSettings().DistortionEnabled);
            Assert.IsTrue(new PcPlatformSettings().DistortionEnabled);
            var low = new VfxQuality(0.5f, true, false);
            Assert.IsFalse(low.Lights, "a halved particle budget goes without blast lights");
        }

        private static VfxSheet Sheet(string name)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<VfxSheet>($"{BlenderVfxBuilder.SheetFolder}/{name}.asset");
            Assert.IsNotNull(sheet, $"no sheet {name}");
            return sheet;
        }

        private static ImpactVfxDefinition Impact(string name)
        {
            var vfx = AssetDatabase.LoadAssetAtPath<ImpactVfxDefinition>($"Assets/_Project/Data/Vfx/Vfx_{name}.asset");
            Assert.IsNotNull(vfx, $"no impact {name}");
            return vfx;
        }
    }
}
