using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One entry of the combat rules' family table (round 87): what a damage type is worth on a creature family, and why.
    /// A pair without an entry is worth one, so the table lists only the pairs that differ.
    /// </summary>
    [Serializable]
    public struct FamilyRule
    {
        public DamageType Type;

        public CreatureFamily Family;

        [Tooltip("What a hit of this damage type is multiplied by on a creature of this family, before the armour and air shares.")]
        [Range(0f, 4f)] public float Factor;

        [Tooltip("One line for the rules page: why the pair is what it is.")]
        public string Reason;
    }
}
