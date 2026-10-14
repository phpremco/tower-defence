using System.Collections.Generic;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>The permanent upgrades shown on the upgrade screen, in display order.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();

        public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;
    }
}
