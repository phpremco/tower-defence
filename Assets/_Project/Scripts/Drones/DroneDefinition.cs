using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>What a drone does on its own once it is in the level. One drone type per job.</summary>
    public enum DroneKind
    {
        /// <summary>Flies to the most damaged tower, gate or base and repairs it for free.</summary>
        Repair,

        /// <summary>Mines a resource node and carries the yield to the base.</summary>
        Harvest,

        /// <summary>Hunts the enemy that is furthest along the road and shoots it from the air.</summary>
        Attack,
    }

    /// <summary>
    /// Stats and look of one drone type. Drones are bought in the level with money; a permanent upgrade adds
    /// speed. Only the fields of the drone's own kind matter.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Drone Definition", fileName = "DroneDefinition")]
    public sealed class DroneDefinition : ScriptableObject
    {
        /// <summary>The longest role that fits on a card at the readable text size.</summary>
        public const int MaxRoleLength = 8;

        [SerializeField] private string _displayName = "Drone";

        [Tooltip("Round 60: the drone's job in one short word, shown on its card in the level (\"Repair\", \"Harvest\").")]
        [SerializeField] private string _role = string.Empty;

        [Tooltip("Round 60: what the drone does, in one short sentence, shown while its card is focused or pointed at.")]
        [SerializeField] [TextArea(2, 3)] private string _summary = string.Empty;

        [SerializeField] private DroneKind _kind;
        [SerializeField] [Min(0.5f)] private float _speed = 3.5f;
        [SerializeField] [Min(1f)] private float _maxHealth = 40f;

        [Tooltip("Seconds a knocked-out drone charges at home before it flies out again.")]
        [SerializeField] [Min(1f)] private float _rechargeSeconds = 12f;

        /// <summary>Money a drone of this type costs to launch, and how many may fly at once.</summary>
        [SerializeField] [Min(0)] private int _cost = 120;
        [SerializeField] [Min(1)] private int _maxActive = 3;

        [Header("Repair")]
        [SerializeField] [Min(0f)] private float _repairPerSecond = 8f;

        [Header("Harvest")]
        /// <summary>Seconds spent at a resource node, and the money one trip brings home.</summary>
        [SerializeField] [Min(0.5f)] private float _harvestSeconds = 4f;
        [SerializeField] [Min(0)] private int _harvestValue = 12;

        [Header("Attack")]
        [SerializeField] [Min(0f)] private float _damage = 6f;
        [SerializeField] [Min(0.05f)] private float _fireInterval = 0.5f;
        [SerializeField] [Min(0.5f)] private float _range = 3f;
        [SerializeField] [Min(1f)] private float _projectileSpeed = 14f;
        [SerializeField] private TargetLayers _targets = TargetLayers.Ground | TargetLayers.Air;
        [SerializeField] private ProjectileStyle _projectileStyle = ProjectileStyle.Bolt;

        [Header("Look")]
        [SerializeField] private Color _color = new Color(0.4f, 0.9f, 1f);

        /// <summary>The skin on the hull and the colour of its markings (cross, stripes, chevrons).</summary>
        [SerializeField] private CreatureSkin.Kind _skin = CreatureSkin.Kind.Plate;
        [SerializeField] private Color _accentColor = Color.white;

        [Tooltip("The imported (Blender) model with its Idle, Fly and Work clips; empty keeps the procedural drone.")]
        [SerializeField] private ModelArtDefinition _art;

        public string DisplayName => _displayName;
        public string Role => _role;
        public string Summary => _summary;
        public DroneKind Kind => _kind;
        public float Speed => _speed;
        public float MaxHealth => _maxHealth;
        public float RechargeSeconds => _rechargeSeconds;
        public int Cost => _cost;
        public int MaxActive => _maxActive;
        public float RepairPerSecond => _repairPerSecond;
        public float HarvestSeconds => _harvestSeconds;
        public int HarvestValue => _harvestValue;
        public float Damage => _damage;
        public float FireInterval => _fireInterval;
        public float Range => _range;
        public float ProjectileSpeed => _projectileSpeed;
        public TargetLayers Targets => _targets;
        public ProjectileStyle ProjectileStyle => _projectileStyle;
        public Color Color => _color;
        public CreatureSkin.Kind Skin => _skin;
        public Color AccentColor => _accentColor;
        public ModelArtDefinition Art => _art;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_role) || _role.Length > MaxRoleLength)
            {
                Debug.LogWarning($"Drone '{name}' needs a role of 1 to {MaxRoleLength} letters for its card, not '{_role}'.", this);
            }

            if (string.IsNullOrWhiteSpace(_summary))
            {
                Debug.LogWarning($"Drone '{name}' has no summary: its card cannot say what it does.", this);
            }

            if (_kind == DroneKind.Attack && _targets == 0)
            {
                Debug.LogWarning($"Drone '{name}' attacks nothing: no target layers set.", this);
            }
        }
    }
}
