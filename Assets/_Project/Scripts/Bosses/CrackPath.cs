using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The shape of a crack line, planned on the grid (Remco 2026-11-05: "sommige overlappen elkaar"): a cell path is a
    /// staircase whose wide strip folds over itself at every step, so it is pulled straight where the ground allows,
    /// its back-tracking points dropped and its corners rounded; and a crack never lies over another one still on the
    /// field. Pure geometry on flat (y ignored) points.
    /// </summary>
    public static class CrackPath
    {
        // A turn sharper than this (cosine of the angle between legs) folds a wide strip back over itself.
        private const float BacktrackCos = -0.2f;
        private const int SmoothPasses = 2;
        private const float CornerShare = 0.25f;
        private const float SampleStep = 0.5f;
        // Path length of a half turn whose two arms are a distance apart (pi / 2 of the distance, rounded up).
        private const float HalfTurn = 2f;

        /// <summary>
        /// Pulls the path straight: from each kept point it jumps to the farthest later point the straight line to which is
        /// clear. Ends stay where they are.
        /// </summary>
        public static void Straighten(List<Vector3> points, Func<Vector3, Vector3, bool> clear)
        {
            if (points.Count < 3)
            {
                return;
            }

            var kept = new List<Vector3>(points.Count) { points[0] };
            int at = 0;
            while (at < points.Count - 1)
            {
                int next = at + 1;
                for (int j = points.Count - 1; j > at + 1; j--)
                {
                    if (clear(points[at], points[j]))
                    {
                        next = j;
                        break;
                    }
                }

                kept.Add(points[next]);
                at = next;
            }

            points.Clear();
            points.AddRange(kept);
        }

        /// <summary>Drops every inner point where the path turns back on itself (and points on top of each other).</summary>
        public static void RemoveBacktracks(List<Vector3> points, float minLeg)
        {
            bool changed = true;
            while (changed && points.Count > 2)
            {
                changed = false;
                for (int i = 1; i < points.Count - 1; i++)
                {
                    Vector3 a = Flat(points[i] - points[i - 1]);
                    Vector3 b = Flat(points[i + 1] - points[i]);
                    if (a.magnitude < minLeg || b.magnitude < minLeg || Vector3.Dot(a.normalized, b.normalized) < BacktrackCos)
                    {
                        points.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }
        }

        /// <summary>Rounds the corners (Chaikin), each cut at most <paramref name="maxCut"/> long; the ends stay.</summary>
        public static void Smooth(List<Vector3> points, float maxCut)
        {
            for (int pass = 0; pass < SmoothPasses && points.Count > 2; pass++)
            {
                var smooth = new List<Vector3>(points.Count * 2) { points[0] };
                for (int i = 1; i < points.Count - 1; i++)
                {
                    Vector3 corner = points[i];
                    smooth.Add(Cut(corner, points[i - 1], maxCut));
                    smooth.Add(Cut(corner, points[i + 1], maxCut));
                }

                smooth.Add(points[points.Count - 1]);
                points.Clear();
                points.AddRange(smooth);
            }
        }

        private static Vector3 Cut(Vector3 corner, Vector3 towards, float maxCut)
        {
            Vector3 leg = towards - corner;
            float length = Mathf.Min(leg.magnitude * CornerShare, maxCut);
            return leg.sqrMagnitude > 0.0001f ? corner + leg.normalized * length : corner;
        }

        /// <summary>The nearest flat distance between two paths' segments.</summary>
        public static float Distance(IReadOnlyList<Vector3> a, IReadOnlyList<Vector3> b)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i < a.Count - 1; i++)
            {
                for (int j = 0; j < b.Count - 1; j++)
                {
                    best = Mathf.Min(best, SegmentDistance(Flat(a[i]), Flat(a[i + 1]), Flat(b[j]), Flat(b[j + 1])));
                }
            }

            return best;
        }

        /// <summary>
        /// True when the path comes back within <paramref name="distance"/> of itself: two segments that are not neighbours
        /// along it (at least <paramref name="skip"/> metres of path apart) lie that close, so a strip that wide overlaps.
        /// </summary>
        public static bool SelfOverlaps(IReadOnlyList<Vector3> points, float distance, float skip)
        {
            // Sampled along the path: two samples closer than the distance while farther apart along it than a bend that
            // wide could bring them (a half turn) means the strip comes back over itself.
            var samples = new List<Vector3>();
            var along = new List<float>();
            float travelled = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = Flat(points[i]);
                Vector3 b = Flat(points[i + 1]);
                float length = (b - a).magnitude;
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
                for (int s = 0; s < steps; s++)
                {
                    samples.Add(Vector3.Lerp(a, b, s / (float)steps));
                    along.Add(travelled + length * s / steps);
                }

                travelled += length;
            }

            float apart = Mathf.Max(skip, distance * HalfTurn);
            for (int i = 0; i < samples.Count; i++)
            {
                for (int j = i + 1; j < samples.Count; j++)
                {
                    if (along[j] - along[i] > apart && (samples[j] - samples[i]).sqrMagnitude < distance * distance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The cells within a radius of the path (a crack's footprint on the grid).</summary>
        public static void Cover(IReadOnlyList<Vector3> points, float radius, float cellSize, Func<Vector3, Vector2Int> toCell, HashSet<Vector2Int> cells)
        {
            int reach = Mathf.CeilToInt(radius / Mathf.Max(0.01f, cellSize));
            float step = cellSize * 0.5f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float length = Flat(points[i + 1] - points[i]).magnitude;
                int samples = Mathf.Max(1, Mathf.CeilToInt(length / step));
                for (int s = 0; s <= samples; s++)
                {
                    Vector3 at = Vector3.Lerp(points[i], points[i + 1], s / (float)samples);
                    Vector2Int centre = toCell(at);
                    for (int x = -reach; x <= reach; x++)
                    {
                        for (int y = -reach; y <= reach; y++)
                        {
                            if ((x * x + y * y) * cellSize * cellSize <= (radius + cellSize * 0.5f) * (radius + cellSize * 0.5f))
                            {
                                cells.Add(centre + new Vector2Int(x, y));
                            }
                        }
                    }
                }
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            if (Intersect(p1, q1, p2, q2))
            {
                return 0f;
            }

            return Mathf.Min(Mathf.Min(PointSegment(p1, p2, q2), PointSegment(q1, p2, q2)), Mathf.Min(PointSegment(p2, p1, q1), PointSegment(q2, p1, q1)));
        }

        private static float PointSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(p, a + ab * t);
        }

        private static bool Intersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float d1 = Cross(c, d, a);
            float d2 = Cross(c, d, b);
            float d3 = Cross(a, b, c);
            float d4 = Cross(a, b, d);
            return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
        }

        private static float Cross(Vector3 a, Vector3 b, Vector3 p) => (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
    }
}
