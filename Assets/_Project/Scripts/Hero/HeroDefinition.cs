using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Stats, weapons, abilities and earnable perks of the hero. Everything about how it plays is data: each weapon and
    /// each ability is its own asset, and the abilities are listed by button slot.
    /// </summary>
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

        /// <summary>The ability buttons, by slot: slot 0 is the first button (F, left trigger), slot 1 the second (G, right trigger).</summary>
        [SerializeField] private HeroAbilityDefinition[] _abilities = new HeroAbilityDefinition[0];

        /// <summary>The perks this hero can earn in the campaign; the unlocked ones apply when a level starts.</summary>
        [SerializeField] private HeroPerkDefinition[] _perks = new HeroPerkDefinition[0];

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
        public HeroAbilityDefinition[] Abilities => _abilities;
        public HeroPerkDefinition[] Perks => _perks;
        public Color Color => _color;

        /// <summary>What the hero's weapon can hit.</summary>
        public TargetLayers Targets => _weapon.Targets;

        private void OnValidate()
        {
            if (_weapon == null)
            {
                Debug.LogWarning($"HeroDefinition '{name}' has no weapon.", this);
            }

            for (int i = 0; i < _abilities.Length; i++)
            {
                if (_abilities[i] == null)
                {
                    Debug.LogWarning($"HeroDefinition '{name}' has an empty ability slot {i}.", this);
                }
            }
        }
    }
}
