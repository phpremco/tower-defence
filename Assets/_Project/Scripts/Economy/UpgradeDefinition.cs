using UnityEngine;

namespace Game.Economy
{
    /// <summary>One permanent upgrade bought with research points between levels.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade Definition", fileName = "UpgradeDefinition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private UpgradeTarget _target;
        [SerializeField] private int[] _costs = { 5, 10, 15 };

        /// <summary>Added per level. A fraction (0.1 is +10%) for multipliers, a flat amount for base health.</summary>
        [SerializeField] private float _bonusPerLevel = 0.1f;

        public string Id => name;
        public string DisplayName => _displayName;
        public UpgradeTarget Target => _target;
        public float BonusPerLevel => _bonusPerLevel;
        public int MaxLevel => _costs.Length;

        /// <summary>Cost to go from the given level to the next one.</summary>
        public int CostForNextLevel(int currentLevel) => _costs[currentLevel];
    }
}
