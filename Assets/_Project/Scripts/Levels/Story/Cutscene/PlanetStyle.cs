using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// How a planet looks from space, as data for the planet shaders: sea and land colours, alien biomes or the
    /// painted map, crystal glow or city lights at night, the cloud deck and the air along the limb. The cast carries
    /// one for S-69-CRYS and one for Earth; <see cref="Crys"/> and <see cref="Earth"/> hold the agreed looks.
    /// </summary>
    [Serializable]
    public sealed class PlanetStyle
    {
        [Tooltip("Colours the land with noise biomes in the colours below (an alien world) instead of the painted map.")]
        public bool Alien;
        public Color DeepSea = new Color(0.01f, 0.03f, 0.08f);
        public Color ShallowSea = new Color(0.03f, 0.15f, 0.2f);
        public Color LandA = new Color(0.2f, 0.3f, 0.2f);
        public Color LandB = new Color(0.3f, 0.25f, 0.2f);
        public Color Highland = new Color(0.35f, 0.3f, 0.25f);
        public Color Peak = new Color(0.85f, 0.88f, 0.9f);
        [ColorUsage(false, true)] public Color Crystal = new Color(0.3f, 1.2f, 1.5f);
        [ColorUsage(false, true)] public Color City = new Color(2f, 1.3f, 0.6f);
        [ColorUsage(false, true)] public Color Atmosphere = new Color(0.35f, 0.6f, 1f);
        [ColorUsage(false, true)] public Color Twilight = new Color(1f, 0.45f, 0.2f);
        public Color Cloud = Color.white;

        [Range(0f, 1f)] public float TextureTint = 0.35f;
        [Range(0f, 0.05f)] public float CoastWarp = 0.012f;
        [Range(0f, 1f)] public float CrystalDensity = 0.5f;
        [Range(0f, 1f)] public float CloudShadow = 0.5f;
        [Range(0f, 1f)] public float CloudWisps = 0.6f;
        [Range(0f, 4f)] public float SeaGlint = 1.2f;
        [Range(8f, 400f)] public float SeaGlintTightness = 220f;
        [Range(0f, 3f)] public float LimbHaze = 1f;
        [Range(0.5f, 8f)] public float LimbHazeFalloff = 3f;
        [Range(0f, 1f)] public float NightAmbient = 0.015f;
        [Range(0f, 4f)] public float Relief = 2f;
        [Tooltip("How much of the painted normal map shows; its coast ridges read as dark lines when strong.")]
        [Range(0f, 1f)] public float MapRelief = 0.12f;
        [Tooltip("How far the painted sea mask is blurred (mip level) before noise cuts the coast; higher drowns the small islands.")]
        [Range(0f, 6f)] public float CoastBlur = 4f;
        [Range(0f, 0.5f)] public float DayHaze = 0.15f;

        [Header("Air past the limb")]
        [Range(0f, 6f)] public float AtmosphereStrength = 1.5f;
        [Range(1f, 20f)] public float AtmosphereFalloff = 6f;
        [Range(1.01f, 1.3f)] public float AtmosphereLift = 1.06f;
        [Range(0f, 6f)] public float ForwardScatter = 1.5f;

        [Header("World map (readability over drama)")]
        [Tooltip("Share of the painted map (region colours) on the land of the map globe.")]
        [Range(0f, 1f)] public float MapTextureTint = 0.6f;
        [Tooltip("Light on the night side of the map globe, so no region sinks into the dark.")]
        [Range(0f, 1f)] public float MapAmbient = 0.22f;
        [Range(0f, 1f)] public float MapDayLights = 0.7f;
        [Tooltip("Sea mask blur on the map: lower keeps the coasts where the region outlines are painted.")]
        [Range(0f, 6f)] public float MapCoastBlur = 1.5f;
        [Range(0f, 4f)] public float MapSeaGlint = 0.3f;

        [Header("Quality")]
        [Range(1, 8)] public int HighOctaves = 6;
        [Range(1, 8)] public int LowOctaves = 3;

        /// <summary>S-69-CRYS: deep teal seas, violet and rust lowlands, pale mineral highlands, crystal fields glowing at night.</summary>
        public static PlanetStyle Crys() => new PlanetStyle
        {
            Alien = true,
            DeepSea = new Color(0.03f, 0.11f, 0.21f),
            ShallowSea = new Color(0.06f, 0.36f, 0.42f),
            LandA = new Color(0.08f, 0.3f, 0.34f),
            LandB = new Color(0.36f, 0.19f, 0.42f),
            Highland = new Color(0.52f, 0.36f, 0.26f),
            Peak = new Color(0.62f, 0.6f, 0.68f),
            Crystal = new Color(0.3f, 1.8f, 2.2f),
            City = new Color(1.6f, 1.4f, 1f),
            Atmosphere = new Color(0.3f, 0.75f, 1f),
            Twilight = new Color(1f, 0.42f, 0.35f),
            Cloud = new Color(0.95f, 0.98f, 1f),
            TextureTint = 0.08f,
            CrystalDensity = 0.9f,
        };

        /// <summary>Earth of 2053: murky seas, dry brown land, a brown smog deck and dying amber city lights.</summary>
        public static PlanetStyle Earth() => new PlanetStyle
        {
            Alien = false,
            DeepSea = new Color(0.02f, 0.04f, 0.05f),
            ShallowSea = new Color(0.06f, 0.1f, 0.09f),
            LandA = new Color(0.3f, 0.26f, 0.2f),
            LandB = new Color(0.36f, 0.3f, 0.22f),
            Highland = new Color(0.4f, 0.35f, 0.3f),
            Peak = new Color(0.7f, 0.68f, 0.64f),
            City = new Color(4f, 2.2f, 0.8f),
            Atmosphere = new Color(0.75f, 0.58f, 0.38f),
            Twilight = new Color(1f, 0.45f, 0.18f),
            Cloud = new Color(0.72f, 0.6f, 0.46f),
            TextureTint = 1f,
            CloudShadow = 0.4f,
            SeaGlint = 0.7f,
            SeaGlintTightness = 160f,
            AtmosphereStrength = 1.2f,
            Relief = 1.2f,
            MapRelief = 0.3f,
            CloudWisps = 0.85f,
        };
    }
}
