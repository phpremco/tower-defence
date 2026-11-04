namespace Game.Editor
{
    /// <summary>
    /// One art definition made from a Blender export: which export (and which baked skin of it), how it sits on its
    /// owner, the clips with moments, and the material look at play size. The look numbers are tuned on in-game
    /// screenshots; everything else follows the export's README notes and report.
    /// </summary>
    public sealed class BlenderArtEntry
    {
        public const float DefaultEmission = 2.5f;
        public const float DefaultSmoothness = 1f;
        public const float DefaultDetail = 0.8f;
        public const float DefaultDetail2 = 0.6f;
        public const float DefaultRim = 0.6f;

        // The procedural species are drawn at their data scale times 1.4 (EnemyModelFactory); the exports are built at the
        // factory model's size in their own units, so the same factor keeps every species as big as players know it.
        public const float SpeciesScale = 1.4f;

        public BlenderArtEntry(string id, string asset, string skin = null)
        {
            Id = id;
            Asset = asset;
            Skin = skin;
        }

        /// <summary>Name of the art definition and its materials.</summary>
        public string Id { get; }

        /// <summary>The export folder and FBX name.</summary>
        public string Asset { get; }

        /// <summary>A baked skin under <c>Skins/</c>, or null for the export's own maps.</summary>
        public string Skin { get; }

        /// <summary>Factor on the owner's scale: the species factor, or the boss factor for a boss.</summary>
        public float Scale { get; set; } = SpeciesScale;

        public bool BodyCentrePivot { get; set; }

        /// <summary>Model units one Move loop covers, when the report does not say.</summary>
        public float MoveDistance { get; set; }

        /// <summary>Death clip frames over which the glow dies; zero for the default share of the clip.</summary>
        public int DeathFadeFrom { get; set; }
        public int DeathFadeTo { get; set; }
        public bool Embers { get; set; }
        public bool Bleeds { get; set; } = true;

        public BlenderClipSpec[] Clips { get; set; } = new BlenderClipSpec[0];

        public float Emission { get; set; } = DefaultEmission;
        public float Smoothness { get; set; } = DefaultSmoothness;
        public float Detail { get; set; } = DefaultDetail;
        public float Detail2 { get; set; } = DefaultDetail2;
        public float Rim { get; set; } = DefaultRim;

        /// <summary>Lightens (above 1) or darkens the baked albedo, for a model that reads too dark at play size.</summary>
        public float Brightness { get; set; } = 1f;

        /// <summary>File prefix of the maps: the asset, or asset and skin.</summary>
        public string MapPrefix => Skin == null ? Asset : $"{Asset}_{Skin}";
    }
}
