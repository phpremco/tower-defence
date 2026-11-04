using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One pool of a level as the eye sees it: its connected liquid cells, the smoothed shore loops round them
    /// (<see cref="LiquidContour"/>) and the signed distance to that shore (positive in the liquid). The terrain digs its
    /// basin from this distance, the surface mesh and the shore foam follow it and the bank is swept along the loops, so
    /// they all agree. Rifts are fissures, not pools, and keep their cell outline.
    /// </summary>
    public sealed class LiquidShape
    {
        /// <summary>How far from the shore the distance is measured exactly; beyond, it is this value with its sign.</summary>
        public const float Reach = 1.2f;

        private const float BucketSize = 0.5f;
        private static readonly Vector2Int[] Steps = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

        private readonly HashSet<Vector2Int> _cells;
        private readonly GridMap _grid;
        // True for a pool known by its shore alone (no cells, no grid).
        private readonly bool _outlined;
        private readonly List<Vector2> _starts = new List<Vector2>();
        private readonly List<Vector2> _ends = new List<Vector2>();
        private readonly List<Vector2> _startNormals = new List<Vector2>();
        private readonly List<Vector2> _endNormals = new List<Vector2>();
        private readonly Dictionary<Vector2Int, List<int>> _buckets = new Dictionary<Vector2Int, List<int>>();

        public LiquidShape(TerrainKind kind, ICollection<Vector2Int> cells, GridMap grid, int seed)
        {
            Kind = kind;
            _grid = grid;
            _cells = new HashSet<Vector2Int>(cells);
            Loops = LiquidContour.Build(_cells, grid, seed);
            Bounds = LoopBounds(Loops);
            Index();
        }

        /// <summary>
        /// A pool given by its shore alone (the menu island's basins, cut in Blender): it has no cells, so a point beyond
        /// the measured reach is signed by the outer loop. Loops run counter-clockwise (liquid on the left).
        /// </summary>
        public LiquidShape(TerrainKind kind, IReadOnlyList<Vector2[]> loops)
        {
            Kind = kind;
            _cells = new HashSet<Vector2Int>();
            _outlined = true;
            Loops = loops;
            Bounds = LoopBounds(Loops);
            Index();
        }

        public TerrainKind Kind { get; }

        public IReadOnlyCollection<Vector2Int> Cells => _cells;

        /// <summary>The smoothed shore: the outer loop first (liquid on the left, counter-clockwise), then the islands.</summary>
        public IReadOnlyList<Vector2[]> Loops { get; }

        /// <summary>The loops' extent in world x/z (Rect y is world z).</summary>
        public Rect Bounds { get; }

        /// <summary>Every pool of a level that shows as a liquid sheet (all liquids but rifts), 4-connected per kind.</summary>
        public static List<LiquidShape> ForLevel(LevelDefinition level)
        {
            var shapes = new List<LiquidShape>();
            TerrainMap map = level.Terrain;
            var seen = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> entry in map.Cells)
            {
                if (!IsPool(entry.Value) || seen.Contains(entry.Key))
                {
                    continue;
                }

                List<Vector2Int> pool = Flood(map, entry.Key, entry.Value, seen);
                shapes.Add(new LiquidShape(entry.Value, pool, level.Grid, Seed(pool)));
            }

            return shapes;
        }

        /// <summary>A liquid that lies in a pool with a shore (not a rift).</summary>
        public static bool IsPool(TerrainKind kind) => TerrainMap.IsLiquid(kind) && kind != TerrainKind.Rift;

        /// <summary>
        /// The distance from a point to the smoothed shore, positive in the liquid, clamped to <see cref="Reach"/> (and
        /// signed by the cells there, which the shore never strays far from).
        /// </summary>
        public float SignedDistance(float x, float z)
        {
            var point = new Vector2(x, z);
            if (!_buckets.TryGetValue(BucketOf(point), out List<int> segments))
            {
                return InCells(x, z) ? Reach : -Reach;
            }

            float best = float.MaxValue;
            float sign = 1f;
            for (int i = 0; i < segments.Count; i++)
            {
                int s = segments[i];
                Vector2 a = _starts[s];
                Vector2 ab = _ends[s] - a;
                float lengthSqr = ab.sqrMagnitude;
                float t = lengthSqr > 1e-10f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSqr) : 0f;
                Vector2 nearest = a + ab * t;
                float sqr = (point - nearest).sqrMagnitude;
                if (sqr >= best)
                {
                    continue;
                }

                best = sqr;
                // Inside the segment the side decides; at an end the normal shared with the next segment does, so a
                // point off a bend is never signed by the wrong segment.
                Vector2 outward = t <= 0f ? _startNormals[s] : t >= 1f ? _endNormals[s] : new Vector2(ab.y, -ab.x);
                sign = Vector2.Dot(point - nearest, outward) > 0f ? -1f : 1f;
            }

            if (best == float.MaxValue)
            {
                return InCells(x, z) ? Reach : -Reach;
            }

            return Mathf.Clamp(sign * Mathf.Sqrt(best), -Reach, Reach);
        }

        private bool InCells(float x, float z) => !_outlined ? _cells.Contains(_grid.WorldToCell(new Vector3(x, 0f, z))) : InsideLoop(Loops[0], new Vector2(x, z));

        // Even-odd crossing test against one loop.
        private static bool InsideLoop(Vector2[] loop, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = loop.Length - 1; i < loop.Length; j = i++)
            {
                Vector2 a = loop[i];
                Vector2 b = loop[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static int Seed(List<Vector2Int> pool)
        {
            Vector2Int first = pool[0];
            foreach (Vector2Int cell in pool)
            {
                if (cell.x < first.x || (cell.x == first.x && cell.y < first.y))
                {
                    first = cell;
                }
            }

            return first.x * 7919 + first.y * 104729 + pool.Count;
        }

        private static List<Vector2Int> Flood(TerrainMap map, Vector2Int start, TerrainKind kind, HashSet<Vector2Int> seen)
        {
            var pool = new List<Vector2Int>();
            var open = new Stack<Vector2Int>();
            open.Push(start);
            seen.Add(start);
            while (open.Count > 0)
            {
                Vector2Int cell = open.Pop();
                pool.Add(cell);
                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (!seen.Contains(next) && map.KindAt(next) == kind)
                    {
                        seen.Add(next);
                        open.Push(next);
                    }
                }
            }

            return pool;
        }

        private static Rect LoopBounds(IReadOnlyList<Vector2[]> loops)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector2[] loop in loops)
            {
                foreach (Vector2 point in loop)
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Every segment goes into each bucket within reach of it, so a query only looks at its own bucket.
        private void Index()
        {
            foreach (Vector2[] loop in Loops)
            {
                int count = loop.Length;
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = loop[i];
                    Vector2 b = loop[(i + 1) % count];
                    int index = _starts.Count;
                    _starts.Add(a);
                    _ends.Add(b);
                    _startNormals.Add(LiquidContour.Outward(loop, i));
                    _endNormals.Add(LiquidContour.Outward(loop, (i + 1) % count));
                    Vector2 low = Vector2.Min(a, b) - Vector2.one * Reach;
                    Vector2 high = Vector2.Max(a, b) + Vector2.one * Reach;
                    Vector2Int from = BucketOf(low);
                    Vector2Int to = BucketOf(high);
                    for (int bx = from.x; bx <= to.x; bx++)
                    {
                        for (int by = from.y; by <= to.y; by++)
                        {
                            var key = new Vector2Int(bx, by);
                            if (!_buckets.TryGetValue(key, out List<int> list))
                            {
                                list = new List<int>();
                                _buckets[key] = list;
                            }

                            list.Add(index);
                        }
                    }
                }
            }
        }

        private static Vector2Int BucketOf(Vector2 point) => new Vector2Int(Mathf.FloorToInt(point.x / BucketSize), Mathf.FloorToInt(point.y / BucketSize));
    }
}
