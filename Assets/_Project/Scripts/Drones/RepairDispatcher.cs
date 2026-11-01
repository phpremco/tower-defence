using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// Hands repair jobs to repair drones so they work like a crew instead of a swarm. Every damaged structure is
    /// scored by how badly it needs help against how long the drone would fly to reach it, each structure is
    /// claimed by one drone at a time, and a drone keeps its job until the structure is whole unless something
    /// far more urgent appears. One dispatcher per level, shared by every repair drone. Pure logic.
    /// </summary>
    public sealed class RepairDispatcher
    {
        /// <summary>Structures with this share of health or less count as critical and pull drones off other work.</summary>
        public const float CriticalFraction = 0.3f;

        /// <summary>A new job must beat the current one by this factor before a drone abandons work in progress.</summary>
        public const float SwitchFactor = 2.2f;

        private const float CriticalWeight = 3f;
        private const float BlockerWeight = 1.6f;
        private const float TravelWeight = 1f;

        private readonly StructureRegistry _structures;
        private readonly Dictionary<IStructure, object> _claims = new Dictionary<IStructure, object>();

        public RepairDispatcher(StructureRegistry structures)
        {
            _structures = structures;
        }

        /// <summary>
        /// The structure the worker should repair next: its current job while that still needs work, or the best
        /// unclaimed damaged structure, scored by need over travel time. Null when nothing needs repair.
        /// </summary>
        public IStructure Assign(object worker, Vector3 position, float speed, IStructure current)
        {
            if (current != null && !NeedsWork(current))
            {
                Release(worker, current);
                current = null;
            }

            float currentScore = current != null ? Score(current, position, speed) : 0f;
            IStructure best = null;
            float bestScore = 0f;
            for (int i = 0; i < _structures.Count; i++)
            {
                IStructure candidate = _structures[i];
                if (candidate == current || !NeedsWork(candidate) || IsClaimedByOther(candidate, worker))
                {
                    continue;
                }

                float score = Score(candidate, position, speed);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (current != null)
            {
                // Finish what is under way, unless the newcomer is critical and clearly more pressing.
                bool pressing = best != null && best.HealthFraction <= CriticalFraction && bestScore > currentScore * SwitchFactor;
                if (!pressing || current.HealthFraction <= CriticalFraction)
                {
                    return current;
                }

                Release(worker, current);
            }

            if (best != null)
            {
                _claims[best] = worker;
            }

            return best;
        }

        /// <summary>Gives a job back, when the drone is lost or done.</summary>
        public void Release(object worker, IStructure structure)
        {
            if (structure != null && _claims.TryGetValue(structure, out object owner) && owner == worker)
            {
                _claims.Remove(structure);
            }
        }

        public bool IsClaimed(IStructure structure) => structure != null && _claims.ContainsKey(structure);

        private bool IsClaimedByOther(IStructure structure, object worker)
        {
            return _claims.TryGetValue(structure, out object owner) && owner != worker;
        }

        private static bool NeedsWork(IStructure structure) => structure.IsAlive && structure.HealthFraction < 1f;

        // Need grows with the missing share of health, jumps for critical structures and for gates and the base,
        // and is divided by the seconds of flight so nearby work wins over a far one that is only slightly worse.
        private static float Score(IStructure structure, Vector3 position, float speed)
        {
            float missing = 1f - structure.HealthFraction;
            float need = missing * (structure.HealthFraction <= CriticalFraction ? CriticalWeight : 1f) * (structure.BlocksGround ? BlockerWeight : 1f);
            Vector3 delta = structure.Position - position;
            delta.y = 0f;
            float seconds = delta.magnitude / Mathf.Max(0.1f, speed);
            return need / (1f + seconds * TravelWeight);
        }
    }
}
