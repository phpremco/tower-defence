using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Stats and look of one enemy type.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField] private TargetLayers _layer = TargetLayers.Ground;
        [SerializeField] [Min(1f)] private float _maxHealth = 20f;
        [SerializeField] [Min(0.1f)] private float _speed = 2f;
        [SerializeField] [Min(0)] private int _reward = 5;
        [SerializeField] [Min(1)] private int _baseDamage = 1;
        [SerializeField] [Min(0f)] private float _attackDamage;
        [SerializeField] [Min(0f)] private float _blockadeDamage = 4f;
        [SerializeField] [Min(0.5f)] private float _attackRange = 2.5f;
        [SerializeField] [Min(0.1f)] private float _attackInterval = 1.2f;
        [SerializeField] private Color _color = new Color(0.9f, 0.3f, 0.15f);
        [SerializeField] [Min(0.1f)] private float _scale = 0.6f;
        [SerializeField] private EnemyShape _shape;
        [SerializeField] private GameObject _model;
        [SerializeField] [Min(0.1f)] private float _modelScale = 1f;

        public string DisplayName => _displayName;
        public TargetLayers Layer => _layer;
        public float MaxHealth => _maxHealth;
        public float Speed => _speed;
        public int Reward => _reward;
        public int BaseDamage => _baseDamage;
        /// <summary>Damage to structures per attack. Zero means this enemy does not attack structures.</summary>
        public float AttackDamage => _attackDamage;
        /// <summary>Damage per attack to a blockade that holds this enemy up. Only ground enemies meet blockades.</summary>
        public float BlockadeDamage => _blockadeDamage;
        public float AttackRange => _attackRange;
        public float AttackInterval => _attackInterval;
        public Color Color => _color;
        public float Scale => _scale;

        /// <summary>The procedural creature this enemy wears, or None to use <see cref="Model"/> or the placeholder.</summary>
        public EnemyShape Shape => _shape;

        /// <summary>The imported model shown for this enemy, or null to use placeholder shapes.</summary>
        public GameObject Model => _model;
        public float ModelScale => _modelScale;

        private void OnValidate()
        {
            if (_layer != TargetLayers.Ground && _layer != TargetLayers.Air)
            {
                Debug.LogWarning($"EnemyDefinition '{name}' must have exactly one layer (Ground or Air).", this);
            }
        }
    }
}


