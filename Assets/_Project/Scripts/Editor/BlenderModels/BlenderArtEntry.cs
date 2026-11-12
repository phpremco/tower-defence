using UnityEngine;

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

        // A machine's glow colour is in its emission map; white times this lights it. README round 7 suggests about 1.5;
        // at the play zoom (imp2_r5) the lenses and coils read better a little hotter.
        public const float TowerEmission = 2f;

        // Our machines' cool rim, a little stronger than the creatures' so the small heads separate from the night ground.
        public const float TowerRim = 0.75f;

        // The wreck's embers bloomed into yellow flames at 1.5 (imp2_r5_wreck): they only smoulder.
        public const float WreckEmission = 0.6f;

        // The burnt husk read as a dark rock on the night ground (imp2_r7_wreck): its albedo is lifted.
        public const float WreckBrightness = 1.7f;

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

        /// <summary>
        /// Name of the vent definition (<c>Data/Vfx/Vents_&lt;name&gt;.asset</c>) whose smoke and embers rise from the model's vent
        /// sockets, or null for none. Created with its defaults on the first build; tuned in the asset after that.
        /// </summary>
        public string Vents { get; set; }

        public bool Bleeds { get; set; } = true;

        /// <summary>The bones a boss's beam and bolts leave from, in order of preference (the first the rig has wins); empty for none.</summary>
        public string[] BeamSockets { get; set; } = new string[0];

        /// <summary>
        /// Bones of a crown that fans towards and away from the camera (spires, a crest): the health bar clears them as the
        /// looking-down play camera draws them, not only by their height (<see cref="CreatureBarHeight"/>); empty for none.
        /// </summary>
        public string[] BarCrestBones { get; set; } = new string[0];

        public BlenderClipSpec[] Clips { get; set; } = new BlenderClipSpec[0];

        /// <summary>The second material's name in the export (<c>Crystal</c> for <c>&lt;Asset&gt;_Crystal_*.png</c>), or null for one material.</summary>
        public string SecondMaterial { get; set; }

        /// <summary>Extra objects of the model with maps of their own (<c>&lt;Asset&gt;_&lt;Part&gt;_*.png</c>), shown or hidden on their own.</summary>
        public string[] ExtraParts { get; set; } = new string[0];

        /// <summary>
        /// Named looks of the glow baked beside the model (<c>&lt;Asset&gt;_&lt;Set&gt;_Emission.png</c> and the second material's
        /// <c>&lt;Asset&gt;_&lt;Second&gt;_&lt;Set&gt;_Emission.png</c>), strengths from the report's round-28 phase looks.
        /// </summary>
        public string[] EmissionSets { get; set; } = new string[0];

        /// <summary>The glow look the body takes at its death clip's moment (a white release), or null.</summary>
        public string DeathEmissionSet { get; set; }

        /// <summary>The report key of the bones shed with the armour, in order, or null.</summary>
        public string ShedBonesKey { get; set; }

        public float Emission { get; set; } = DefaultEmission;
        public float Smoothness { get; set; } = DefaultSmoothness;
        public float Detail { get; set; } = DefaultDetail;
        public float Detail2 { get; set; } = DefaultDetail2;
        public float Rim { get; set; } = DefaultRim;

        /// <summary>Lightens (above 1) or darkens the baked albedo, for a model that reads too dark at play size.</summary>
        public float Brightness { get; set; } = 1f;

        /// <summary>Factor on the painted colour on a display stage (codex, upgrade screen, menus): below 1 for a pale model that washes out white.</summary>
        public float DisplayExposure { get; set; } = 1f;

        /// <summary>Our side: rimmed in the colony's cool light instead of the aliens' warm one.</summary>
        public bool Colony { get; set; }

        /// <summary>
        /// Ice on an alien (the Frost Warden's armour and glaives): the second slot and the ice parts get a darker, colder
        /// tint over their albedo, a bright cold rim and a glow from the cracks, so they read as clear ice with lit edges at
        /// play size instead of flat painted plastic. Null keeps the row's own look.
        /// </summary>
        public IceLook Ice { get; set; }

        /// <summary>The extra parts that are ice and take <see cref="Ice"/> (the Frost Warden's armour, not its eyes or teeth).</summary>
        public string[] IceParts { get; set; } = new string[0];

        /// <summary>
        /// Factor on the extra parts' albedo and glow colour. A baked skin has no maps for the parts (they keep the export's
        /// own), so a skinned row leans them towards its palette with these instead.
        /// </summary>
        public Color PartTint { get; set; } = Color.white;
        public Color PartGlowTint { get; set; } = Color.white;

        /// <summary>The same row wearing a baked skin of the same mesh (a corrupted variant), under its own id.</summary>
        public BlenderArtEntry WithSkin(string id, string skin)
        {
            return new BlenderArtEntry(id, Asset, skin)
            {
                Scale = Scale,
                BodyCentrePivot = BodyCentrePivot,
                MoveDistance = MoveDistance,
                DeathFadeFrom = DeathFadeFrom,
                DeathFadeTo = DeathFadeTo,
                Embers = Embers,
                Vents = Vents,
                Bleeds = Bleeds,
                BeamSockets = BeamSockets,
                BarCrestBones = BarCrestBones,
                Clips = Clips,
                SecondMaterial = SecondMaterial,
                ExtraParts = ExtraParts,
                Ice = Ice,
                IceParts = IceParts,
                ShedBonesKey = ShedBonesKey,
                Emission = Emission,
                Smoothness = Smoothness,
                Detail = Detail,
                Detail2 = Detail2,
                Rim = Rim,
                Brightness = Brightness,
                Colony = Colony,
            };
        }

        /// <summary>File prefix of the maps: the asset, or asset and skin.</summary>
        public string MapPrefix => Skin == null ? Asset : $"{Asset}_{Skin}";
    }
}
