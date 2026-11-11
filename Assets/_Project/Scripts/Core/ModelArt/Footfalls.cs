using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Round 44 (Remco: "footsteps play while Spencer is not walking"): when a clip's feet touch the ground. A footfall is the
    /// moment a foot that has swung through the air comes to rest: its speed drops under the planted speed after it travelled
    /// at least a stride. A weight shift on the spot moves a foot only a little, so a standing clip has none. The baker samples
    /// the foot bones of a clip into positions; the cutscene plays a step only when its clock crosses one of the moments.
    /// </summary>
    public static class Footfalls
    {
        /// <summary>A foot slower than this (metres per second, model units) stands on the ground.</summary>
        public const float PlantedSpeed = 0.35f;

        /// <summary>A foot must travel at least this far between two plants for the second to count as a step.</summary>
        public const float MinStride = 0.08f;

        /// <summary>Two footfalls closer than this (seconds) are one (both feet landing together count once).</summary>
        public const float MergeSeconds = 0.08f;

        /// <summary>
        /// The footfalls of a clip from its feet's positions, one sample per frame at <paramref name="framesPerSecond"/>, in the
        /// model's own space and with any travel of an in-place cycle added back (so a planted foot stands still). A looping
        /// clip wraps round, so a step across its seam is found once; <paramref name="loopTravel"/> is how far the added travel
        /// carries a foot over one loop, so the seam does not read as a jump.
        /// </summary>
        public static float[] Detect(IReadOnlyList<Vector3> left, IReadOnlyList<Vector3> right, float framesPerSecond, bool loop, Vector3 loopTravel = default)
        {
            var moments = new List<float>();
            AddFoot(left, framesPerSecond, loop, loopTravel, moments);
            AddFoot(right, framesPerSecond, loop, loopTravel, moments);
            moments.Sort();
            var merged = new List<float>();
            foreach (float moment in moments)
            {
                if (merged.Count == 0 || moment - merged[merged.Count - 1] > MergeSeconds)
                {
                    merged.Add(moment);
                }
            }

            return merged.ToArray();
        }

        /// <summary>
        /// True when a footfall lies in (from, to] of a clip's clock (seconds unwrapped for a loop, so a step at the seam is
        /// caught; a one-off clip holds its last frame, so nothing lies past its length).
        /// </summary>
        public static bool Crossed(float[] moments, float length, bool loop, float from, float to)
        {
            if (moments == null || moments.Length == 0 || to <= from)
            {
                return false;
            }

            if (!loop || length <= 0f)
            {
                foreach (float moment in moments)
                {
                    if (moment > from && moment <= to)
                    {
                        return true;
                    }
                }

                return false;
            }

            float cycles = Mathf.Floor(from / length);
            foreach (float moment in moments)
            {
                for (float at = cycles * length + moment; at <= to; at += length)
                {
                    if (at > from)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AddFoot(IReadOnlyList<Vector3> foot, float framesPerSecond, bool loop, Vector3 loopTravel, List<float> moments)
        {
            int count = foot != null ? foot.Count : 0;
            if (count < 3 || framesPerSecond <= 0f)
            {
                return;
            }

            var planted = new bool[count];
            for (int i = 0; i < count; i++)
            {
                planted[i] = Speed(foot, i, framesPerSecond, loop, loopTravel) < PlantedSpeed;
            }

            // Start the walk round the clip at a planted frame, so a loop's first plant is judged by the swing before it.
            int start = 0;
            if (loop)
            {
                while (start < count && !planted[start])
                {
                    start++;
                }

                if (start == count)
                {
                    return;
                }
            }

            float travelled = 0f;
            bool wasPlanted = planted[start];
            // A loop goes once round and back onto its first frame, so a plant there is judged by the swing that ends the cycle.
            int steps = loop ? count + 1 : count - start;
            for (int k = 1; k < steps; k++)
            {
                int i = (start + k) % count;
                travelled += Vector3.Distance(At(foot, start + k, loopTravel), At(foot, start + k - 1, loopTravel));
                if (planted[i] && !wasPlanted && travelled >= MinStride)
                {
                    moments.Add(i / framesPerSecond);
                }

                if (planted[i])
                {
                    travelled = 0f;
                }

                wasPlanted = planted[i];
            }
        }

        // Central difference; a one-off clip uses one side at its ends.
        private static float Speed(IReadOnlyList<Vector3> foot, int i, float framesPerSecond, bool loop, Vector3 loopTravel)
        {
            int count = foot.Count;
            int before = loop ? i - 1 : Mathf.Max(0, i - 1);
            int after = loop ? i + 1 : Mathf.Min(count - 1, i + 1);
            float frames = after - before;
            return frames > 0f ? Vector3.Distance(At(foot, after, loopTravel), At(foot, before, loopTravel)) * framesPerSecond / frames : 0f;
        }

        // A sample by an index that may run past either end of a loop: the frame of the next or last cycle, moved by its travel.
        private static Vector3 At(IReadOnlyList<Vector3> foot, int index, Vector3 loopTravel)
        {
            int count = foot.Count;
            int cycles = Mathf.FloorToInt(index / (float)count);
            return foot[index - cycles * count] + loopTravel * cycles;
        }
    }
}
