namespace Game.Balance
{
    /// <summary>The two kinds of spawn point a level has for creatures on the ground or out of it (round 81): both carry a role in the level's data.</summary>
    public enum SpawnPointKind
    {
        /// <summary>An element nest (a rift): creatures of its element rise there and make for the road.</summary>
        Nest = 0,

        /// <summary>The start of a ground path (a route gate).</summary>
        Path = 1,
    }
}
