namespace Game.Balance
{
    /// <summary>What comes from where the player is not looking, in the middle of a wave (round 79, Docs/WAVE_GRAMMAR.md).</summary>
    public enum SurpriseKind
    {
        None = 0,

        /// <summary>The level's rift troops surface out of their nest: behind the gate where the level has such a nest.</summary>
        Rift = 1,

        /// <summary>A share of the wave comes down the second road alone, while the front is busy on the first.</summary>
        Flank = 2,

        /// <summary>The level's own surprise for this wave (<see cref="SurpriseSpec"/>, round 81): its entries, out of the points it names.</summary>
        Authored = 3,
    }
}
