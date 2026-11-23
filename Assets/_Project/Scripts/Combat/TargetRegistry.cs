using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>All live targets of one level. Replaces Find calls: towers query this instead.</summary>
    public sealed class TargetRegistry
    {
        private readonly List<ITargetable> _targets = new List<ITargetable>();

        // Targets hidden under the ground or a liquid: no tower aims at them, but a blast that lands on them still reaches them.
        private readonly HashSet<ITargetable> _concealed = new HashSet<ITargetable>();

        public int Count => _targets.Count;

        public ITargetable this[int index] => _targets[index];

        public void Add(ITargetable target) => _targets.Add(target);

        public void Remove(ITargetable target)
        {
            _targets.Remove(target);
            _concealed.Remove(target);
        }

        /// <summary>Hides a target from aiming (a boss under a pool) or shows it again; splash still finds it.</summary>
        public void SetConcealed(ITargetable target, bool concealed)
        {
            if (concealed)
            {
                _concealed.Add(target);
            }
            else
            {
                _concealed.Remove(target);
            }
        }

        public bool IsConcealed(ITargetable target) => _concealed.Contains(target);

        /// <summary>Returns the target in range with the least road left to its goal (the threat nearest the base), or null.</summary>
        public ITargetable FindBest(Vector3 origin, float range, TargetLayers mask) => FindBest(origin, range, mask, null, 0);

        /// <summary>
        /// As <see cref="FindBest(Vector3, float, TargetLayers)"/>, passing over the first <paramref name="exceptCount"/> entries of
        /// <paramref name="except"/>: creatures the caller has already dealt with this frame (a beam's pulse is on its way to them).
        /// </summary>
        public ITargetable FindBest(Vector3 origin, float range, TargetLayers mask, ITargetable[] except, int exceptCount)
        {
            ITargetable best = null;
            float bestLeft = float.PositiveInfinity;
            float rangeSqr = range * range;

            for (int i = 0; i < _targets.Count; i++)
            {
                ITargetable candidate = _targets[i];
                if (!candidate.IsAlive || !mask.CanHit(candidate.Layer) || _concealed.Contains(candidate))
                {
                    continue;
                }

                if (FlatDistanceSqr(origin, candidate.Position) > rangeSqr || candidate.DistanceToGoal >= bestLeft || Excepted(candidate, except, exceptCount))
                {
                    continue;
                }

                best = candidate;
                bestLeft = candidate.DistanceToGoal;
            }

            return best;
        }

        private static bool Excepted(ITargetable candidate, ITargetable[] except, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(except[i], candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Fills results with every live target within radius. The caller owns and reuses the list.</summary>
        public void FindInRadius(Vector3 origin, float radius, TargetLayers mask, List<ITargetable> results)
        {
            results.Clear();
            float radiusSqr = radius * radius;

            for (int i = 0; i < _targets.Count; i++)
            {
                ITargetable candidate = _targets[i];
                if (candidate.IsAlive && mask.CanHit(candidate.Layer) && FlatDistanceSqr(origin, candidate.Position) <= radiusSqr)
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
