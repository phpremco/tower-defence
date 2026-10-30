using System;
using System.Collections.Generic;

namespace Game.Economy
{
    /// <summary>Everything that is saved between sessions.</summary>
    [Serializable]
    public sealed class ProgressData
    {
        public int ResearchPoints;
        public int PowerCurrency;
        public int MasterVolumePercent = 100;
        public int MusicVolumePercent = 70;
        public int EffectsVolumePercent = 100;
        public int AmbienceVolumePercent = 80;
        public bool IntroSeen;
        public List<string> UnlockedTowerIds = new List<string>();
        public List<string> CompletedLevels = new List<string>();
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();
        public List<StarRecord> Stars = new List<StarRecord>();

        /// <summary>The street weapons last taken into a level, by asset name, so the choice is offered again.</summary>
        public List<string> LoadoutIds = new List<string>();
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

