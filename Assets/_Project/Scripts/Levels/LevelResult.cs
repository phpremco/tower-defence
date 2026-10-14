using Game.Towers;

namespace Game.Levels
{
    /// <summary>The outcome of a finished level as shown on the result screen.</summary>
    public sealed class LevelResult
    {
        public bool Won { get; set; }
        public int ResearchPoints { get; set; }
        public int PowerCurrency { get; set; }
        public TowerDefinition NewTower { get; set; }
        public UnlockSource Source { get; set; }
        public string Story { get; set; }
    }
}
