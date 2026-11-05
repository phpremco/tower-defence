namespace Game.Enemies
{
    /// <summary>What a boss does at a living gate on its road (Docs/BOSSES.md, "Use the terrain").</summary>
    public enum GateBehaviour
    {
        /// <summary>It climbs onto the gate, weighs it down and strikes it from above; walkers follow it over.</summary>
        Climb = 0,

        /// <summary>It stops in front of the gate and rams it with its own clip (a body that cannot climb, such as Rotbloom).</summary>
        Batter = 1,

        /// <summary>It is held and bites the gate like any creature.</summary>
        Besiege = 2,
    }
}
