using System.Collections.Generic;
using Game.Core;
using Game.Levels;

namespace Game.Editor
{
    /// <summary>
    /// The living spawn origins and route gates of ArtSource README rounds 23 and 25b: which exports the game imports, which
    /// terrain each origin opens in and what each take is for.
    /// </summary>
    internal static class BlenderOriginCatalog
    {
        /// <summary>The living origins are named <c>Origin2_&lt;Kind&gt;</c> under <c>Env/</c>; the round-14 static ones were <c>Origin_</c>.</summary>
        public const string LivingPrefix = "Origin2_";

        public const string GroundGate = "SpawnGate_Ground";
        public const string AirGate = "SpawnGate_Air";
        public const string BurstVfxPath = "Assets/_Project/Data/Vfx/Vfx_OriginBurst.asset";

        /// <summary>One origin export and the terrains it opens in.</summary>
        public readonly struct Row
        {
            public Row(string asset, bool tintWithElement, bool glowMarks, params TerrainKind[] terrains)
            {
                Asset = asset;
                TintWithElement = tintWithElement;
                GlowMarks = glowMarks;
                Terrains = terrains;
            }

            /// <summary>Export path under <c>ArtSource/Exports</c>.</summary>
            public string Asset { get; }
            public bool TintWithElement { get; }
            public bool GlowMarks { get; }
            public TerrainKind[] Terrains { get; }
        }

        // The burrow opens in the ground (a rift nest's burrow sits over the fissure); every liquid has its own vent. The
        // burrow's glow is baked warm and shows only in its cracks (README: closed is dark, marked pulses); the vents' glow
        // is a white mask tinted with the element and stays on.
        public static readonly Row[] Origins =
        {
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Burrow", false, true, TerrainKind.Ground, TerrainKind.Raised, TerrainKind.Rift),
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Water", true, false, TerrainKind.Water),
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Lava", true, false, TerrainKind.Lava),
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Quicksand", true, false, TerrainKind.Quicksand),
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Ice", true, false, TerrainKind.Ice),
            new Row($"{BlenderSync.EnvironmentFolder}/{LivingPrefix}Bog", true, false, TerrainKind.Bog),
        };

        /// <summary>What each take is for, by its name in the exports (README rounds 23 and 25b).</summary>
        public static readonly IReadOnlyDictionary<string, ModelClipRole> Roles = new Dictionary<string, ModelClipRole>
        {
            { "Closed", ModelClipRole.Closed },
            { "Marked", ModelClipRole.Marked },
            { "Open", ModelClipRole.Open },
            { "Idle", ModelClipRole.Idle },
            { "Spawn", ModelClipRole.Spawn },
        };

        /// <summary>Every export folder this catalog reads.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            foreach (Row row in Origins)
            {
                yield return row.Asset;
            }

            yield return GroundGate;
            yield return AirGate;
        }
    }
}
