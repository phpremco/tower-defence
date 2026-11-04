using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The round outline of a pool (ArtSource README round 24b): the cell-union outline as closed loops with the liquid
    /// on the left, resampled densely, smoothed with a circular Gaussian so no square corner or straight cell run is left,
    /// and given a faint periodic wobble so two pools never share a shape. Gameplay stays on the cells; only the look
    /// follows this line. Pure maths on world x/z (a <see cref="Vector2"/>'s y is world z).
    /// </summary>
    public static class LiquidContour
    {
        /// <summary>Spacing of the dense resample the smoothing runs on.</summary>
        public const float SampleSpacing = 0.02f;

        /// <summary>Width of the smoothing along the loop, world units (README: 0.55).</summary>
        public const float SmoothSigma = 0.55f;

        /// <summary>Spacing of the finished line.</summary>
        public const float OutputSpacing = 0.05f;

        /// <summary>Largest sideways wobble of the finished line (README: 0.04).</summary>
        public const float WobbleAmplitude = 0.04f;

        private const int FirstHarmonic = 2;
        private const int Harmonics = 4;
        private const float KernelSigmas = 3f;

        private static readonly Vector2Int[] CellSides = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };

        /// <summary>Every smoothed loop of a pool: its outer shore first (counter-clockwise), then any dry islands (clockwise).</summary>
        public static List<Vector2[]> Build(ICollection<Vector2Int> cells, GridMap grid, int seed)
        {
            var loops = new List<Vector2[]>();
            List<Vector2[]> outlines = CellOutlines(cells, grid);
            for (int i = 0; i < outlines.Count; i++)
            {
                loops.Add(Smooth(outlines[i], seed * 31 + i));
            }

            loops.Sort((a, b) => Mathf.Abs(SignedArea(b)).CompareTo(Mathf.Abs(SignedArea(a))));
            return loops;
        }

        /// <summary>
        /// The outline of a union of cells as closed loops of cell corners, the cells on the left of the walk (outer loops
        /// counter-clockwise, holes clockwise). Two cells touching only at a corner stay two loops.
        /// </summary>
        public static List<Vector2[]> CellOutlines(ICollection<Vector2Int> cells, GridMap grid)
        {
            var set = cells as HashSet<Vector2Int> ?? new HashSet<Vector2Int>(cells);
            var outgoing = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (Vector2Int cell in set)
            {
                for (int s = 0; s < CellSides.Length; s++)
                {
                    if (set.Contains(cell + CellSides[s]))
                    {
                        continue;
                    }

                    (Vector2Int from, Vector2Int to) = SideEdge(cell, s);
                    if (!outgoing.TryGetValue(from, out List<Vector2Int> list))
                    {
                        list = new List<Vector2Int>(2);
                        outgoing[from] = list;
                    }

                    list.Add(to);
                }
            }

            var loops = new List<Vector2[]>();
            float half = grid.CellSize * 0.5f;
            while (outgoing.Count > 0)
            {
                Vector2Int start = FirstKey(outgoing);
                var corners = new List<Vector2Int> { start };
                Vector2Int current = start;
                Vector2Int heading = Vector2Int.zero;
                while (true)
                {
                    Vector2Int next = TakeEdge(outgoing, current, heading);
                    heading = next - current;
                    current = next;
                    if (current == start)
                    {
                        break;
                    }

                    corners.Add(current);
                }

                loops.Add(ToWorld(DropStraightCorners(corners), grid, half));
            }

            return loops;
        }

        // A cell's side as a directed edge of corner points, walked counter-clockwise round the cell (the cell on its left).
        private static (Vector2Int From, Vector2Int To) SideEdge(Vector2Int cell, int side)
        {
            int x = cell.x;
            int y = cell.y;
            switch (side)
            {
                case 0: return (new Vector2Int(x, y), new Vector2Int(x + 1, y));
                case 1: return (new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
                case 2: return (new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                default: return (new Vector2Int(x, y + 1), new Vector2Int(x, y));
            }
        }

        // A loop starts on a plain corner: one starting on a corner two cells share diagonally could close on the wrong edge.
        private static Vector2Int FirstKey(Dictionary<Vector2Int, List<Vector2Int>> outgoing)
        {
            Vector2Int any = default;
            foreach (KeyValuePair<Vector2Int, List<Vector2Int>> pair in outgoing)
            {
                if (pair.Value.Count == 1)
                {
                    return pair.Key;
                }

                any = pair.Key;
            }

            return any;
        }

        // At a corner two cells only touch diagonally there are two ways on: the sharpest left turn keeps the cells apart.
        private static Vector2Int TakeEdge(Dictionary<Vector2Int, List<Vector2Int>> outgoing, Vector2Int corner, Vector2Int heading)
        {
            List<Vector2Int> options = outgoing[corner];
            int best = 0;
            int bestTurn = int.MinValue;
            for (int i = 0; i < options.Count; i++)
            {
                Vector2Int direction = options[i] - corner;
                int turn = heading == Vector2Int.zero ? 0 : heading.x * direction.y - heading.y * direction.x;
                if (turn > bestTurn)
                {
                    bestTurn = turn;
                    best = i;
                }
            }

            Vector2Int next = options[best];
            options.RemoveAt(best);
            if (options.Count == 0)
            {
                outgoing.Remove(corner);
            }

            return next;
        }

        private static List<Vector2Int> DropStraightCorners(List<Vector2Int> corners)
        {
            var kept = new List<Vector2Int>(corners.Count);
            int count = corners.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2Int previous = corners[(i + count - 1) % count];
                Vector2Int next = corners[(i + 1) % count];
                Vector2Int a = corners[i] - previous;
                Vector2Int b = next - corners[i];
                if (a.x * b.y - a.y * b.x != 0)
                {
                    kept.Add(corners[i]);
                }
            }

            return kept;
        }

        private static Vector2[] ToWorld(List<Vector2Int> corners, GridMap grid, float half)
        {
            var points = new Vector2[corners.Count];
            for (int i = 0; i < corners.Count; i++)
            {
                Vector3 centre = grid.CellToWorld(corners[i]);
                points[i] = new Vector2(centre.x - half, centre.z - half);
            }

            return points;
        }

        /// <summary>
        /// A closed loop resampled every <see cref="SampleSpacing"/>, smoothed with a circular Gaussian of
        /// <paramref name="sigma"/> world units, resampled every <see cref="OutputSpacing"/> and pushed sideways by a
        /// periodic wobble of a few low harmonics seeded by <paramref name="seed"/>.
        /// </summary>
        public static Vector2[] Smooth(Vector2[] loop, int seed, float sigma = SmoothSigma, float wobble = WobbleAmplitude)
        {
            Vector2[] dense = Resample(loop, SampleSpacing);
            Vector2[] smooth = Gaussian(dense, sigma / Mathf.Max(1e-5f, Perimeter(dense) / dense.Length));
            Vector2[] even = Resample(smooth, OutputSpacing);
            return wobble > 0f ? Wobble(even, seed, wobble) : even;
        }

        /// <summary>The loop as points at equal steps along it, as close to <paramref name="spacing"/> as divides it evenly.</summary>
        public static Vector2[] Resample(Vector2[] loop, float spacing)
        {
            float perimeter = Perimeter(loop);
            int count = Mathf.Max(8, Mathf.CeilToInt(perimeter / spacing));
            float step = perimeter / count;
            var result = new Vector2[count];
            int segment = 0;
            float segmentStart = 0f;
            float segmentLength = Vector2.Distance(loop[0], loop[1 % loop.Length]);
            for (int i = 0; i < count; i++)
            {
                float u = i * step;
                while (segmentStart + segmentLength < u && segment < loop.Length - 1)
                {
                    segmentStart += segmentLength;
                    segment++;
                    segmentLength = Vector2.Distance(loop[segment], loop[(segment + 1) % loop.Length]);
                }

                float t = segmentLength > 1e-6f ? Mathf.Clamp01((u - segmentStart) / segmentLength) : 0f;
                result[i] = Vector2.Lerp(loop[segment], loop[(segment + 1) % loop.Length], t);
            }

            return result;
        }

        // The circular convolution with a Gaussian of sigma samples, its reach capped at half the loop.
        private static Vector2[] Gaussian(Vector2[] points, float sigma)
        {
            int count = points.Length;
            int reach = Mathf.Min(count / 2, Mathf.CeilToInt(sigma * KernelSigmas));
            var weights = new float[reach + 1];
            float total = 0f;
            for (int k = 0; k <= reach; k++)
            {
                weights[k] = Mathf.Exp(-0.5f * k * k / Mathf.Max(1e-5f, sigma * sigma));
                total += k == 0 ? weights[k] : 2f * weights[k];
            }

            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 sum = points[i] * weights[0];
                for (int k = 1; k <= reach; k++)
                {
                    sum += (points[(i + k) % count] + points[(i - k + count) % count]) * weights[k];
                }

                result[i] = sum / total;
            }

            return result;
        }

        // A sum of whole-number harmonics round the loop, so the wobble closes on itself without a step.
        private static Vector2[] Wobble(Vector2[] points, int seed, float amplitude)
        {
            var random = new System.Random(seed);
            var weights = new float[Harmonics];
            var phases = new float[Harmonics];
            float total = 0f;
            for (int h = 0; h < Harmonics; h++)
            {
                weights[h] = 0.4f + (float)random.NextDouble();
                phases[h] = (float)random.NextDouble() * Mathf.PI * 2f;
                total += weights[h];
            }

            int count = points.Length;
            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float u = i / (float)count * Mathf.PI * 2f;
                float offset = 0f;
                for (int h = 0; h < Harmonics; h++)
                {
                    offset += weights[h] * Mathf.Sin(u * (FirstHarmonic + h) + phases[h]);
                }

                result[i] = points[i] + Outward(points, i) * (offset / total * amplitude);
            }

            return result;
        }

        /// <summary>The unit normal at a loop point pointing away from the liquid (to the right of the walk).</summary>
        public static Vector2 Outward(Vector2[] loop, int index)
        {
            int count = loop.Length;
            Vector2 tangent = loop[(index + 1) % count] - loop[(index - 1 + count) % count];
            float length = tangent.magnitude;
            return length > 1e-6f ? new Vector2(tangent.y, -tangent.x) / length : Vector2.zero;
        }

        public static float Perimeter(Vector2[] loop)
        {
            float length = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                length += Vector2.Distance(loop[i], loop[(i + 1) % loop.Length]);
            }

            return length;
        }

        /// <summary>Positive for a counter-clockwise loop (liquid inside), negative for a hole.</summary>
        public static float SignedArea(Vector2[] loop)
        {
            float area = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                Vector2 a = loop[i];
                Vector2 b = loop[(i + 1) % loop.Length];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }
    }
}
