using Game.Towers;

namespace Game.Levels
{
    /// <summary>The outcome of a finished level as shown on the result screen.</summary>
    public sealed class LevelResult
    {
        public bool Won { get; set; }

        /// <summary>One to three stars for a win, based on how much of the base was left.</summary>
        public int Stars { get; set; }
        /// <summary>True when this level was already beaten on this difficulty, so no points are paid again.</summary>
        public bool RewardsAlreadyEarned { get; set; }
        public int ResearchPoints { get; set; }
        public int PowerCurrency { get; set; }
        public TowerDefinition NewTower { get; set; }
        public UnlockSource Source { get; set; }
        public string Story { get; set; }
    }
}

