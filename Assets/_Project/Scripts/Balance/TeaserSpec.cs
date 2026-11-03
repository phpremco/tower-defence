namespace Game.Balance
{
    /// <summary>
    /// A first glimpse of a creature the next level is about (STORY: a new enemy shows up shortly in the level before the
    /// weapon that answers it): a small fixed group on one wave, late in the level, on every difficulty. It is not part of
    /// the wave's budget, so the crowd around it stays as the script says.
    /// </summary>
    public sealed class TeaserSpec
    {
        public string Enemy = string.Empty;

        /// <summary>The wave number (from 1) the teaser walks in on.</summary>
        public int Wave;
        public int Count = 1;
    }
}
