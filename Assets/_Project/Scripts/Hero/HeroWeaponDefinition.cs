using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// A weapon the commander can carry: what it does (melee or ranged, damage, reach, pace) and how it looks.
    /// Swapping the weapon on the hero definition changes both the fighting and the animation, without code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Weapon", fileName = "HeroWeapon")]
    public sealed class HeroWeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Energy sword";
        [SerializeField] private HeroWeaponKind _kind = HeroWeaponKind.Melee;
        [SerializeField] private HeroWeaponStyle _style = HeroWeaponStyle.EnergySword;

        /// <summary>Damage of one hit.</summary>
        [SerializeField] [Min(1f)] private float _damage = 18f;

        /// <summary>How far from the hero an enemy can be and still be hit.</summary>
        [SerializeField] [Min(0.5f)] private float _reach = 1.5f;

        [SerializeField] [Min(0.05f)] private float _attackInterval = 0.7f;

        [SerializeField] private TargetLayers _targets = TargetLayers.Ground;

        [Tooltip("What the damage is made of: a blade or a bullet, both reduced by armour (round 64); the rules say by how much.")]
        [SerializeField] private DamageType _damageType = DamageType.Blade;

        [Tooltip("The one rule table every damage source reads (Data/Balance/CombatRules): what a hit is worth against armour and in the air follows from the damage type there, nothing is typed here (round 67).")]
        [SerializeField] private CombatRules _rules;

        [System.NonSerialized] private ProfileCache _profileCache;

        /// <summary>Shots before the weapon must reload; zero means it never reloads.</summary>
        [SerializeField] [Min(0)] private int _magazine;

        [SerializeField] [Min(0f)] private float _reloadSeconds;

        [Tooltip("Seconds from taking the weapon in hand to its first blow or shot: the draw, ending in the aim or the guard (the hero art's draw clip). The first strike of an engagement waits this long; strikes while it stays drawn do not, so the rate of fire is unchanged.")]
        [SerializeField] [Min(0f)] private float _drawSeconds;

        [Tooltip("Seconds the weapon stays in hand after its last blow or shot (or its reload) before it is put away; a strike within this time needs no new draw.")]
        [SerializeField] [Min(0f)] private float _stayDrawnSeconds = 4f;

        /// <summary>Colour of the blade, the bolt and the muzzle glow.</summary>
        [SerializeField] private Color _color = new Color(0.3f, 0.95f, 1f);

        public string DisplayName => Loc.Get(_displayName);
        public HeroWeaponKind Kind => _kind;
        public HeroWeaponStyle Style => _style;
        public float Damage => _damage;
        public float Reach => _reach;
        public float AttackInterval => _attackInterval;
        public TargetLayers Targets => _targets;
        public DamageType DamageType => _damageType;
        /// <summary>What a hit is worth against armour and in the air, from the rule table (and its named exceptions).</summary>
        public DamageProfile Profile => _profileCache.Get(_rules, _damageType, this);
        public int Magazine => _magazine;
        public float ReloadSeconds => _reloadSeconds;
        public float DrawSeconds => _drawSeconds;
        public float StayDrawnSeconds => _stayDrawnSeconds;
        public Color Color => _color;

        private void OnValidate() => _profileCache.Clear();
    }
}
