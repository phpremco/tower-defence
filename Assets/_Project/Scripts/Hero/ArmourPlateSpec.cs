using System;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One armour plate on the hero's body: the bone it rides on, where it sits in that bone's space, its size and the
    /// armour tier that adds it. The generator seats the plates on the body's surface in the idle pose, so they hug the
    /// suit and move with the limb.
    /// </summary>
    [Serializable]
    public struct ArmourPlateSpec
    {
        [Tooltip("The bone the plate is fixed to.")]
        public string Bone;

        [Tooltip("The middle of the plate's inner face in the bone's space.")]
        public Vector3 LocalPosition;

        [Tooltip("The plate's turn in the bone's space: its +Z points away from the body, its +Y runs along the limb.")]
        public Vector3 LocalEuler;

        [Tooltip("Width across the limb (x), length along it (y) and how far the middle bulges out (z), in metres at the tier that adds it.")]
        public Vector3 Size;

        [Tooltip("The first armour tier that shows this plate.")]
        [Min(1)] public int FromTier;
    }
}
