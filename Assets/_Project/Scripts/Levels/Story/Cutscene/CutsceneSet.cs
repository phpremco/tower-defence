namespace Game.Levels
{
    /// <summary>Where a cutscene shot takes place. Each set is built once and shown while its shots play.</summary>
    public enum CutsceneSet
    {
        /// <summary>The exhausted Earth seen from orbit.</summary>
        Earth = 0,

        /// <summary>The AI survey of S-69-CRYS: the planet under scan rings.</summary>
        Survey = 1,

        /// <summary>The mothership above the planet and the capsules falling.</summary>
        Orbit = 2,

        /// <summary>The real first level: terrain, props and mist, without gameplay markers.</summary>
        Field = 3,

        /// <summary>The dark under the field, where the crystals tremble and the eyes open.</summary>
        Underground = 4,
    }
}
