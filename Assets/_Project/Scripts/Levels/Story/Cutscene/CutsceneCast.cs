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
        [SerializeField] private EnemyDefinition[] _swarm = new EnemyDefinition[0];
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
        [SerializeField] [Min(1)] private int _swarmCount = 36;

        [Header("Optional models (empty builds the procedural one)")]
        [SerializeField] private GameObject _mothership;
        [SerializeField] private GameObject _capsule;
        [SerializeField] private GameObject _drill;
        [SerializeField] private GameObject _beacon;
        [SerializeField] private Texture2D _earthSurface;

        public HeroDefinition Hero => _hero;
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
