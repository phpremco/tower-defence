using UnityEngine;

namespace Game.Drones
{
    /// <summary>What a drone does on its own once it is in the level.</summary>
    public enum DroneKind
    {
        /// <summary>Flies to the most damaged tower or blockade and repairs it for free.</summary>
        Repair,

        /// <summary>Flies to loot dropped by defeated enemies and brings it in as money.</summary>
        Collector,
    }

    /// <summary>Stats and look of one drone type. How many drones a level starts with comes from permanent upgrades.</summary>
    [CreateAssetMenu(menuName = "Game/Drone Definition", fileName = "DroneDefinition")]
    public sealed class DroneDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Drone";
        [SerializeField] private DroneKind _kind;
        [SerializeField] [Min(0.5f)] private float _speed = 3.5f;
        [SerializeField] [Min(1f)] private float _maxHealth = 40f;

        /// <summary>Hit points a repair drone restores per second while it hovers at its target.</summary>
        [SerializeField] [Min(0f)] private float _repairPerSecond = 8f;

        [SerializeField] private Color _color = new Color(0.4f, 0.9f, 1f);

        public string DisplayName => _displayName;
        public DroneKind Kind => _kind;
        public float Speed => _speed;
        public float MaxHealth => _maxHealth;
        public float RepairPerSecond => _repairPerSecond;
        public Color Color => _color;
    }
}
