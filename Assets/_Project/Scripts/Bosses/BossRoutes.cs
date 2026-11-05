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
        private const int RoadTries = 6;
        private const float SideOffset = 1f;
        private const float AwayFromRoadPenalty = 16f;
        private const float BesideStructurePenalty = 25f;

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
        public static bool Crack(BossField field, Vector3 travelStart, Vector3 hole, float quicksandFactor, List<Vector3> points)
        {
            points.Clear();
            ClimbLayer layer = field.Climb;
            GridMap grid = field.Grid;
            var cells = new List<Vector2Int>();
            Vector2Int start = Clamp(grid, grid.WorldToCell(travelStart));
            if (!GridRouter.TryFind(grid.Size, start, grid.WorldToCell(hole), c => layer.CrackCost(c, quicksandFactor), cells, false, 1f / Mathf.Max(1f, quicksandFactor)))
            {
                return false;
            }

            points.Add(new Vector3(travelStart.x, 0f, travelStart.z));
            for (int i = 1; i < cells.Count - 1; i++)
            {
                points.Add(grid.CellToWorld(cells[i]));
            }

            points.Add(new Vector3(hole.x, 0f, hole.z));
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
        public static bool SurfaceBeside(BossField field, Tower tower, Vector3 near, float holeAhead, float travel, out Vector3 hole, out Vector3 pivot, out Vector3 forward)
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
                    + (BesideOtherFootprint(field, cell, towerCell) ? cellSqr * BesideStructurePenalty : 0f);
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
