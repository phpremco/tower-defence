using Game.Core;

namespace Game.Economy
{
    /// <summary>Turns bought upgrades into the modifiers a level starts with.</summary>
    public static class UpgradeEffects
    {
        public static PlayerModifiers Compute(ProgressService progress, UpgradeCatalog catalog)
        {
            float damage = 1f;
            float range = 1f;
            float baseHealth = 0f;
            float blockade = 1f;

            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                float bonus = progress.GetUpgradeLevel(upgrade.Id) * upgrade.BonusPerLevel;
                switch (upgrade.Target)
                {
                    case UpgradeTarget.TowerDamage:
                        damage += bonus;
                        break;
                    case UpgradeTarget.TowerRange:
                        range += bonus;
                        break;
                    case UpgradeTarget.BaseHealth:
                        baseHealth += bonus;
                        break;
                    case UpgradeTarget.BlockadeHealth:
                        blockade += bonus;
                        break;
                }
            }

            return new PlayerModifiers(damage, range, (int)System.Math.Round(baseHealth), blockade);
        }
    }
}
