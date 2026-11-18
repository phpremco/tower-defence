namespace Game.Balance
{
    /// <summary>
    /// One group of an authored surprise (<see cref="SurpriseSpec"/>): which creature, how many, out of which surprise point
    /// of the level and in what pattern. The count is the script's, as every count in a wave: the difficulty's count
    /// multiplier still applies in play, so write a surprise per difficulty where the exact number matters.
    /// </summary>
    public sealed class SurpriseEntrySpec
    {
        public string Enemy = string.Empty;
        public int Count = 1;

        /// <summary>The kind of spawn point and which one: a nest counted from 1 in the level's nests, or a ground path's index.</summary>
        public SpawnPointKind Kind = SpawnPointKind.Nest;
        public int Point = 1;

        /// <summary>
        /// How the group arrives: <see cref="WavePattern.Stream"/> one file at the creature's own spacing,
        /// <see cref="WavePattern.SpreadClusters"/> the file cut into clusters with the difficulty's breather,
        /// <see cref="WavePattern.Swarm"/> close together as a rush wave's crowd. Other patterns read as a stream.
        /// </summary>
        public WavePattern Pattern = WavePattern.Stream;

        /// <summary>Seconds between two creatures; zero takes the spacing of the creature's troop in the roster (one second without one).</summary>
        public float Interval;

        /// <summary>Seconds after the surprise's own moment in the wave (halfway the front, see the grammar) this group starts.</summary>
        public float Delay;
    }
}
