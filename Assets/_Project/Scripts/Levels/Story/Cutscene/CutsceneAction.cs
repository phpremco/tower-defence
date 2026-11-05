namespace Game.Levels
{
    /// <summary>
    /// What a set does during a shot. Each action belongs to one set (see <see cref="CutsceneActions"/>). Values are only
    /// appended, so the numbers are not the story order: <see cref="CutsceneActions.Order"/> is.
    /// </summary>
    public enum CutsceneAction
    {
        EarthDrift = 0,
        ScanPlanet = 1,
        CapsulesFall = 2,
        PathfinderLands = 3,
        BaseUnfolds = 4,
        DrillDown = 5,
        DronesHarvest = 6,
        CoreIgnites = 7,
        CrystalsTremble = 8,
        EyesOpen = 9,
        GroundShivers = 10,
        SwarmPours = 11,
        GateStand = 12,
        Handoff = 13,

        /// <summary>A close shot of the commander as the creatures gather: disbelief, a step back, a hand to the visor.</summary>
        MercerReacts = 14,
    }
}
