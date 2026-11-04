using Game.Towers;

namespace Game.Editor
{
    /// <summary>
    /// One tower made from a Blender export (<c>Tower_&lt;Name&gt;</c>, README rounds 7, 9 and 11): which tower definition
    /// wears it, its tiers in level order, how its head aims and where its shots leave, which wreck debris it leaves, and
    /// the material look at play size. All tiers share one map set, so one material (High and Low) per tower.
    /// </summary>
    public sealed class BlenderTowerEntry
    {
        // A standard tower has a level-3 look for each upgrade path; until the data has paths, A is the level-3 look.
        public static readonly string[] PathTiers = { "T1", "T2", "T3A", "T3B" };

        // Hybrids and the gate gun have three looks and no paths.
        public static readonly string[] PlainTiers = { "T1", "T2", "T3" };

        public BlenderTowerEntry(string tower, string asset, string[] tiers)
        {
            Tower = tower;
            Asset = asset;
            Tiers = tiers;
        }

        /// <summary>The tower definition's asset name.</summary>
        public string Tower { get; }

        /// <summary>The export folder and file prefix.</summary>
        public string Asset { get; }

        /// <summary>The tier names in level order; a fourth is the level-3 look of path B.</summary>
        public string[] Tiers { get; }

        public TowerAimMode Aim { get; set; } = TowerAimMode.YawAndPitch;

        /// <summary>Bone name prefix of the shot origins (<c>Muzzle</c>, Tesla <c>Arc</c>, Rocket <c>Tube</c>).</summary>
        public string MuzzlePrefix { get; set; } = TowerArtDefinition.DefaultMuzzlePrefix;

        public TowerMuzzleChoice MuzzleChoice { get; set; } = TowerMuzzleChoice.Alternate;

        /// <summary>The <c>Debris_&lt;Name&gt;</c> object of the shared wreck, or null for no wreck model (the gate gun).</summary>
        public string Debris { get; set; }

        /// <summary>Glow factor reached at the end of a ChargeUp (1 keeps the glow as it is).</summary>
        public float ChargeGlow { get; set; } = 1f;

        /// <summary>The gun hides its own rank plate stars (the gate shows its stars on the beam).</summary>
        public bool HideStars { get; set; }

        public float Emission { get; set; } = BlenderArtEntry.TowerEmission;
        public float Smoothness { get; set; } = BlenderArtEntry.DefaultSmoothness;
        public float Rim { get; set; } = BlenderArtEntry.TowerRim;
        public float Brightness { get; set; } = 1f;

        /// <summary>The FBX file of a tier.</summary>
        public string ModelFile(string tier) => $"{Asset}_{tier}.fbx";

        /// <summary>The material row: the tower's maps with its look, rimmed in the colony's cool light.</summary>
        public BlenderArtEntry MaterialEntry() => new BlenderArtEntry(Asset, Asset)
        {
            Emission = Emission,
            Smoothness = Smoothness,
            Rim = Rim,
            Brightness = Brightness,
            Colony = true,
        };
    }
}
