using System;
using System.Collections.Generic;

namespace Game.Economy
{
    /// <summary>
    /// Brings a loaded save up to the current format. Each step upgrades one version to the next, so a save from any
    /// older build walks every step in order. A new format adds one method and one entry at the end of
    /// <see cref="Steps"/>; existing steps are never edited, because saves in the wild already passed through them.
    /// Plain delegates to static methods keep this IL2CPP-safe (no reflection).
    /// </summary>
    public static class ProgressMigrator
    {
        // Entry i upgrades a save from version i to version i + 1.
        private static readonly Action<ProgressData>[] Steps =
        {
            AddStoryFlags,
            RestartCampaign,
        };

        public static int CurrentVersion => Steps.Length;

        /// <summary>
        /// Upgrades the save in memory; it reaches disk with the next save, so loading alone never rewrites a file.
        /// A save from a newer build is left as it is.
        /// </summary>
        public static void Migrate(ProgressData data)
        {
            // A damaged file can carry a negative version; it is treated as the oldest format, not as an index.
            for (int version = Math.Max(0, data.Version); version < CurrentVersion; version++)
            {
                Steps[version](data);
                data.Version = version + 1;
            }
        }

        // Version 1 introduced the seen-story list; saves from before it may carry no list at all.
        private static void AddStoryFlags(ProgressData data)
        {
            if (data.SeenStoryIds == null)
            {
                data.SeenStoryIds = new List<string>();
            }
        }

        // Version 2 replaced the seven old levels with the continent campaign. No old level maps onto a new one, and a
        // mapped completion would open levels whose weapon and story introductions were never seen, so the campaign
        // starts again: completions, stars, unlocked weapons (the starting set comes back on load), gadgets, story flags
        // and the intro are cleared. Currencies, bought upgrades, the loadout choice and the volumes are kept, so a
        // returning player is ahead, not behind; upgrades of towers now out of the campaign stay saved, unrefunded.
        private static void RestartCampaign(ProgressData data)
        {
            data.CompletedLevels = new List<string>();
            data.Stars = new List<StarRecord>();
            data.UnlockedTowerIds = new List<string>();
            data.SeenStoryIds = new List<string>();
            data.IntroSeen = false;
            data.UnlockedHeroPerkIds = new List<string>();
            data.UnlockedPathObjectIds = new List<string>();
            if (data.Upgrades == null)
            {
                data.Upgrades = new List<UpgradeLevel>();
            }

            if (data.LoadoutIds == null)
            {
                data.LoadoutIds = new List<string>();
            }
        }
    }
}
