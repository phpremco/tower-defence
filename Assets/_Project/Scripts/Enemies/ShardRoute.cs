namespace Game.Enemies
{
    /// <summary>The route a boss's shard takes after the split (Docs/BOSSES.md, the Shattered: land, air and the second road).</summary>
    public enum ShardRoute
    {
        /// <summary>The road the giant was walking.</summary>
        OwnRoad = 0,

        /// <summary>Another road of the level; the giant's own when the level has only one.</summary>
        OtherRoad = 1,

        /// <summary>The level's first flight path; the giant's own road when it has none.</summary>
        Air = 2,
    }
}
