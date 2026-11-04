using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Where the environment kit goes, read from the grid alone (ArtSource README rounds 8 and 17): a kerb on every side
    /// of a road cell that borders something else, with corner pieces where the outline turns, plaza edges and joins,
    /// end caps beside each gate and a plinth under it, a pad on every buildable cell, banks round the pools, terrace
    /// steps round raised ground, rift pieces along straight fissures and cliff walls behind the field. Pure layout:
    /// whatever the set lacks is skipped by the builder, so the rules serve every continent.
    /// </summary>
    /// <remarks>
    /// Frames after the import's axis change (Blender -Y is Unity -Z): an edge piece has the road (or liquid) on its
    /// local -Z, an outer corner its road in the (-X, -Z) quadrant, an inner corner its ground in (+X, +Z), a terrace step
    /// its plateau on +Z, a cliff its field on -Z, a rift runs along X.
    /// </remarks>
    public sealed class EnvironmentLayout
    {
        // Openings in the border wall: road ends and the base stand clear of it by this much.
        private const float WallClearance = 2.6f;
        private const float CliffLength = 2f;
        private const int RiftLongCells = 3;

        private static readonly Vector2Int[] Sides = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private readonly LevelDefinition _level;
        private readonly TerrainField _field;
        private readonly GridMap _grid;
        private readonly RoadCorridor _road;
        private readonly TerrainMap _terrain;
        private readonly HashSet<Vector2Int> _gateCells = new HashSet<Vector2Int>();
        private readonly List<EnvironmentPlacement> _placements = new List<EnvironmentPlacement>();
        private readonly float _cell;

        /// <param name="field">The landscape heights; without one (tests) everything stands on the cell height.</param>
        public EnvironmentLayout(LevelDefinition level, TerrainField field)
        {
            _level = level;
            _field = field;
            _grid = level.Grid;
            _road = level.Road;
            _terrain = level.Terrain;
            _cell = level.CellSize;
            foreach (BlockadeSpot spot in level.Blockades)
            {
                _gateCells.UnionWith(_road.Rung(spot.Cell));
            }
        }

        public IReadOnlyList<EnvironmentPlacement> Placements => _placements;

        /// <summary>Lays out every rule. <paramref name="cliffInset"/> and <paramref name="sideReach"/> come from the set.</summary>
        public EnvironmentLayout Build(float cliffInset, float sideReach)
        {
            PlaceRoadEdges();
            PlaceRoadCorners();
            PlaceGatePlinths();
            PlaceBuildPads();
            PlaceBanks();
            PlaceTerraces();
            PlaceRifts();
            PlaceBorder(cliffInset, sideReach);
            return this;
        }

        public int Count(EnvironmentPieceKind kind)
        {
            int count = 0;
            foreach (EnvironmentPlacement placement in _placements)
            {
                count += placement.Kind == kind ? 1 : 0;
            }

            return count;
        }

        /// <summary>The yaw that turns a horizontal direction of the piece's own frame onto a world direction.</summary>
        public static float YawBetween(Vector2 local, Vector2 world) => YawOf(world) - YawOf(local);

        private static float YawOf(Vector2 direction) => Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        private void Add(EnvironmentPieceKind kind, Vector3 position, float yaw) => _placements.Add(new EnvironmentPlacement(kind, position, yaw));

        private Vector3 Ground(Vector3 position)
        {
            position.y = _field != null ? _field.HeightAt(position.x, position.z) : 0f;
            return position;
        }

        private Vector3 SideMiddle(Vector2Int cell, Vector2Int side) =>
            _grid.CellToWorld(cell) + new Vector3(side.x, 0f, side.y) * (_cell * 0.5f);

        // The point shared by a cell and its neighbours towards (dx, dy), each -1 or +1.
        private Vector3 CornerPoint(Vector2Int cell, int dx, int dy) =>
            _grid.CellToWorld(cell) + new Vector3(dx, 0f, dy) * (_cell * 0.5f);

        private bool IsRoad(Vector2Int cell) => _grid.Contains(cell) && _road.IsRoad(cell);

        private void PlaceRoadEdges()
        {
            foreach (Vector2Int cell in _road.Cells)
            {
                if (_gateCells.Contains(cell))
                {
                    continue;
                }

                bool plaza = _road.IsPlaza(cell);
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int beyond = cell + side;
                    if (!_grid.Contains(beyond) || _road.IsRoad(beyond))
                    {
                        continue;
                    }

                    // Along the kerb line, the piece's +X runs to the right of the outward side.
                    var along = new Vector2Int(side.y, -side.x);
                    Add(EdgeKind(cell, side, along, plaza), SideMiddle(cell, side), YawBetween(Vector2.up, side));
                }
            }
        }

        private EnvironmentPieceKind EdgeKind(Vector2Int cell, Vector2Int side, Vector2Int along, bool plaza)
        {
            if (plaza)
            {
                return EnvironmentPieceKind.PlazaEdgeStraight;
            }

            if (_gateCells.Contains(cell + along))
            {
                return EnvironmentPieceKind.RoadEdgeEndCap;
            }

            if (_gateCells.Contains(cell - along))
            {
                return EnvironmentPieceKind.RoadEdgeEndCapMirrored;
            }

            if (OpenPlaza(cell + along, side))
            {
                return EnvironmentPieceKind.RoadEdgePlazaJoin;
            }

            return OpenPlaza(cell - along, side) ? EnvironmentPieceKind.RoadEdgePlazaJoinMirrored : EnvironmentPieceKind.RoadEdgeStraight;
        }

        private bool OpenPlaza(Vector2Int cell, Vector2Int side) =>
            _road.IsPlaza(cell) && _grid.Contains(cell + side) && !_road.IsRoad(cell + side);

        // Every grid point where the road's outline turns: one road cell of the four is an outer corner, three an inner.
        private void PlaceRoadCorners()
        {
            var done = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in _road.Cells)
            {
                for (int dx = -1; dx <= 1; dx += 2)
                {
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        // The point's id is the cell to its upper right.
                        var id = new Vector2Int(cell.x + (dx > 0 ? 1 : 0), cell.y + (dy > 0 ? 1 : 0));
                        if (done.Add(id))
                        {
                            PlaceRoadCorner(id);
                        }
                    }
                }
            }
        }

        private void PlaceRoadCorner(Vector2Int id)
        {
            int roads = 0;
            bool plaza = false;
            Vector2 roadQuadrant = Vector2.zero;
            Vector2 groundQuadrant = Vector2.zero;
            for (int qx = -1; qx <= 0; qx++)
            {
                for (int qy = -1; qy <= 0; qy++)
                {
                    var cell = new Vector2Int(id.x + qx, id.y + qy);
                    if (!_grid.Contains(cell) || _gateCells.Contains(cell))
                    {
                        // A corner on the field's edge or by a gate closes nothing: the road runs on, or the plinth stands there.
                        return;
                    }

                    var quadrant = new Vector2(qx * 2 + 1, qy * 2 + 1);
                    if (_road.IsRoad(cell))
                    {
                        roads++;
                        roadQuadrant = quadrant;
                        plaza |= _road.IsPlaza(cell);
                    }
                    else
                    {
                        groundQuadrant = quadrant;
                    }
                }
            }

            Vector3 point = _grid.CellToWorld(id) - new Vector3(0.5f, 0f, 0.5f) * _cell;
            if (roads == 1)
            {
                Add(plaza ? EnvironmentPieceKind.PlazaEdgeCorner : EnvironmentPieceKind.RoadEdgeOuterCorner, point, YawBetween(new Vector2(-1f, -1f), roadQuadrant));
            }
            else if (roads == 3)
            {
                Add(plaza ? EnvironmentPieceKind.PlazaEdgeCorner : EnvironmentPieceKind.RoadEdgeInnerCorner, point, YawBetween(new Vector2(1f, 1f), groundQuadrant));
            }
        }

        // The plinth spans the corridor under the gate: across it on X, along the road on Z.
        private void PlaceGatePlinths()
        {
            foreach (BlockadeSpot spot in _level.Blockades)
            {
                Vector3 along = _road.DirectionAt(spot.Cell);
                Vector3 position = LevelPlayPose.GatePosition(_level, spot.Cell);
                Add(EnvironmentPieceKind.GatePlinth, position, YawBetween(Vector2.up, new Vector2(along.x, along.z)));
            }
        }

        // A pad on every cell a tower may stand on, a quarter turn by the cell so the tiles do not repeat in step.
        private void PlaceBuildPads()
        {
            var build = new BuildGrid(_level);
            for (int x = 0; x < _grid.Size.x; x++)
            {
                for (int y = 0; y < _grid.Size.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (build.CanBuild(cell))
                    {
                        Vector3 position = _grid.CellToWorld(cell) + Vector3.up * _terrain.HeightAt(cell);
                        Add(EnvironmentPieceKind.BuildPad, position, 90f * (int)(TerrainNoise.Hash(x, y, 5) * 4f));
                    }
                }
            }
        }

        // Banks round every pool of a liquid that is not a rift, on the sides that touch dry ground (a kerb takes a road side).
        private void PlaceBanks()
        {
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in _terrain.Cells)
            {
                if (!IsPool(pair.Value))
                {
                    continue;
                }

                foreach (Vector2Int side in Sides)
                {
                    Vector2Int beyond = pair.Key + side;
                    if (_terrain.KindAt(beyond) != pair.Value && !IsRoad(beyond))
                    {
                        Add(EnvironmentPieceKind.BankStraight, Ground(SideMiddle(pair.Key, side)), YawBetween(Vector2.up, side));
                    }
                }
            }

            PlaceQuadrantCorners(cell => IsPool(_terrain.KindAt(cell)), EnvironmentPieceKind.BankOuterCorner, EnvironmentPieceKind.BankInnerCorner, new Vector2(-1f, -1f), true);
        }

        private static bool IsPool(TerrainKind kind) => TerrainMap.IsLiquid(kind) && kind != TerrainKind.Rift;

        // Steps round raised ground on every side that drops to a lower cell; an outer corner where one cell of four is raised.
        private void PlaceTerraces()
        {
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in _terrain.Cells)
            {
                if (pair.Value != TerrainKind.Raised)
                {
                    continue;
                }

                foreach (Vector2Int side in Sides)
                {
                    if (_terrain.KindAt(pair.Key + side) != TerrainKind.Raised)
                    {
                        Vector3 position = SideMiddle(pair.Key, side);
                        Add(EnvironmentPieceKind.TerraceStep, position, YawBetween(Vector2.up, -side));
                    }
                }
            }

            PlaceQuadrantCorners(cell => _terrain.KindAt(cell) == TerrainKind.Raised, EnvironmentPieceKind.TerraceStepOuter, null, new Vector2(1f, 1f), false);
        }

        // Corner pieces at the grid points round a set of cells: the outer piece where one cell of four belongs (its
        // quadrant turned onto the outer frame), the inner where three do (the odd cell onto the inner frame's (+X, +Z)).
        private void PlaceQuadrantCorners(System.Func<Vector2Int, bool> inside, EnvironmentPieceKind outer, EnvironmentPieceKind? inner, Vector2 outerFrame, bool skipRoad)
        {
            var done = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in _terrain.Cells)
            {
                if (!inside(pair.Key))
                {
                    continue;
                }

                for (int dx = 0; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        var id = new Vector2Int(pair.Key.x + dx, pair.Key.y + dy);
                        if (!done.Add(id))
                        {
                            continue;
                        }

                        int count = 0;
                        bool road = false;
                        Vector2 inQuadrant = Vector2.zero;
                        Vector2 outQuadrant = Vector2.zero;
                        for (int qx = -1; qx <= 0; qx++)
                        {
                            for (int qy = -1; qy <= 0; qy++)
                            {
                                var cell = new Vector2Int(id.x + qx, id.y + qy);
                                var quadrant = new Vector2(qx * 2 + 1, qy * 2 + 1);
                                road |= IsRoad(cell);
                                if (inside(cell))
                                {
                                    count++;
                                    inQuadrant = quadrant;
                                }
                                else
                                {
                                    outQuadrant = quadrant;
                                }
                            }
                        }

                        if (skipRoad && road)
                        {
                            continue;
                        }

                        Vector3 point = Ground(_grid.CellToWorld(id) - new Vector3(0.5f, 0f, 0.5f) * _cell);
                        if (count == 1)
                        {
                            Add(outer, point, YawBetween(outerFrame, inQuadrant));
                        }
                        else if (count == 3 && inner.HasValue)
                        {
                            Add(inner.Value, point, YawBetween(new Vector2(1f, 1f), outQuadrant));
                        }
                    }
                }
            }
        }

        // Straight fissures, one cell wide: a cap at each end and the longest pieces between. Wider rifts keep the
        // carved ground with the rift rock layer (the kit has no bends or blobs yet).
        private void PlaceRifts()
        {
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in _terrain.Cells)
            {
                if (pair.Value != TerrainKind.Rift)
                {
                    continue;
                }

                TryRiftRun(pair.Key, Vector2Int.right, Vector2Int.up);
                TryRiftRun(pair.Key, Vector2Int.up, Vector2Int.right);
            }
        }

        private bool IsRift(Vector2Int cell) => _terrain.KindAt(cell) == TerrainKind.Rift;

        // From the first cell of a run along the direction, as long as nothing beside it is rift too.
        private void TryRiftRun(Vector2Int start, Vector2Int direction, Vector2Int across)
        {
            if (IsRift(start - direction))
            {
                return;
            }

            var run = new List<Vector2Int>();
            for (Vector2Int cell = start; IsRift(cell); cell += direction)
            {
                if (IsRift(cell + across) || IsRift(cell - across))
                {
                    return;
                }

                run.Add(cell);
            }

            if (run.Count < 2)
            {
                return;
            }

            float yaw = YawBetween(Vector2.right, direction);
            Add(EnvironmentPieceKind.RiftCap, Ground(_grid.CellToWorld(run[0])), yaw + 180f);
            Add(EnvironmentPieceKind.RiftCap, Ground(_grid.CellToWorld(run[run.Count - 1])), yaw);
            int index = 1;
            while (index < run.Count - 1)
            {
                int length = Mathf.Min(RiftLongCells, run.Count - 1 - index);
                EnvironmentPieceKind kind = length == 3 ? EnvironmentPieceKind.RiftLong : length == 2 ? EnvironmentPieceKind.RiftMedium : EnvironmentPieceKind.RiftShort;
                Vector3 middle = (_grid.CellToWorld(run[index]) + _grid.CellToWorld(run[index + length - 1])) * 0.5f;
                Add(kind, Ground(middle), yaw);
                index += length;
            }
        }

        // Walls behind the field (the back and the far part of both flanks) where the rim hills begin; the front stays
        // open so a wall never hides a row of the field. Road ends and the base keep an opening.
        private void PlaceBorder(float inset, float sideReach)
        {
            Vector2 half = _level.WorldSize * 0.5f;
            float d = inset * _cell;
            float north = half.y + d;
            float west = -half.x - d;
            float east = half.x + d;
            float south = half.y - sideReach * _level.WorldSize.y;
            List<Vector3> openings = Openings();

            Add(EnvironmentPieceKind.CliffInnerCorner, Ground(new Vector3(east, 0f, north)), YawBetween(new Vector2(-1f, -1f), new Vector2(-1f, -1f)));
            Add(EnvironmentPieceKind.CliffInnerCorner, Ground(new Vector3(west, 0f, north)), YawBetween(new Vector2(-1f, -1f), new Vector2(1f, -1f)));
            for (float x = west + _cell + CliffLength * 0.5f; x < east - _cell + CliffLength * 0.5f; x += CliffLength)
            {
                AddWall(new Vector3(Mathf.Min(x, east - _cell - CliffLength * 0.5f), 0f, north), Vector2.up, openings);
            }

            for (float z = north - _cell - CliffLength * 0.5f; z > south; z -= CliffLength)
            {
                AddWall(new Vector3(west, 0f, z), Vector2.left, openings);
                AddWall(new Vector3(east, 0f, z), Vector2.right, openings);
            }
        }

        private void AddWall(Vector3 position, Vector2 outward, List<Vector3> openings)
        {
            foreach (Vector3 opening in openings)
            {
                if (new Vector2(position.x - opening.x, position.z - opening.z).magnitude < WallClearance + CliffLength * 0.5f)
                {
                    return;
                }
            }

            Add(EnvironmentPieceKind.CliffStraight, Ground(position), YawBetween(Vector2.up, outward));
        }

        // Where the road (and the flight routes) leave the field, and the base.
        private List<Vector3> Openings()
        {
            var openings = new List<Vector3>();
            foreach (EnemyPath path in _level.Paths)
            {
                if (path.Waypoints == null || path.Waypoints.Length == 0)
                {
                    continue;
                }

                openings.Add(_grid.CellToWorld(path.Waypoints[0]));
                openings.Add(_grid.CellToWorld(path.Waypoints[path.Waypoints.Length - 1]));
            }

            if (_level.MainRoad.Length > 0)
            {
                openings.Add(LevelVisualBuilder.BasePosition(_level));
            }

            return openings;
        }
    }
}
