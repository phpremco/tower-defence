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
        [SerializeField] private Color _color = new Color(0.9f, 0.3f, 0.15f);
        [SerializeField] [Min(0.1f)] private float _scale = 0.6f;

        public string DisplayName => _displayName;
        public TargetLayers Layer => _layer;
        public float MaxHealth => _maxHealth;
        public float Speed => _speed;
        public int Reward => _reward;
        public int BaseDamage => _baseDamage;
        public Color Color => _color;
        public float Scale => _scale;

        private void OnValidate()
        {
            if (_layer != TargetLayers.Ground && _layer != TargetLayers.Air)
            {
                Debug.LogWarning($"EnemyDefinition '{name}' must have exactly one layer (Ground or Air).", this);
            }
        }
    }
}
