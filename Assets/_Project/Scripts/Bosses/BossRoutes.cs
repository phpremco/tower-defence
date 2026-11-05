using System.Collections.Generic;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The routes a boss plans over the climb layer: its walk back to the road (round towers, over rocks, through
    /// fragile decor), the crack line of a burrow (round towers and gates, through quicksand as a shortcut), and the
    /// spot beside a tower where it breaks out with room for its whole body. Pure planning over the field's data.
    /// </summary>
    public static class BossRoutes
    {
        private const float SampleStep = 0.25f;
        private const float OldCrackPenalty = 12f;
        private const float MinCrackLeg = 0.3f;
        private const float MaxCornerCut = 1.2f;
        private const int RoadTries = 6;
        private const float SideOffset = 1f;
        private const float AwayFromRoadPenalty = 16f;
        private const float BesideStructurePenalty = 25f;

        // A spot where the body would lie over another tower loses to any spot where it does not (Docs/BOSSES.md: the field
        // stays consistent), but still beats coming up on the road, so the signature never fails on a dense field.
        private const float BodyOverlapPenalty = 60f;
        private const int StrikeDirections = 16;

        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        /// <summary>
        /// Plans a walk from a point to the nearest road cell it can reach without crossing a footprint; the points are
        /// the start and the centres of the cells after it. False when no road can be reached.
        /// </summary>
        public static bool WalkToRoad(BossField field, Vector3 from, bool climbsRocks, List<Vector3> points)
        {
            points.Clear();
            ClimbLayer layer = field.Climb;
            GridMap grid = field.Grid;
            Vector2Int start = Clamp(grid, grid.WorldToCell(from));
            if (field.Level.Road.IsRoad(start))
            {
                points.Add(from);
                return true;
            }

            var cells = new List<Vector2Int>();
            foreach (Vector2Int goal in NearestRoadCells(field, from, RoadTries))
            {
                if (GridRouter.TryFind(grid.Size, start, goal, c => layer.WalkCost(c, climbsRocks), cells, false, ClimbLayer.CheapestWalk))
                {
                    points.Add(from);
                    for (int i = 1; i < cells.Count; i++)
                    {
                        points.Add(grid.CellToWorld(cells[i]));
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Plans a crack line from where the boss starts travelling under the ground to the hole it breaks out of: the
        /// points start at <paramref name="travelStart"/> and end on the hole. False when no line reaches it.
        /// </summary>
        /// <param name="avoid">Cells under cracks still on the field: the line keeps off them where it can (never closed).</param>
        public static bool Crack(BossField field, Vector3 travelStart, Vector3 hole, float quicksandFactor, List<Vector3> points, HashSet<Vector2Int> avoid = null)
        {
            points.Clear();
            ClimbLayer layer = field.Climb;
            GridMap grid = field.Grid;
            var cells = new List<Vector2Int>();
            Vector2Int start = Clamp(grid, grid.WorldToCell(travelStart));
            System.Func<Vector2Int, float> cost = c => layer.CrackCost(c, quicksandFactor) + (avoid != null && avoid.Contains(c) ? OldCrackPenalty : 0f);
            if (!GridRouter.TryFind(grid.Size, start, grid.WorldToCell(hole), cost, cells, false, 1f / Mathf.Max(1f, quicksandFactor)))
            {
                return false;
            }

            points.Add(new Vector3(travelStart.x, 0f, travelStart.z));
            for (int i = 1; i < cells.Count - 1; i++)
            {
                points.Add(grid.CellToWorld(cells[i]));
            }

            points.Add(new Vector3(hole.x, 0f, hole.z));
            Shape(field, points, cost);
            return true;
        }

        // The cell staircase pulled straight where no costlier ground lies between, back-tracks dropped, corners rounded:
        // one clean fissure whose wide strip never folds over itself at a step.
        private static void Shape(BossField field, List<Vector3> points, System.Func<Vector2Int, float> cost)
        {
            GridMap grid = field.Grid;
            CrackPath.Straighten(points, (a, b) => Straight(grid, cost, a, b));
            CrackPath.RemoveBacktracks(points, grid.CellSize * MinCrackLeg);
            CrackPath.Smooth(points, grid.CellSize * MaxCornerCut);
        }

        private static bool Straight(GridMap grid, System.Func<Vector2Int, float> cost, Vector3 a, Vector3 b)
        {
            float limit = Mathf.Max(cost(grid.WorldToCell(a)), cost(grid.WorldToCell(b))) + 0.01f;
            float length = Vector3.Distance(a, b);
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
            for (int i = 1; i < samples; i++)
            {
                if (cost(grid.WorldToCell(Vector3.Lerp(a, b, i / (float)samples))) > limit)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds where a boss breaks out beside a tower (Docs/BOSSES.md: "next to the road, beside the tower"): a free cell
        /// next to the tower for the hole, with the tower on the boss's right (the throw goes up and to the right) and the
        /// body's whole length from its first pivot to where the clip leaves it on free ground. Cells on or by the road
        /// win, then the ones nearest <paramref name="near"/>.
        /// </summary>
        /// <param name="holeAhead">How far ahead of the pivot the hole lies (world units).</param>
        /// <param name="travel">How far the pivot moves forward over the clip (world units).</param>
        public static bool SurfaceBeside(BossField field, Tower tower, Vector3 near, float holeAhead, float travel, float halfWidth, out Vector3 hole, out Vector3 pivot, out Vector3 forward)
        {
            hole = pivot = forward = Vector3.zero;
            GridMap grid = field.Grid;
            Vector2Int towerCell = grid.WorldToCell(tower.Position);
            float best = float.PositiveInfinity;
            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int cell = towerCell + step;
                if (!Breakable(field, cell))
                {
                    continue;
                }

                Vector3 at = grid.CellToWorld(cell);
                Vector3 toTower = tower.Position - at;
                toTower.y = 0f;
                Vector3 ahead = Vector3.Cross(toTower.normalized, Vector3.up);
                Vector3 start = at - ahead * holeAhead;
                Vector3 end = start + ahead * travel;
                if (!Clear(field, start, end))
                {
                    continue;
                }

                float cellSqr = grid.CellSize * grid.CellSize;
                float score = (at - near).sqrMagnitude + (NearRoad(field, cell) ? 0f : cellSqr * AwayFromRoadPenalty)
                    + (BesideOtherFootprint(field, cell, towerCell) ? cellSqr * BesideStructurePenalty : 0f)
                    + BodyOverlaps(field, start, end, halfWidth, towerCell) * cellSqr * BodyOverlapPenalty;
                if (score < best)
                {
                    best = score;
                    hole = at;
                    pivot = start;
                    forward = ahead;
                }
            }

            return !float.IsPositiveInfinity(best);
        }

        /// <summary>
        /// A spot to break out on when there is nothing to throw: the first free road-side cell some way down the road
        /// ahead of a point, with room for the body along the road.
        /// </summary>
        public static bool SurfaceOnRoad(BossField field, Vector3 from, Vector3 heading, float distance, float travel, out Vector3 pivot)
        {
            GridMap grid = field.Grid;
            pivot = from;
            for (float d = distance; d > 1f; d -= grid.CellSize)
            {
                Vector3 at = from + heading * d;
                Vector2Int cell = grid.WorldToCell(at);
                if (grid.Contains(cell) && field.Level.Road.IsRoad(cell) && Clear(field, at, at + heading * travel))
                {
                    pivot = grid.CellToWorld(cell);
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the straight line between two points crosses no footprint, rock or liquid.</summary>
        public static bool Clear(BossField field, Vector3 a, Vector3 b)
        {
            GridMap grid = field.Grid;
            float length = Vector3.Distance(a, b);
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
            for (int i = 0; i <= samples; i++)
            {
                Vector2Int cell = grid.WorldToCell(Vector3.Lerp(a, b, i / (float)samples));
                if (!grid.Contains(cell))
                {
                    return false;
                }

                SurfaceKind kind = field.Climb.KindAt(cell);
                if (field.Climb.IsFootprint(cell) || kind == SurfaceKind.Rock || kind == SurfaceKind.Liquid)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True when the straight line crosses no footprint (rocks and liquids allowed: it is under the ground).</summary>
        public static bool ClearOfFootprints(BossField field, Vector3 a, Vector3 b)
        {
            GridMap grid = field.Grid;
            float length = Vector3.Distance(a, b);
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
            for (int i = 0; i <= samples; i++)
            {
                Vector2Int cell = grid.WorldToCell(Vector3.Lerp(a, b, i / (float)samples));
                if (field.Climb.IsFootprint(cell))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// How many footprint cells (other than <paramref name="except"/>) the body lying from one point to another covers
        /// along its flanks: zero when the whole body is clear of every tower and gate.
        /// </summary>
        public static int BodyOverlaps(BossField field, Vector3 from, Vector3 to, float halfWidth, Vector2Int except)
        {
            GridMap grid = field.Grid;
            Vector3 along = to - from;
            along.y = 0f;
            Vector3 side = along.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, along.normalized) * halfWidth : Vector3.right * halfWidth;
            float length = along.magnitude;
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
            var hit = new HashSet<Vector2Int>();
            for (int i = 0; i <= samples; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)samples);
                for (int s = -1; s <= 1; s++)
                {
                    Vector2Int cell = grid.WorldToCell(at + side * s);
                    if (cell != except && field.Climb.IsFootprint(cell))
                    {
                        hit.Add(cell);
                    }
                }
            }

            return hit.Count;
        }

        /// <summary>
        /// Plans a walk off the road to a goal cell round every footprint (and, with <paramref name="clearance"/>, a cell
        /// clear of every tower so its whole body passes between the pads); the points are the start and the cell centres
        /// after it. False when the goal cannot be reached that way.
        /// </summary>
        public static bool WalkTo(BossField field, Vector3 from, Vector2Int goal, bool climbsRocks, int clearance, List<Vector3> points) =>
            WalkTo(field.Climb, from, goal, climbsRocks, clearance, points);

        /// <summary>The same walk over a bare climb layer (the tests).</summary>
        public static bool WalkTo(ClimbLayer layer, Vector3 from, Vector2Int goal, bool climbsRocks, int clearance, List<Vector3> points)
        {
            points.Clear();
            GridMap grid = layer.Grid;
            Vector2Int start = Clamp(grid, grid.WorldToCell(from));
            var cells = new List<Vector2Int>();
            System.Func<Vector2Int, float> cost = c => Crowded(layer, c, start, goal, clearance) ? float.PositiveInfinity : layer.WalkCost(c, climbsRocks);
            if (!GridRouter.TryFind(grid.Size, start, goal, cost, cells, false, ClimbLayer.CheapestWalk))
            {
                return false;
            }

            points.Add(from);
            for (int i = 1; i < cells.Count; i++)
            {
                points.Add(grid.CellToWorld(cells[i]));
            }

            return true;
        }

        // A cell within the clearance (cells) of a tower, away from the start and the goal (where it must stand anyway).
        private static bool Crowded(ClimbLayer layer, Vector2Int cell, Vector2Int start, Vector2Int goal, int clearance)
        {
            if (clearance <= 0 || Near(cell, start, 1) || Near(cell, goal, 1))
            {
                return false;
            }

            for (int x = -clearance; x <= clearance; x++)
            {
                for (int y = -clearance; y <= clearance; y++)
                {
                    if (layer.KindAt(cell + new Vector2Int(x, y)) == SurfaceKind.Tower)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Near(Vector2Int a, Vector2Int b, int cells) => Mathf.Abs(a.x - b.x) <= cells && Mathf.Abs(a.y - b.y) <= cells;

        /// <summary>
        /// Where a boss stands to strike a tower that lies <paramref name="reach"/> ahead of its pivot (the Smash clip): a
        /// walkable cell at that distance, facing the tower, its body from <paramref name="tail"/> behind the pivot to the
        /// tower clear of every other footprint; the one nearest <paramref name="near"/> wins.
        /// </summary>
        public static bool StrikeSpot(BossField field, Vector3 target, float reach, float tail, float halfWidth, Vector3 near, out Vector3 pivot, out Vector3 facing)
        {
            pivot = facing = Vector3.zero;
            GridMap grid = field.Grid;
            Vector2Int targetCell = grid.WorldToCell(target);
            float best = float.PositiveInfinity;
            for (int t = 0; t < StrikeDirections; t++)
            {
                Vector3 dir = Quaternion.Euler(0f, t * 360f / StrikeDirections, 0f) * Vector3.forward;
                Vector3 at = target - dir * reach;
                Vector2Int cell = grid.WorldToCell(at);
                if (!grid.Contains(cell) || field.Climb.IsFootprint(cell) || float.IsPositiveInfinity(field.Climb.WalkCost(cell, false))
                    || !Clear(field, at - dir * tail, at) || BodyOverlaps(field, at - dir * tail, target - dir * 0.5f, halfWidth, targetCell) > 0)
                {
                    continue;
                }

                float score = (at - near).sqrMagnitude;
                if (score < best)
                {
                    best = score;
                    pivot = grid.CellToWorld(cell);
                    facing = BossGeometry.Flat(target - pivot);
                }
            }

            return !float.IsPositiveInfinity(best);
        }

        /// <summary>
        /// Where a boss stands on a pool's bank with its tail over the liquid (to lay eggs from its sac): a walkable, clear
        /// cell <paramref name="behind"/> in front of a pool cell, facing away from the pool; the one nearest
        /// <paramref name="near"/> wins.
        /// </summary>
        public static bool BankSpot(BossField field, IReadOnlyList<Vector2Int> pool, float behind, float halfWidth, Vector3 near, out Vector3 pivot, out Vector3 facing, out Vector3 over)
        {
            pivot = facing = over = Vector3.zero;
            GridMap grid = field.Grid;
            float best = float.PositiveInfinity;
            foreach (Vector2Int liquid in pool)
            {
                Vector3 at = grid.CellToWorld(liquid);
                for (int t = 0; t < 8; t++)
                {
                    Vector3 dir = Quaternion.Euler(0f, t * 45f, 0f) * Vector3.forward;
                    Vector3 stand = at + dir * behind;
                    Vector2Int cell = grid.WorldToCell(stand);
                    if (!grid.Contains(cell) || field.Climb.IsFootprint(cell) || float.IsPositiveInfinity(field.Climb.WalkCost(cell, false)))
                    {
                        continue;
                    }

                    // A bank where her body would lie over a tower loses to any clear one, but a crowded bank still serves.
                    int overlaps = BodyOverlaps(field, stand, stand + dir * behind, halfWidth, new Vector2Int(-1, -1));
                    float score = (stand - near).sqrMagnitude + overlaps * grid.CellSize * grid.CellSize * BodyOverlapPenalty;
                    if (score < best)
                    {
                        best = score;
                        pivot = grid.CellToWorld(cell);
                        facing = dir;
                        over = at;
                    }
                }
            }

            return !float.IsPositiveInfinity(best);
        }

        /// <summary>The point a little to the side of a crack where a creature climbs out.</summary>
        public static Vector3 BesideCrack(Vector3 at, Vector3 along, int side) =>
            at + Vector3.Cross(Vector3.up, along.normalized) * (side * SideOffset * 0.5f);

        private static bool Breakable(BossField field, Vector2Int cell)
        {
            if (!field.Grid.Contains(cell) || field.Climb.IsFootprint(cell))
            {
                return false;
            }

            SurfaceKind kind = field.Climb.KindAt(cell);
            return kind == SurfaceKind.Open || kind == SurfaceKind.Road || kind == SurfaceKind.Raised || kind == SurfaceKind.Fragile;
        }

        // A hole right beside another tower or a gate would seem to break that one open as well.
        private static bool BesideOtherFootprint(BossField field, Vector2Int cell, Vector2Int target)
        {
            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int next = cell + step;
                if (next != target && field.Climb.IsFootprint(next))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NearRoad(BossField field, Vector2Int cell)
        {
            RoadCorridor road = field.Level.Road;
            if (road.IsRoad(cell))
            {
                return true;
            }

            foreach (Vector2Int step in Neighbours)
            {
                if (road.IsRoad(cell + step))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Vector2Int> NearestRoadCells(BossField field, Vector3 from, int count)
        {
            var sorted = new List<(float Distance, Vector2Int Cell)>();
            GridMap grid = field.Grid;
            foreach (Vector2Int cell in field.Level.Road.Cells)
            {
                if (!field.Climb.IsFootprint(cell))
                {
                    sorted.Add(((grid.CellToWorld(cell) - from).sqrMagnitude, cell));
                }
            }

            sorted.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            for (int i = 0; i < sorted.Count && i < count; i++)
            {
                yield return sorted[i].Cell;
            }
        }

        private static Vector2Int Clamp(GridMap grid, Vector2Int cell) =>
            new Vector2Int(Mathf.Clamp(cell.x, 0, grid.Size.x - 1), Mathf.Clamp(cell.y, 0, grid.Size.y - 1));
    }
}
