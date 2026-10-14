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
        [SerializeField] private Color _color = new Color(0.3f, 0.6f, 1f);

        public string Id => name;
        public string DisplayName => _displayName;
        public TargetLayers Targets => _targets;
        public float ProjectileSpeed => _projectileSpeed;
        public float SplashRadius => _splashRadius;
        public Color Color => _color;
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
