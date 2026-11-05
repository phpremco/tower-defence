namespace Game.Enemies
{
    /// <summary>
    /// The one mechanic that makes a continent boss play differently (Docs/BOSSES.md). The boss framework runs the
    /// signature a boss names; a boss with <see cref="None"/> only walks, climbs and dives.
    /// </summary>
    public enum BossSignature
    {
        None = 0,

        /// <summary>Gravemaw: dives under at its phase thresholds, runs a crack line and throws the tower that hurt it most.</summary>
        Burrow = 1,

        /// <summary>Cinder Matriarch: leaves the road to hunt the cold towers.</summary>
        HuntTheCold = 2,

        /// <summary>Frost Warden: freezes each gate it reaches and walks through it.</summary>
        GateBreaker = 3,

        /// <summary>Rotbloom: overgrows small groups of build pads.</summary>
        Overgrowth = 4,

        /// <summary>The Shattered: splits into shards on separate routes and pulls them back together.</summary>
        Splitting = 5,

        /// <summary>The Sealed One: learns the tower type that hurt it most and halves its damage.</summary>
        Recognition = 6,
    }
}
