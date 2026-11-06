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
        /// <summary>The part of the planet this level takes, as the map names it.</summary>
        [SerializeField] private string _regionName = "";
        /// <summary>The piece of story read before the first wave. Empty means no briefing.</summary>
        [SerializeField] [TextArea(3, 6)] private string _briefing = "";
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(16, 10);
        [SerializeField] [Min(0.1f)] private float _cellSize = 1f;
        [SerializeField] private VisualTheme _theme;
        [SerializeField] private CameraSettings _camera = CameraSettings.Default;
        [SerializeField] private EnemyPath[] _paths = new EnemyPath[0];
        /// <summary>How many cells wide every road is. The waypoints are the first row; extra rows lie to the right of travel.</summary>
        [SerializeField] [Min(1)] private int _roadWidth = 1;
        /// <summary>Open road areas where roads may meet: road cells that belong to no single path.</summary>
        [SerializeField] private Vector2Int[] _plazaCells = new Vector2Int[0];
        [SerializeField] private Vector2Int[] _blockedCells = new Vector2Int[0];
        [SerializeField] private BlockadeSpot[] _blockades = new BlockadeSpot[0];
        [SerializeField] private TerrainPatch[] _terrain = new TerrainPatch[0];
        [SerializeField] private Vector2Int[] _mineCells = new Vector2Int[0];
        /// <summary>Money each mining node holds before it is spent; harvest drones empty it trip by trip.</summary>
        [SerializeField] [Min(0)] private int _mineStock = 140;
        /// <summary>Where bosses come up: the gate, a pool or the ground. Pool and ground entrances name a cell.</summary>
        [SerializeField] private BossEntrance _bossEntrance;
        /// <summary>Where element creatures come up. Each nest names a cell off the road and the element it belongs to.</summary>
        [SerializeField] private ElementNest[] _nests = new ElementNest[0];
        /// <summary>How the nests open and close per wave and what they and the route gates look like; empty keeps the static ones.</summary>
        [SerializeField] private SpawnOriginDefinition _spawnOrigins;
        [SerializeField] private Texture2D _terrainAlbedo;
        [SerializeField] private Texture2D _terrainEmission;
        /// <summary>Makes every enemy of this level tougher, so later levels are harder before difficulty is applied.</summary>
        [SerializeField] [Min(0.1f)] private float _enemyHealthMultiplier = 1f;

        /// <summary>
        /// Balance model only, per difficulty (Easy to Psycho; a shorter array repeats its last entry): how much more the towers do
        /// on this map than the ledger reckons, measured with the PlayMode probes. The wave solver counts the towers that much
        /// stronger, so the waves play on the difficulty curve. 1 = as the ledger reckons (Docs/BALANCE.md, "Roles by enemy").
        /// </summary>
        [SerializeField] private float[] _balanceFieldEase = { 1f };

        /// <summary>
        /// The permanent tower upgrade (damage times fire rate, minus one) the balance model expects a Medium player to
        /// bring by now, written by the generator for reference. It does not scale enemies: the wave solver already
        /// sized the waves against it.
        /// </summary>
        [SerializeField] [Range(0f, 1f)] private float _expectedUpgradeBonus;

        /// <summary>The base's own gun, built out by the permanent base weapon upgrade. May be null.</summary>
        [SerializeField] private TowerDefinition _baseTurret;
        [SerializeField] private HeroDefinition _hero;
        [SerializeField] private DroneDefinition _repairDrone;
        [SerializeField] private DroneDefinition _harvestDrone;
        [SerializeField] private DroneDefinition _attackDrone;
        [SerializeField] private CombineRecipe[] _recipes = new CombineRecipe[0];
        [SerializeField] private PathObjectDefinition[] _pathObjects = new PathObjectDefinition[0];
        [SerializeField] private TowerDefinition[] _availableTowers = new TowerDefinition[0];
        [SerializeField] [Min(0)] private int _startingMoney = 150;
        [SerializeField] [Min(1)] private int _baseHealth = 20;
        [SerializeField] private LevelWaves _waves;
        [SerializeField] private LevelReward _reward;

        /// <summary>Share of base health that must be left for two and for three stars, in that order.</summary>
        [SerializeField] private float[] _starBaseHealthFractions = { 0.5f, 0.9f };

        /// <summary>Dialogues tied to moments in this level, played in array order when several match one moment.</summary>
        [SerializeField] private StoryTrigger[] _story = new StoryTrigger[0];

        public string Id => name;
        public string DisplayName => _displayName;
        public string RegionName => _regionName;
        public string Briefing => _briefing;
        public Vector2Int GridSize => _gridSize;
        public float CellSize => _cellSize;
        public VisualTheme Theme => _theme;
        public CameraSettings Camera => _camera;
        public EnemyPath[] Paths => _paths;
        public int RoadWidth => _roadWidth;
        public Vector2Int[] PlazaCells => _plazaCells;

        /// <summary>Road cells, rows, directions and centre lines, cached because every system that touches the road asks for it.</summary>
        public RoadCorridor Road => _road ??= new RoadCorridor(this);

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

        /// <summary>Resource nodes that collector drones mine for money. They cannot be built on.</summary>
        public Vector2Int[] MineCells => _mineCells;
        public int MineStock => _mineStock;

        /// <summary>How and where the bosses of this level arrive.</summary>
        public BossEntrance BossEntrance => _bossEntrance;

        /// <summary>The element nests of this level; a spawn group with source Nest picks one that matches its creature.</summary>
        public ElementNest[] Nests => _nests;

        /// <summary>The living spawn origins and gates, or null for the static nests and procedural gates.</summary>
        public SpawnOriginDefinition SpawnOrigins => _spawnOrigins;

        /// <summary>Raised ground and liquids. Liquids cannot hold towers; every other cell is plain ground.</summary>
        public TerrainPatch[] TerrainPatches => _terrain;

        /// <summary>The ground texture painted ahead of time by the generator, or null to paint it when the level loads.</summary>
        public Texture2D TerrainAlbedo => _terrainAlbedo;

        public Texture2D TerrainEmission => _terrainEmission;

        /// <summary>Lookup built from <see cref="TerrainPatches"/>, cached because several systems ask for it.</summary>
        public TerrainMap Terrain => _terrainMap ??= new TerrainMap(_terrain);

        /// <summary>The commander for this level, or null for a level without one.</summary>
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public float ExpectedUpgradeBonus => _expectedUpgradeBonus;

        /// <summary>
        /// Everything the level itself adds to enemy health. The permanent upgrades the player brings are no longer a flat
        /// factor here: the wave solver sizes every wave against the upgrades the progression model expects (see
        /// Docs/BALANCE.md), and <see cref="ExpectedUpgradeBonus"/> only reports that expectation.
        /// </summary>
        public float EnemyHealthScale => _enemyHealthMultiplier;

        /// <summary>The balance model's field ease per difficulty (see the field).</summary>
        public float[] BalanceFieldEase => _balanceFieldEase != null && _balanceFieldEase.Length > 0 ? _balanceFieldEase : new[] { 1f };
        public TowerDefinition BaseTurret => _baseTurret;
        public HeroDefinition Hero => _hero;

        /// <summary>The kinds of drone this level uses. How many fly comes from permanent upgrades.</summary>
        public DroneDefinition RepairDrone => _repairDrone;
        public DroneDefinition HarvestDrone => _harvestDrone;
        public DroneDefinition AttackDrone => _attackDrone;

        /// <summary>The drone kinds this level offers, in the order the base menu shows them; missing kinds are left out.</summary>
        public DroneDefinition[] Drones
        {
            get
            {
                var list = new System.Collections.Generic.List<DroneDefinition>(3);
                foreach (DroneDefinition drone in new[] { _repairDrone, _harvestDrone, _attackDrone })
                {
                    if (drone != null)
                    {
                        list.Add(drone);
                    }
                }

                return list.ToArray();
            }
        }

        /// <summary>Recipes the player can use in this level to combine neighbouring towers.</summary>
        public CombineRecipe[] Recipes => _recipes;

        /// <summary>Path objects that are for sale in this level.</summary>
        public PathObjectDefinition[] PathObjects => _pathObjects;
        public TowerDefinition[] AvailableTowers => _availableTowers;
        public int StartingMoney => _startingMoney;
        public int BaseHealth => _baseHealth;
        /// <summary>The asset that holds every wave of this level, or null for a level without waves.</summary>
        public LevelWaves Waves => _waves;
        public DifficultyWaves[] DifficultyWaves => _waves != null ? _waves.Sets : NoWaves;
        public LevelReward Reward => _reward;
        public float[] StarBaseHealthFractions => _starBaseHealthFractions;
        public StoryTrigger[] StoryTriggers => _story ?? NoStory;

        public GridMap Grid => new GridMap(_gridSize, _cellSize);

        /// <summary>Size of the playing field in world units.</summary>
        public Vector2 WorldSize => new Vector2(_gridSize.x * _cellSize, _gridSize.y * _cellSize);

        /// <summary>Returns the waves for a difficulty, or null when the level has none for it.</summary>
        public WaveDefinition[] GetWaves(DifficultyProfile difficulty) => _waves != null ? _waves.For(difficulty) : null;

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
        [System.NonSerialized] private RoadCorridor _road;
        private static readonly DifficultyWaves[] NoWaves = new DifficultyWaves[0];
        private static readonly StoryTrigger[] NoStory = new StoryTrigger[0];

        private void OnValidate()
        {
            _terrainMap = null;
            for (int i = 0; _balanceFieldEase != null && i < _balanceFieldEase.Length; i++)
            {
                if (_balanceFieldEase[i] < 0.1f)
                {
                    Debug.LogWarning($"LevelDefinition '{name}': field ease {_balanceFieldEase[i]} on difficulty {i} must be at least 0.1.", this);
                }
            }

            _road = null;
            if (!IsValid(out string error))
            {
                Debug.LogWarning($"LevelDefinition '{name}' is invalid: {error}", this);
            }

            for (int i = 0; _story != null && i < _story.Length; i++)
            {
                string problem = _story[i].Problem();
                if (problem != null)
                {
                    Debug.LogWarning($"LevelDefinition '{name}': story trigger {i + 1} ({_story[i].Kind}) {problem}", this);
                }
            }
        }
    }
}




