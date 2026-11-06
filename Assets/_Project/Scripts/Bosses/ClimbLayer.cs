using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The height and surface map a boss moves over (Docs/BOSSES.md, "climb layer"): per cell what stands there (open
    /// ground, the road, a plateau, a solid rock, a fragile crystal, a pool, a tower, a gate, the base) and how high its
    /// top is. The ground part comes from the level's data and the formations as they were drawn; the structures are
    /// marked again before each plan, since towers come and go. Pure logic over the grid; the costs it hands out keep a
    /// crack line or a walk off every footprint.
    /// </summary>
    public sealed class ClimbLayer
    {
        /// <summary>How high a gate's beam stands: a climbing boss stands on it.</summary>
        public const float GateHeight = 1.15f;

        /// <summary>How high the base's roof stands.</summary>
        public const float BaseHeight = 1.4f;

        /// <summary>The lowest price of a walked cell (a rock a climbing boss goes over), for the route search.</summary>
        public const float CheapestWalk = RockWalkCost;

        // A formation lower than this is rubble a boss simply walks over.
        private const float MinRockHeight = 0.35f;

        // A climbable piece at least this high is too tall to step over: it is climbed like a rock (Climb_Up/Climb_Down).
        public const float MaxStepHeight = 0.9f;

        // Staying a cell clear of a tower costs this much extra, so a walk keeps its distance where it can.
        private const float NearTowerPenalty = 0.8f;
        private const float RaisedCost = 1.3f;
        // A climbing boss goes over a rock rather than round it (Docs/BOSSES.md: "instead of walking round them").
        private const float RockWalkCost = 0.8f;
        private const float FragileCost = 1.3f;
        private const float FragileCrackCost = 1.5f;
        private const float RaisedCrackCost = 2f;

        // A crack keeps a cell clear of every footprint where it can, so it never seems to run under a tower or a gate.
        private const float NearFootprintCrackPenalty = 2.5f;

        private static readonly Vector2Int[] Around =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        private readonly Dictionary<Vector2Int, SurfaceKind> _ground = new Dictionary<Vector2Int, SurfaceKind>();
        private readonly Dictionary<Vector2Int, float> _heights = new Dictionary<Vector2Int, float>();
        private readonly Dictionary<Vector2Int, float> _decorTops = new Dictionary<Vector2Int, float>();
        private readonly Dictionary<Vector2Int, SurfaceKind> _structures = new Dictionary<Vector2Int, SurfaceKind>();
        private readonly HashSet<Vector2Int> _nearTower = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _nearFootprint = new HashSet<Vector2Int>();

        public ClimbLayer(GridMap grid, IEnumerable<Vector2Int> road, TerrainMap terrain, IReadOnlyList<DecorFormation> formations)
        {
            Grid = grid;
            foreach (KeyValuePair<Vector2Int, TerrainKind> cell in terrain.Cells)
            {
                SurfaceKind kind = Of(cell.Value);
                if (kind != SurfaceKind.Open)
                {
                    _ground[cell.Key] = kind;
                    _heights[cell.Key] = cell.Value == TerrainKind.Raised ? TerrainMap.PlateauHeight : 0f;
                }
            }

            foreach (Vector2Int cell in road)
            {
                _ground[cell] = SurfaceKind.Road;
            }

            if (formations == null)
            {
                return;
            }

            foreach (DecorFormation formation in formations)
            {
                Vector2Int cell = grid.WorldToCell(formation.Centre);
                if (!grid.Contains(cell) || IsRoad(cell))
                {
                    continue;
                }

                switch (formation.Contact)
                {
                    case DecorContact.Breakable:
                        _ground[cell] = SurfaceKind.Fragile;
                        break;
                    case DecorContact.Obstacle:
                        MarkObstacle(formation);
                        break;
                    case DecorContact.Climbable when formation.Top >= MinRockHeight:
                        MarkRise(cell, formation.Top);
                        break;
                }
            }
        }

        private bool IsRoad(Vector2Int cell) => _ground.TryGetValue(cell, out SurfaceKind under) && under == SurfaceKind.Road;

        // A spire or column closes every cell its body stands over (it can be wider than a cell), never the road.
        private void MarkObstacle(DecorFormation formation)
        {
            Vector2Int centre = Grid.WorldToCell(formation.Centre);
            float reach = formation.PlanRadius;
            int cells = Mathf.CeilToInt(reach / Grid.CellSize);
            for (int x = -cells; x <= cells; x++)
            {
                for (int y = -cells; y <= cells; y++)
                {
                    var cell = centre + new Vector2Int(x, y);
                    Vector3 at = Grid.CellToWorld(cell);
                    float dx = at.x - formation.Centre.x;
                    float dz = at.z - formation.Centre.z;
                    if (Grid.Contains(cell) && !IsRoad(cell) && dx * dx + dz * dz <= reach * reach)
                    {
                        _ground[cell] = SurfaceKind.Obstacle;
                    }
                }
            }

            _ground[centre] = SurfaceKind.Obstacle;
        }

        // A low rise is walked over with the body following its top; one too high for a step is a rock, climbed with the
        // climb clip by a boss that climbs and walked round by one that does not.
        private void MarkRise(Vector2Int cell, float top)
        {
            float height = Mathf.Max(top, _heights.TryGetValue(cell, out float h) ? h : 0f);
            _heights[cell] = height;
            if (height >= MaxStepHeight)
            {
                _ground[cell] = SurfaceKind.Rock;
                return;
            }

            _ground[cell] = SurfaceKind.Raised;
            _decorTops[cell] = height;
        }

        /// <summary>The top of a climbable decoration piece on a cell (a boulder, a drift), 0 where none stands; the landscape's
        /// own rises are in the terrain field.</summary>
        public float DecorTopAt(Vector2Int cell) => _decorTops.TryGetValue(cell, out float top) ? top : 0f;

        /// <summary>The climb layer of a level, with the formations as the environment drew them.</summary>
        public static ClimbLayer For(LevelDefinition level, IReadOnlyList<DecorFormation> formations) =>
            new ClimbLayer(level.Grid, level.Road.Cells, level.Terrain, formations);

        public GridMap Grid { get; }

        public SurfaceKind KindAt(Vector2Int cell)
        {
            if (_structures.TryGetValue(cell, out SurfaceKind structure))
            {
                return structure;
            }

            return _ground.TryGetValue(cell, out SurfaceKind kind) ? kind : SurfaceKind.Open;
        }

        public SurfaceKind KindAt(Vector3 position) => KindAt(Grid.WorldToCell(position));

        /// <summary>How high the top of a cell stands: a rock's top, a plateau, a gate's beam, the base's roof; 0 for the plain.</summary>
        public float HeightAt(Vector2Int cell)
        {
            switch (KindAt(cell))
            {
                case SurfaceKind.Gate: return GateHeight;
                case SurfaceKind.Base: return BaseHeight;
                case SurfaceKind.Tower: return 0f;
                default: return _heights.TryGetValue(cell, out float height) ? height : 0f;
            }
        }

        /// <summary>True for the footprints nothing may cross: towers (wrecks included), gates and the base.</summary>
        public bool IsFootprint(Vector2Int cell)
        {
            SurfaceKind kind = KindAt(cell);
            return kind == SurfaceKind.Tower || kind == SurfaceKind.Gate || kind == SurfaceKind.Base;
        }

        public void ClearStructures()
        {
            _structures.Clear();
            _nearTower.Clear();
            _nearFootprint.Clear();
        }

        public void MarkTower(Vector2Int cell)
        {
            Mark(cell, SurfaceKind.Tower);
            foreach (Vector2Int step in Around)
            {
                _nearTower.Add(cell + step);
            }
        }

        public void MarkGate(Vector2Int cell) => Mark(cell, SurfaceKind.Gate);

        public void MarkBase(Vector2Int cell) => Mark(cell, SurfaceKind.Base);

        private void Mark(Vector2Int cell, SurfaceKind kind)
        {
            _structures[cell] = kind;
            foreach (Vector2Int step in Around)
            {
                _nearFootprint.Add(cell + step);
            }
        }

        /// <summary>
        /// What it costs a crack line to run through a cell: plain ground, the road and plateaus cost one, a fragile
        /// formation a little more (it breaks as the crack opens), quicksand less (the burrow's shortcut); rocks, the
        /// other liquids and every footprint are closed.
        /// </summary>
        public float CrackCost(Vector2Int cell, float quicksandFactor)
        {
            if (!Grid.Contains(cell))
            {
                return float.PositiveInfinity;
            }

            float near = _nearFootprint.Contains(cell) ? NearFootprintCrackPenalty : 0f;
            switch (KindAt(cell))
            {
                case SurfaceKind.Open:
                case SurfaceKind.Road:
                    return 1f + near;
                case SurfaceKind.Raised:
                    // Over a plateau the crack would climb its faces; it keeps to the plain where it can.
                    return RaisedCrackCost + near;
                case SurfaceKind.Fragile:
                    return FragileCrackCost + near;
                case SurfaceKind.Quicksand:
                    return 1f / Mathf.Max(1f, quicksandFactor) + near;
                default:
                    return float.PositiveInfinity;
            }
        }

        /// <summary>
        /// What it costs a boss to walk a cell off the road: rocks are climbed (if it climbs), fragile decor is broken,
        /// liquids and footprints are closed, and a cell beside a tower costs extra so it keeps clear where it can.
        /// </summary>
        public float WalkCost(Vector2Int cell, bool climbsRocks)
        {
            if (!Grid.Contains(cell))
            {
                return float.PositiveInfinity;
            }

            float cost;
            switch (KindAt(cell))
            {
                case SurfaceKind.Open:
                case SurfaceKind.Road:
                    cost = 1f;
                    break;
                case SurfaceKind.Raised:
                    cost = RaisedCost;
                    break;
                case SurfaceKind.Fragile:
                    cost = FragileCost;
                    break;
                case SurfaceKind.Rock:
                    cost = climbsRocks ? RockWalkCost : float.PositiveInfinity;
                    break;
                default:
                    return float.PositiveInfinity;
            }

            return _nearTower.Contains(cell) ? cost + NearTowerPenalty : cost;
        }

        private static SurfaceKind Of(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Ground: return SurfaceKind.Open;
                case TerrainKind.Raised: return SurfaceKind.Raised;
                case TerrainKind.Quicksand: return SurfaceKind.Quicksand;
                default: return SurfaceKind.Liquid;
            }
        }
    }
}
