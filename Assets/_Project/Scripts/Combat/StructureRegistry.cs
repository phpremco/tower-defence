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
