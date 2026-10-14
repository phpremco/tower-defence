using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>All live targets of one level. Replaces Find calls: towers query this instead.</summary>
    public sealed class TargetRegistry
    {
        private readonly List<ITargetable> _targets = new List<ITargetable>();

        public int Count => _targets.Count;

        public void Add(ITargetable target) => _targets.Add(target);

        public void Remove(ITargetable target) => _targets.Remove(target);

        /// <summary>Returns the target in range that is furthest along its path, or null.</summary>
        public ITargetable FindBest(Vector3 origin, float range, TargetLayers mask)
        {
            ITargetable best = null;
            float bestProgress = float.NegativeInfinity;
            float rangeSqr = range * range;

            for (int i = 0; i < _targets.Count; i++)
            {
                ITargetable candidate = _targets[i];
                if (!candidate.IsAlive || (candidate.Layer & mask) == 0)
                {
                    continue;
                }

                if (FlatDistanceSqr(origin, candidate.Position) > rangeSqr || candidate.PathProgress <= bestProgress)
                {
                    continue;
                }

                best = candidate;
                bestProgress = candidate.PathProgress;
            }

            return best;
        }

        /// <summary>Fills results with every live target within radius. The caller owns and reuses the list.</summary>
        public void FindInRadius(Vector3 origin, float radius, TargetLayers mask, List<ITargetable> results)
        {
            results.Clear();
            float radiusSqr = radius * radius;

            for (int i = 0; i < _targets.Count; i++)
            {
                ITargetable candidate = _targets[i];
                if (candidate.IsAlive && (candidate.Layer & mask) != 0 && FlatDistanceSqr(origin, candidate.Position) <= radiusSqr)
                {
                    results.Add(candidate);
                }
            }
        }

        // Height is ignored: the game plays on a plane and air units only differ in layer.
        private static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
