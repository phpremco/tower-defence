namespace Game.Balance
{
    /// <summary>
    /// The main roads a level writes for one wave (round 82): it replaces the grammar's default choice for that wave on the
    /// difficulties it names (<see cref="WaveGrammar.RoadsOn"/>). Docs/WAVE_GRAMMAR.md, "Which roads a wave uses".
    /// </summary>
    public sealed class RoadSpec
    {
        /// <summary>The wave number (from 1).</summary>
        public int Wave;

        /// <summary>The easiest and the hardest difficulty index it applies to; the others keep the grammar's default.</summary>
        public int MinDifficulty;
        public int MaxDifficulty = int.MaxValue;

        public MainRoads Roads = MainRoads.Both;

        public bool AppliesTo(int number, int difficulty) => Wave == number && MinDifficulty <= difficulty && difficulty <= MaxDifficulty;
    }
}
