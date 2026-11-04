using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The exhausted Earth of 2053 painted in Blender (README round 26, Earth), in the map layout of
    /// <see cref="PlanetCoordinates.ToUv"/>: albedo, relief normal, a gloss map whose alpha is the smoothness
    /// (0.05 + 0.22 x the mask's wetness, baked at import), the dying city lights and the brown smog deck (RGB its
    /// colour, A its cover). One set per quality profile.
    /// </summary>
    [Serializable]
    public sealed class EarthMaps
    {
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Gloss;
        public Texture2D Emission;
        public Texture2D Smog;

        public bool IsComplete => Albedo != null && Normal != null && Gloss != null && Emission != null && Smog != null;
    }
}
