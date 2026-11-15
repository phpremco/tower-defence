using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A named exception to the rule table: one weapon whose factor against one trait is not its damage type's, with the
    /// reason. The only place a weapon may differ from its type; the rules page lists every one.
    /// </summary>
    [Serializable]
    public struct CombatException
    {
        [Tooltip("The weapon's asset name (a tower id, a hero weapon, a drone, a path object).")]
        public string Weapon;

        [Tooltip("Armoured or Airborne: the two traits a factor exists for.")]
        public EnemyTraits Trait;

        [Range(0f, 3f)] public float Factor;

        public string Reason;
    }
}
