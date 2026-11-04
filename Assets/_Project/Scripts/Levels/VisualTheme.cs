using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Look and sound of one level as data: palette, light, fog, bloom and ambience. Changing it needs no code.</summary>
    [CreateAssetMenu(menuName = "Game/Visual Theme", fileName = "VisualTheme")]
    public sealed class VisualTheme : ScriptableObject
    {
        [SerializeField] private Color _backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        [SerializeField] private Color _groundColor = new Color(0.25f, 0.28f, 0.33f);
        [SerializeField] private Color _pathColor = new Color(0.12f, 0.14f, 0.18f);
        [SerializeField] private Color _slotColor = new Color(0.2f, 0.38f, 0.6f);
        [SerializeField] private Color _baseColor = new Color(0.3f, 0.7f, 1f);
        [Header("Models")]
        [SerializeField] private GameObject _groundTile;
        [SerializeField] private GameObject _roadStraight;
        [SerializeField] private GameObject _roadCorner;
        [SerializeField] private GameObject _roadEnd;
        [SerializeField] private GameObject _roadCross;
        [SerializeField] private GameObject _baseModel;
        [SerializeField] private GameObject _spawnModel;
        [SerializeField] private GameObject[] _blockedProps = new GameObject[0];
        [SerializeField] private GameObject[] _scenery = new GameObject[0];

        [Header("Landscape")]
        [SerializeField] private Color _rockColor = new Color(0.3f, 0.3f, 0.34f);
        [SerializeField] private Color _accentColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color _waterColor = new Color(0.1f, 0.4f, 0.55f);
        [SerializeField] private Color _lavaColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color _sandColor = new Color(0.62f, 0.5f, 0.3f);
        [SerializeField] private Color _iceColor = new Color(0.75f, 0.9f, 1f);
        [SerializeField] private Color _bogColor = new Color(0.18f, 0.24f, 0.12f);

        /// <summary>The glow of the veins at the bottom of a rift.</summary>
        [SerializeField] private Color _riftColor = new Color(1f, 0.58f, 0.25f);

        /// <summary>The glow of the gates the enemies come through: hot red on most worlds, cold on the ice.</summary>
        [SerializeField] private Color _spawnColor = new Color(1f, 0.4f, 0.25f);
        [SerializeField] private Color _plantColor = new Color(0.2f, 0.5f, 0.35f);
        [SerializeField] private PropKind[] _propKinds = { PropKind.Rock, PropKind.Boulder, PropKind.Crystal, PropKind.Frond };
        [SerializeField] [Range(0f, 1f)] private float _veinStrength = 0.5f;
        [SerializeField] [Min(0f)] private float _ambientRate = 10f;
        [SerializeField] private float _ambientRise = 0.15f;

        [Header("Sound")]
        [SerializeField] private AmbienceKind _ambience = AmbienceKind.Wind;

        [Header("Light and atmosphere")]
        [SerializeField] private Color _lightColor = new Color(0.8f, 0.85f, 1f);
        [SerializeField] [Min(0f)] private float _lightIntensity = 1.1f;
        [SerializeField] private Color _fogColor = new Color(0.06f, 0.08f, 0.12f);
        [SerializeField] [Min(0f)] private float _fogDensity = 0.015f;
        [SerializeField] [Min(0f)] private float _bloomIntensity = 1.2f;

        [Header("Light rig")]
        [Tooltip("Height of the sun above the horizon. Lower suns throw longer shadows and model shapes better.")]
        [SerializeField] [Range(15f, 80f)] private float _sunPitch = 42f;
        [SerializeField] [Range(-180f, 180f)] private float _sunYaw = -35f;
        [Tooltip("A second, shadowless light from behind the field that lines the edges of every shape.")]
        [SerializeField] private Color _rimColor = new Color(0.35f, 0.85f, 1f);
        [SerializeField] [Min(0f)] private float _rimIntensity = 0.5f;
        [Tooltip("Colour of the light coming from above: ambient on upward faces and the reflection on shiny metal.")]
        [SerializeField] private Color _skyColor = new Color(0.35f, 0.45f, 0.65f);

        [Header("Colour grading")]
        [SerializeField] [Range(-50f, 80f)] private float _contrast = 12f;
        [SerializeField] [Range(-50f, 80f)] private float _saturation = 8f;
        [SerializeField] private Color _shadowTint = new Color(0.9f, 0.95f, 1.1f);
        [SerializeField] private Color _highlightTint = new Color(1.05f, 1f, 0.95f);

        [Header("Imported models")]
        [Tooltip("The Blender colony base; empty keeps the procedural outpost.")]
        [SerializeField] private BaseArtDefinition _baseArt;
        [Tooltip("The Blender resource node (full, half and empty meshes); empty keeps the procedural crystal cluster.")]
        [SerializeField] private ModelArtDefinition _resourceNodeArt;
        [Tooltip("The continent's Blender environment (ground layers, kit, decor); empty keeps the procedural landscape.")]
        [SerializeField] private EnvironmentSet _environment;

        [Header("Decor clearance")]
        [Tooltip("Free ground kept round every structure (base, gates, entrances, mines, nests): no rock, plant, cliff or silhouette may reach into it.")]
        [SerializeField] [Min(0f)] private float _decorClearance = 0.5f;
        [Tooltip("How far the procedural outpost reaches round its middle; an imported base measures its own model.")]
        [SerializeField] [Min(0.5f)] private float _baseFootprint = 1.9f;

        public float DecorClearance => _decorClearance;
        public float BaseFootprint => _baseFootprint;
        public Color BackgroundColor => _backgroundColor;
        public Color GroundColor => _groundColor;
        public Color PathColor => _pathColor;
        public Color SlotColor => _slotColor;
        public Color BaseColor => _baseColor;
        public GameObject GroundTile => _groundTile;
        public GameObject RoadStraight => _roadStraight;
        public GameObject RoadCorner => _roadCorner;
        public GameObject RoadEnd => _roadEnd;
        public GameObject RoadCross => _roadCross;
        public GameObject BaseModel => _baseModel;
        public GameObject SpawnModel => _spawnModel;

        /// <summary>Rocks and crystals placed on blocked cells.</summary>
        public GameObject[] BlockedProps => _blockedProps;

        /// <summary>Props scattered on the ground around the playing field.</summary>
        public GameObject[] Scenery => _scenery;

        public Color RockColor => _rockColor;

        /// <summary>The glow colour of the level's plants, crystals and veins in the ground.</summary>
        public Color AccentColor => _accentColor;

        public Color WaterColor => _waterColor;
        public Color LavaColor => _lavaColor;
        public Color SandColor => _sandColor;
        public Color IceColor => _iceColor;
        public Color BogColor => _bogColor;
        public Color RiftColor => _riftColor;
        public Color SpawnColor => _spawnColor;

        /// <summary>The colour of leaves, stalks and caps; the glow on them comes from the accent.</summary>
        public Color PlantColor => _plantColor;

        /// <summary>The decorations this level grows. A kind listed twice is scattered twice as often.</summary>
        public PropKind[] PropKinds => _propKinds;

        /// <summary>How strongly glowing veins show in the ground, from none to a full network.</summary>
        public float VeinStrength => _veinStrength;

        /// <summary>Floating motes per second (spores, embers, dust) and their vertical speed; negative falls.</summary>
        public float AmbientRate => _ambientRate;
        public float AmbientRise => _ambientRise;

        /// <summary>The looping background sound of the level.</summary>
        public AmbienceKind Ambience => _ambience;

        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public float BloomIntensity => _bloomIntensity;
        public float SunPitch => _sunPitch;
        public float SunYaw => _sunYaw;
        public Color RimColor => _rimColor;
        public float RimIntensity => _rimIntensity;
        public Color SkyColor => _skyColor;
        public float Contrast => _contrast;
        public float Saturation => _saturation;
        public Color ShadowTint => _shadowTint;
        public Color HighlightTint => _highlightTint;
        public BaseArtDefinition BaseArt => _baseArt;
        public ModelArtDefinition ResourceNodeArt => _resourceNodeArt;
        public EnvironmentSet Environment => _environment;
    }
}



