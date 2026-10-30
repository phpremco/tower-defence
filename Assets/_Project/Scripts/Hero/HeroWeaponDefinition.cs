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

        /// <summary>Colour of the blade, the bolt and the muzzle glow.</summary>
        [SerializeField] private Color _color = new Color(0.3f, 0.95f, 1f);

        public string DisplayName => _displayName;
        public HeroWeaponKind Kind => _kind;
        public HeroWeaponStyle Style => _style;
        public float Damage => _damage;
        public float Reach => _reach;
        public float AttackInterval => _attackInterval;
        public TargetLayers Targets => _targets;
        public Color Color => _color;
    }
}
