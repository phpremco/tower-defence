using UnityEngine;
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
        [SerializeField] private TowerDefinition[] _availableTowers = new TowerDefinition[0];
        [SerializeField] [Min(0)] private int _startingMoney = 150;
        [SerializeField] [Min(1)] private int _baseHealth = 20;
        [SerializeField] private DifficultyWaves[] _difficultyWaves = new DifficultyWaves[0];
        [SerializeField] private LevelReward _reward;

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
        public TowerDefinition[] AvailableTowers => _availableTowers;
        public int StartingMoney => _startingMoney;
        public int BaseHealth => _baseHealth;
        public DifficultyWaves[] DifficultyWaves => _difficultyWaves;
        public LevelReward Reward => _reward;

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

        private void OnValidate()
        {
            if (!IsValid(out string error))
            {
                Debug.LogWarning($"LevelDefinition '{name}' is invalid: {error}", this);
            }
        }
    }
}



