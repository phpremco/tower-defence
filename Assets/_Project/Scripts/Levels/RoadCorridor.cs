using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The road as cells and lines, derived once from the level data. The waypoints of a road path are its first
    /// row; a road wider than one cell adds rows to the right of the direction of travel, and plaza cells join the
    /// set as open road. Enemies walk the centre line of the corridor, gates span it, and everything that asks
    /// "is this road" or "which way does the road run here" reads from this one object, so the picture on the
    /// ground, the build grid and the routes can never disagree.
    /// </summary>
    public sealed class RoadCorridor
    {
        /// <summary>How far the painted road stays inside the edge of its cells, so a build pad never touches it.</summary>
        public const float EdgeInsetCells = 0.1f;

        private readonly struct Segment
        {
            public readonly int Path;
            public readonly int Row;
            public readonly int Next;
            public readonly Vector2Int Step;

            public Segment(int path, int row, int next, Vector2Int step)
            {
                Path = path;
                Row = row;
                Next = next;
                Step = step;
            }
        }

        private readonly GridMap _grid;
        private readonly EnemyPath[] _paths;
        private readonly int _width;
        private readonly HashSet<Vector2Int> _cells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _plaza = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, Segment> _segmentAt = new Dictionary<Vector2Int, Segment>();
        private readonly Vector3[][] _routes;

        public RoadCorridor(LevelDefinition level) : this(level.Grid, level.Paths, level.RoadWidth, level.PlazaCells)
        {
        }

        public RoadCorridor(GridMap grid, EnemyPath[] paths, int width, Vector2Int[] plazaCells)
        {
            _grid = grid;
            _paths = paths;
            _width = Mathf.Max(1, width);

            // First rows of every path before any second row, so a cell that is the first row of one road and a
            // later row of another answers for the road it is the first row of.
            for (int row = 0; row < _width; row++)
            {
                for (int p = 0; p < paths.Length; p++)
                {
                    if (!paths[p].Flying && paths[p].Waypoints != null && paths[p].Waypoints.Length >= 2)
                    {
                        AddRow(p, row, ShiftCells(paths[p].Waypoints, row));
                    }
                }
            }

            if (plazaCells != null)
            {
                foreach (Vector2Int cell in plazaCells)
                {
                    _plaza.Add(cell);
                    _cells.Add(cell);
                }
            }

            _routes = new Vector3[paths.Length][];
            for (int p = 0; p < paths.Length; p++)
            {
                _routes[p] = BuildRoute(p);
            }
        }

        /// <summary>Width of every road in cells.</summary>
        public int Width => _width;

        /// <summary>Half the width of the painted road, in cells: the corridor less the inset on either side.</summary>
        public float HalfWidthCells => _width * 0.5f - EdgeInsetCells;

        /// <summary>Every road cell: all rows of every road path and the plaza cells.</summary>
        public IReadOnlyCollection<Vector2Int> Cells => _cells;

        public IReadOnlyCollection<Vector2Int> PlazaCells => _plaza;

        public bool IsRoad(Vector2Int cell) => _cells.Contains(cell);

        public bool IsPlaza(Vector2Int cell) => _plaza.Contains(cell);

        /// <summary>Which row of its road a cell is (0 for the first row), or -1 when it is not on a road path.</summary>
        public int RowOf(Vector2Int cell) => _segmentAt.TryGetValue(cell, out Segment segment) ? segment.Row : -1;

        /// <summary>Unit direction of travel on the ground plane through a road cell, or forward off the road.</summary>
        public Vector3 DirectionAt(Vector2Int cell) =>
            _segmentAt.TryGetValue(cell, out Segment segment) ? new Vector3(segment.Step.x, 0f, segment.Step.y) : Vector3.forward;

        /// <summary>World position on the centre line of the corridor, level with the given road cell.</summary>
        public Vector3 CentreOf(Vector2Int cell)
        {
            Vector3 world = _grid.CellToWorld(cell);
            if (!_segmentAt.TryGetValue(cell, out Segment segment))
            {
                return world;
            }

            Vector2Int right = RightOf(segment.Step);
            float rows = (_width - 1) * 0.5f - segment.Row;
            return world + new Vector3(right.x, 0f, right.y) * (rows * _grid.CellSize);
        }

        /// <summary>The cells across the corridor through a road cell, first row first.</summary>
        public Vector2Int[] Rung(Vector2Int cell)
        {
            if (!_segmentAt.TryGetValue(cell, out Segment segment))
            {
                return new[] { cell };
            }

            Vector2Int right = RightOf(segment.Step);
            var rung = new Vector2Int[_width];
            for (int row = 0; row < _width; row++)
            {
                rung[row] = cell + right * (row - segment.Row);
            }

            return rung;
        }

        /// <summary>True when the other cell lies on the same rung across the corridor as the given road cell.</summary>
        public bool RungContains(Vector2Int cell, Vector2Int other)
        {
            if (cell == other)
            {
                return true;
            }

            if (!_segmentAt.TryGetValue(cell, out Segment segment))
            {
                return false;
            }

            Vector2Int right = RightOf(segment.Step);
            for (int row = 0; row < _width; row++)
            {
                if (cell + right * (row - segment.Row) == other)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The line enemies follow along a path, in world space: the centre of the corridor for a road, the plain
        /// waypoints for a flying route. A flying route that ends on the base cell ends at the base itself.
        /// </summary>
        public Vector3[] Route(int pathIndex) => _routes[pathIndex];

        /// <summary>
        /// Finds the route a rolling path object placed on a road cell should follow and the waypoint it heads for
        /// first. On a plaza the nearest route is used. False off the road.
        /// </summary>
        public bool TryLocate(Vector2Int cell, out Vector3[] route, out int nextWaypoint)
        {
            if (_segmentAt.TryGetValue(cell, out Segment segment))
            {
                route = _routes[segment.Path];
                nextWaypoint = segment.Next;
                return true;
            }

            route = null;
            nextWaypoint = 0;
            return _plaza.Contains(cell) && TryLocateNearest(_grid.CellToWorld(cell), out route, out nextWaypoint);
        }

        /// <summary>
        /// The waypoints shifted a whole number of cells to the right of the direction of travel. Corners are
        /// mitred, so the shifted corner of a right-angle turn lies on both shifted legs and the outer corner of the
        /// corridor is filled.
        /// </summary>
        public static Vector2Int[] ShiftCells(Vector2Int[] waypoints, int distance)
        {
            var result = new Vector2Int[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector2Int incoming = i > 0 ? Direction(waypoints[i - 1], waypoints[i]) : Vector2Int.zero;
                Vector2Int outgoing = i < waypoints.Length - 1 ? Direction(waypoints[i], waypoints[i + 1]) : Vector2Int.zero;
                if (incoming == Vector2Int.zero)
                {
                    incoming = outgoing;
                }

                if (outgoing == Vector2Int.zero)
                {
                    outgoing = incoming;
                }

                Vector2Int normalIn = RightOf(incoming);
                Vector2Int normalOut = RightOf(outgoing);
                Vector2Int shift = normalIn == normalOut || normalIn == -normalOut ? normalIn : normalIn + normalOut;
                result[i] = waypoints[i] + shift * distance;
            }

            return result;
        }

        private void AddRow(int path, int row, Vector2Int[] waypoints)
        {
            for (int i = 1; i < waypoints.Length; i++)
            {
                Vector2Int from = waypoints[i - 1];
                Vector2Int to = waypoints[i];
                Vector2Int step = Direction(from, to);
                var segment = new Segment(path, row, i, step);
                if (i == 1)
                {
                    Claim(from, segment);
                }

                if (from.x != to.x && from.y != to.y)
                {
                    Claim(to, segment);
                    continue;
                }

                Vector2Int cell = from;
                while (cell != to)
                {
                    cell += step;
                    Claim(cell, segment);
                }
            }
        }

        private void Claim(Vector2Int cell, Segment segment)
        {
            _cells.Add(cell);
            if (!_segmentAt.ContainsKey(cell))
            {
                _segmentAt[cell] = segment;
            }
        }

        private Vector3[] BuildRoute(int pathIndex)
        {
            EnemyPath path = _paths[pathIndex];
            if (path.Waypoints == null)
            {
                return new Vector3[0];
            }

            var world = new Vector3[path.Waypoints.Length];
            for (int i = 0; i < world.Length; i++)
            {
                world[i] = _grid.CellToWorld(path.Waypoints[i]);
            }

            if (!path.Flying)
            {
                return PathOffset.Shift(world, (_width - 1) * 0.5f * _grid.CellSize);
            }

            Vector2Int baseCell = BaseCell();
            if (world.Length > 0 && path.Waypoints[world.Length - 1] == baseCell && _segmentAt.ContainsKey(baseCell))
            {
                world[world.Length - 1] = CentreOf(baseCell);
            }

            return world;
        }

        private Vector2Int BaseCell()
        {
            for (int p = 0; p < _paths.Length; p++)
            {
                Vector2Int[] waypoints = _paths[p].Waypoints;
                if (!_paths[p].Flying && waypoints != null && waypoints.Length > 0)
                {
                    return waypoints[waypoints.Length - 1];
                }
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private bool TryLocateNearest(Vector3 position, out Vector3[] route, out int nextWaypoint)
        {
            route = null;
            nextWaypoint = 0;
            float best = float.MaxValue;
            for (int p = 0; p < _paths.Length; p++)
            {
                if (_paths[p].Flying)
                {
                    continue;
                }

                Vector3[] candidate = _routes[p];
                for (int i = 1; i < candidate.Length; i++)
                {
                    float distance = DistanceToSegment(position, candidate[i - 1], candidate[i]);
                    if (distance < best)
                    {
                        best = distance;
                        route = candidate;
                        nextWaypoint = i;
                    }
                }
            }

            return route != null;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSqr) : 0f;
            return Vector3.Distance(point, a + ab * t);
        }

        private static Vector2Int Direction(Vector2Int from, Vector2Int to) =>
            new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));

        // Facing east (+x) the right hand points south (-y): the same handedness as PathOffset on the ground plane.
        private static Vector2Int RightOf(Vector2Int step) => new Vector2Int(step.y, -step.x);
    }
}
