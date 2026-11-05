using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The pods of a boss's spore volleys in the air (Docs/BOSSES.md, Rotbloom): each flies an arc to its pad, and only a
    /// pod still whole when it gets there overgrows the pad; one shot down on the way does nothing. Pure logic: the arc
    /// and the fate of each pod by its number.
    /// </summary>
    public sealed class SporeVolley
    {
        private enum Fate
        {
            Flying,
            Landed,
            ShotDown,
        }

        private readonly List<Fate> _fates = new List<Fate>(8);
        private readonly List<Vector2Int> _cells = new List<Vector2Int>(8);

        public int Landed { get; private set; }
        public int ShotDown { get; private set; }

        /// <summary>Pods still in the air.</summary>
        public int Flying { get; private set; }

        /// <summary>A pod leaves for a pad; returns its number.</summary>
        public int Launch(Vector2Int pad)
        {
            _fates.Add(Fate.Flying);
            _cells.Add(pad);
            Flying++;
            return _fates.Count - 1;
        }

        public Vector2Int PadOf(int pod) => _cells[pod];

        public bool IsFlying(int pod) => pod >= 0 && pod < _fates.Count && _fates[pod] == Fate.Flying;

        /// <summary>The pod was shot down in the air: its pad is spared.</summary>
        public void Shoot(int pod)
        {
            if (!IsFlying(pod))
            {
                return;
            }

            _fates[pod] = Fate.ShotDown;
            Flying--;
            ShotDown++;
        }

        /// <summary>The pod reached its pad; true when it overgrows the pad (it was not shot down first).</summary>
        public bool Land(int pod)
        {
            if (!IsFlying(pod))
            {
                return false;
            }

            _fates[pod] = Fate.Landed;
            Flying--;
            Landed++;
            return true;
        }

        public void Clear()
        {
            _fates.Clear();
            _cells.Clear();
            Landed = ShotDown = Flying = 0;
        }

        /// <summary>Where a pod is on its arc at a share of its flight: straight from bank to pad, lifted by a parabola.</summary>
        public static Vector3 Arc(Vector3 from, Vector3 to, float height, float t)
        {
            t = Mathf.Clamp01(t);
            return Vector3.Lerp(from, to, t) + Vector3.up * (4f * height * t * (1f - t));
        }
    }
}
