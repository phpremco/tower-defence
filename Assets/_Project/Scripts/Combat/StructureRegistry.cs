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
