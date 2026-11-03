using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// What each ability kind does, in one switch: plain static code instead of a class per ability, so it stays
    /// IL2CPP-safe and a new kind is one case here plus its values on the definition.
    /// </summary>
    public static class HeroAbilityEffects
    {
        /// <summary>True when the ability could do something here, apart from its cooldown and its price.</summary>
        public static bool CanApply(HeroAbilityDefinition ability, IGuardable guarded, bool standing)
        {
            switch (ability.Kind)
            {
                case HeroAbilityKind.GateRepair:
                    // The repair is done by hand at the gate, so the hero must stand at a post that has one, and a wreck or
                    // an unhurt gate would take the money and the cooldown for nothing.
                    return standing && guarded != null && guarded.CanRestore;
                default:
                    return true;
            }
        }

        /// <summary>Applies the ability at the hero's position. The buffer is the caller's, reused between calls.</summary>
        public static void Apply(HeroAbilityDefinition ability, Vector3 position, TargetRegistry targets, List<ITargetable> buffer, IGuardable guarded)
        {
            switch (ability.Kind)
            {
                case HeroAbilityKind.Shockwave:
                    targets.FindInRadius(position, ability.Radius, ability.Targets, buffer);
                    for (int i = 0; i < buffer.Count; i++)
                    {
                        buffer[i].TakeDamage(ability.Damage);
                    }

                    break;
                case HeroAbilityKind.GateRepair:
                    guarded?.Restore(ability.Fraction);
                    break;
            }
        }
    }
}
