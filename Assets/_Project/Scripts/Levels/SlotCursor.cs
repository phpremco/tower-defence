using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Moves a cursor between build slots with a stick or d-pad, so no free-roaming pointer is needed.</summary>
    public static class SlotCursor
    {
        /// <summary>Minimum alignment between the stick direction and the direction to a slot (cosine of the angle).</summary>
        private const float MinAlignment = 0.5f;

        /// <summary>Returns the index of the closest slot in the given direction, or current when none qualifies.</summary>
        public static int Next(IReadOnlyList<Vector3> slots, int current, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f || current < 0 || current >= slots.Count)
            {
                return current;
            }

            Vector2 wanted = direction.normalized;
            Vector3 origin = slots[current];
            int best = current;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i == current)
                {
                    continue;
                }

                var delta = new Vector2(slots[i].x - origin.x, slots[i].z - origin.z);
                float distance = delta.magnitude;
                if (distance < 0.0001f || Vector2.Dot(delta / distance, wanted) < MinAlignment)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Returns the slot closest to a world position within maxDistance, or -1.</summary>
        public static int Nearest(IReadOnlyList<Vector3> slots, Vector3 position, float maxDistance)
        {
            int best = -1;
            float bestDistance = maxDistance;

            for (int i = 0; i < slots.Count; i++)
            {
                var delta = new Vector2(slots[i].x - position.x, slots[i].z - position.z);
                float distance = delta.magnitude;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
