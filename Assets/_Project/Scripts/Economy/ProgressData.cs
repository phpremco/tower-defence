using System;
using System.Collections.Generic;

namespace Game.Economy
{
    /// <summary>Everything that is saved between sessions.</summary>
    [Serializable]
    public sealed class ProgressData
    {
        /// <summary>Save format version. Saves written before versioning load as 0; see <see cref="ProgressMigrator"/>.</summary>
        public int Version;
        public int ResearchPoints;
        public int PowerCurrency;
        public int MasterVolumePercent = 100;
        public int MusicVolumePercent = 70;
        public int EffectsVolumePercent = 100;
        public int AmbienceVolumePercent = 80;
        public bool IntroSeen;

        /// <summary>Ids of story moments already shown, so a story trigger plays once per save.</summary>
        public List<string> SeenStoryIds = new List<string>();

        /// <summary>Asset names of the bosses the player has met, so the Codex describes them only once they were seen.</summary>
        public List<string> SeenEnemyIds = new List<string>();

        public List<string> UnlockedTowerIds = new List<string>();
        public List<string> CompletedLevels = new List<string>();
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();
        public List<StarRecord> Stars = new List<StarRecord>();

        /// <summary>The street weapons last taken into a level, by asset name, so the choice is offered again.</summary>
        public List<string> LoadoutIds = new List<string>();

        /// <summary>Hero perks a first clear has granted, by asset name (version 2).</summary>
        public List<string> UnlockedHeroPerkIds = new List<string>();

        /// <summary>Gadgets (path objects) a first clear has granted for the loadout, by asset name (version 2).</summary>
        public List<string> UnlockedPathObjectIds = new List<string>();
    }

    /// <summary>Best star rating (1 to 3) for one level on one difficulty. Key is "levelId/difficultyId".</summary>
    [Serializable]
    public struct StarRecord
    {
        public string Key;
        public int Stars;
    }

    [Serializable]
    public struct UpgradeLevel
    {
        public string Id;
        public int Level;
    }
}

