using Game.Core;

namespace Game.Economy
{
    /// <summary>Turns bought upgrades into the modifiers a level starts with, per tower and for the base.</summary>
    public static class UpgradeEffects
    {
        /// <summary>Modifiers for one tower type. Base and blockade upgrades are not included.</summary>
        public static PlayerModifiers ComputeForTower(ProgressService progress, UpgradeCatalog catalog, string towerId)
        {
            return Compute(progress, catalog, towerId, includeGlobal: false);
        }

        /// <summary>Modifiers that do not belong to a tower: base health, blockade strength and the hero.</summary>
        public static PlayerModifiers ComputeGlobal(ProgressService progress, UpgradeCatalog catalog)
        {
            return Compute(progress, catalog, string.Empty, includeGlobal: true);
        }

        private static PlayerModifiers Compute(ProgressService progress, UpgradeCatalog catalog, string towerId, bool includeGlobal)
        {
            float damage = 1f;
            float range = 1f;
            float fireRate = 1f;
            float health = 1f;
            float baseHealth = 0f;
            float blockade = 1f;
            float heroDamage = 1f;
            float heroHealth = 1f;
            float repairDrones = 0f;
            float collectorDrones = 0f;
            float droneSpeed = 1f;

            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                bool isGlobal = string.IsNullOrEmpty(upgrade.TowerId);
                if (isGlobal != includeGlobal || (!isGlobal && upgrade.TowerId != towerId))
                {
                    continue;
                }

                float bonus = progress.GetUpgradeLevel(upgrade.Id) * upgrade.BonusPerLevel;
                switch (upgrade.Target)
                {
                    case UpgradeTarget.TowerDamage:
                        damage += bonus;
                        break;
                    case UpgradeTarget.TowerRange:
                        range += bonus;
                        break;
                    case UpgradeTarget.TowerFireRate:
                        fireRate += bonus;
                        break;
                    case UpgradeTarget.TowerHealth:
                        health += bonus;
                        break;
                    case UpgradeTarget.BaseHealth:
                        baseHealth += bonus;
                        break;
                    case UpgradeTarget.BlockadeHealth:
                        blockade += bonus;
                        break;
                    case UpgradeTarget.HeroDamage:
                        heroDamage += bonus;
                        break;
                    case UpgradeTarget.HeroHealth:
                        heroHealth += bonus;
                        break;
                    case UpgradeTarget.RepairDrones:
                        repairDrones += bonus;
                        break;
                    case UpgradeTarget.CollectorDrones:
                        collectorDrones += bonus;
                        break;
                    case UpgradeTarget.DroneSpeed:
                        droneSpeed += bonus;
                        break;
                }
            }

            return new PlayerModifiers(damage, range, fireRate, health, (int)System.Math.Round(baseHealth), blockade, heroDamage, heroHealth, (int)System.Math.Round(repairDrones), (int)System.Math.Round(collectorDrones), droneSpeed);
        }
    }
}
