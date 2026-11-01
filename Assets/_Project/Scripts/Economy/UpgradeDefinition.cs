using UnityEngine;

namespace Game.Economy
{
    /// <summary>One permanent upgrade bought with research points or power between levels.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade Definition", fileName = "UpgradeDefinition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private UpgradeTarget _target;

        /// <summary>Id of the tower this upgrade belongs to. Empty for upgrades that are not tower specific.</summary>
        [SerializeField] private string _towerId = string.Empty;

        /// <summary>Asset name of the drone this upgrade belongs to. Empty for upgrades that are not drone specific.</summary>
        [SerializeField] private string _droneId = string.Empty;

        /// <summary>Heading shown on the upgrade screen, such as the tower name or "Base".</summary>
        [SerializeField] private string _groupName = "General";
        [SerializeField] private int[] _costs = { 5, 10, 15 };

        /// <summary>Which currency pays for this upgrade.</summary>
        [SerializeField] private UpgradeCurrency _currency = UpgradeCurrency.ResearchPoints;

        /// <summary>One line shown under the group heading on the upgrade screen. The first upgrade of a group provides it.</summary>
        [SerializeField] private string _groupDescription = string.Empty;

        /// <summary>Added per level. A fraction (0.1 is +10%) for multipliers, a flat amount for base health and drone count.</summary>
        [SerializeField] private float _bonusPerLevel = 0.1f;

        public string Id => name;
        public string DisplayName => _displayName;
        public UpgradeTarget Target => _target;
        public string TowerId => _towerId;
        public string DroneId => _droneId;
        public string GroupName => _groupName;
        public UpgradeCurrency Currency => _currency;
        public string GroupDescription => _groupDescription;
        public float BonusPerLevel => _bonusPerLevel;
        public int MaxLevel => _costs.Length;

        /// <summary>True for the upgrades that belong to no tower and no drone: base, blockades and the hero.</summary>
        public bool IsGlobal => string.IsNullOrEmpty(_towerId) && string.IsNullOrEmpty(_droneId);

        /// <summary>Cost to go from the given level to the next one.</summary>
        public int CostForNextLevel(int currentLevel) => _costs[currentLevel];

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_towerId) && !string.IsNullOrEmpty(_droneId))
            {
                Debug.LogWarning($"Upgrade '{name}' names both a tower and a drone; only one applies.", this);
            }

            if (_costs == null || _costs.Length == 0)
            {
                Debug.LogWarning($"Upgrade '{name}' has no levels.", this);
            }
        }
    }
}
