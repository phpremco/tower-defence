using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The build pads a boss's spores have overgrown (Docs/BOSSES.md, Rotbloom): nothing may be built on them until a
    /// blast that reaches the pad (or the hero) burns them clean. Pure logic: cells and their centres.
    /// </summary>
    public sealed class OvergrowthPads
    {
        // A blast burns a pad clean when it reaches the pad's edge, not only its middle.
        private const float PadHalfWidth = 0.5f;

        private readonly Dictionary<Vector2Int, Vector3> _pads = new Dictionary<Vector2Int, Vector3>();
        private readonly List<Vector2Int> _scratch = new List<Vector2Int>(4);

        public int Count => _pads.Count;

        public IEnumerable<Vector2Int> Cells => _pads.Keys;

        public bool Contains(Vector2Int cell) => _pads.ContainsKey(cell);

        /// <summary>Overgrows a pad; false when it already was.</summary>
        public bool Add(Vector2Int cell, Vector3 centre)
        {
            if (_pads.ContainsKey(cell))
            {
                return false;
            }

            _pads.Add(cell, centre);
            return true;
        }

        /// <summary>Burns one pad clean (the hero); false when it was not overgrown.</summary>
        public bool Remove(Vector2Int cell) => _pads.Remove(cell);

        /// <summary>Burns clean every pad a blast of that radius reaches; the cleaned cells go into the list.</summary>
        public void Burn(Vector3 at, float radius, List<Vector2Int> cleaned)
        {
            cleaned.Clear();
            float reach = radius + PadHalfWidth;
            _scratch.Clear();
            foreach (KeyValuePair<Vector2Int, Vector3> pad in _pads)
            {
                float dx = pad.Value.x - at.x;
                float dz = pad.Value.z - at.z;
                if (dx * dx + dz * dz <= reach * reach)
                {
                    _scratch.Add(pad.Key);
                }
            }

            foreach (Vector2Int cell in _scratch)
            {
                _pads.Remove(cell);
                cleaned.Add(cell);
            }
        }

        public void Clear() => _pads.Clear();
    }
}
