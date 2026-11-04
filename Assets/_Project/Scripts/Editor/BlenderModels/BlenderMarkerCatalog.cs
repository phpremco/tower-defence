using System;
using System.Collections.Generic;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 25b: the gameplay markers, bars, projectiles, beams, debris, the scout drone and the particle
    /// atlas. Every marker texture is a linear channel pack (R/G/B masks, A coverage) that a material tints; the lit
    /// pieces (missile, scout, debris) carry the tower map set. The spawn gates of the same round have their own builder.
    /// </summary>
    public static class BlenderMarkerCatalog
    {
        /// <summary>Every export folder of the round (the gates excluded).</summary>
        public static readonly string[] Folders =
        {
            "UI_HealthBar", "Decal_GlowPool", "Decal_BlobShadow", "Marker_PlacementCursor", "Marker_RangeRing",
            "Marker_HeroSpot", "Marker_HeroStand", "Marker_SlowField", "Marker_RoadCue", "Proj_Bolt", "Proj_RailSlug",
            "Proj_Missile", "Proj_EnemyBolt", "Proj_PulseRing", "Beam_Tool", "Recon_Scout", "Debris_Chunks",
            "Particles_Atlas",
        };

        // README: these channel packs tile along a direction (ribbons, bands round a ring, cone scan rings, the
        // field's scanlines); everything else is a single picture that clamps.
        private static readonly HashSet<string> TilingTextures = new HashSet<string>
        {
            "Cue_Trail", "Cue_Dash", "Marker_RangeRing", "Marker_HeroRing", "Marker_Beacon", "Field_Energy",
            "Proj_PulseRing", "Beam_Tool", "Proj_EnemyBolt", "Proj_Glow",
        };

        private static readonly HashSet<string> FolderSet = new HashSet<string>(Folders);

        /// <summary>True for an asset path inside one of the round's folders.</summary>
        public static bool Contains(string exportFolder) => FolderSet.Contains(exportFolder);

        /// <summary>A painted map of a lit piece (Albedo, Normal, Emission, Mask) rather than a channel pack.</summary>
        public static bool IsLitMap(string file) =>
            file.EndsWith("_Albedo", StringComparison.Ordinal) || file.EndsWith("_Normal", StringComparison.Ordinal) ||
            file.EndsWith("_Emission", StringComparison.Ordinal) || file.EndsWith("_Mask", StringComparison.Ordinal);

        public static bool Tiles(string file) => TilingTextures.Contains(file);
    }
}
