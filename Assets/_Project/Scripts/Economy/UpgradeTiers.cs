using System.Collections.Generic;

namespace Game.Economy
{
    /// <summary>
    /// The rule that reads a "tiers per continent" list: which tier is open on which continent. Pure, so the upgrade
    /// catalog, the progress service and the tests share one reading of the data.
    /// </summary>
    public static class UpgradeTiers
    {
        /// <summary>No cap: every tier an upgrade has can be bought.</summary>
        public const int Unlimited = int.MaxValue;

        /// <summary>The highest tier open on a continent; a continent past the list keeps its last entry, an empty list caps nothing.</summary>
        public static int Cap(IReadOnlyList<int> tiersPerContinent, int continent)
        {
            if (tiersPerContinent == null || tiersPerContinent.Count == 0)
            {
                return Unlimited;
            }

            int index = continent < 0 ? 0 : continent >= tiersPerContinent.Count ? tiersPerContinent.Count - 1 : continent;
            return tiersPerContinent[index];
        }

        /// <summary>Index of the first continent whose cap reaches this tier (1-based), or -1 when none does.</summary>
        public static int ContinentOpening(IReadOnlyList<int> tiersPerContinent, int tier)
        {
            if (tiersPerContinent == null || tiersPerContinent.Count == 0)
            {
                return 0;
            }

            for (int i = 0; i < tiersPerContinent.Count; i++)
            {
                if (tiersPerContinent[i] >= tier)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>How many tiers of an upgrade can be bought under a cap.</summary>
        public static int Open(UpgradeDefinition upgrade, int tierCap) => upgrade.MaxLevel < tierCap ? upgrade.MaxLevel : tierCap;
    }
}
