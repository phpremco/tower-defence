using System;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The effect textures painted in Blender (README round 20, <c>VFX_report.json</c>) and how each becomes a material:
    /// which shader, what colour each packed channel glows in, whether frames blend, whether it fades against the
    /// ground, which noise runs through it. The atlas layout (cells, frames, rate, life, size, pivot) comes from the
    /// report. The look per weapon is not here but in the generator's weapon recipes.
    /// </summary>
    public static class BlenderVfxCatalog
    {
        /// <summary>The export folder (and the folder under the Blender target root).</summary>
        public const string Folder = "VFX";

        public const string Report = "VFX_report.json";

        public const string BeamNoise = "Beam_Noise";
        public const string TrailNoise = "Trail_Noise";

        /// <summary>What a sheet is drawn as.</summary>
        public enum Kind
        {
            /// <summary>Additive particles (flipbook flashes, spark streaks).</summary>
            Glow,

            /// <summary>Particles that cover what is behind them (shards, splashes).</summary>
            Cover,

            /// <summary>A card or line showing one atlas cell (muzzle flashes, bolts, flames, arcs, rings).</summary>
            Card,

            /// <summary>A line or trail that tiles or stretches along its length (beams, ribbons).</summary>
            Ribbon,

            /// <summary>Six-way lit fire, smoke and dust (Pos and Neg maps).</summary>
            SixWay,

            /// <summary>A refraction of the opaque scene (shock rings, heat haze).</summary>
            Distortion,
        }

        /// <summary>One sheet: its name in the export, how it is drawn and the colour per channel.</summary>
        public readonly struct Sheet
        {
            public Sheet(string name, Kind kind, Color core, Color glow, Color third, float opacity = 0f, string noise = null, float noiseStrength = 0f, Vector4 noiseScroll = default, float softness = 1f)
            {
                Name = name;
                Kind = kind;
                Core = core;
                Glow = glow;
                Third = third;
                Opacity = opacity;
                Noise = noise;
                NoiseStrength = noiseStrength;
                NoiseScroll = noiseScroll;
                Softness = softness;
            }

            public string Name { get; }
            public Kind Kind { get; }

            /// <summary>R for glowing sheets; the smoke colour (albedo) for six-way sheets.</summary>
            public Color Core { get; }

            /// <summary>G for glowing sheets (multiplied by the weapon tint); x = fire strength for six-way sheets.</summary>
            public Color Glow { get; }

            public Color Third { get; }
            public float Opacity { get; }
            public string Noise { get; }
            public float NoiseStrength { get; }
            public Vector4 NoiseScroll { get; }

            /// <summary>Six-way sheets: the power on the painted alpha; above 1 the thin rims fade sooner (softer smoke edges).</summary>
            public float Softness { get; }

            /// <summary>Six-way sheets come as a pair of maps.</summary>
            public bool SixWay => Kind == Kind.SixWay;
        }

        private static Color Hdr(float r, float g, float b, float intensity) => new Color(r * intensity, g * intensity, b * intensity, 1f);

        private static readonly Color White3 = Hdr(1f, 1f, 1f, 3f);
        private static readonly Color White2 = Hdr(1f, 1f, 1f, 2f);
        private static readonly Color None = new Color(0f, 0f, 0f, 0f);

        // Remco 2026-11-04: the smoke covered the field like a solid blot. The power on its alpha thins the wisps; how much
        // it covers and how long it lives are on each impact recipe (ImpactVfxDefinition: smoke opacity and life).
        private const float SmokeSoftness = 1.8f;
        private const float ExplosionSoftness = 1.3f;

        // The textures the game wears. Scorch_Decal stays in ArtSource until ground decals are wanted.
        public static readonly Sheet[] Sheets =
        {
            new Sheet("Muzzle_Side", Kind.Card, White3, Hdr(1f, 0.82f, 0.55f, 2.6f), Hdr(0.35f, 0.34f, 0.33f, 0.25f)),
            new Sheet("Muzzle_Front", Kind.Card, White3, Hdr(1f, 0.82f, 0.55f, 2.6f), Hdr(0.35f, 0.34f, 0.33f, 0.25f)),
            new Sheet("Bolt_Core", Kind.Card, White3, White2, Hdr(1f, 1f, 1f, 0.8f)),
            new Sheet("Plasma_Bolt", Kind.Card, Hdr(1f, 1f, 1f, 2.5f), White2, Hdr(0.85f, 0.95f, 1f, 1.2f)),
            new Sheet("Rocket_Exhaust", Kind.Card, White3, Hdr(1f, 1f, 1f, 3f), Hdr(0.35f, 0.34f, 0.33f, 0.2f)),
            new Sheet("Tesla_Arcs", Kind.Card, White3, Hdr(1f, 1f, 1f, 2.4f), Hdr(1f, 1f, 1f, 1f)),
            new Sheet("Shockwave_Ring", Kind.Card, White2, Hdr(1f, 1f, 1f, 0.7f), Hdr(0.45f, 0.38f, 0.3f, 0.12f)),
            new Sheet("Beam_Core", Kind.Ribbon, White3, Hdr(1f, 1f, 1f, 2.4f), None, 0f, BeamNoise, 0.6f, new Vector4(-3f, 0f, 1.2f, 0.3f)),
            new Sheet("Trail_Ribbon", Kind.Ribbon, White2, Hdr(1f, 1f, 1f, 1.6f), None, 0f, TrailNoise, 0.5f, new Vector4(-2f, 0f, -1f, 0.2f)),
            new Sheet("Smoke_Trail", Kind.Ribbon, Hdr(0.36f, 0.35f, 0.34f, 1f), Hdr(0.05f, 0.05f, 0.05f, 1f), None, 1f, TrailNoise, 0.3f, new Vector4(-0.4f, 0f, 0.2f, 0.1f)),
            new Sheet("Energy_Impact", Kind.Glow, White3, Hdr(1f, 1f, 1f, 2.6f), Hdr(0.7f, 0.85f, 1f, 1.2f)),
            new Sheet("Spark_Burst", Kind.Glow, White3, Hdr(1f, 0.75f, 0.4f, 2.6f), None),
            new Sheet("Spark_Streaks", Kind.Glow, White3, Hdr(1f, 0.7f, 0.35f, 2.4f), None),
            new Sheet("Cryo_Burst", Kind.Glow, White2, Hdr(0.55f, 0.95f, 1f, 2f), Hdr(0.8f, 0.95f, 1f, 0.5f)),
            new Sheet("Acid_Splash", Kind.Cover, Hdr(0.55f, 1f, 0.15f, 1.3f), Hdr(0.25f, 0.6f, 0.05f, 1.5f), Hdr(1f, 1f, 1f, 0.6f), 1f),
            new Sheet("Debris_Shards", Kind.Cover, Hdr(1f, 0.45f, 0.1f, 3f), None, Hdr(0.16f, 0.15f, 0.14f, 1f), 1f),
            new Sheet("Explosion_Small", Kind.SixWay, new Color(0.34f, 0.31f, 0.28f), new Color(1f, 0f, 0f), None, softness: ExplosionSoftness),
            new Sheet("Explosion_Large", Kind.SixWay, new Color(0.34f, 0.31f, 0.28f), new Color(1f, 0f, 0f), None, softness: ExplosionSoftness),
            new Sheet("Explosion_Boss", Kind.SixWay, new Color(0.32f, 0.29f, 0.27f), new Color(1.2f, 0f, 0f), None, softness: ExplosionSoftness),
            // The cooled smoke a shade lighter than the fireball's soot: at 0.4 it read as a dark blot on the dark levels.
            new Sheet("Smoke_Puff", Kind.SixWay, new Color(0.54f, 0.52f, 0.5f), new Color(0.35f, 0f, 0f), None, softness: SmokeSoftness),
            new Sheet("Smoke_Linger", Kind.SixWay, new Color(0.5f, 0.48f, 0.46f), new Color(0.25f, 0f, 0f), None, softness: SmokeSoftness),
            new Sheet("Dust_Burst", Kind.SixWay, new Color(0.45f, 0.38f, 0.3f), new Color(0f, 0f, 0f), None),
            new Sheet("Shockwave_Distort", Kind.Distortion, None, None, None),
            new Sheet("HeatHaze_Distort", Kind.Distortion, None, None, None),
        };

        // Tiling textures (repeat wrap); everything else is an atlas that clamps.
        private static readonly string[] TilingTextures = { "Beam_Core", BeamNoise, TrailNoise, "Trail_Ribbon", "Smoke_Trail", "HeatHaze_Distort" };

        public static bool Tiles(string file) => Array.IndexOf(TilingTextures, file) >= 0;

        public static bool IsDistortion(string file) => file.EndsWith("_Distort", StringComparison.Ordinal);

        /// <summary>The PNG files of a sheet (two for six-way sheets).</summary>
        public static string[] Files(Sheet sheet) => sheet.SixWay ? new[] { sheet.Name + "_SixWayPos.png", sheet.Name + "_SixWayNeg.png" } : new[] { sheet.Name + ".png" };

        /// <summary>Every PNG the game syncs: the sheets and the noise layers.</summary>
        public static string[] AllFiles()
        {
            var files = new System.Collections.Generic.List<string> { BeamNoise + ".png", TrailNoise + ".png" };
            foreach (Sheet sheet in Sheets)
            {
                files.AddRange(Files(sheet));
            }

            return files.ToArray();
        }
    }
}
