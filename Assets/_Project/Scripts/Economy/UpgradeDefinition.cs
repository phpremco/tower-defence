using UnityEngine;

namespace Game.Economy
{
    /// <summary>One permanent upgrade bought with research points between levels.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade Definition", fileName = "UpgradeDefinition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private UpgradeTarget _target;

        /// <summary>Id of the tower this upgrade belongs to. Empty for upgrades that are not tower specific.</summary>
        [SerializeField] private string _towerId = string.Empty;

        /// <summary>Heading shown on the upgrade screen, such as the tower name or "Base".</summary>
        [SerializeField] private string _groupName = "General";
        [SerializeField] private int[] _costs = { 5, 10, 15 };

        /// <summary>Added per level. A fraction (0.1 is +10%) for multipliers, a flat amount for base health.</summary>
        [SerializeField] private float _bonusPerLevel = 0.1f;

        public string Id => name;
        public string DisplayName => _displayName;
        public UpgradeTarget Target => _target;
        public string TowerId => _towerId;
        public string GroupName => _groupName;
        public float BonusPerLevel => _bonusPerLevel;
        public int MaxLevel => _costs.Length;

        /// <summary>Cost to go from the given level to the next one.</summary>
        public int CostForNextLevel(int currentLevel) => _costs[currentLevel];
    }
}

