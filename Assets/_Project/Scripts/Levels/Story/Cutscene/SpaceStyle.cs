using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The look of deep space in the cutscene's space sets, as data: the galaxy band and nebula behind the stars, the
    /// colours and sizes of the stars, the sun's glow and the cool rim light that picks out hard-surface models.
    /// </summary>
    [Serializable]
    public sealed class SpaceStyle
    {
        [Header("Sky")]
        [ColorUsage(false, true)] public Color NebulaA = new Color(0.32f, 0.12f, 0.42f);
        [ColorUsage(false, true)] public Color NebulaB = new Color(0.06f, 0.32f, 0.42f);
        [ColorUsage(false, true)] public Color Band = new Color(0.62f, 0.56f, 0.5f);
        public Vector3 BandNormal = new Vector3(0.35f, 0.85f, 0.4f);
        [Range(0.02f, 1f)] public float BandWidth = 0.2f;
        [Range(0f, 2f)] public float SkyStrength = 0.3f;
        [Range(0f, 1f)] public float Haze = 0.12f;

        [Header("Stars")]
        [Tooltip("Share of the stars that crowd into the galaxy band.")]
        [Range(0f, 1f)] public float BandStars = 0.45f;
        [Tooltip("Cells of the star grid across the sky; more cells, more and smaller stars.")]
        [Range(20f, 200f)] public float StarScale = 70f;
        [Range(0.5f, 5f)] public float StarBrightness = 2.2f;

        [Header("Sun")]
        [ColorUsage(false, true)] public Color SunCore = new Color(6f, 5.4f, 4.6f);
        [ColorUsage(false, true)] public Color SunHalo = new Color(1.1f, 0.8f, 0.55f);
        [Range(0f, 40f)] public float SunSize = 9f;
        [Range(0f, 200f)] public float HaloSize = 70f;
        [Range(0f, 400f)] public float StreakLength = 160f;

        [Header("Rim light for ships")]
        public Color RimColour = new Color(0.45f, 0.62f, 1f);
        [Range(0f, 3f)] public float RimIntensity = 0.6f;
    }
}
