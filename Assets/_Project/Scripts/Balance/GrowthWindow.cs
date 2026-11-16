namespace Game.Balance
{
    /// <summary>
    /// The grace window of the combat rules' growth layer (Remco 2026-11-15, Docs/COMBAT_RULES.md section 6), per difficulty
    /// from Easy to Psycho: after a creature trait that needs a counter tower first appears, a defence without that tower may
    /// hold <see cref="GraceWaves"/> waves and must have lost within <see cref="FallWithinWaves"/> waves after them. One
    /// table: the generator writes it into the rules asset (what the game's checks read) and onto every wave script (when the
    /// trait's troops start to grow). Pure C#.
    /// </summary>
    public static class GrowthWindow
    {
        public static readonly int[] GraceWaves = { 2, 1, 1, 0 };
        public static readonly int[] FallWithinWaves = { 4, 4, 3, 3 };
    }
}
