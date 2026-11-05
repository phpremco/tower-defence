namespace Game.Combat
{
    /// <summary>
    /// A blocking structure a boss has climbed onto (Docs/BOSSES.md: "walkers can follow it over a gate it has climbed"):
    /// while it is bridged it holds no walker back, and walkers passing it climb over its top instead of through it.
    /// </summary>
    public interface IBridgeable
    {
        bool IsBridged { get; }

        /// <summary>How high its top stands, for the walkers climbing over it.</summary>
        float BridgeHeight { get; }
    }
}
