namespace Game.Balance
{
    /// <summary>
    /// One wave's surprise as the level's designer writes it (round 81, Remco: "what comes out of a surprise path must be
    /// configurable per level, per wave"). It replaces the wave grammar's default for that wave on the difficulties it names:
    /// its entries come out of the level's surprise points mid-wave, any number of points at once, and the wave's front
    /// gives up what they cost. A surprise without entries says "no surprise on this wave", against the default as well.
    /// A small group, even one big creature, is fine here: it was chosen. Docs/WAVE_GRAMMAR.md, "Writing a level's surprises".
    /// </summary>
    public sealed class SurpriseSpec
    {
        /// <summary>The wave number (from 1) the surprise rides on.</summary>
        public int Wave;

        /// <summary>The easiest and the hardest difficulty index it applies to; the others keep the grammar's default.</summary>
        public int MinDifficulty;
        public int MaxDifficulty = int.MaxValue;

        public SurpriseEntrySpec[] Entries = new SurpriseEntrySpec[0];

        public bool AppliesTo(int number, int difficulty) => Wave == number && MinDifficulty <= difficulty && difficulty <= MaxDifficulty;
    }
}
