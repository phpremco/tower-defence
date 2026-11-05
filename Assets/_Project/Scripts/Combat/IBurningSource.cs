namespace Game.Combat
{
    /// <summary>
    /// What hit a creature, when it burns (a lava ball): a boss with ice armour feels the heat (Docs/BOSSES.md, Frost
    /// Warden). Passed as the source of <see cref="ISourcedTarget.TakeDamageFrom"/>.
    /// </summary>
    public interface IBurningSource
    {
        bool Burns { get; }
    }
}
