namespace Game.Boot
{
    /// <summary>
    /// What a one-shot sound is for, which decides its voice budget: filler kinds share a few voices each and get
    /// ducked and dulled when the camera is far out; a priority sound always plays and ducks the rest for a moment.
    /// </summary>
    public enum SfxKind
    {
        Shot = 0,
        Kill = 1,
        Hit = 2,
        Spawn = 3,

        /// <summary>Creature steps, attacks and small cries: the busiest kind, kept quiet and few.</summary>
        Creature = 4,

        /// <summary>Drone work and the hero's own actions.</summary>
        Crew = 5,

        Priority = 6,
    }
}
