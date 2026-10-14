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
        public List<string> UnlockedTowerIds = new List<string>();
        public List<string> CompletedLevels = new List<string>();
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();
    }

    [Serializable]
    public struct UpgradeLevel
    {
        public string Id;
        public int Level;
    }
}
