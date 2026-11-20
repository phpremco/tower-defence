using Game.Core;

namespace Game.Combat
{
    /// <summary>
    /// A target that is made of something the combat rules have a row for (round 87): <see cref="Hits.Deal"/> asks it,
    /// so a hit is worth on it what the weapon's damage type is worth on its family. A target that does not say is
    /// <see cref="CreatureFamily.None"/>: a gate, the hero, a test fixture.
    /// </summary>
    public interface IFamilyMember
    {
        CreatureFamily Family { get; }
    }
}
