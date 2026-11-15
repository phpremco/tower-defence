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
        // A standard tower's exports carry two level-3 looks, from when level 3 was to split into paths A and B. The paths
        // were cut (round 67, Remco 2026-11-15: every step improves one named thing): the A look is the level-3 look, the B
        // export stays in ArtSource and is not imported.
        public static readonly string[] StandardTiers = { "T1", "T2", "T3A" };

        // Hybrids and the gate gun were exported with one level-3 look.
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

        /// <summary>The tier names in level order.</summary>
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
