using Game.Core;
using Game.Drones;
using Game.Enemies;
using Game.Hero;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Who and what appears in a cutscene, as data: the commander, the drones, the creatures, the field level the
    /// story happens on, the look of each set and how many of everything. A role with a prefab override shows that
    /// prefab; without one the set builds its procedural model, so a finished model arrives by data alone.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Cutscene Cast", fileName = "CutsceneCast")]
    public sealed class CutsceneCast : ScriptableObject
    {
        [Header("Characters")]
        [SerializeField] private HeroDefinition _hero;
        [SerializeField] private DroneDefinition _hauler;
        [SerializeField] private DroneDefinition _builder;
        [Tooltip("The brood that climbs out with the Queen and stands glaring at the colony: walkers, one of each in turn.")]
        [SerializeField] private EnemyDefinition[] _swarm = new EnemyDefinition[0];
        [Tooltip("The Queen: she rises at the head of the brood and screams.")]
        [SerializeField] private EnemyDefinition _screamer;

        [Header("Places")]
        [Tooltip("The level the field shots play on; empty uses the first level of the campaign.")]
        [SerializeField] private LevelDefinition _fieldLevel;
        [Tooltip("The look of the Earth shots; empty falls back to the field level's theme.")]
        [SerializeField] private VisualTheme _earthTheme;
        [Tooltip("The look of the underground shots; empty falls back to the field level's theme.")]
        [SerializeField] private VisualTheme _undergroundTheme;

        [Header("Counts (scaled down by the Low profile)")]
        [SerializeField] [Min(1)] private int _capsuleCount = 9;
        [SerializeField] [Min(1)] private int _eyePairs = 300;
        [SerializeField] [Min(1)] private int _droneCount = 8;
        [Tooltip("How many of the brood stand with the Queen; a few, never a horde (the set has places for six).")]
        [SerializeField] [Min(1)] private int _swarmCount = 5;

        [Header("Optional models (empty builds the procedural one)")]
        [SerializeField] private GameObject _mothership;
        [SerializeField] private GameObject _capsule;
        [SerializeField] private GameObject _drill;
        [SerializeField] private GameObject _beacon;
        [SerializeField] private Texture2D _earthSurface;
        [Tooltip("How Earth looks from orbit in the prologue.")]
        [SerializeField] private PlanetStyle _earthPlanet = PlanetStyle.Earth();

        [Header("Imported (Blender) models: worn before the prefab overrides above and the procedural models")]
        [SerializeField] private ModelArtDefinition _mothershipArt;
        [SerializeField] private ModelArtDefinition _capsuleArt;
        [Tooltip("The deorbit stage on each capsule in the orbit shot; empty drops the capsules without one.")]
        [SerializeField] private ModelArtDefinition _boosterArt;
        [Tooltip("The NTI livery plate on the mothership's starboard cargo frame, facing the orbit camera; empty leaves the hull bare.")]
        [SerializeField] private Texture2D _hullLivery;
        [SerializeField] private ModelArtDefinition _drillArt;
        [SerializeField] private ModelArtDefinition _beaconArt;
        [Tooltip("The welding drones that fly round the base as it goes up.")]
        [SerializeField] private ModelArtDefinition _builderArt;
        [Tooltip("The haulers that carry the ore from the drill.")]
        [SerializeField] private ModelArtDefinition _haulerArt;
        [Tooltip("The command post's radar console the radar shot shows.")]
        [SerializeField] private ModelArtDefinition _consoleArt;

        [Header("Imported (Blender) sets and sprites")]
        [Tooltip("The cave kit and the Burrower eyes of the underground shots; empty keeps the procedural cave.")]
        [SerializeField] private UndergroundArt _underground;
        [Tooltip("The glow, trail and crack sprites; empty keeps the soft textures the code paints.")]
        [SerializeField] private CutsceneFxSet _fx;
        [Tooltip("The Earth of the prologue painted in Blender; incomplete maps fall back to the painter.")]
        [SerializeField] private EarthMaps _earthMaps = new EarthMaps();
        [SerializeField] private EarthMaps _earthMapsLow = new EarthMaps();

        [Header("Attack")]
        [Tooltip("Flyers that hover over the brood.")]
        [SerializeField] private EnemyDefinition[] _flyers = new EnemyDefinition[0];
        [SerializeField] [Min(0)] private int _flyerCount = 1;

        [Tooltip("Which of the commander's clips plays as he steps out of the landing capsule and down its ramp; when his model has no such clip he walks out.")]
        [SerializeField] private ModelClipRole _capsuleExitClip = ModelClipRole.CapsuleExit;

        public HeroDefinition Hero => _hero;

        /// <summary>The commander's clip for stepping out of the capsule (round 39); the walk stands in while his model lacks it.</summary>
        public ModelClipRole CapsuleExitClip => _capsuleExitClip;
        public DroneDefinition Hauler => _hauler;
        public DroneDefinition Builder => _builder;
        public EnemyDefinition[] Swarm => _swarm;
        public EnemyDefinition Screamer => _screamer;
        public LevelDefinition FieldLevel => _fieldLevel;
        public VisualTheme EarthTheme => _earthTheme;
        public VisualTheme UndergroundTheme => _undergroundTheme;
        public int CapsuleCount => _capsuleCount;
        public int EyePairs => _eyePairs;
        public int DroneCount => _droneCount;
        public int SwarmCount => _swarmCount;
        public GameObject Mothership => _mothership;
        public GameObject Capsule => _capsule;
        public GameObject Drill => _drill;
        public GameObject Beacon => _beacon;
        public Texture2D EarthSurface => _earthSurface;
        public PlanetStyle EarthPlanet => _earthPlanet ?? (_earthPlanet = PlanetStyle.Earth());
        public ModelArtDefinition MothershipArt => _mothershipArt;
        public ModelArtDefinition CapsuleArt => _capsuleArt;
        public ModelArtDefinition BoosterArt => _boosterArt;
        public Texture2D HullLivery => _hullLivery;
        public ModelArtDefinition DrillArt => _drillArt;
        public ModelArtDefinition BeaconArt => _beaconArt;
        public ModelArtDefinition BuilderArt => _builderArt;
        public ModelArtDefinition HaulerArt => _haulerArt;
        public ModelArtDefinition ConsoleArt => _consoleArt;
        public UndergroundArt Underground => _underground;
        public CutsceneFxSet Fx => _fx;
        public EnemyDefinition[] Flyers => _flyers;
        public int FlyerCount => _flyerCount;

        /// <summary>The painted Earth for the quality profile, or null when its maps are incomplete.</summary>
        public EarthMaps EarthMapsFor(bool low)
        {
            if (low && _earthMapsLow != null && _earthMapsLow.IsComplete)
            {
                return _earthMapsLow;
            }

            return _earthMaps != null && _earthMaps.IsComplete ? _earthMaps : null;
        }

        private void OnValidate()
        {
            if (_hero == null)
            {
                Debug.LogWarning($"Cutscene cast '{name}' has no commander.", this);
            }

            if (_swarm == null || _swarm.Length == 0)
            {
                Debug.LogWarning($"Cutscene cast '{name}' has no swarm creatures.", this);
            }
        }
    }
}
