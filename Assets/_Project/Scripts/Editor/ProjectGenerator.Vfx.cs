using System.Collections.Generic;
using Game.Combat;
using Game.PathObjects;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the effects: one impact definition per kind of blast, one weapon recipe per projectile style
    /// (muzzle, flight, impact), and the library that maps them. They wear the sheets painted in Blender
    /// (<see cref="BlenderVfxBuilder"/>); without the sheets every part falls back to its plain procedural look.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string VfxFolder = DataRoot + "/Vfx";
        private const string VfxLibraryPath = VfxFolder + "/VfxLibrary.asset";

        private const string ExplosionSmall = "Explosion_Small";
        private const string ExplosionLarge = "Explosion_Large";
        private const string ExplosionBoss = "Explosion_Boss";
        private const string SmokePuff = "Smoke_Puff";
        private const string SmokeLinger = "Smoke_Linger";
        private const string DustBurst = "Dust_Burst";
        private const string EnergyImpact = "Energy_Impact";
        private const string SparkBurst = "Spark_Burst";
        private const string SparkStreaks = "Spark_Streaks";
        private const string CryoBurst = "Cryo_Burst";
        private const string DebrisShards = "Debris_Shards";
        private const string ShockRing = "Shockwave_Ring";
        private const string ShockDistort = "Shockwave_Distort";
        private const string HeatHaze = "HeatHaze_Distort";
        private const string MuzzleSide = "Muzzle_Side";
        private const string BoltCore = "Bolt_Core";
        private const string PlasmaBolt = "Plasma_Bolt";
        private const string RocketExhaust = "Rocket_Exhaust";
        private const string TeslaArcs = "Tesla_Arcs";
        private const string BeamCore = "Beam_Core";
        private const string TrailRibbon = "Trail_Ribbon";
        private const string SmokeTrail = "Smoke_Trail";

        // Rows of the muzzle flash atlas (VFX_report.json).
        private const int GunRow = 0;
        private const int CannonRow = 1;
        private const int FlakRow = 2;
        private const int RailRow = 3;

        private sealed class VfxSpec
        {
            public string Name;
            public float FlashScale = 1f, FlashSeconds = 0.25f, FireShare = 0.6f;
            public bool Ring;
            public float RingScale, RingAlpha;
            public int Sparks, SparksPerRadius;
            public float SparkSpeed = 3f;
            public int Smoke, SmokePerRadius, Shards, ShardsPerRadius;
            public float ShardSpeed = 5f, LightIntensity, LightRange = 4f, LightSeconds = 0.3f, Shake, ShakePerRadius;
            public string Fireball, Flash, SmokeSheet, Sparkle = SparkStreaks, Debris, RingSheet, Dust, ShockSheet, Haze, Linger;
            public int Fireballs = 1;
            public float FireballSize = 1.5f, FireballSizePerRadius = 1.2f, FlashSize = 0.8f, FlashSizePerRadius = 1f, SmokeSize = 1.2f, SmokeDelay = 0.15f, DustSize;
            public float ShockStrength, HazeSeconds = 0.9f;
            // Remco 2026-11-04: smoke must never hide the field for long; a boss death keeps a little more of its hanging smoke.
            public float SmokeOpacity = 0.45f, SmokeLifeScale = 0.7f;
            public int Chain = 1;
            public float ChainInterval = 0.25f, ChainSpread = 1f, ChainGrowth = 0.15f;
        }

        // Energy hits flash in the weapon's colour and spit sparks; blasts leave a white-hot fireball that cools to
        // orange, then smoke. Only heavy artillery, path-object blasts, the boss pulse and a boss going down kick the
        // camera, and only briefly (CameraShaker): an everyday hit never shakes it.
        private static readonly VfxSpec[] VfxSpecs =
        {
            new VfxSpec { Name = "Hit", FlashSeconds = 0.18f, FireShare = 0.4f, Sparks = 5, Flash = EnergyImpact, FlashSize = 0.6f, FlashSizePerRadius = 0f },
            new VfxSpec { Name = "Frost", FlashSeconds = 0.2f, FireShare = 0f, Sparks = 3, SparkSpeed = 2f, Flash = CryoBurst, FlashSize = 0.9f, FlashSizePerRadius = 0f, LightIntensity = 1.5f, LightRange = 2.5f, LightSeconds = 0.2f },
            new VfxSpec { Name = "Arc", FlashSeconds = 0.18f, FireShare = 0.15f, Sparks = 8, SparkSpeed = 4f, Flash = SparkBurst, FlashSize = 0.8f, FlashSizePerRadius = 0.4f, LightIntensity = 2.5f, LightRange = 3f, LightSeconds = 0.15f },
            new VfxSpec { Name = "Rail", FlashSeconds = 0.22f, FireShare = 0.2f, Sparks = 12, SparkSpeed = 5f, Flash = EnergyImpact, FlashSize = 1.1f, FlashSizePerRadius = 0f, LightIntensity = 3f, LightRange = 3f, LightSeconds = 0.18f },
            new VfxSpec
            {
                Name = "Shell", FlashScale = 0.8f, FlashSeconds = 0.3f, FireShare = 0.7f, Ring = true, RingScale = 2.4f, RingAlpha = 0.5f, Sparks = 8, SparksPerRadius = 6, SparkSpeed = 4f,
                Smoke = 2, SmokePerRadius = 1, Shards = 4, ShardsPerRadius = 4, LightIntensity = 6f, LightRange = 5f,
                Fireball = ExplosionSmall, FireballSize = 1.3f, FireballSizePerRadius = 0.9f, Flash = SparkBurst, FlashSize = 0.5f, FlashSizePerRadius = 0.4f, SmokeSheet = SmokePuff, SmokeSize = 1.1f,
                Debris = DebrisShards, RingSheet = ShockRing, ShockSheet = ShockDistort, ShockStrength = 0.02f,
            },
            new VfxSpec
            {
                Name = "Rocket", FlashScale = 0.9f, FlashSeconds = 0.35f, FireShare = 0.8f, Ring = true, RingScale = 2.8f, RingAlpha = 0.55f, Sparks = 10, SparksPerRadius = 8, SparkSpeed = 5f,
                Smoke = 2, SmokePerRadius = 1, Shards = 3, ShardsPerRadius = 3, ShardSpeed = 6f, LightIntensity = 8f, LightRange = 6f, LightSeconds = 0.35f,
                Fireball = ExplosionLarge, FireballSize = 1.4f, FireballSizePerRadius = 1f, Flash = SparkBurst, FlashSize = 0.6f, FlashSizePerRadius = 0.4f, SmokeSheet = SmokePuff, SmokeSize = 1.2f,
                Debris = DebrisShards, RingSheet = ShockRing, ShockSheet = ShockDistort, ShockStrength = 0.025f,
            },
            // Flak bursts at flyer height: a small hot puff, shrapnel sparks and a short grey cloud; no ground ring, no kick.
            new VfxSpec
            {
                Name = "FlakBurst", FlashScale = 0.6f, FlashSeconds = 0.1f, FireShare = 0.9f, Sparks = 10, SparkSpeed = 5f, Smoke = 1, LightIntensity = 4f, LightRange = 3f, LightSeconds = 0.12f,
                Fireball = ExplosionSmall, FireballSize = 0.6f, FireballSizePerRadius = 0.5f, Flash = SparkBurst, FlashSize = 0.5f, FlashSizePerRadius = 0.3f, SmokeSheet = SmokePuff, SmokeSize = 0.7f, SmokeDelay = 0.08f,
            },
            new VfxSpec
            {
                Name = "Mortar", FlashSeconds = 0.4f, FireShare = 0.75f, Ring = true, RingScale = 3f, RingAlpha = 0.6f, Sparks = 14, SparksPerRadius = 8, SparkSpeed = 4.5f,
                Smoke = 3, SmokePerRadius = 1, Shards = 10, ShardsPerRadius = 6, ShardSpeed = 6f, LightIntensity = 9f, LightRange = 6f, LightSeconds = 0.4f, Shake = 0.25f, ShakePerRadius = 0.1f,
                Fireball = ExplosionLarge, Fireballs = 2, FireballSize = 1.6f, FireballSizePerRadius = 1.1f, Flash = SparkBurst, FlashSize = 0.8f, FlashSizePerRadius = 0.5f, SmokeSheet = SmokePuff, SmokeSize = 1.5f,
                Debris = DebrisShards, RingSheet = ShockRing, Dust = DustBurst, DustSize = 2.2f, ShockSheet = ShockDistort, ShockStrength = 0.022f, Haze = HeatHaze, HazeSeconds = 1f,
            },
            new VfxSpec
            {
                Name = "Death", FlashScale = 0.8f, FlashSeconds = 0.25f, FireShare = 0.4f, Ring = true, RingScale = 2f, RingAlpha = 0.3f, Sparks = 6, SparksPerRadius = 6, Smoke = 1,
                LightIntensity = 2f, LightRange = 3f, LightSeconds = 0.2f, Flash = SparkBurst, FlashSize = 0.6f, FlashSizePerRadius = 0.4f, SmokeSheet = SmokePuff, SmokeSize = 0.8f, RingSheet = ShockRing,
            },
            // A boss goes down in three blasts that grow, white-hot to orange, with a kick on the first and smoke that hangs.
            new VfxSpec
            {
                Name = "BossDeath", FlashSeconds = 0.45f, FireShare = 0.9f, Ring = true, RingScale = 3.4f, RingAlpha = 0.7f, Sparks = 18, SparksPerRadius = 8, SparkSpeed = 5f,
                Smoke = 3, SmokePerRadius = 0, Shards = 12, ShardsPerRadius = 6, ShardSpeed = 7f, LightIntensity = 14f, LightRange = 10f, LightSeconds = 0.5f, Shake = 0.8f,
                Fireball = ExplosionBoss, Fireballs = 2, FireballSize = 2.6f, FireballSizePerRadius = 1.2f, Flash = EnergyImpact, FlashSize = 1.2f, FlashSizePerRadius = 0.6f, SmokeSheet = SmokePuff, SmokeSize = 1.8f,
                Debris = DebrisShards, RingSheet = ShockRing, Dust = DustBurst, DustSize = 2.5f, ShockSheet = ShockDistort, ShockStrength = 0.03f, Haze = HeatHaze, HazeSeconds = 1.4f, Linger = SmokeLinger,
                Chain = 3, ChainInterval = 0.28f, ChainSpread = 1.4f, ChainGrowth = 0.2f, SmokeOpacity = 0.55f, SmokeLifeScale = 0.85f,
            },
            new VfxSpec
            {
                Name = "Mine", FlashScale = 0.9f, FlashSeconds = 0.35f, FireShare = 0.7f, Ring = true, RingScale = 3f, RingAlpha = 0.6f, Sparks = 12, SparksPerRadius = 8, SparkSpeed = 4.5f,
                Smoke = 3, Shards = 10, ShardsPerRadius = 6, ShardSpeed = 7f, LightIntensity = 8f, LightRange = 5f, Shake = 0.25f,
                Fireball = ExplosionSmall, Fireballs = 2, FireballSize = 1.3f, FireballSizePerRadius = 0.9f, Flash = SparkBurst, FlashSize = 0.6f, FlashSizePerRadius = 0.4f, SmokeSheet = SmokePuff,
                Debris = DebrisShards, RingSheet = ShockRing, Dust = DustBurst, DustSize = 1.6f, ShockSheet = ShockDistort, ShockStrength = 0.02f,
            },
            new VfxSpec
            {
                Name = "Barrel", FlashSeconds = 0.4f, FireShare = 0.85f, Ring = true, RingScale = 3.2f, RingAlpha = 0.6f, Sparks = 16, SparksPerRadius = 6, SparkSpeed = 5f,
                Smoke = 4, Shards = 10, ShardsPerRadius = 6, ShardSpeed = 6f, LightIntensity = 10f, LightRange = 7f, LightSeconds = 0.4f, Shake = 0.3f,
                Fireball = ExplosionLarge, Fireballs = 2, FireballSize = 1.8f, FireballSizePerRadius = 1f, Flash = SparkBurst, FlashSize = 0.8f, FlashSizePerRadius = 0.4f, SmokeSheet = SmokePuff, SmokeSize = 1.5f,
                Debris = DebrisShards, RingSheet = ShockRing, Dust = DustBurst, DustSize = 2f, ShockSheet = ShockDistort, ShockStrength = 0.022f, Haze = HeatHaze, HazeSeconds = 1f,
            },
            // A spawn origin breaking open: earth thrown up, dust and grit, a faint flash from the maw, no fire; a short kick.
            new VfxSpec
            {
                Name = "OriginBurst", FlashScale = 0.5f, FlashSeconds = 0.25f, FireShare = 0f, Ring = true, RingScale = 2.6f, RingAlpha = 0.35f, Sparks = 6, SparkSpeed = 3f,
                Smoke = 3, SmokePerRadius = 1, Shards = 14, ShardsPerRadius = 6, ShardSpeed = 5f, LightIntensity = 4f, LightRange = 4f, LightSeconds = 0.3f, Shake = 0.35f,
                Flash = SparkBurst, FlashSize = 0.5f, FlashSizePerRadius = 0.2f, SmokeSheet = SmokePuff, SmokeSize = 1.4f, SmokeDelay = 0.05f,
                Debris = DebrisShards, RingSheet = ShockRing, Dust = DustBurst, DustSize = 2.4f,
            },
            // The boss pulse keeps its own ring and sparks (BossAbilityView) and adds the flash, the light and a kick.
            new VfxSpec { Name = "BossPulse", FlashScale = 0.45f, FlashSeconds = 0.45f, FireShare = 0f, LightIntensity = 12f, LightRange = 9f, LightSeconds = 0.5f, Shake = 0.45f, Flash = EnergyImpact, FlashSize = 1.6f, FlashSizePerRadius = 0f, Sparkle = null },
        };

        private sealed class WeaponSpec
        {
            public string Name;
            public ProjectileStyle Style;
            public string Muzzle = MuzzleSide;
            public int MuzzleRow;
            public float MuzzleLength = 0.5f, MuzzleSeconds = 0.05f;
            public string Projectile;
            public float ProjectileLength = 0.5f, ProjectileWidth = 0.14f;
            public string Trail = TrailRibbon;
            public float TrailWidth = 0.08f, TrailSeconds = 0.15f;
            public string Exhaust, SmokeRibbon, Beam;
            public float ExhaustLength = 0.5f, SmokeTrailWidth = 0.25f, SmokeTrailSeconds = 0.8f, BeamWidth = 0.3f;
            public string Impact;
        }

        // One recipe per weapon family; every tower of a style wears it in its own colour.
        private static readonly WeaponSpec[] WeaponSpecs =
        {
            new WeaponSpec { Name = "Pulse", Style = ProjectileStyle.Bolt, MuzzleRow = GunRow, MuzzleLength = 0.45f, Projectile = BoltCore, ProjectileLength = 0.55f, ProjectileWidth = 0.16f, TrailWidth = 0.07f, TrailSeconds = 0.12f, Impact = "Hit" },
            new WeaponSpec { Name = "Cannon", Style = ProjectileStyle.Shell, MuzzleRow = CannonRow, MuzzleLength = 0.85f, MuzzleSeconds = 0.07f, TrailWidth = 0.08f, TrailSeconds = 0.1f, Impact = "Shell" },
            new WeaponSpec { Name = "Rocket", Style = ProjectileStyle.Rocket, MuzzleRow = CannonRow, MuzzleLength = 0.6f, Trail = null, Exhaust = RocketExhaust, ExhaustLength = 0.5f, SmokeRibbon = SmokeTrail, SmokeTrailWidth = 0.22f, SmokeTrailSeconds = 0.7f, Impact = "Rocket" },
            new WeaponSpec { Name = "Laser", Style = ProjectileStyle.Laser, MuzzleRow = RailRow, MuzzleLength = 0.4f, Trail = null, Beam = BeamCore, BeamWidth = 0.32f, Impact = "Hit" },
            new WeaponSpec { Name = "Cryo", Style = ProjectileStyle.Frost, MuzzleRow = RailRow, MuzzleLength = 0.4f, Projectile = PlasmaBolt, ProjectileLength = 0.55f, ProjectileWidth = 0.4f, TrailWidth = 0.1f, TrailSeconds = 0.2f, Impact = "Frost" },
            new WeaponSpec { Name = "Tesla", Style = ProjectileStyle.Lightning, Muzzle = null, Trail = null, Beam = TeslaArcs, BeamWidth = 0.7f, Impact = "Arc" },
            new WeaponSpec { Name = "Rail", Style = ProjectileStyle.Rail, MuzzleRow = RailRow, MuzzleLength = 0.9f, MuzzleSeconds = 0.06f, Projectile = BoltCore, ProjectileLength = 1.1f, ProjectileWidth = 0.22f, TrailWidth = 0.2f, TrailSeconds = 0.35f, Impact = "Rail" },
            new WeaponSpec { Name = "Artillery", Style = ProjectileStyle.Mortar, MuzzleRow = CannonRow, MuzzleLength = 1f, MuzzleSeconds = 0.08f, TrailWidth = 0.06f, TrailSeconds = 0.12f, Impact = "Mortar" },
            new WeaponSpec { Name = "Flak", Style = ProjectileStyle.Flak, MuzzleRow = FlakRow, MuzzleLength = 0.6f, Projectile = BoltCore, ProjectileLength = 0.4f, ProjectileWidth = 0.12f, TrailWidth = 0.05f, TrailSeconds = 0.08f, Impact = "FlakBurst" },
        };

        private static readonly (string PathObject, string Vfx)[] PathObjectVfx = { ("Mine", "Mine"), ("ExplosiveBarrel", "Barrel") };

        // The generator step: the Blender sheets first, then the recipes that wear them.
        private static void CreateVfxLibrary() => CreateVfxLibraryFrom(BlenderVfxBuilder.SyncAndBuild());

        internal static void CreateVfxLibraryFrom(IReadOnlyDictionary<string, VfxSheet> sheets)
        {
            foreach (VfxSpec spec in VfxSpecs)
            {
                WriteImpact(LoadOrCreate<ImpactVfxDefinition>(VfxPath(spec.Name)), spec, sheets);
            }

            foreach (WeaponSpec spec in WeaponSpecs)
            {
                WriteWeapon(LoadOrCreate<WeaponVfxDefinition>(WeaponPath(spec.Name)), spec, sheets);
            }

            var library = LoadOrCreate<VfxLibrary>(VfxLibraryPath);
            var lib = new SerializedObject(library);
            lib.FindProperty("_default").objectReferenceValue = Vfx("Hit");
            lib.FindProperty("_death").objectReferenceValue = Vfx("Death");
            lib.FindProperty("_bossDeath").objectReferenceValue = Vfx("BossDeath");
            lib.FindProperty("_mine").objectReferenceValue = Vfx("Mine");
            lib.FindProperty("_bossPulse").objectReferenceValue = Vfx("BossPulse");
            SerializedProperty styles = lib.FindProperty("_styles");
            styles.arraySize = WeaponSpecs.Length;
            for (int i = 0; i < WeaponSpecs.Length; i++)
            {
                SerializedProperty entry = styles.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Style").enumValueIndex = (int)WeaponSpecs[i].Style;
                entry.FindPropertyRelative("Weapon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponVfxDefinition>(WeaponPath(WeaponSpecs[i].Name));
            }

            lib.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);

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

        private static void WriteImpact(ImpactVfxDefinition vfx, VfxSpec spec, IReadOnlyDictionary<string, VfxSheet> sheets)
        {
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
            so.FindProperty("_fireball").objectReferenceValue = Sheet(sheets, spec.Fireball);
            so.FindProperty("_fireballs").intValue = spec.Fireballs;
            so.FindProperty("_fireballSize").floatValue = spec.FireballSize;
            so.FindProperty("_fireballSizePerRadius").floatValue = spec.FireballSizePerRadius;
            so.FindProperty("_flash").objectReferenceValue = Sheet(sheets, spec.Flash);
            so.FindProperty("_flashSize").floatValue = spec.FlashSize;
            so.FindProperty("_flashSizePerRadius").floatValue = spec.FlashSizePerRadius;
            so.FindProperty("_smokeSheet").objectReferenceValue = Sheet(sheets, spec.SmokeSheet);
            so.FindProperty("_smokeSize").floatValue = spec.SmokeSize;
            so.FindProperty("_smokeDelay").floatValue = spec.SmokeDelay;
            so.FindProperty("_smokeOpacity").floatValue = spec.SmokeOpacity;
            so.FindProperty("_smokeLifeScale").floatValue = spec.SmokeLifeScale;
            so.FindProperty("_sparkSheet").objectReferenceValue = Sheet(sheets, spec.Sparkle);
            so.FindProperty("_debrisSheet").objectReferenceValue = Sheet(sheets, spec.Debris);
            so.FindProperty("_ringSheet").objectReferenceValue = Sheet(sheets, spec.RingSheet);
            so.FindProperty("_dustSheet").objectReferenceValue = Sheet(sheets, spec.Dust);
            so.FindProperty("_dustSize").floatValue = spec.DustSize;
            so.FindProperty("_shockDistortion").objectReferenceValue = Sheet(sheets, spec.ShockSheet);
            so.FindProperty("_shockStrength").floatValue = spec.ShockStrength;
            so.FindProperty("_heatHaze").objectReferenceValue = Sheet(sheets, spec.Haze);
            so.FindProperty("_heatHazeSeconds").floatValue = spec.HazeSeconds;
            so.FindProperty("_chain").intValue = spec.Chain;
            so.FindProperty("_chainInterval").floatValue = spec.ChainInterval;
            so.FindProperty("_chainSpread").floatValue = spec.ChainSpread;
            so.FindProperty("_chainGrowth").floatValue = spec.ChainGrowth;
            so.FindProperty("_lingerSmoke").objectReferenceValue = Sheet(sheets, spec.Linger);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(vfx);
        }

        private static void WriteWeapon(WeaponVfxDefinition weapon, WeaponSpec spec, IReadOnlyDictionary<string, VfxSheet> sheets)
        {
            var so = new SerializedObject(weapon);
            so.FindProperty("_muzzle").objectReferenceValue = Sheet(sheets, spec.Muzzle);
            so.FindProperty("_muzzleRow").intValue = spec.MuzzleRow;
            so.FindProperty("_muzzleLength").floatValue = spec.MuzzleLength;
            so.FindProperty("_muzzleSeconds").floatValue = spec.MuzzleSeconds;
            so.FindProperty("_projectile").objectReferenceValue = Sheet(sheets, spec.Projectile);
            so.FindProperty("_projectileLength").floatValue = spec.ProjectileLength;
            so.FindProperty("_projectileWidth").floatValue = spec.ProjectileWidth;
            so.FindProperty("_trail").objectReferenceValue = Sheet(sheets, spec.Trail);
            so.FindProperty("_trailWidth").floatValue = spec.TrailWidth;
            so.FindProperty("_trailSeconds").floatValue = spec.TrailSeconds;
            so.FindProperty("_exhaust").objectReferenceValue = Sheet(sheets, spec.Exhaust);
            so.FindProperty("_exhaustLength").floatValue = spec.ExhaustLength;
            so.FindProperty("_smokeTrail").objectReferenceValue = Sheet(sheets, spec.SmokeRibbon);
            so.FindProperty("_smokeTrailWidth").floatValue = spec.SmokeTrailWidth;
            so.FindProperty("_smokeTrailSeconds").floatValue = spec.SmokeTrailSeconds;
            so.FindProperty("_beam").objectReferenceValue = Sheet(sheets, spec.Beam);
            so.FindProperty("_beamWidth").floatValue = spec.BeamWidth;
            so.FindProperty("_impact").objectReferenceValue = Vfx(spec.Impact);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }

        // A sheet the Blender export lacks stays empty, and its part keeps the procedural look.
        private static VfxSheet Sheet(IReadOnlyDictionary<string, VfxSheet> sheets, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return sheets.TryGetValue(name, out VfxSheet sheet) ? sheet : AssetDatabase.LoadAssetAtPath<VfxSheet>($"{BlenderVfxBuilder.SheetFolder}/{name}.asset");
        }

        private static string VfxPath(string name) => $"{VfxFolder}/Vfx_{name}.asset";

        private static string WeaponPath(string name) => $"{VfxFolder}/Weapon_{name}.asset";

        private static ImpactVfxDefinition Vfx(string name) => AssetDatabase.LoadAssetAtPath<ImpactVfxDefinition>(VfxPath(name));
    }
}
