namespace Game.Combat
{
    /// <summary>
    /// A structure whose wreck a repair crew can raise again (round 88d: a base turret, which is part of the base and can
    /// be neither sold nor bought again). The repair drones treat such a wreck as a job like any damaged structure.
    /// </summary>
    public interface ICrewRaisable
    {
        /// <summary>True while the structure lies wrecked and waits for a crew to raise it.</summary>
        bool AwaitsCrew { get; }
    }
}
