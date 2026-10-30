using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>Stats, upgrade levels and look of one tower type.</summary>
    [CreateAssetMenu(menuName = "Game/Tower Definition", fileName = "TowerDefinition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public const int LevelCount = 3;

        [SerializeField] private string _displayName = "Tower";
        [SerializeField] private TargetLayers _targets = TargetLayers.Ground;
        [SerializeField] private TowerLevel[] _levels = new TowerLevel[LevelCount];
        [SerializeField] [Min(1f)] private float _projectileSpeed = 10f;
        [SerializeField] [Min(0f)] private float _splashRadius;
        [SerializeField] [Min(1f)] private float _maxHealth = 100f;

        /// <summary>Share of everything spent on the tower that selling gives back.</summary>
        [SerializeField] [Range(0f, 1f)] private float _sellRefundFraction = 0.7f;

        /// <summary>Share of everything spent that scrapping a wreck gives back.</summary>
        [SerializeField] [Range(0f, 1f)] private float _scrapFraction = 0.3f;

        [SerializeField] [Min(0f)] private float _repairCostPerHealth = 0.4f;

        /// <summary>Power of a nearly destroyed tower: 1 is no penalty, 0.5 means half damage and half fire rate.</summary>
        [SerializeField] [Range(0f, 1f)] private float _damagedPowerFloor = 0.5f;

        /// <summary>Speed share enemies hit by this tower are slowed to. One means no slow.</summary>
        [SerializeField] [Range(0.1f, 1f)] private float _slowFactor = 1f;

        [SerializeField] [Min(0f)] private float _slowSeconds;

        /// <summary>A support tower does no damage. It boosts the range and fire rate of towers within its range.</summary>
        [SerializeField] private bool _isSupport;

        [SerializeField] [Min(0f)] private float _supportRangeBonus;

        [SerializeField] [Min(0f)] private float _supportRateBonus;

        /// <summary>A blockade stops ground enemies until it is destroyed. It is set in the level and cannot be sold.</summary>
        [SerializeField] private bool _blocksGroundEnemies;

        [SerializeField] private Color _color = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private TowerShape _shape;
        [SerializeField] private ProjectileStyle _projectileStyle;
        [SerializeField] private GameObject _model;
        [SerializeField] [Min(0.1f)] private float _modelScale = 1.4f;

        /// <summary>Show the model with its own materials, without the tower tint and glow. For hand-coloured models.</summary>
        [SerializeField] private bool _keepModelColours;

        public string Id => name;
        public string DisplayName => _displayName;
        public TargetLayers Targets => _targets;
        public float ProjectileSpeed => _projectileSpeed;
        public float SplashRadius => _splashRadius;
        public float MaxHealth => _maxHealth;
        public float SellRefundFraction => _sellRefundFraction;
        public float ScrapFraction => _scrapFraction;
        public float RepairCostPerHealth => _repairCostPerHealth;
        public float DamagedPowerFloor => _damagedPowerFloor;
        public float SlowFactor => _slowFactor;
        public float SlowSeconds => _slowSeconds;
        public bool IsSupport => _isSupport;
        public float SupportRangeBonus => _supportRangeBonus;
        public float SupportRateBonus => _supportRateBonus;
        public bool BlocksGroundEnemies => _blocksGroundEnemies;
        public Color Color => _color;

        /// <summary>The imported model shown for this tower, or null to use placeholder shapes.</summary>
        /// <summary>The procedural model this tower wears, or None to use <see cref="Model"/> or the placeholder.</summary>
        public TowerShape Shape => _shape;

        /// <summary>How this tower's shots look and sound.</summary>
        public ProjectileStyle ProjectileStyle => _projectileStyle;

        public GameObject Model => _model;
        public float ModelScale => _modelScale;
        public bool KeepModelColours => _keepModelColours;
        public int MaxLevelIndex => _levels.Length - 1;

        /// <summary>Price to build the tower, which is the cost of its first level.</summary>
        public int BuildCost => _levels[0].Cost;

        public TowerLevel GetLevel(int index) => _levels[index];

        private void OnValidate()
        {
            if (_levels == null || _levels.Length != LevelCount)
            {
                Debug.LogWarning($"TowerDefinition '{name}' must have exactly {LevelCount} levels.", this);
            }
        }
    }
}


