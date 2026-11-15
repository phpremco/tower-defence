using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The reference fight for one creature trait: which creature, how many, how they stand, and by how much the towers
    /// made for the trait must beat every other source per coin. The role tests and the matrix on the rules page play it.
    /// </summary>
    [Serializable]
    public struct RoleScenario
    {
        public EnemyTraits Trait;

        [Tooltip("Asset name of the creature that stands for the trait.")]
        public string Enemy;

        [Min(1)] public int Count;

        [Tooltip("Cells between two creatures of the group.")]
        [Min(0.1f)] public float Spacing;

        [Tooltip("True when the group is on the move (an unguided shell lands where its target was), false for a queue at a gate.")]
        public bool Moving;

        [Tooltip("An on-role tower clears the group at least this many times faster per coin than any source without the role.")]
        [Min(1f)] public float Margin;

        public string Reason;
    }
}
