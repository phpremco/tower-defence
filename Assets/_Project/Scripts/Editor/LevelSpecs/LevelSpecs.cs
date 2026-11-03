using System.Collections.Generic;

namespace Game.Editor
{
    /// <summary>
    /// Every campaign level as a spec, one partial file per continent (<c>LevelSpecs.Crystal.cs</c>, later the other five).
    /// A continent's index here is its region index on the globe, and its specs are its levels in play order; a continent
    /// holds twenty levels, and the ones without a spec yet show as "coming soon".
    /// </summary>
    public static partial class LevelSpecs
    {
        /// <summary>The continents in campaign order, each with its level specs in play order.</summary>
        public static CampaignLevelSpec[][] Continents => new[] { Crystal };

        /// <summary>The towers each continent sells, by id; the build bar hides the ones not unlocked yet.</summary>
        public static string[][] Rosters => new[] { CrystalRoster };

        /// <summary>The first continent whose roster has the tower, or -1 for a tower out of the campaign.</summary>
        public static int ContinentOfTower(string towerId)
        {
            string[][] rosters = Rosters;
            for (int c = 0; c < rosters.Length; c++)
            {
                if (System.Array.IndexOf(rosters[c], towerId) >= 0)
                {
                    return c;
                }
            }

            return -1;
        }

        public static IEnumerable<CampaignLevelSpec> All
        {
            get
            {
                foreach (CampaignLevelSpec[] continent in Continents)
                {
                    foreach (CampaignLevelSpec spec in continent)
                    {
                        yield return spec;
                    }
                }
            }
        }

        public static CampaignLevelSpec ByKey(string key)
        {
            foreach (CampaignLevelSpec spec in All)
            {
                if (spec.Key == key)
                {
                    return spec;
                }
            }

            return null;
        }

        /// <summary>The continent and the 0-based index within it of a level, or false for a key no spec has.</summary>
        public static bool TryLocate(string key, out int continent, out int index)
        {
            CampaignLevelSpec[][] continents = Continents;
            for (continent = 0; continent < continents.Length; continent++)
            {
                for (index = 0; index < continents[continent].Length; index++)
                {
                    if (continents[continent][index].Key == key)
                    {
                        return true;
                    }
                }
            }

            continent = -1;
            index = -1;
            return false;
        }
    }
}
