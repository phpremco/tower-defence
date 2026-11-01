using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>All attackable structures of one level.</summary>
    public sealed class StructureRegistry
    {
        private readonly List<IStructure> _structures = new List<IStructure>();

        public void Add(IStructure structure) => _structures.Add(structure);

        public void Remove(IStructure structure) => _structures.Remove(structure);

        public int Count => _structures.Count;

        public IStructure this[int index] => _structures[index];

        /// <summary>Returns the living structure with the lowest health share below full, or null when all are healthy.</summary>
        public IStructure FindMostDamaged()
        {
            IStructure worst = null;
            float lowest = 1f;

            for (int i = 0; i < _structures.Count; i++)
            {
                IStructure candidate = _structures[i];
                if (candidate.IsAlive && candidate.HealthFraction < lowest)
                {
                    worst = candidate;
                    lowest = candidate.HealthFraction;
                }
            }

            return worst;
        }

        /// <summary>Adds up the boosts of every support structure whose radius reaches the position.</summary>
        public void SupportAt(Vector3 position, out float rangeBonus, out float rateBonus)
        {
            rangeBonus = 0f;
            rateBonus = 0f;

            for (int i = 0; i < _structures.Count; i++)
            {
                if (!(_structures[i] is ISupportSource source) || !source.IsAlive || source.SupportRadius <= 0f)
                {
                    continue;
                }

                float dx = source.Position.x - position.x;
                float dz = source.Position.z - position.z;
                if (dx * dx + dz * dz <= source.SupportRadius * source.SupportRadius)
                {
                    rangeBonus += source.RangeBonus;
                    rateBonus += source.RateBonus;
                }
            }
        }

        /// <summary>Returns the nearest living blockade within the stopping distance, or null.</summary>
        public IStructure FindBlocker(Vector3 origin, float distance)
        {
            IStructure best = null;
            float bestSqr = distance * distance;

            for (int i = 0; i < _structures.Count; i++)
            {
                IStructure candidate = _structures[i];
                if (!candidate.BlocksGround)
                {
                    continue;
                }

                float dx = candidate.Position.x - origin.x;
                float dz = candidate.Position.z - origin.z;
                float sqr = dx * dx + dz * dz;
                if (sqr <= bestSqr)
                {
                    best = candidate;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        /// <summary>Damages every living structure within a radius and returns how many were hit.</summary>
        public int DamageInRadius(Vector3 origin, float radius, float damage)
        {
            int hits = 0;
            float radiusSqr = radius * radius;
            for (int i = _structures.Count - 1; i >= 0; i--)
            {
                IStructure candidate = _structures[i];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                float dx = candidate.Position.x - origin.x;
                float dz = candidate.Position.z - origin.z;
                if (dx * dx + dz * dz <= radiusSqr)
                {
                    candidate.TakeDamage(damage);
                    hits++;
                }
            }

            return hits;
        }

        /// <summary>Fills results with the nearest living structures within range, closest first, at most count of them.</summary>
        public void FindNearest(Vector3 origin, float range, int count, List<IStructure> results)
        {
            results.Clear();
            float rangeSqr = range * range;
            for (int i = 0; i < _structures.Count; i++)
            {
                IStructure candidate = _structures[i];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                float dx = candidate.Position.x - origin.x;
                float dz = candidate.Position.z - origin.z;
                float sqr = dx * dx + dz * dz;
                if (sqr > rangeSqr)
                {
                    continue;
                }

                // Insertion keeps the short list sorted; a volley never asks for more than a handful.
                int at = results.Count;
                while (at > 0 && DistanceSqr(results[at - 1], origin) > sqr)
                {
                    at--;
                }

                results.Insert(at, candidate);
                if (results.Count > count)
                {
                    results.RemoveAt(results.Count - 1);
                }
            }
        }

        private static float DistanceSqr(IStructure structure, Vector3 origin)
        {
            float dx = structure.Position.x - origin.x;
            float dz = structure.Position.z - origin.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Returns the nearest living structure within range, or null.</summary>
        public IStructure FindNearest(Vector3 origin, float range)
        {
            IStructure best = null;
            float bestSqr = range * range;

            for (int i = 0; i < _structures.Count; i++)
            {
                IStructure candidate = _structures[i];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                float dx = candidate.Position.x - origin.x;
                float dz = candidate.Position.z - origin.z;
                float sqr = dx * dx + dz * dz;
                if (sqr <= bestSqr)
                {
                    best = candidate;
                    bestSqr = sqr;
                }
            }

            return best;
        }
    }
}
