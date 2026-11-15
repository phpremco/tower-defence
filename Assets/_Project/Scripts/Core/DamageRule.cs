using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>One row of the combat rule table: what a damage type is worth against armour and in the air, and why.</summary>
    [Serializable]
    public struct DamageRule
    {
        public DamageType Type;

        [Tooltip("Share of a hit that works on an armour pool. One strips it point for point, under one only chips it, over one is made for it.")]
        [Range(0f, 3f)] public float Armour;

        [Tooltip("Share of a hit that lands on a creature in the air. Zero: this kind of damage never reaches the sky (no weapon of the type may aim up).")]
        [Range(0f, 1f)] public float Air;

        [Tooltip("One line for the rules page: why the row is what it is.")]
        public string Reason;
    }
}
