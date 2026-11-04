using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported (Blender) environment of one continent as data: the tiling ground sets the terrain shader blends,
    /// the kit pieces placed from the grid (kerbs, pads, plinths, banks, cliffs, terraces, rifts, burrows), the meshes
    /// that replace the procedural decoration kinds, scatter, background silhouettes and the spawn openings per terrain.
    /// A <see cref="VisualTheme"/> points at its continent's set; a theme without one keeps the procedural landscape.
    /// Every continent uses the same piece names under its own prefix, so a new continent is a new asset, not new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Environment Set", fileName = "EnvironmentSet")]
    public sealed class EnvironmentSet : ScriptableObject
    {
        [Tooltip("The export prefix of the continent (empty for Crystal Plains, Ash_ for Ashen Reach, ...).")]
        [SerializeField] private string _prefix = string.Empty;

        [Tooltip("The ground shader (Game/EnvironmentGround), referenced so a build keeps it.")]
        [SerializeField] private Shader _groundShader;

        [Header("Ground layers")]
        [SerializeField] private EnvironmentLayer _ground = new EnvironmentLayer();
        [Tooltip("The second ground layer (flora, ash, moss): noise patches away from the road.")]
        [SerializeField] private EnvironmentLayer _groundAlt = new EnvironmentLayer();
        [SerializeField] private EnvironmentLayer _road = new EnvironmentLayer();
        [SerializeField] private EnvironmentLayer _plaza = new EnvironmentLayer();
        [Tooltip("On steep ground: plateau faces, rift walls, the rim hills.")]
        [SerializeField] private EnvironmentLayer _cliff = new EnvironmentLayer();
        [Tooltip("The element layer under the continent's element terrain (the rifts of Crystal Plains).")]
        [SerializeField] private EnvironmentLayer _element = new EnvironmentLayer();
        [SerializeField] private TerrainKind _elementTerrain = TerrainKind.Rift;
        [SerializeField] private Texture2D _detailNormal;
        [SerializeField] [Min(0.05f)] private float _detailTileSize = 0.6f;
        [SerializeField] [Range(0f, 2f)] private float _detailStrength = 0.5f;

        [Header("Ground blend")]
        [Tooltip("How much the baked level painting (patches, shores, far darkening) tints the natural layers.")]
        [SerializeField] [Range(0f, 1f)] private float _paintInfluence = 0.6f;
        [Tooltip("The painting's plain ground colour as a multiple of the theme's ground colour (neutral tint).")]
        [SerializeField] [Min(0.1f)] private float _paintReference = 1.3f;
        [Tooltip("Mip bias of the painting: higher keeps only the broad patches.")]
        [SerializeField] [Range(0f, 6f)] private float _paintBlur = 2.5f;
        [Tooltip("The painting's glow (veins, cliff seams, lava cracks) kept off the road.")]
        [SerializeField] [Min(0f)] private float _paintGlow = 0.55f;
        [Tooltip("How far a layer's height pushes its edge (README: 0.35).")]
        [SerializeField] [Range(0f, 1f)] private float _heightBlend = 0.35f;
        [SerializeField] [Min(1f)] private float _roadSharpness = 7f;
        [SerializeField] [Min(1f)] private float _plazaSharpness = 7f;
        [SerializeField] [Min(1f)] private float _altSharpness = 4f;
        [SerializeField] [Min(1f)] private float _elementSharpness = 5f;
        [Tooltip("Slope (1 - normal.y) where the cliff layer starts and where it is full.")]
        [SerializeField] private Vector2 _cliffSlope = new Vector2(0.28f, 0.5f);
        [Tooltip("Noise threshold of the second ground layer: higher, fewer patches.")]
        [SerializeField] [Range(0f, 1f)] private float _altCoverage = 0.55f;
        [SerializeField] [Min(0.01f)] private float _altNoiseScale = 0.3f;
        [Tooltip("The second layer keeps this many cells off the road, so the road edge stays one clean line.")]
        [SerializeField] [Min(0f)] private float _altRoadClearance = 0.8f;
        [SerializeField] [Min(0f)] private float _sparkle = 0.6f;
        [SerializeField] private Color _sparkleColour = new Color(1f, 0.7f, 0.95f);
        [SerializeField] [Range(0f, 1f)] private float _occlusion = 0.6f;
        [SerializeField] [Range(0f, 2f)] private float _smoothness = 1f;

        [Header("Kit")]
        [SerializeField] private EnvironmentPiece[] _pieces = new EnvironmentPiece[0];
        [SerializeField] private EnvironmentDecor[] _decor = new EnvironmentDecor[0];
        [SerializeField] private EnvironmentOrigin[] _origins = new EnvironmentOrigin[0];

        [Header("Banks")]
        [Tooltip("The bendable bank segments (README round 24b, 1 unit along x, liquid on -z), swept along every pool's smoothed shore. Empty keeps the square bank kit.")]
        [SerializeField] private EnvironmentMesh[] _bankSweeps = new EnvironmentMesh[0];
        [Tooltip("Loose shore rocks laid on the bank to hide the joints between segments.")]
        [SerializeField] private EnvironmentMesh _bankAccent;
        [Tooltip("Share of the joints between bank segments that get a shore rock.")]
        [SerializeField] [Range(0f, 1f)] private float _bankAccentShare = 0.6f;
        [Tooltip("Shore rocks per pool laid anywhere along the bank, beside the ones on the joints.")]
        [SerializeField] [Min(0)] private int _looseBankAccents = 3;

        [Header("Far decor")]
        [Tooltip("Decor more than this many cells outside the field is drawn dimmer, so the wild land recedes behind the field.")]
        [SerializeField] [Min(0f)] private float _farDecorStart = 2f;
        [SerializeField] [Range(0f, 1f)] private float _farDecorAlbedo = 0.65f;
        [SerializeField] [Range(0f, 1f)] private float _farDecorGlow = 0.35f;
        [Tooltip("Share of the far decor the Low (Switch) profile keeps; far decor never throws a shadow.")]
        [SerializeField] [Range(0f, 1f)] private float _lowFarDecorKeep = 0.5f;

        [Header("Scatter")]
        [SerializeField] private EnvironmentMesh[] _scatter = new EnvironmentMesh[0];
        [Tooltip("Scatter only near nests (shells, egg fragments).")]
        [SerializeField] private EnvironmentMesh[] _nestScatter = new EnvironmentMesh[0];
        [Tooltip("Chance of a small scatter piece per free cell (README: about one per two cells).")]
        [SerializeField] [Range(0f, 1f)] private float _scatterChance = 0.5f;
        [SerializeField] [Min(0f)] private float _nestScatterRadius = 2.5f;
        [SerializeField] [Range(0f, 1f)] private float _veinChance = 0.04f;

        [Header("Border")]
        [Tooltip("Walls stand this many cells outside the field, where the rim hills begin.")]
        [SerializeField] [Min(0f)] private float _cliffInsetCells = 1.5f;
        [Tooltip("Share of the side edges that get walls, from the back; the front stays open so walls never hide the field.")]
        [SerializeField] [Range(0f, 1f)] private float _cliffSideReach = 0.75f;
        [SerializeField] private EnvironmentMesh[] _silhouettes = new EnvironmentMesh[0];
        [SerializeField] [Min(0)] private int _silhouetteCount = 7;
        [Tooltip("Cells outside the field where silhouettes stand, nearest and farthest.")]
        [SerializeField] private Vector2 _silhouetteDistance = new Vector2(6f, 12f);

        [Header("Build spots")]
        [Tooltip("Draw the build pad tile on every buildable cell. Off: buildable ground is plain ground at rest (Remco 2026-11-04: no grid of squares).")]
        [SerializeField] private bool _showBuildPads;
        [Tooltip("While a tower is being placed, the free cells glow softly in this colour on the ground.")]
        [SerializeField] private Color _placingColour = new Color(0.45f, 0.8f, 1f);
        [Tooltip("Glow of a free cell next to the cursor while placing.")]
        [SerializeField] [Min(0f)] private float _placingGlow = 0.9f;
        [Tooltip("Cells from the cursor where the free cells' glow has faded out completely.")]
        [SerializeField] [Min(1f)] private float _placingRadiusCells = 4.5f;

        [Header("Landmarks")]
        [Tooltip("The continent's story set pieces (a seal in its states, a wreck), by mesh name; placed only by the level code that asks for one.")]
        [SerializeField] private EnvironmentMesh[] _landmarks = new EnvironmentMesh[0];

        [Header("Wind")]
        [SerializeField] private Vector3 _windDirection = new Vector3(1f, 0f, 0.4f);
        [Tooltip("Sway of the flora tips in world units (README: about 0.04).")]
        [SerializeField] [Min(0f)] private float _swayAmplitude = 0.05f;
        [SerializeField] [Min(0f)] private float _swaySpeed = 1f;
        [SerializeField] [Min(0f)] private float _flutter = 0.012f;

        public string Prefix => _prefix;
        public Shader GroundShader => _groundShader;
        public EnvironmentLayer Ground => _ground;
        public EnvironmentLayer GroundAlt => _groundAlt;
        public EnvironmentLayer Road => _road;
        public EnvironmentLayer Plaza => _plaza;
        public EnvironmentLayer Cliff => _cliff;
        public EnvironmentLayer Element => _element;
        public TerrainKind ElementTerrain => _elementTerrain;
        public Texture2D DetailNormal => _detailNormal;
        public float DetailTileSize => _detailTileSize;
        public float DetailStrength => _detailStrength;
        public float PaintInfluence => _paintInfluence;
        public float PaintReference => _paintReference;
        public float PaintBlur => _paintBlur;
        public float PaintGlow => _paintGlow;
        public float HeightBlend => _heightBlend;
        public Vector4 Sharpness => new Vector4(_roadSharpness, _plazaSharpness, _altSharpness, _elementSharpness);
        public Vector2 CliffSlope => _cliffSlope;
        public float AltCoverage => _altCoverage;
        public float AltNoiseScale => _altNoiseScale;
        public float AltRoadClearance => _altRoadClearance;
        public float Sparkle => _sparkle;
        public Color SparkleColour => _sparkleColour;
        public float Occlusion => _occlusion;
        public float Smoothness => _smoothness;
        public float FarDecorStart => _farDecorStart;
        public float FarDecorAlbedo => _farDecorAlbedo;
        public float FarDecorGlow => _farDecorGlow;
        public float LowFarDecorKeep => _lowFarDecorKeep;
        public EnvironmentMesh[] BankSweeps => _bankSweeps;
        public EnvironmentMesh BankAccent => _bankAccent;
        public float BankAccentShare => _bankAccentShare;
        public int LooseBankAccents => _looseBankAccents;

        /// <summary>Whether the pools get swept banks (and the square bank kit is left out).</summary>
        public bool HasBankSweeps
        {
            get
            {
                foreach (EnvironmentMesh sweep in _bankSweeps)
                {
                    if (sweep != null && sweep.IsValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public EnvironmentMesh[] Scatter => _scatter;
        public EnvironmentMesh[] NestScatter => _nestScatter;
        public float ScatterChance => _scatterChance;
        public float NestScatterRadius => _nestScatterRadius;
        public float VeinChance => _veinChance;
        public float CliffInsetCells => _cliffInsetCells;
        public float CliffSideReach => _cliffSideReach;
        public EnvironmentMesh[] Silhouettes => _silhouettes;
        public int SilhouetteCount => _silhouetteCount;
        public Vector2 SilhouetteDistance => _silhouetteDistance;
        public bool ShowBuildPads => _showBuildPads;
        public Color PlacingColour => _placingColour;
        public float PlacingGlow => _placingGlow;
        public float PlacingRadiusCells => _placingRadiusCells;
        public EnvironmentMesh[] Landmarks => _landmarks;
        public Vector3 WindDirection => _windDirection.sqrMagnitude > 0f ? _windDirection.normalized : Vector3.right;
        public float SwayAmplitude => _swayAmplitude;
        public float SwaySpeed => _swaySpeed;
        public float Flutter => _flutter;

        /// <summary>The kit piece of that kind, or null when the set has none (the placer then leaves it out).</summary>
        public EnvironmentMesh Piece(EnvironmentPieceKind kind)
        {
            foreach (EnvironmentPiece piece in _pieces)
            {
                if (piece != null && piece.Kind == kind && piece.Model != null && piece.Model.IsValid)
                {
                    return piece.Model;
                }
            }

            return null;
        }

        /// <summary>The imported stand-in for a procedural decoration kind, or null to keep the procedural one.</summary>
        public EnvironmentDecor Decor(PropKind kind)
        {
            foreach (EnvironmentDecor decor in _decor)
            {
                if (decor != null && decor.Kind == kind && decor.Variants.Length > 0)
                {
                    return decor;
                }
            }

            return null;
        }

        /// <summary>The spawn opening for a nest on that terrain, or null to keep the procedural crater.</summary>
        public EnvironmentOrigin Origin(TerrainKind terrain)
        {
            foreach (EnvironmentOrigin origin in _origins)
            {
                if (origin != null && origin.Terrain == terrain && origin.Model != null && origin.Model.IsValid)
                {
                    return origin;
                }
            }

            return null;
        }

        /// <summary>The story set piece of that mesh name (<c>Core_Seal_Cracked</c>), or null when the set has none.</summary>
        public EnvironmentMesh Landmark(string meshName)
        {
            foreach (EnvironmentMesh landmark in _landmarks)
            {
                if (landmark != null && landmark.IsValid && landmark.Name == meshName)
                {
                    return landmark;
                }
            }

            return null;
        }

        /// <summary>Whether the terrain shader has the layers it needs.</summary>
        public bool HasGround => _groundShader != null && _ground.IsValid && _road.IsValid;

        private void OnValidate()
        {
            if (!_ground.IsValid || !_road.IsValid)
            {
                Debug.LogWarning($"Environment set '{name}' lacks its ground or road layer: levels fall back to the painted ground.", this);
            }

            foreach (EnvironmentPiece piece in _pieces)
            {
                if (piece == null || piece.Model == null || !piece.Model.IsValid)
                {
                    Debug.LogWarning($"Environment set '{name}' has a kit piece without mesh or material.", this);
                }
            }

            foreach (EnvironmentDecor decor in _decor)
            {
                foreach (EnvironmentMesh variant in decor.Variants)
                {
                    if (variant == null || !variant.IsValid)
                    {
                        Debug.LogWarning($"Environment set '{name}': decor {decor.Kind} has a variant without mesh or material.", this);
                    }
                }
            }
        }
    }
}
