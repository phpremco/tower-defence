using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// A boss whose rage grows with every gate it breaks (Docs/BOSSES.md, Frost Warden): each broken gate adds to its pace
    /// up to a ceiling, and the icicles on its back grow from calm to full rage with it. Pure logic.
    /// </summary>
    public sealed class GateRage
    {
        private readonly float _perGate;
        private readonly int _max;

        public GateRage(float perGate, int max)
        {
            _perGate = Mathf.Max(0f, perGate);
            _max = Mathf.Max(1, max);
        }

        /// <summary>Gates broken so far, counted up to the ceiling.</summary>
        public int Level { get; private set; }

        /// <summary>Its speed factor from the rage alone.</summary>
        public float Pace => 1f + _perGate * Level;

        /// <summary>From 0 (calm) to 1 (full rage), for the icicles.</summary>
        public float Share => Level / (float)_max;

        public void Reset() => Level = 0;

        /// <summary>Another gate broke; returns true when its rage rose (not yet at the ceiling).</summary>
        public bool GateBroken()
        {
            if (Level >= _max)
            {
                return false;
            }

            Level++;
            return true;
        }

        /// <summary>The scale of the rage bones between their calm and their full size.</summary>
        public float BoneScale(Vector2 calmToFull) => Mathf.Lerp(calmToFull.x, calmToFull.y, Share);
    }
}
