using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Stats, skills and look of one enemy species.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Body")]
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField] private TargetLayers _layer = TargetLayers.Ground;
        [SerializeField] private bool _flies;
        [SerializeField] [Min(1f)] private float _maxHealth = 20f;
        [SerializeField] [Min(0f)] private float _armor;
        [SerializeField] [Range(0f, 1f)] private float _armorReduction = 0.6f;
        [SerializeField] [Min(0.1f)] private float _speed = 2f;
        [SerializeField] [Min(0)] private int _reward = 5;
        [SerializeField] [Min(1)] private int _baseDamage = 1;
        [SerializeField] private Element _element;

        [Header("Attack")]
        [SerializeField] private AttackStyle _attackStyle;
        [SerializeField] [Min(0f)] private float _attackDamage;
        [SerializeField] [Min(0f)] private float _blockadeDamage = 4f;
        [SerializeField] [Min(0.5f)] private float _attackRange = 2.5f;
        [SerializeField] [Min(0.1f)] private float _attackInterval = 1.2f;
        [SerializeField] [Min(0)] private int _gateStrikes;

        [Header("Skills")]
        [SerializeField] [Min(0f)] private float _walkSeconds;
        [SerializeField] [Min(0f)] private float _rollSeconds;
        [SerializeField] [Min(1f)] private float _rollSpeedMultiplier = 2.5f;
        [SerializeField] [Min(0f)] private float _trailSeconds;
        [SerializeField] [Min(1f)] private float _trailHaste = 1.4f;
        [SerializeField] [Min(0f)] private float _trailDamagePerSecond;
        [SerializeField] [Min(0f)] private float _rageSeconds;
        [SerializeField] [Min(1f)] private float _rageSpeedMultiplier = 1.5f;
        [SerializeField] [Min(0f)] private float _rageRadius = 3f;
        [SerializeField] private bool _isBoss;
        [SerializeField] private BossAbilityDefinition[] _abilities = new BossAbilityDefinition[0];

        [Header("Look")]
        [SerializeField] private Color _color = new Color(0.9f, 0.3f, 0.15f);
        [SerializeField] private Color _eyeColor = new Color(1f, 0.9f, 0.35f);
        [SerializeField] private Color _accentColor = new Color(0.4f, 1f, 0.9f);
        [SerializeField] private Color _bloodColor = new Color(0.6f, 1f, 0.3f);
        [SerializeField] [Min(0.1f)] private float _scale = 0.6f;
        [SerializeField] private EnemyShape _shape;
        [SerializeField] private DeathStyle _deathStyle;
        [Tooltip("The imported (Blender) model this species wears, with its clips. Empty: the procedural body of its shape.")]
        [SerializeField] private ModelArtDefinition _art;

        [Header("Sound")]
        [Tooltip("Every sound this species makes. Empty: the generic generated sounds.")]
        [SerializeField] private CreatureVoice _voice;
        [SerializeField] [TextArea] private string _description = string.Empty;

        public string DisplayName => _displayName;

        /// <summary>Which towers may shoot this creature: ground guns, air guns or both.</summary>
        public TargetLayers Layer => _layer;

        /// <summary>True for creatures that travel through the air: they take flight routes and pass over gates.</summary>
        public bool Flies => _flies;

        public float MaxHealth => _maxHealth;

        /// <summary>Hit points of the armour plates in front of the body; zero for soft creatures.</summary>
        public float Armor => _armor;

        /// <summary>Share of every hit the plates absorb while any are left.</summary>
        public float ArmorReduction => _armorReduction;

        public bool HasArmor => _armor > 0f;

        public float Speed => _speed;
        public int Reward => _reward;
        public int BaseDamage => _baseDamage;

        /// <summary>The element this creature is born from, or None. Element creatures come up out of nests.</summary>
        public Element Element => _element;

        /// <summary>How the attack looks; audio and views read it, logic does not.</summary>
        public AttackStyle AttackStyle => _attackStyle;

        /// <summary>Damage to structures per attack. Zero means this enemy does not attack structures in passing.</summary>
        public float AttackDamage => _attackDamage;

        /// <summary>Damage per attack to a blockade that holds this enemy up. Only ground enemies meet blockades.</summary>
        public float BlockadeDamage => _blockadeDamage;
        public float AttackRange => _attackRange;
        public float AttackInterval => _attackInterval;

        /// <summary>How many times a flyer stops to hit a gate it passes. Zero means it flies straight on.</summary>
        public int GateStrikes => _gateStrikes;

        /// <summary>Seconds of walking between two rolls. Only rollers (a roll time above zero) use it.</summary>
        public float WalkSeconds => _walkSeconds;

        /// <summary>Seconds the creature stays curled up and rolling; zero for creatures that never roll.</summary>
        public float RollSeconds => _rollSeconds;

        /// <summary>How much faster the creature moves while it rolls.</summary>
        public float RollSpeedMultiplier => _rollSpeedMultiplier;

        public bool Rolls => _rollSeconds > 0f;

        /// <summary>Speed over a whole walk-and-roll cycle: the walking speed for a creature that never rolls.</summary>
        public float AverageSpeed => Rolls ? _speed * (_walkSeconds + _rollSeconds * _rollSpeedMultiplier) / (_walkSeconds + _rollSeconds) : _speed;

        /// <summary>How long each patch of the trail this creature leaves stays on the road; zero for no trail.</summary>
        public float TrailSeconds => _trailSeconds;

        /// <summary>Speed factor for creatures of other species that cross the trail.</summary>
        public float TrailHaste => _trailHaste;

        /// <summary>Damage per second the trail does to the hero and to gates that stand in it.</summary>
        public float TrailDamagePerSecond => _trailDamagePerSecond;

        public bool LeavesTrail => _trailSeconds > 0f;

        /// <summary>How long pack mates run faster after one of their kind dies nearby; zero for loners.</summary>
        public float RageSeconds => _rageSeconds;

        public float RageSpeedMultiplier => _rageSpeedMultiplier;

        /// <summary>How close a pack mate must be to a death to feel the rage.</summary>
        public float RageRadius => _rageRadius;

        public bool Rages => _rageSeconds > 0f;

        /// <summary>Bosses announce themselves and fall with more weight. Audio and effects read this, logic does not.</summary>
        public bool IsBoss => _isBoss;

        /// <summary>The special powers of a boss; empty for ordinary enemies.</summary>
        public BossAbilityDefinition[] Abilities => _abilities;

        public Color Color => _color;
        public Color EyeColor => _eyeColor;

        /// <summary>The second glow of the creature: plates, blades, bolts and veins. Views read it, logic does not.</summary>
        public Color AccentColor => _accentColor;

        /// <summary>What the creature bleeds: venom, goo, sparks. Views read it, logic does not.</summary>
        public Color BloodColor => _bloodColor;
        public float Scale => _scale;

        /// <summary>The procedural creature this enemy wears, or None for the placeholder.</summary>
        public EnemyShape Shape => _shape;

        public DeathStyle DeathStyle => _deathStyle;

        /// <summary>The imported, animated model of this species, or null for the procedural body.</summary>
        public ModelArtDefinition Art => _art;

        /// <summary>The sounds of this species, or null for the generic ones.</summary>
        public CreatureVoice Voice => _voice;

        /// <summary>What the codex says about this creature: its habit and how to answer it.</summary>
        public string Description => _description;

        private void OnValidate()
        {
            if (_layer == 0)
            {
                Debug.LogWarning($"EnemyDefinition '{name}' can be hit by nothing: set Ground, Air or both.", this);
            }

            if (_flies && (_layer & TargetLayers.Air) == 0)
            {
                Debug.LogWarning($"EnemyDefinition '{name}' flies but air defence cannot hit it.", this);
            }

            if (_rollSeconds > 0f && _walkSeconds <= 0f)
            {
                Debug.LogWarning($"EnemyDefinition '{name}' rolls but never walks: set a walk time.", this);
            }
        }
    }
}
