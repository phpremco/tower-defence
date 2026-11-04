using System.Collections.Generic;
using Game.Core;
using Game.Levels;

namespace Game.Editor
{
    /// <summary>
    /// Which Blender exports the game imports in stage 3b (README rounds 6, 13, 16 and 18): the hero Mercer with his
    /// weapons, the story characters for the dialogue portraits, and the opening cutscene's machines and drones. Holds
    /// the take-to-role table, the clips that play on the upper body, and how each portrait is framed.
    /// </summary>
    public static class BlenderStoryCatalog
    {
        public const string HeroAsset = "Mercer";
        public const string WeaponFolder = "Weapons";
        public const string WeaponMaps = "MercerWeapons";
        public const string Pistol = "Mercer_Pistol";
        public const string SwordHilt = "Mercer_SwordHilt";
        public const string SwordBlade = "Mercer_SwordBlade";

        // README round 6: the upper-body layer is Spine1 and all its children.
        public const string UpperBodyBone = "Spine1";

        public const string Capsule = "Cutscene_Capsule";
        public const string Drill = "Cutscene_Drill";
        public const string Mothership = "Cutscene_Mothership";
        public const string Beacon = "Cutscene_Beacon";
        public const string Builder = "Drone_Builder";
        public const string Hauler = "Drone_Hauler";
        public const string Console = "RadarConsole";

        /// <summary>Built ahead of its scene: the wreck is level decor for a later level (environment stage).</summary>
        public const string Wreck = "Pioneer1_Wreck";

        // Meshes in the weapon files for perks the game does not have yet (Quick Reload, Wide Slash).
        public static readonly string[] HiddenWeaponParts = { "Pistol_Mag_Quick", "Sword_Hilt_Wide", "Sword_Blade_Wide" };

        /// <summary>One dialogue bust: which export, which portrait it stands in for, and how the picture frames it.</summary>
        public readonly struct Bust
        {
            public Bust(string asset, PortraitModel portrait, float centreY, float turn, float zoom, float glow, float poseSeconds)
            {
                Asset = asset;
                Portrait = portrait;
                CentreY = centreY;
                Turn = turn;
                Zoom = zoom;
                Glow = glow;
                PoseSeconds = poseSeconds;
            }

            public string Asset { get; }
            public PortraitModel Portrait { get; }
            public float CentreY { get; }
            public float Turn { get; }
            public float Zoom { get; }
            public float Glow { get; }
            public float PoseSeconds { get; }
        }

        // Framing from README round 13 (Voss aimed at y 1.36, ORA at its core 0.34, Pioneer-1's body at 0.9), tuned on the
        // rendered portraits (imp3b_shots): Voss slightly lower and turned a touch further for a kinder light on her face,
        // the emblem's star lowered to gold, the wide probe shrunk to fit.
        public static readonly Bust[] Busts =
        {
            new Bust("Voss", PortraitModel.Scientist, 1.4f, -14f, 1.25f, 1f, 10f / 30f),
            new Bust("Ora", PortraitModel.Hologram, 0.34f, 0f, 1.1f, 1f, 10f / 30f),
            new Bust("NTI_Emblem", PortraitModel.Emblem, 0f, 8f, 1.05f, 0.35f, 0f),
            new Bust("Pioneer1", PortraitModel.Probe, 0.95f, 10f, 0.24f, 1f, 0f),
        };

        /// <summary>Mercer's bust (README round 6: the camera aimed at y 1.33).</summary>
        public static readonly Bust HeroBust = new Bust(HeroAsset, PortraitModel.Hero, 1.36f, 50f, 0.85f, 1f, 10f / 30f);

        /// <summary>What each take is for, by its name in the exports.</summary>
        public static readonly IReadOnlyDictionary<string, ModelClipRole> Roles = new Dictionary<string, ModelClipRole>
        {
            { "Idle", ModelClipRole.Idle },
            { "Walk", ModelClipRole.Walk },
            { "Run", ModelClipRole.Move },
            { "Fly", ModelClipRole.Move },
            { "Shoot", ModelClipRole.Shoot },
            { "Reload", ModelClipRole.Reload },
            { "Slash", ModelClipRole.Slash },
            { "Shockwave", ModelClipRole.Ability },
            { "Repair", ModelClipRole.Repair },
            { "Hit", ModelClipRole.Hit },
            { "Down", ModelClipRole.Down },
            { "Revive", ModelClipRole.Revive },
            { "Talk", ModelClipRole.Talk },
            { "Blink", ModelClipRole.Blink },
            { "Glitch", ModelClipRole.Glitch },
            { "Land", ModelClipRole.Land },
            { "Open", ModelClipRole.Open },
            // The drill's and the ring's turning are their resting cycles; the drill sinking is how it sets itself up.
            { "Spin", ModelClipRole.Idle },
            { "Sink", ModelClipRole.Deploy },
            { "Deploy", ModelClipRole.Deploy },
            { "Work", ModelClipRole.Work },
        };

        /// <summary>Mercer's clips that play on the upper body (README round 6), with how much of the pose each takes.</summary>
        public static readonly IReadOnlyDictionary<string, float> UpperBody = new Dictionary<string, float>
        {
            { "Shoot", 1f },
            { "Reload", 1f },
            { "Slash", 1f },
            // A hit only flinches: most of the stance keeps going underneath it.
            { "Hit", 0.7f },
        };

        /// <summary>The report key of each Mercer clip's moment (a frame number), by take.</summary>
        public static readonly IReadOnlyDictionary<string, string> EventKeys = new Dictionary<string, string>
        {
            { "Shoot", "muzzle_flash" },
            { "Reload", "mag_in" },
            { "Slash", "hit" },
            { "Shockwave", "impact" },
            { "Down", "ground" },
        };

        /// <summary>The report key of each Mercer travel cycle's distance per loop.</summary>
        public static readonly IReadOnlyDictionary<string, string> DistanceKeys = new Dictionary<string, string>
        {
            { "Walk", "walk_distance_per_loop" },
            { "Run", "run_distance_per_loop" },
        };

        /// <summary>The cutscene's machines and drones, and the ready decor.</summary>
        public static readonly string[] CutsceneModels = { Capsule, Drill, Mothership, Beacon, Builder, Hauler, Console, Wreck };

        /// <summary>Every export folder stage 3b reads.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            yield return HeroAsset;
            foreach (Bust bust in Busts)
            {
                yield return bust.Asset;
            }

            foreach (string asset in CutsceneModels)
            {
                yield return asset;
            }
        }

        /// <summary>The weapon files in Mercer's export (their own FBX files, maps and report in a sub folder).</summary>
        public static IReadOnlyDictionary<string, string[]> ExtraModels()
        {
            var files = new List<string>();
            foreach (string model in new[] { Pistol, SwordHilt, SwordBlade })
            {
                files.Add($"{WeaponFolder}/{model}.fbx");
            }

            foreach (string map in new[] { "Albedo", "Normal", "Emission", "Mask" })
            {
                files.Add($"{WeaponFolder}/{WeaponMaps}_{map}.png");
                files.Add($"{WeaponFolder}/{BlenderSync.LowFolder}/{WeaponMaps}_{map}.png");
            }

            files.Add($"{WeaponFolder}/{WeaponMaps}_report.json");
            return new Dictionary<string, string[]> { { HeroAsset, files.ToArray() } };
        }
    }
}
