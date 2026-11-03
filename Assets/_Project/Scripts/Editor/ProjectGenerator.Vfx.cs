using Game.Combat;
using Game.PathObjects;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the impact effects: one definition per kind of blast and the library that maps them.</summary>
    public static partial class ProjectGenerator
    {
        private const string VfxFolder = DataRoot + "/Vfx";
        private const string VfxLibraryPath = VfxFolder + "/VfxLibrary.asset";

        private struct VfxSpec
        {
            public string Name;
            public float FlashScale, FlashSeconds, FireShare;
            public bool Ring;
            public float RingScale, RingAlpha;
            public int Sparks, SparksPerRadius;
            public float SparkSpeed;
            public int Smoke, SmokePerRadius, Shards, ShardsPerRadius;
            public float ShardSpeed, LightIntensity, LightRange, LightSeconds, Shake, ShakePerRadius;
        }

        // A plain hit (bolt, beam, shard, arc, slug) is small and quick; every blast gets a ring, a light and a kick
        // that grow with its radius; the boss pulse keeps its own ring and sparks (BossAbilityView) and adds only
        // the flash, the light and the kick.
        private static readonly VfxSpec[] VfxSpecs =
        {
            new VfxSpec { Name = "Hit", FlashScale = 1f, FlashSeconds = 0.18f, FireShare = 0.4f, Ring = false, RingScale = 0f, RingAlpha = 0f, Sparks = 6, SparksPerRadius = 0, SparkSpeed = 3f, Smoke = 0, SmokePerRadius = 0, Shards = 0, ShardsPerRadius = 0, ShardSpeed = 0f, LightIntensity = 0f, LightRange = 0f, LightSeconds = 0.1f, Shake = 0f, ShakePerRadius = 0f },
            new VfxSpec { Name = "Shell", FlashScale = 0.8f, FlashSeconds = 0.3f, FireShare = 0.7f, Ring = true, RingScale = 2.8f, RingAlpha = 0.55f, Sparks = 14, SparksPerRadius = 14, SparkSpeed = 4f, Smoke = 6, SmokePerRadius = 6, Shards = 8, ShardsPerRadius = 8, ShardSpeed = 5f, LightIntensity = 6f, LightRange = 5f, LightSeconds = 0.3f, Shake = 0.15f, ShakePerRadius = 0.15f },
            new VfxSpec { Name = "Rocket", FlashScale = 0.9f, FlashSeconds = 0.35f, FireShare = 0.8f, Ring = true, RingScale = 3.2f, RingAlpha = 0.6f, Sparks = 18, SparksPerRadius = 16, SparkSpeed = 5f, Smoke = 10, SmokePerRadius = 8, Shards = 6, ShardsPerRadius = 6, ShardSpeed = 6f, LightIntensity = 9f, LightRange = 6f, LightSeconds = 0.35f, Shake = 0.2f, ShakePerRadius = 0.18f },
            // Flak bursts at flyer height: a short amber flash, ten shrapnel sparks and a drifting grey-brown puff; no ground ring, no kick.
            new VfxSpec { Name = "FlakBurst", FlashScale = 0.6f, FlashSeconds = 0.1f, FireShare = 0.9f, Ring = false, RingScale = 0f, RingAlpha = 0f, Sparks = 10, SparksPerRadius = 0, SparkSpeed = 5f, Smoke = 3, SmokePerRadius = 0, Shards = 0, ShardsPerRadius = 0, ShardSpeed = 0f, LightIntensity = 4f, LightRange = 3f, LightSeconds = 0.12f, Shake = 0f, ShakePerRadius = 0f },
            new VfxSpec { Name = "Mortar", FlashScale = 1f, FlashSeconds = 0.4f, FireShare = 0.75f, Ring = true, RingScale = 3.4f, RingAlpha = 0.6f, Sparks = 16, SparksPerRadius = 16, SparkSpeed = 4.5f, Smoke = 12, SmokePerRadius = 10, Shards = 14, ShardsPerRadius = 10, ShardSpeed = 6f, LightIntensity = 9f, LightRange = 6f, LightSeconds = 0.4f, Shake = 0.25f, ShakePerRadius = 0.2f },
            new VfxSpec { Name = "Death", FlashScale = 0.8f, FlashSeconds = 0.25f, FireShare = 0.4f, Ring = true, RingScale = 2f, RingAlpha = 0.35f, Sparks = 8, SparksPerRadius = 8, SparkSpeed = 3f, Smoke = 2, SmokePerRadius = 3, Shards = 0, ShardsPerRadius = 0, ShardSpeed = 0f, LightIntensity = 2f, LightRange = 3f, LightSeconds = 0.2f, Shake = 0f, ShakePerRadius = 0f },
            new VfxSpec { Name = "Mine", FlashScale = 0.9f, FlashSeconds = 0.35f, FireShare = 0.7f, Ring = true, RingScale = 3f, RingAlpha = 0.6f, Sparks = 16, SparksPerRadius = 12, SparkSpeed = 4.5f, Smoke = 8, SmokePerRadius = 6, Shards = 16, ShardsPerRadius = 10, ShardSpeed = 7f, LightIntensity = 8f, LightRange = 5f, LightSeconds = 0.3f, Shake = 0.3f, ShakePerRadius = 0.12f },
            new VfxSpec { Name = "Barrel", FlashScale = 1f, FlashSeconds = 0.4f, FireShare = 0.85f, Ring = true, RingScale = 3.2f, RingAlpha = 0.6f, Sparks = 20, SparksPerRadius = 10, SparkSpeed = 5f, Smoke = 14, SmokePerRadius = 8, Shards = 12, ShardsPerRadius = 8, ShardSpeed = 6f, LightIntensity = 10f, LightRange = 7f, LightSeconds = 0.4f, Shake = 0.35f, ShakePerRadius = 0.12f },
            new VfxSpec { Name = "BossPulse", FlashScale = 0.45f, FlashSeconds = 0.45f, FireShare = 0f, Ring = false, RingScale = 0f, RingAlpha = 0f, Sparks = 0, SparksPerRadius = 0, SparkSpeed = 0f, Smoke = 0, SmokePerRadius = 0, Shards = 0, ShardsPerRadius = 0, ShardSpeed = 0f, LightIntensity = 12f, LightRange = 9f, LightSeconds = 0.5f, Shake = 0.6f, ShakePerRadius = 0f },
        };

        private static readonly (ProjectileStyle Style, string Vfx)[] StyleVfx =
        {
            (ProjectileStyle.Bolt, "Hit"), (ProjectileStyle.Laser, "Hit"), (ProjectileStyle.Frost, "Hit"), (ProjectileStyle.Lightning, "Hit"), (ProjectileStyle.Rail, "Hit"),
            (ProjectileStyle.Shell, "Shell"), (ProjectileStyle.Rocket, "Rocket"), (ProjectileStyle.Mortar, "Mortar"), (ProjectileStyle.Flak, "FlakBurst"),
        };

        private static readonly (string PathObject, string Vfx)[] PathObjectVfx = { ("Mine", "Mine"), ("ExplosiveBarrel", "Barrel") };

        private static void CreateVfxLibrary()
        {
            foreach (VfxSpec spec in VfxSpecs)
            {
                var vfx = LoadOrCreate<ImpactVfxDefinition>(VfxPath(spec.Name));
                var so = new SerializedObject(vfx);
                so.FindProperty("_flashScale").floatValue = spec.FlashScale;
                so.FindProperty("_flashSeconds").floatValue = spec.FlashSeconds;
                so.FindProperty("_fireShare").floatValue = spec.FireShare;
                so.FindProperty("_ring").boolValue = spec.Ring;
                so.FindProperty("_ringScale").floatValue = spec.RingScale;
                so.FindProperty("_ringAlpha").floatValue = spec.RingAlpha;
                so.FindProperty("_sparks").intValue = spec.Sparks;
                so.FindProperty("_sparksPerRadius").intValue = spec.SparksPerRadius;
                so.FindProperty("_sparkSpeed").floatValue = spec.SparkSpeed;
                so.FindProperty("_smoke").intValue = spec.Smoke;
                so.FindProperty("_smokePerRadius").intValue = spec.SmokePerRadius;
                so.FindProperty("_shards").intValue = spec.Shards;
                so.FindProperty("_shardsPerRadius").intValue = spec.ShardsPerRadius;
                so.FindProperty("_shardSpeed").floatValue = spec.ShardSpeed;
                so.FindProperty("_lightIntensity").floatValue = spec.LightIntensity;
                so.FindProperty("_lightRange").floatValue = spec.LightRange;
                so.FindProperty("_lightSeconds").floatValue = spec.LightSeconds;
                so.FindProperty("_shake").floatValue = spec.Shake;
                so.FindProperty("_shakePerRadius").floatValue = spec.ShakePerRadius;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var library = LoadOrCreate<VfxLibrary>(VfxLibraryPath);
            var lib = new SerializedObject(library);
            lib.FindProperty("_default").objectReferenceValue = Vfx("Hit");
            lib.FindProperty("_death").objectReferenceValue = Vfx("Death");
            lib.FindProperty("_mine").objectReferenceValue = Vfx("Mine");
            lib.FindProperty("_bossPulse").objectReferenceValue = Vfx("BossPulse");
            SerializedProperty styles = lib.FindProperty("_styles");
            styles.arraySize = StyleVfx.Length;
            for (int i = 0; i < StyleVfx.Length; i++)
            {
                SerializedProperty entry = styles.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Style").enumValueIndex = (int)StyleVfx[i].Style;
                entry.FindPropertyRelative("Vfx").objectReferenceValue = Vfx(StyleVfx[i].Vfx);
            }

            lib.ApplyModifiedPropertiesWithoutUndo();

            foreach ((string id, string name) in PathObjectVfx)
            {
                var asset = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>($"{DataRoot}/PathObjects/{id}.asset");
                if (asset != null)
                {
                    var so = new SerializedObject(asset);
                    so.FindProperty("_impactVfx").objectReferenceValue = Vfx(name);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static string VfxPath(string name) => $"{VfxFolder}/Vfx_{name}.asset";

        private static ImpactVfxDefinition Vfx(string name) => AssetDatabase.LoadAssetAtPath<ImpactVfxDefinition>(VfxPath(name));
    }
}
