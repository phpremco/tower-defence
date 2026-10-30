using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// One moving piece of a procedural creature: its mesh, where it hangs on the body and how it is animated.
    /// Legs carry a second mesh for the lower segment, which pivots at <see cref="Knee"/> inside the upper one.
    /// Mirrored parts are the left-hand copies of a right-hand mesh.
    /// </summary>
    public readonly struct CreaturePart
    {
        public readonly CreatureRole Role;
        public readonly Mesh Mesh;
        public readonly Mesh Lower;
        public readonly Vector3 Pivot;
        public readonly Vector3 Knee;
        public readonly bool Mirrored;

        /// <summary>Gait or beat phase in radians, so legs and wings alternate.</summary>
        public readonly float Phase;

        /// <summary>Order among parts of the same role: bands from head to tail, wing pairs from front to back.</summary>
        public readonly int Index;

        public CreaturePart(CreatureRole role, Mesh mesh, Vector3 pivot, bool mirrored = false, float phase = 0f, int index = 0, Mesh lower = null, Vector3 knee = default)
        {
            Role = role;
            Mesh = mesh;
            Lower = lower;
            Pivot = pivot;
            Knee = knee;
            Mirrored = mirrored;
            Phase = phase;
            Index = index;
        }
    }
}
