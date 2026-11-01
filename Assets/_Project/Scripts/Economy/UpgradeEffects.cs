using Game.Core;

namespace Game.Economy
{
    /// <summary>Turns bought upgrades into the modifiers a level starts with, per tower, per drone and for the base.</summary>
    public static class UpgradeEffects
    {
        /// <summary>Modifiers for one tower type. Base and blockade upgrades are not included.</summary>
        public static PlayerModifiers ComputeForTower(ProgressService progress, UpgradeCatalog catalog, string towerId)
        {
            return Compute(progress, catalog, towerId, includeGlobal: false);
        }

        /// <summary>Modifiers that do not belong to a tower or drone: base health, blockade strength and the hero.</summary>
        public static PlayerModifiers ComputeGlobal(ProgressService progress, UpgradeCatalog catalog)
        {
            return Compute(progress, catalog, string.Empty, includeGlobal: true);
        }

        /// <summary>Modifiers for one drone type, by the drone's asset name.</summary>
        public static DroneModifiers ComputeForDrone(ProgressService progress, UpgradeCatalog catalog, string droneId)
        {
            float speed = 1f;
            float health = 1f;
            float output = 1f;
            float tempo = 1f;
            float count = 0f;

            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                if (upgrade.DroneId != droneId || string.IsNullOrEmpty(droneId))
                {
                    continue;
                }

                float bonus = progress.GetUpgradeLevel(upgrade.Id) * upgrade.BonusPerLevel;
                switch (upgrade.Target)
                {
                    case UpgradeTarget.DroneSpeed:
                        speed += bonus;
                        break;
                    case UpgradeTarget.DroneArmour:
                        health += bonus;
                        break;
                    case UpgradeTarget.DroneOutput:
                        output += bonus;
                        break;
                    case UpgradeTarget.DroneTempo:
                        tempo += bonus;
                        break;
                    case UpgradeTarget.DroneCount:
                        count += bonus;
                        break;
                }
            }

            return new DroneModifiers(speed, health, output, tempo, (int)System.Math.Round(count));
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
            float baseWeapon = 0f;

            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                bool wanted = includeGlobal ? upgrade.IsGlobal : upgrade.TowerId == towerId && !string.IsNullOrEmpty(towerId);
                if (!wanted)
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
                    case UpgradeTarget.BaseWeapon:
                        baseWeapon += bonus;
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
                }
            }

            return new PlayerModifiers(damage, range, fireRate, health, (int)System.Math.Round(baseHealth), blockade, heroDamage, heroHealth, (int)System.Math.Round(baseWeapon));
        }
    }
}
