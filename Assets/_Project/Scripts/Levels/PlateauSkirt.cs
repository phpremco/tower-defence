using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The rules of raised ground (round 65, Remco: a gun on a plateau must never fire through the ground). A plateau no
    /// longer ends in a cliff inside its own cells: it slopes down over the ring of ground cells round it, and that ring
    /// holds no tower. So a plateau keeps one cell clear of everything that must stay level (the road, a liquid, a mining
    /// node, a nest), and a raised cell that looks at the road across more plateau than its gun can see over holds no
    /// tower either. Pure rules on cells: the generator shapes a level with them, the validator checks a level against
    /// them, and the landscape (<see cref="TerrainField"/>) draws the same ring.
    /// </summary>
    public static class PlateauSkirt
    {
        // How finely a line of sight is walked, in cells.
        private const float SightStepCells = 0.02f;
        // The walk overshoots the lip by up to a step; a line that leaves exactly at the ratio still passes.
        private const float SightSlack = 0.008f;

        private static readonly Vector2Int[] Around =
        {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0),
            new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        };

        // Where a line of sight is tested on a road cell: its middle and as far to each side as a walker strays past
        // the middle of the road's outer row, so the whole walked width counts and not only the rows' centre lines.
        private static readonly Vector2[] RoadSamples = { Vector2.zero, Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        /// <summary>The eight cells round a cell.</summary>
        public static IReadOnlyList<Vector2Int> Neighbours => Around;

        /// <summary>
        /// The patches with every raised cell taken down that touches a cell which must stay level, so the slope always
        /// has a ring of plain ground to run out on. A cell taken down is plain ground again (and part of the ring when
        /// raised ground is left beside it).
        /// </summary>
        public static TerrainPatch[] KeepClear(IReadOnlyList<TerrainPatch> patches, Vector2Int gridSize, Func<Vector2Int, bool> staysLevel)
        {
            var kinds = new Dictionary<Vector2Int, TerrainKind>();
            foreach (TerrainPatch patch in patches)
            {
                kinds[patch.Cell] = patch.Kind;
            }

            var kept = new List<TerrainPatch>(patches.Count);
            foreach (TerrainPatch patch in patches)
            {
                if (patch.Kind != TerrainKind.Raised || !TouchesLevelCell(patch.Cell, kinds, gridSize, staysLevel))
                {
                    kept.Add(patch);
                }
            }

            return kept.ToArray();
        }

        private static bool TouchesLevelCell(Vector2Int cell, Dictionary<Vector2Int, TerrainKind> kinds, Vector2Int gridSize, Func<Vector2Int, bool> staysLevel)
        {
            foreach (Vector2Int step in Around)
            {
                Vector2Int other = cell + step;
                bool liquid = kinds.TryGetValue(other, out TerrainKind kind) && TerrainMap.IsLiquid(kind);
                if (liquid || (Inside(other, gridSize) && staysLevel(other)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The ring: every plain ground cell of the grid that touches raised ground, in a fixed order.</summary>
        public static Vector2Int[] Ring(TerrainMap terrain, Vector2Int gridSize)
        {
            var ring = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in terrain.Cells)
            {
                if (pair.Value != TerrainKind.Raised)
                {
                    continue;
                }

                foreach (Vector2Int step in Around)
                {
                    Vector2Int other = pair.Key + step;
                    if (Inside(other, gridSize) && terrain.KindAt(other) == TerrainKind.Ground && seen.Add(other))
                    {
                        ring.Add(other);
                    }
                }
            }

            ring.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return ring.ToArray();
        }

        /// <summary>
        /// The raised cells a gun could not see the road from: somewhere on a road cell within <paramref name="reach"/>
        /// (world units) the straight line from the cell's middle leaves the plateau later than
        /// <paramref name="sightRatio"/> of the way, so the shot would dip under the plateau's own lip
        /// (<see cref="TerrainRules.SightRatio"/>). <paramref name="strayCells"/> is how far past the middle of a road
        /// cell a walker may be (EnemySpawner.StrayPastRowCells).
        /// </summary>
        public static List<Vector2Int> Shadowed(TerrainMap terrain, GridMap grid, IEnumerable<Vector2Int> roadCells, float strayCells, float reach, float sightRatio)
        {
            var road = new List<Vector2Int>(roadCells);
            var shadowed = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in terrain.Cells)
            {
                if (pair.Value == TerrainKind.Raised && IsShadowed(pair.Key, terrain, grid, road, strayCells, reach, sightRatio))
                {
                    shadowed.Add(pair.Key);
                }
            }

            shadowed.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return shadowed;
        }

        private static bool IsShadowed(Vector2Int cell, TerrainMap terrain, GridMap grid, List<Vector2Int> road, float strayCells, float reach, float sightRatio)
        {
            float reachCells = reach / grid.CellSize;
            foreach (Vector2Int roadCell in road)
            {
                foreach (Vector2 sample in RoadSamples)
                {
                    Vector2 to = new Vector2(roadCell.x, roadCell.y) + sample * strayCells - new Vector2(cell.x, cell.y);
                    float distance = to.magnitude;
                    if (distance <= reachCells && distance > 0f && PlateauShare(cell, to, distance, terrain) > sightRatio + SightSlack)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The share of the way from a raised cell's middle along <paramref name="to"/> (in cells) at which the line is
        /// over raised ground for the last time.
        /// </summary>
        public static float PlateauShare(Vector2Int cell, Vector2 to, float distance, TerrainMap terrain)
        {
            Vector2 direction = to / distance;
            for (float walked = distance; walked > 0f; walked -= SightStepCells)
            {
                Vector2 at = direction * walked;
                var under = new Vector2Int(cell.x + Mathf.RoundToInt(at.x), cell.y + Mathf.RoundToInt(at.y));
                if (terrain.KindAt(under) == TerrainKind.Raised)
                {
                    return walked / distance;
                }
            }

            return 0f;
        }

        /// <summary>True when the level has a raised cell a tower can stand on and that lengthens its reach.</summary>
        public static bool HasBuildSpot(LevelDefinition level)
        {
            if (level.TerrainRules == null || level.TerrainRules.HighGroundRange <= 1f)
            {
                return false;
            }

            var build = new BuildGrid(level);
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in level.Terrain.Cells)
            {
                if (pair.Value == TerrainKind.Raised && build.CanBuild(pair.Key))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Inside(Vector2Int cell, Vector2Int gridSize) => cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
    }
}
