using System;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One look of Spencer's blade as its damage upgrade climbs: from which tier it shows, the colour the blade and its
    /// embers burn in, and how strong the aura is (the halfway stage is calmer than the last). Data on
    /// <see cref="HeroGearArtDefinition"/>; the tiers are absolute (tier 5 and tier 10 of ten), not "the highest tier open
    /// on this continent".
    /// </summary>
    [Serializable]
    public struct BladeAuraStage
    {
        [Tooltip("The blade damage tier this look starts at.")]
        [Min(1)] public int FromTier;

        public Color BladeColor;
        public Color EmberColor;

        [Tooltip("Share of the aura's glow, halo and embers this stage shows (1 is the full aura).")]
        [Range(0f, 1f)] public float Strength;
    }
}
