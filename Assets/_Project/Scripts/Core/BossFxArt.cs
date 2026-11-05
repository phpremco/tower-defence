using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The art bosses leave on the field (ArtSource README round 29): ground marks (<c>Game/BossDecal</c> strips and the
    /// frost atlas), flipbook sheets (<c>Game/VfxParticle</c>), the props a boss uses (the ruin gate it breaks out of, the
    /// seal crystals round its rift, the ice block a frozen gate becomes, the slabs heaved up along a burrow crack) and the
    /// drained emission maps of a gate whose gun a boss has drained. Written by the generator (Tools/Generate/Blender Boss
    /// FX); hung on the <c>VfxLibrary</c> and registered in <see cref="Services"/> at boot. Without it every view keeps its
    /// plain stand-in.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss FX Art", fileName = "BossFxArt")]
    public sealed class BossFxArt : ScriptableObject
    {
        public enum Mark
        {
            GroundCrack,
            LavaTrail,
            Furrow,
            RotTrail,
            RoadSink,
            FrostCreep,
        }

        public enum Sheet
        {
            SporeCloud,
            VineBurn,
            ShardFlash,
            WakeWater,
            WakeLava,
            WakeBog,
            WakeQuicksand,
            WakeIce,
        }

        [Serializable]
        private sealed class MarkEntry
        {
            [SerializeField] private Mark _mark;
            [SerializeField] private Material _material;
            [SerializeField] private Material _lowMaterial;
            [Tooltip("World width of the strip (or across one atlas cell), metres.")]
            [SerializeField] [Min(0.1f)] private float _width = 2f;
            [Tooltip("World length of one tile along the strip, metres.")]
            [SerializeField] [Min(0.1f)] private float _tileLength = 8f;

            public Mark Mark => _mark;
            public float Width => _width;
            public float TileLength => _tileLength;
            public Material Material(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;
        }

        [Serializable]
        private sealed class SheetEntry
        {
            [SerializeField] private Sheet _sheet;
            [SerializeField] private Material _material;
            [SerializeField] private Material _lowMaterial;
            [SerializeField] [Min(1f)] private float _framesPerSecond = 12f;

            public Sheet Sheet => _sheet;
            public float FramesPerSecond => _framesPerSecond;
            public Material Material(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;
        }

        /// <summary>A gate part's lit emission map and the drained one painted on the same atlas.</summary>
        [Serializable]
        public sealed class DrainedMap
        {
            [SerializeField] private Texture _lit;
            [SerializeField] private Texture _drained;

            public Texture Lit => _lit;
            public Texture Drained => _drained;
        }

        public const int SheetColumns = 4;
        public const int SheetRows = 4;

        [SerializeField] private MarkEntry[] _marks = new MarkEntry[0];
        [SerializeField] private SheetEntry[] _sheets = new SheetEntry[0];
        [Header("Props")]
        [Tooltip("The sealed ruin gate a giant breaks out of (Closed, Open and Idle clips).")]
        [SerializeField] private ModelArtDefinition _ruinGate;
        [SerializeField] [Min(0.1f)] private float _ruinGateScale = 0.65f;
        [Tooltip("Seal crystals: one model holding the variants, a white emission mask tinted per crystal.")]
        [SerializeField] private ModelArtDefinition _sealCrystals;
        [SerializeField] private string[] _sealCrystalParts = { "SealCrystal_A", "SealCrystal_B", "SealCrystal_C" };
        [Tooltip("The ice block round a frozen gate (alpha = opacity), sized to the largest gate.")]
        [SerializeField] private ModelArtDefinition _iceBlock;
        [Tooltip("Gravemaw's crack slabs (README round 30): one model holding the variants, each pivoted on its hinge, rising and glowing towards the fissure.")]
        [SerializeField] private ModelArtDefinition _crackSlabs;
        [SerializeField] private string[] _crackSlabParts = { "CrackSlab_A", "CrackSlab_B", "CrackSlab_C", "CrackSlab_D" };
        [Header("Gravemaw fissure (README round 34)")]
        [Tooltip("The modular fissure kit: one model holding the modules (start, straights, curves, head, branches), bent along a burrow crack.")]
        [SerializeField] private GameObject _fissureKit;
        [Tooltip("The same modules with fewer rows and pebbles, for the low profile.")]
        [SerializeField] private GameObject _fissureKitLow;
        [SerializeField] private Material _fissureRock;
        [SerializeField] private Material _fissureRockLow;
        [Tooltip("The first punch stage: marks the gap opening where it is in view (Game/DepthPunch), so the walls below can show.")]
        [SerializeField] private Material _fissurePunch;
        [Tooltip("The second punch stage: the far depth where the first marked the opening (Game/DepthPunch with _FAR_DEPTH).")]
        [SerializeField] private Material _fissureClear;
        [Tooltip("Uniform scale of the kit (modelled 1.44 m wide): 2 gives a crack about 2.9 m wide with a 1.1 m gap.")]
        [SerializeField] [Min(0.5f)] private float _fissureScale = 2f;
        [Tooltip("Metres behind the open front over which the crack tears from shut to fully open.")]
        [SerializeField] [Min(0.1f)] private float _fissureOpenLength = 2.2f;
        [Tooltip("Seconds the molten core keeps glowing after the crack has fully opened.")]
        [SerializeField] [Min(0.1f)] private float _fissureCoolSeconds = 10f;
        [SerializeField] private DrainedMap[] _drainedMaps = new DrainedMap[0];
        [Header("HUD")]
        [Tooltip("The broken-ring overlay over a dampened tower's picture beside the boss bar (white, tinted).")]
        [SerializeField] private Texture2D _dampened;

        /// <summary>The registered art, or null in tests and tools that run without it.</summary>
        public static BossFxArt Active => Services.TryGet(out BossFxArt art) ? art : null;

        public ModelArtDefinition RuinGate => _ruinGate;
        public float RuinGateScale => _ruinGateScale;
        public ModelArtDefinition SealCrystals => _sealCrystals;
        public string[] SealCrystalParts => _sealCrystalParts;
        public ModelArtDefinition IceBlock => _iceBlock;
        public ModelArtDefinition CrackSlabs => _crackSlabs;
        public string[] CrackSlabParts => _crackSlabParts;
        public Texture2D Dampened => _dampened;

        /// <summary>True when the fissure kit, its rock and its punch are all there.</summary>
        public bool HasFissure => _fissureKit != null && _fissureRock != null && _fissurePunch != null && _fissureClear != null;

        /// <summary>The fissure kit for the current profile.</summary>
        public GameObject FissureKit => ModelQuality.IsLow && _fissureKitLow != null ? _fissureKitLow : _fissureKit;

        public Material FissureRock => ModelQuality.IsLow && _fissureRockLow != null ? _fissureRockLow : _fissureRock;
        public Material FissurePunch => _fissurePunch;
        public Material FissureClear => _fissureClear;
        public float FissureScale => _fissureScale;
        public float FissureOpenLength => _fissureOpenLength;
        public float FissureCoolSeconds => _fissureCoolSeconds;

        public bool Has(Mark mark) => FindMark(mark) != null;

        public bool Has(Sheet sheet) => FindSheet(sheet) != null;

        /// <summary>The shared material of a mark for the current profile; views copy it before changing it.</summary>
        public Material Material(Mark mark) => FindMark(mark)?.Material(ModelQuality.IsLow);

        public float Width(Mark mark) => FindMark(mark)?.Width ?? 1f;

        public float TileLength(Mark mark) => FindMark(mark)?.TileLength ?? 1f;

        public Material Material(Sheet sheet) => FindSheet(sheet)?.Material(ModelQuality.IsLow);

        public float FramesPerSecond(Sheet sheet) => FindSheet(sheet)?.FramesPerSecond ?? 12f;

        /// <summary>The drained version of a lit emission map, or null when the map has none.</summary>
        public Texture DrainedFor(Texture lit)
        {
            if (lit == null)
            {
                return null;
            }

            foreach (DrainedMap map in _drainedMaps)
            {
                if (map != null && map.Lit != null && (map.Lit == lit || map.Lit.name == lit.name))
                {
                    return map.Drained;
                }
            }

            return null;
        }

        private MarkEntry FindMark(Mark mark)
        {
            foreach (MarkEntry entry in _marks)
            {
                if (entry != null && entry.Mark == mark && entry.Material(false) != null)
                {
                    return entry;
                }
            }

            return null;
        }

        private SheetEntry FindSheet(Sheet sheet)
        {
            foreach (SheetEntry entry in _sheets)
            {
                if (entry != null && entry.Sheet == sheet && entry.Material(false) != null)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
