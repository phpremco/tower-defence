using Game.Core;

namespace Game.Combat
{
    /// <summary>
    /// The one place a hit lands on a target (round 67): every damage source in the game (a tower's shot, the hero's blow
    /// or shockwave, a drone's shot, a mine, a lava ball) comes through here with its <see cref="DamageProfile"/> from the
    /// combat rules. The factor for the target's family (round 87) and the share for its layer are taken here, on the
    /// creature actually hit (a blast that catches a flyer and a walker lands on each at its own share), and the armour
    /// share travels on to the creature's armour pool through <see cref="ISourcedTarget"/>.
    /// </summary>
    public static class Hits
    {
        /// <param name="profile">What the weapon's hits are worth against armour and in the air (its definition's Profile).</param>
        /// <param name="splash">True for a blast that caught the target (it reaches a hidden boss in part), false for an aimed blow.</param>
        public static void Deal(ITargetable target, float damage, DamageProfile profile, object source = null, bool splash = true)
        {
            CreatureFamily family = target is IFamilyMember member ? member.Family : CreatureFamily.None;
            damage *= profile.Against(target.Layer, family);
            if (target is ISourcedTarget sourced)
            {
                sourced.TakeDamageFrom(damage, source, splash, profile.Armour);
                return;
            }

            target.TakeDamage(damage);
        }
    }
}
