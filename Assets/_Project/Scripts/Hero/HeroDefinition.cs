using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>Stats and abilities of the commander. Everything about how it plays is data; the weapon it carries is its own asset.</summary>
    [CreateAssetMenu(menuName = "Game/Hero Definition", fileName = "HeroDefinition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Commander";
        [SerializeField] [Min(1f)] private float _maxHealth = 200f;
        [SerializeField] [Min(0.5f)] private float _walkSpeed = 5f;

        [Tooltip("A hero that flies does not stand in the road: ground creatures walk on under it.")]
        [SerializeField] private bool _flies;

        /// <summary>The weapon the hero fights with up close: damage, reach, pace, look and animation.</summary>
        [SerializeField] private HeroWeaponDefinition _weapon;

        /// <summary>
        /// The gun the hero fires at anything beyond the reach of the melee weapon, paced like a simple turret.
        /// Optional: without one the hero only fights up close.
        /// </summary>
        [SerializeField] private HeroWeaponDefinition _rangedWeapon;

        /// <summary>Share of the hits on the guarded blockade that the hero takes instead.</summary>
        [SerializeField] [Range(0f, 1f)] private float _guardShare = 0.4f;

        /// <summary>Seconds a downed hero needs before it stands up again at its post.</summary>
        [SerializeField] [Min(1f)] private float _reviveSeconds = 15f;

        [SerializeField] [Min(1f)] private float _shockwaveDamage = 40f;
        [SerializeField] [Min(0.5f)] private float _shockwaveRadius = 3.2f;
        [SerializeField] [Min(1f)] private float _shockwaveCooldown = 20f;

        [SerializeField] [Range(0.05f, 1f)] private float _repairFraction = 0.4f;
        [SerializeField] [Min(1f)] private float _repairCooldown = 25f;

        /// <summary>Money the emergency repair costs, shown on its button.</summary>
        [SerializeField] [Min(0)] private int _repairCost = 30;

        [SerializeField] private Color _color = new Color(1f, 0.85f, 0.3f);

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float WalkSpeed => _walkSpeed;
        public bool Flies => _flies;
        public HeroWeaponDefinition Weapon => _weapon;
        public HeroWeaponDefinition RangedWeapon => _rangedWeapon;
        public float Damage => _weapon.Damage;
        public float Reach => _weapon.Reach;
        public float StrikeInterval => _weapon.AttackInterval;
        public float GuardShare => _guardShare;
        public float ReviveSeconds => _reviveSeconds;
        public float ShockwaveDamage => _shockwaveDamage;
        public float ShockwaveRadius => _shockwaveRadius;
        public float ShockwaveCooldown => _shockwaveCooldown;
        public float RepairFraction => _repairFraction;
        public float RepairCooldown => _repairCooldown;
        public int RepairCost => _repairCost;
        public Color Color => _color;

        /// <summary>What the hero's weapon can hit.</summary>
        public TargetLayers Targets => _weapon.Targets;

        public TargetLayers ShockwaveTargets => TargetLayers.Ground | TargetLayers.Air;

        private void OnValidate()
        {
            if (_weapon == null)
            {
                Debug.LogWarning($"HeroDefinition '{name}' has no weapon.", this);
            }
        }
    }
}
