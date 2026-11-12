using System.Collections.Generic;
using System.Reflection;
using Game.Combat;
using Game.Core;
using Game.Hero;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 50 (Remco: "Spencer is already shooting while he is still drawing his pistol"): the first shot or blow of an
    /// engagement waits the weapon's draw time; while the weapon stays drawn between strikes nothing waits, so the rate of
    /// fire is unchanged. The draw times in the data cover the hero art's draw clips, which end in the aim or the guard.
    /// </summary>
    public sealed class HeroDrawTests
    {
        private const string HeroFolder = "Assets/_Project/Data/Hero";
        private const float DrawSeconds = 0.5f;
        private const float StayDrawnSeconds = 4f;
        private const float GunDamage = 4f;
        private const float Tolerance = 0.001f;
        private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }
            public bool IsAlive => true;
            public TargetLayers Layer => TargetLayers.Ground;
            public float PathProgress => 0f;
            public float Taken { get; private set; }

            public void TakeDamage(float amount) => Taken += amount;

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }

        private sealed class FakeGuarded : IGuardable
        {
            public Vector3 Position { get; set; }
            public IDamageGuard Guard { get; set; }
            public bool CanRestore => false;

            public void Restore(float fraction)
            {
            }
        }

        [Test]
        public void FirstShot_WaitsTheDraw_ThenTheGunKeepsItsPace_AndDrawsAgainAfterAPause()
        {
            var hero = ScriptableObject.CreateInstance<HeroDefinition>();
            var blade = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            var gun = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            var gunProps = new SerializedObject(gun);
            gunProps.FindProperty("_kind").intValue = (int)HeroWeaponKind.Ranged;
            gunProps.FindProperty("_damage").floatValue = GunDamage;
            gunProps.FindProperty("_reach").floatValue = 3.5f;
            gunProps.FindProperty("_drawSeconds").floatValue = DrawSeconds;
            gunProps.FindProperty("_stayDrawnSeconds").floatValue = StayDrawnSeconds;
            gunProps.ApplyModifiedPropertiesWithoutUndo();
            var heroProps = new SerializedObject(hero);
            heroProps.FindProperty("_weapon").objectReferenceValue = blade;
            heroProps.FindProperty("_rangedWeapon").objectReferenceValue = gun;
            heroProps.ApplyModifiedPropertiesWithoutUndo();

            var targets = new TargetRegistry();
            var far = new FakeTarget { Position = new Vector3(2.35f, 0f, 0f) };
            targets.Add(far);
            var go = new GameObject("DrawHero");
            var commander = go.AddComponent<Commander>();
            int draws = 0;
            int shots = 0;
            commander.Drawing += weapon => draws++;
            commander.Struck += (position, weapon) => shots++;
            commander.Initialize(hero, targets, new List<HeroPost> { new HeroPost(new FakeGuarded(), new Vector3(-0.65f, 0f, 0f), Vector3.right) }, 0, PlayerModifiers.None);
            try
            {
                Assert.IsFalse(StrikeOnce(commander), "the first shot should wait for the draw");
                Assert.AreEqual(1, draws, "the gun should be drawn");
                Assert.AreEqual(0f, far.Taken, Tolerance, "nothing lands while the gun is drawn");

                Tick(commander, DrawSeconds * 0.6f);
                Assert.IsFalse(StrikeOnce(commander), "still drawing");
                Tick(commander, DrawSeconds * 0.5f);
                Assert.IsTrue(StrikeOnce(commander), "drawn and aimed, the gun fires");
                Assert.AreEqual(GunDamage, far.Taken, Tolerance);

                Assert.IsTrue(StrikeOnce(commander), "while drawn the next shot waits for nothing but its interval");
                Assert.AreEqual(1, draws, "no second draw between shots");
                Assert.AreEqual(2, shots);

                Tick(commander, StayDrawnSeconds + 0.1f);
                Assert.IsFalse(StrikeOnce(commander), "after a pause the gun is put away and drawn again");
                Assert.AreEqual(2, draws);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(blade);
                Object.DestroyImmediate(gun);
            }
        }

        [Test]
        public void HeroWeapons_DrawTimes_CoverTheirDrawClips()
        {
            var art = AssetDatabase.LoadAssetAtPath<HeroArtDefinition>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:HeroArtDefinition")[0]));
            foreach (string guid in AssetDatabase.FindAssets("t:HeroWeaponDefinition", new[] { HeroFolder }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<HeroWeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                ModelClipRole role = weapon.Kind == HeroWeaponKind.Ranged ? ModelClipRole.DrawPistol : ModelClipRole.DrawBlade;
                int clip = art.Body.IndexOf(role);
                Assert.GreaterOrEqual(clip, 0, $"the hero art has no {role} clip");
                Assert.GreaterOrEqual(weapon.DrawSeconds, art.Body.Clips[clip].Length - Tolerance, $"{weapon.name}: fires before its {role} clip reaches the aim");
                Assert.Greater(weapon.StayDrawnSeconds, weapon.AttackInterval, $"{weapon.name}: put away between two strikes");
            }
        }

        private static bool StrikeOnce(Commander commander) => (bool)typeof(Commander).GetMethod("StrikeOnce", Private).Invoke(commander, null);

        private static void Tick(Commander commander, float seconds) => typeof(Commander).GetMethod("TickCooldowns", Private).Invoke(commander, new object[] { seconds });
    }
}
