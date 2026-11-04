namespace Game.Editor
{
    /// <summary>
    /// The planets painted in Blender (README round 20): texture-only exports, one folder per planet, whose maps the
    /// planet shaders read. The height map is left in ArtSource (the normal map carries the relief) and the region ids
    /// come from the Low size for both profiles (exact ids, a quarter of the memory).
    /// </summary>
    public static class BlenderPlanetCatalog
    {
        /// <summary>Every planet export folder starts with this; the importer gives them the planet settings.</summary>
        public const string FolderPrefix = "Planet_";

        /// <summary>S-69-CRYS, the campaign planet.</summary>
        public const string Crys = "Planet_S69";

        public const string Albedo = "Albedo";
        public const string Normal = "Normal";
        public const string Mask = "Mask";
        public const string Regions = "Regions";
        public const string Emission = "Emission";
        public const string Clouds = "Clouds";

        /// <summary>The maps synced in both sizes.</summary>
        public static readonly string[] Maps = { Albedo, Normal, Mask, Emission, Clouds };

        /// <summary>The maps synced from <c>Low/</c> only.</summary>
        public static readonly string[] LowOnlyMaps = { Regions };
    }
}
