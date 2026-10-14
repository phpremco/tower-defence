using UnityEngine;
using Game.Drones;
using Game.Hero;
using Game.PathObjects;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// Everything that makes one level, as data: grid, paths, build slots, available towers, waves and look.
    /// A new level is a new asset, not new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "New Level";
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(16, 10);
        [SerializeField] [Min(0.1f)] private float _cellSize = 1f;
        [SerializeField] private VisualTheme _theme;
        [SerializeField] private CameraSettings _camera = CameraSettings.Default;
        [SerializeField] private EnemyPath[] _paths = new EnemyPath[0];
        [SerializeField] private Vector2Int[] _blockedCells = new Vector2Int[0];
        [SerializeField] private BlockadeSpot[] _blockades = new BlockadeSpot[0];
        [SerializeField] private TerrainPatch[] _terrain = new TerrainPatch[0];
        /// <summary>Makes every enemy of this level tougher, so later levels are harder before difficulty is applied.</summary>
        [SerializeField] [Min(0.1f)] private float _enemyHealthMultiplier = 1f;
        [SerializeField] private HeroDefinition _hero;
        [SerializeField] private DroneDefinition _repairDrone;
        [SerializeField] private DroneDefinition _collectorDrone;
        [SerializeField] private CombineRecipe[] _recipes = new CombineRecipe[0];
        [SerializeField] private PathObjectDefinition[] _pathObjects = new PathObjectDefinition[0];
        [SerializeField] private TowerDefinition[] _availableTowers = new TowerDefinition[0];
        [SerializeField] [Min(0)] private int _startingMoney = 150;
        [SerializeField] [Min(1)] private int _baseHealth = 20;
        [SerializeField] private DifficultyWaves[] _difficultyWaves = new DifficultyWaves[0];
        [SerializeField] private LevelReward _reward;

        /// <summary>Share of base health that must be left for two and for three stars, in that order.</summary>
        [SerializeField] private float[] _starBaseHealthFractions = { 0.5f, 0.9f };

        public string Id => name;
        public string DisplayName => _displayName;
        public Vector2Int GridSize => _gridSize;
        public float CellSize => _cellSize;
        public VisualTheme Theme => _theme;
        public CameraSettings Camera => _camera;
        public EnemyPath[] Paths => _paths;

        /// <summary>Waypoints of the first road path: the main road, which also ends at the base.</summary>
        public Vector2Int[] MainRoad
        {
            get
            {
                for (int i = 0; i < _paths.Length; i++)
                {
                    if (!_paths[i].Flying)
                    {
                        return _paths[i].Waypoints;
                    }
                }

                return new Vector2Int[0];
            }
        }
        /// <summary>Cells where nothing can be built. Every other cell off the road is buildable.</summary>
        public Vector2Int[] BlockedCells => _blockedCells;
        /// <summary>Fixed blockades on the road. Ground enemies stop at them until they are destroyed.</summary>
        public BlockadeSpot[] Blockades => _blockades;

        /// <summary>Raised ground and liquids. Liquids cannot hold towers; every other cell is plain ground.</summary>
        public TerrainPatch[] TerrainPatches => _terrain;

        /// <summary>Lookup built from <see cref="TerrainPatches"/>, cached because several systems ask for it.</summary>
        public TerrainMap Terrain => _terrainMap ??= new TerrainMap(_terrain);

        /// <summary>The commander for this level, or null for a level without one.</summary>
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public HeroDefinition Hero => _hero;

        /// <summary>The kinds of drone this level uses. How many fly comes from permanent upgrades.</summary>
        public DroneDefinition RepairDrone => _repairDrone;
        public DroneDefinition CollectorDrone => _collectorDrone;

        /// <summary>Recipes the player can use in this level to combine neighbouring towers.</summary>
        public CombineRecipe[] Recipes => _recipes;

        /// <summary>Path objects that are for sale in this level.</summary>
        public PathObjectDefinition[] PathObjects => _pathObjects;
        public TowerDefinition[] AvailableTowers => _availableTowers;
        public int StartingMoney => _startingMoney;
        public int BaseHealth => _baseHealth;
        public DifficultyWaves[] DifficultyWaves => _difficultyWaves;
        public LevelReward Reward => _reward;
        public float[] StarBaseHealthFractions => _starBaseHealthFractions;

        public GridMap Grid => new GridMap(_gridSize, _cellSize);

        /// <summary>Size of the playing field in world units.</summary>
        public Vector2 WorldSize => new Vector2(_gridSize.x * _cellSize, _gridSize.y * _cellSize);

        /// <summary>Returns the waves for a difficulty, or null when the level has none for it.</summary>
        public WaveDefinition[] GetWaves(DifficultyProfile difficulty)
        {
            for (int i = 0; i < _difficultyWaves.Length; i++)
            {
                if (_difficultyWaves[i].Difficulty == difficulty)
                {
                    return _difficultyWaves[i].Waves;
                }
            }

            return null;
        }

        /// <summary>Basic checks that every level needs. LevelValidator adds the gameplay checks.</summary>
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                error = "Display name is empty.";
                return false;
            }

            if (_gridSize.x <= 0 || _gridSize.y <= 0)
            {
                error = "Grid size must be positive.";
                return false;
            }

            if (_theme == null)
            {
                error = "Visual theme is missing.";
                return false;
            }

            error = null;
            return true;
        }

        [System.NonSerialized] private TerrainMap _terrainMap;

        private void OnValidate()
        {
            _terrainMap = null;
            if (!IsValid(out string error))
            {
                Debug.LogWarning($"LevelDefinition '{name}' is invalid: {error}", this);
            }
        }
    }
}




