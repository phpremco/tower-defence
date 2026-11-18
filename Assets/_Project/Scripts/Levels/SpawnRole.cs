namespace Game.Levels
{
    /// <summary>
    /// What a spawn point of a level is for (round 81, Remco: "all spawn points are the same kind of thing; some are main
    /// paths and others surprise paths"). A role in the level's data, not a different mechanism: a wave may use any main
    /// points together with any surprise points.
    /// </summary>
    public enum SpawnRole
    {
        /// <summary>The front of a wave comes through it.</summary>
        Main = 0,

        /// <summary>Only a surprise comes through it: the wave grammar's default or the level's own schedule.</summary>
        Surprise = 1,
    }
}
