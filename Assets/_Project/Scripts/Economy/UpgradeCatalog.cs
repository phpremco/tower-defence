using System.Collections.Generic;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// The permanent upgrades shown on the upgrade screen, in display order, and how far their tiers are open: every
    /// continent the player reaches opens more of them (Remco, 2026-11-15: ten tiers over five continents).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();

        [Tooltip("The highest upgrade tier that can be bought once the continent at this index has been reached; a continent past the end of the list keeps the last entry. Empty opens every tier.")]
        [SerializeField] private int[] _tiersPerContinent = new int[0];

        public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;

        /// <summary>The tier caps by continent, as authored.</summary>
        public IReadOnlyList<int> TiersPerContinent => _tiersPerContinent;

        /// <summary>The highest tier open to a player who has reached the continent at this index.</summary>
        public int TierCap(int continent) => UpgradeTiers.Cap(_tiersPerContinent, continent);

        /// <summary>Index of the first continent that opens this tier (1-based tier), or -1 when no continent does.</summary>
        public int ContinentOpening(int tier) => UpgradeTiers.ContinentOpening(_tiersPerContinent, tier);

        private void OnValidate()
        {
            for (int i = 1; i < _tiersPerContinent.Length; i++)
            {
                if (_tiersPerContinent[i] < _tiersPerContinent[i - 1])
                {
                    Debug.LogWarning($"Upgrade catalog '{name}': continent {i} opens fewer tiers than the one before it.", this);
                }
            }
        }
    }
}
