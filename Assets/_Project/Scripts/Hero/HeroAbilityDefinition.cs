using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One button of the hero: what it does, how long it cools down, what it costs and how it looks and sounds. The hero
    /// definition lists its abilities by slot, so a new ability or a retuned one is data. The id is the asset name.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Ability", fileName = "HeroAbility")]
    public sealed class HeroAbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Ability";
        [SerializeField] private HeroAbilityKind _kind;
        [SerializeField] [Min(1f)] private float _cooldown = 20f;

        /// <summary>Money paid when the ability is used, shown on its button. Zero is free.</summary>
        [SerializeField] [Min(0)] private int _moneyCost;

        /// <summary>Shockwave: damage to every target within the radius.</summary>
        [SerializeField] [Min(0f)] private float _damage;

        /// <summary>Shockwave: how far from the hero the ring reaches.</summary>
        [SerializeField] [Min(0f)] private float _radius;

        /// <summary>GateRepair: share of the guarded structure's maximum health given back.</summary>
        [SerializeField] [Range(0f, 1f)] private float _fraction;

        [SerializeField] private TargetLayers _targets = TargetLayers.Ground | TargetLayers.Air;

        /// <summary>The perk the hero must have unlocked before this ability shows; null means it is always there.</summary>
        [SerializeField] private HeroPerkDefinition _requiresPerk;

        /// <summary>Optional button picture; without one the HUD draws its own icon for the kind.</summary>
        [SerializeField] private Sprite _icon;

        [SerializeField] private Color _tint = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private SoundSet _sound = new SoundSet();
        [SerializeField] private ImpactVfxDefinition _impactVfx;
        [SerializeField] [TextArea] private string _description = string.Empty;

        public string Id => name;
        public string DisplayName => _displayName;
        public HeroAbilityKind Kind => _kind;
        public float Cooldown => _cooldown;
        public int MoneyCost => _moneyCost;
        public float Damage => _damage;
        public float Radius => _radius;
        public float Fraction => _fraction;
        public TargetLayers Targets => _targets;
        public HeroPerkDefinition RequiresPerk => _requiresPerk;
        public Sprite Icon => _icon;
        public Color Tint => _tint;
        public SoundSet Sound => _sound;
        public ImpactVfxDefinition ImpactVfx => _impactVfx;
        public string Description => _description;

        /// <summary>Null when the ability has every value its kind needs, otherwise what is missing.</summary>
        public string Problem()
        {
            switch (_kind)
            {
                case HeroAbilityKind.Shockwave when _damage <= 0f || _radius <= 0f || _targets == TargetLayers.None:
                    return "is a shockwave without damage, radius or targets.";
                case HeroAbilityKind.GateRepair when _fraction <= 0f:
                    return "is a gate repair that restores nothing.";
                default:
                    return string.IsNullOrWhiteSpace(_displayName) ? "has no display name." : null;
            }
        }

        private void OnValidate()
        {
            string problem = Problem();
            if (problem != null)
            {
                Debug.LogWarning($"Hero ability '{name}' {problem}", this);
            }
        }
    }
}
