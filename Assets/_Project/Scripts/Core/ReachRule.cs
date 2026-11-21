using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One entry of the combat rules' reach table (round 88b): how far an attack style carries to a structure a creature
    /// passes, and why. A style without an entry is a melee style: it reaches as far as the creature's own body does.
    /// </summary>
    [Serializable]
    public struct ReachRule
    {
        [Tooltip("The attack style by name (Game.Enemies.AttackStyle), as the scenarios name their creature.")]
        public string Style;

        [Tooltip("Cells this style reaches to a tower, a turret or a drone beside the road. Gates, the base and the hero are still met at the creature's own range.")]
        [Min(0f)] public float Cells;

        [Tooltip("One line for the rules page: why the style reaches this far.")]
        public string Reason;
    }
}
