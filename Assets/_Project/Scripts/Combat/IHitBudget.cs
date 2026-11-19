using Game.Core;

namespace Game.Combat
{
    /// <summary>
    /// A target that can say how big one aimed hit of a kind must be to kill it now. A beam asks before a pulse (round
    /// 85): the pulse that kills costs the beam only the share of it the creature needed, so a beam neither wastes damage
    /// on a dead body nor gets kills for free (Tower.ReleaseBeam).
    /// </summary>
    public interface IHitBudget
    {
        /// <param name="profile">What the weapon's hits are worth against armour and in the air.</param>
        float HitToKill(DamageProfile profile);
    }
}
