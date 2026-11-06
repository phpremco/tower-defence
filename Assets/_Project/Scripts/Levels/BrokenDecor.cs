using UnityEngine;

namespace Game.Levels
{
    /// <summary>One decoration piece a boss just smashed: where, how big (plan radius) and what it was, for the views.</summary>
    public readonly struct BrokenDecor
    {
        public BrokenDecor(Vector3 position, float radius, DecorRemains kind)
        {
            Position = position;
            Radius = radius;
            Kind = kind;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
        public DecorRemains Kind { get; }
    }
}
