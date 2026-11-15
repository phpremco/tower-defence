using Game.Core;
using UnityEngine;

namespace Game.PathObjects
{
    /// <summary>Data of one purchasable object that is placed on or beside the road: mine, slow field, lava ball and so on.</summary>
    [CreateAssetMenu(menuName = "Game/Path Object Definition", fileName = "PathObjectDefinition")]
    public sealed class PathObjectDefinition : ScriptableObject, Game.Combat.IBurningSource
    {
        [SerializeField] private string _displayName = "Path object";
        [SerializeField] private PathObjectKind _kind;
        [SerializeField] private PathObjectPlacement _placement = PathObjectPlacement.Road;
        [SerializeField] private TargetLayers _targets = TargetLayers.Ground;
        [SerializeField] [Min(0)] private int _cost = 40;

        /// <summary>Area of effect: the blast of a mine, the field of a slow field, the contact size of a roller.</summary>
        [SerializeField] [Min(0.2f)] private float _radius = 1.2f;

        /// <summary>How close an enemy must come to set off a mine.</summary>
        [SerializeField] [Min(0.1f)] private float _triggerRadius = 0.6f;

        /// <summary>Blast damage of a mine, or damage per second of a roller.</summary>
        [SerializeField] [Min(0f)] private float _damage = 100f;

        [Tooltip("What the damage is made of: a mine or a barrel explodes, a lava ball burns, a ground drone rams. Armour reduces all but the explosion (round 64).")]
        [SerializeField] private DamageType _damageType = DamageType.Explosive;

        [Tooltip("Share of the damage that works on a creature's armour while it has any, from the sheet's table per damage type.")]
        [SerializeField] [Range(0.05f, 2f)] private float _armourFactor = 1f;

        [SerializeField] [Range(0.1f, 1f)] private float _slowFactor = 0.5f;

        [Tooltip("It burns what it hits (a lava ball): ice armour cracks under it.")]
        [SerializeField] private bool _burns;

        /// <summary>Movement speed of a roller, in cells per second.</summary>
        [SerializeField] [Min(0f)] private float _speed = 2f;

        /// <summary>Seconds the object lasts. Zero means until it is used up.</summary>
        [SerializeField] [Min(0f)] private float _lifetime;

        [SerializeField] private Color _color = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private PathObjectLook _look;
        [Tooltip("The imported (Blender) model with its clips; empty keeps the procedural look.")]
        [SerializeField] private ModelArtDefinition _art;

        /// <summary>What it does, as the player reads it: the badge on its card and the words in the loadout.</summary>
        [SerializeField] private PathObjectEffect _effect;
        [Tooltip("What its blast looks like; empty uses the mine entry of the VfxLibrary.")]
        [SerializeField] private Game.Combat.ImpactVfxDefinition _impactVfx;

        /// <summary>One or two lines for the loadout screen on what this weapon does and where it goes.</summary>
        [SerializeField] [TextArea] private string _description = string.Empty;

        public string DisplayName => _displayName;
        public PathObjectKind Kind => _kind;
        public PathObjectPlacement Placement => _placement;
        public TargetLayers Targets => _targets;
        public int Cost => _cost;
        public float Radius => _radius;
        public float TriggerRadius => _triggerRadius;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;

        /// <summary>Share of the damage that works on armour while the creature has any (Health.TakeDamage).</summary>
        public float ArmourFactor => _armourFactor;
        public float SlowFactor => _slowFactor;
        public bool Burns => _burns;
        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public Color Color => _color;
        public PathObjectLook Look => _look;
        public ModelArtDefinition Art => _art;
        public PathObjectEffect Effect => _effect;
        public Game.Combat.ImpactVfxDefinition ImpactVfx => _impactVfx;
        public string Description => _description;

        private void OnValidate()
        {
            if (_damage > 0f && !DamageTypeRule.Agrees(_damageType, _armourFactor))
            {
                Debug.LogWarning($"Path object '{name}': {DamageTypeRule.Describe(_damageType)} (it is {_armourFactor}).", this);
            }
        }
    }
}
