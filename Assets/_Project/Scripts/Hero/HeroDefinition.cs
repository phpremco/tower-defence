using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>Stats and abilities of the commander. Everything about how it plays is data.</summary>
    [CreateAssetMenu(menuName = "Game/Hero Definition", fileName = "HeroDefinition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Commander";
        [SerializeField] [Min(1f)] private float _maxHealth = 200f;
        [SerializeField] [Min(0.5f)] private float _walkSpeed = 5f;
        [SerializeField] [Min(1f)] private float _damage = 12f;
        [SerializeField] [Min(0.5f)] private float _range = 3f;
        [SerializeField] [Min(0.05f)] private float _fireInterval = 0.6f;
        [SerializeField] [Min(1f)] private float _projectileSpeed = 14f;

        /// <summary>Share of the hits on the guarded blockade that the hero takes instead.</summary>
        [SerializeField] [Range(0f, 1f)] private float _guardShare = 0.4f;

        /// <summary>Seconds a downed hero needs before it stands up again at its post.</summary>
        [SerializeField] [Min(1f)] private float _reviveSeconds = 15f;

        [SerializeField] [Min(1f)] private float _shockwaveDamage = 40f;
        [SerializeField] [Min(0.5f)] private float _shockwaveRadius = 3.2f;
        [SerializeField] [Min(1f)] private float _shockwaveCooldown = 20f;

        [SerializeField] [Range(0.05f, 1f)] private float _repairFraction = 0.4f;
        [SerializeField] [Min(1f)] private float _repairCooldown = 25f;

        [SerializeField] private Color _color = new Color(0.4f, 0.85f, 1f);

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float WalkSpeed => _walkSpeed;
        public float Damage => _damage;
        public float Range => _range;
        public float FireInterval => _fireInterval;
        public float ProjectileSpeed => _projectileSpeed;
        public float GuardShare => _guardShare;
        public float ReviveSeconds => _reviveSeconds;
        public float ShockwaveDamage => _shockwaveDamage;
        public float ShockwaveRadius => _shockwaveRadius;
        public float ShockwaveCooldown => _shockwaveCooldown;
        public float RepairFraction => _repairFraction;
        public float RepairCooldown => _repairCooldown;
        public Color Color => _color;
        public TargetLayers Targets => TargetLayers.Ground | TargetLayers.Air;
    }
}
