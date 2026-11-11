using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The landscape as numbers on a fine grid of points that reaches past the playing field as far as the widest
    /// screen can see when zoomed all the way out, so no corner of the view is ever black: height, how much of
    /// each terrain kind is under every point, and how steep the ground is there. The ground mesh, its painted
    /// texture, the liquids and the decoration all read from the same field, so they always agree.
    /// </summary>
    public sealed class TerrainField
    {
        public const int SubdivisionsPerCell = 4;
        /// <summary>The least the landscape reaches past the field on any side, in cells; the camera may ask for more.</summary>
        public const int MinMarginCells = 9;

        /// <summary>The widest screen the landscape is built for; anything wider shows a sliver of void in the corners.</summary>
        private const float WidestAspect = 2.4f;
        private const int FootprintSlackCells = 1;

        private const float EdgeWander = 0.7f;
        private const float BasinDepth = 0.6f;
        private const float SandDepth = 0.16f;
        private const float IceDepth = 0.1f;
        private const float BogDepth = 0.22f;
        // A rift is a fissure: steep walls that drop below its floor, so the strata show and nothing reads as a pond.
        private const float RiftDepth = 0.55f;
        private const float RiftWallStart = 0.05f;
        private const float RiftWallFull = 0.3f;
        private const float ShoreWidthCells = 0.55f;
        // A pool's shore follows its smoothed outline: the ground starts to fall just outside it, so the liquid meets the
        // ground about 0.13 inside it, under the swept bank's shelf (README round 24b: the shelf reaches 0.14 to 0.21 in).
        private const float PoolShoreStart = -0.02f;
        private const float PoolShoreEnd = 0.4f;
        private const float ShoreWobbleCells = 0.25f;
        private const float CornerRadiusCells = 0.46f;
        private const float BasinStart = 0.1f;
        private const float BasinFull = 0.6f;
        private const float RimStartCells = 1.5f;
        private const float RimEndCells = 7f;
        private const float RimHeight = 3.6f;
        private const float RoadShoulderCells = 0.3f;
        private const float OuterBumpHeight = 0.1f;
        private const float OuterBumpStartCells = 0.5f;
        private const float OuterBumpEndCells = 2.5f;
        // The colony rig's levelled pad fades out over this much of the field's edge, so it never lifts a playing cell.
        private const float RigPadEdgeFadeCells = 0.5f;

        private static readonly Vector2Int[] Sides = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private readonly float[] _plateau;
        private readonly float[] _water;
        private readonly float[] _lava;
        private readonly float[] _sand;
        private readonly float[] _ice;
        private readonly float[] _bog;
        private readonly float[] _rift;
        private readonly float[] _height;
        private readonly float[] _slope;
        private readonly float[] _roadMask;

        public TerrainField(LevelDefinition level, RoadShape road)
        {
            CellSize = level.CellSize;
            Vector2 size = level.WorldSize;
            HalfSize = size * 0.5f;
            Vector2 footprint = FixedCameraRig.GroundFootprint(level, WidestAspect);
            MarginCellsX = Mathf.Max(MinMarginCells, Mathf.CeilToInt((footprint.x - HalfSize.x) / CellSize) + FootprintSlackCells);
            MarginCellsZ = Mathf.Max(MinMarginCells, Mathf.CeilToInt((footprint.y - HalfSize.y) / CellSize) + FootprintSlackCells);
            MinX = -HalfSize.x - MarginCellsX * CellSize;
            MinZ = -HalfSize.y - MarginCellsZ * CellSize;
            Step = CellSize / SubdivisionsPerCell;
            Nx = Mathf.RoundToInt((size.x + 2f * MarginCellsX * CellSize) / Step) + 1;
            Nz = Mathf.RoundToInt((size.y + 2f * MarginCellsZ * CellSize) / Step) + 1;

            int count = Nx * Nz;
            _plateau = new float[count];
            _water = new float[count];
            _lava = new float[count];
            _sand = new float[count];
            _ice = new float[count];
            _bog = new float[count];
            _rift = new float[count];
            _height = new float[count];
            _slope = new float[count];
            _roadMask = new float[count];

            Pools = LiquidShape.ForLevel(level);
            FillMasks(level);
            FillHeights(road);
            LevelColonyRigPad(level);
            FillSlopes();
        }

        public float CellSize { get; }

        /// <summary>The level's pools with their smoothed shores; the liquid sheets and the swept banks follow the same shapes.</summary>
        public IReadOnlyList<LiquidShape> Pools { get; }

        /// <summary>How many cells the landscape reaches past the field along x and along z.</summary>
        public int MarginCellsX { get; }
        public int MarginCellsZ { get; }
        public Vector2 HalfSize { get; }
        public int Nx { get; }
        public int Nz { get; }
        public float MinX { get; }
        public float MinZ { get; }
        public float Step { get; }
        public float Width => (Nx - 1) * Step;
        public float Depth => (Nz - 1) * Step;

        public float WorldX(int ix) => MinX + ix * Step;
        public float WorldZ(int iz) => MinZ + iz * Step;
        public int Index(int ix, int iz) => iz * Nx + ix;

        public float HeightAt(float x, float z) => Sample(_height, x, z);
        public float PlateauAt(float x, float z) => Sample(_plateau, x, z);
        public float WaterAt(float x, float z) => Sample(_water, x, z);
        public float LavaAt(float x, float z) => Sample(_lava, x, z);
        public float SandAt(float x, float z) => Sample(_sand, x, z);
        public float IceAt(float x, float z) => Sample(_ice, x, z);
        public float BogAt(float x, float z) => Sample(_bog, x, z);
        public float RiftAt(float x, float z) => Sample(_rift, x, z);
        public float SlopeAt(float x, float z) => Sample(_slope, x, z);
        public float RoadAt(float x, float z) => Sample(_roadMask, x, z);

        /// <summary>Distance outside the playing field in cells; zero anywhere inside it.</summary>
        public float OutsideAt(float x, float z)
        {
            float dx = Mathf.Max(Mathf.Abs(x) - HalfSize.x, 0f);
            float dz = Mathf.Max(Mathf.Abs(z) - HalfSize.y, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz) / CellSize;
        }

        public float HeightAtVertex(int ix, int iz) => _height[Index(ix, iz)];

        /// <summary>Bilinear sample of one of the per-point arrays at a world position.</summary>
        private float Sample(float[] data, float x, float z)
        {
            float fx = Mathf.Clamp((x - MinX) / Step, 0f, Nx - 1.001f);
            float fz = Mathf.Clamp((z - MinZ) / Step, 0f, Nz - 1.001f);
            int ix = (int)fx;
            int iz = (int)fz;
            float tx = fx - ix;
            float tz = fz - iz;
            float a = Mathf.Lerp(data[Index(ix, iz)], data[Index(ix + 1, iz)], tx);
            float b = Mathf.Lerp(data[Index(ix, iz + 1)], data[Index(ix + 1, iz + 1)], tx);
            return Mathf.Lerp(a, b, tz);
        }

        private void FillMasks(LevelDefinition level)
        {
            GridMap grid = level.Grid;
            TerrainMap map = level.Terrain;
            var core = new float[Nx * Nz];

            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    float x = WorldX(ix);
                    float z = WorldZ(iz);
                    // The edges between kinds wander a little, so lakes and ridges are not made of squares.
                    float warpX = (TerrainNoise.Fbm(x * 0.8f, z * 0.8f, 2, 21f) - 0.5f) * EdgeWander * CellSize;
                    float warpZ = (TerrainNoise.Fbm(x * 0.8f + 40f, z * 0.8f, 2, 33f) - 0.5f) * EdgeWander * CellSize;
                    Vector2Int cell = grid.WorldToCell(new Vector3(x + warpX, 0f, z + warpZ));
                    int i = Index(ix, iz);
                    if (map.KindAt(cell) == TerrainKind.Raised)
                    {
                        _plateau[i] = 1f;
                    }

                    // The middle of a raised cell must be exactly at plateau height: that is where the tower stands.
                    Vector2Int own = grid.WorldToCell(new Vector3(x, 0f, z));
                    Vector3 centre = grid.CellToWorld(own);
                    bool nearCentre = Mathf.Abs(x - centre.x) < Step * 1.01f && Mathf.Abs(z - centre.z) < Step * 1.01f;
                    core[i] = nearCentre && map.KindAt(own) == TerrainKind.Raised ? 1f : 0f;
                }
            }

            Blur(_plateau);
            for (int i = 0; i < core.Length; i++)
            {
                _plateau[i] = Mathf.Max(_plateau[i], core[i]);
            }

            ConfineToCells(grid, map);
            FillLiquids(grid, map);
        }

        // A pool follows its smoothed shore (LiquidShape); a rift keeps its shore inside its own cells: the mask rises from
        // the cell outline inwards, with a little wobble, so the ground beside it is level right up to the cell edge.
        private void FillLiquids(GridMap grid, TerrainMap map)
        {
            float shore = ShoreWidthCells * CellSize;
            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    float x = WorldX(ix);
                    float z = WorldZ(iz);
                    int i = Index(ix, iz);
                    _water[i] = 0f;
                    _lava[i] = 0f;
                    _sand[i] = 0f;
                    _ice[i] = 0f;
                    _bog[i] = 0f;
                    _rift[i] = 0f;

                    FillPools(i, x, z);
                    Vector2Int cell = grid.WorldToCell(new Vector3(x, 0f, z));
                    if (map.KindAt(cell) != TerrainKind.Rift)
                    {
                        continue;
                    }

                    float wobble = (TerrainNoise.Fbm(x * 1.7f, z * 1.7f, 2, 21f) - 0.5f) * ShoreWobbleCells * CellSize;
                    _rift[i] = TerrainNoise.SmoothStep(0f, shore, InsideDistance(grid, map, cell, TerrainKind.Rift, x, z) + wobble);
                }
            }
        }

        // A pool's mask rises across its smoothed shore, so the basin is round even where the cells make a corner.
        private void FillPools(int i, float x, float z)
        {
            for (int p = 0; p < Pools.Count; p++)
            {
                LiquidShape pool = Pools[p];
                Rect bounds = pool.Bounds;
                if (x < bounds.xMin - LiquidShape.Reach || x > bounds.xMax + LiquidShape.Reach || z < bounds.yMin - LiquidShape.Reach || z > bounds.yMax + LiquidShape.Reach)
                {
                    continue;
                }

                float mask = TerrainNoise.SmoothStep(PoolShoreStart, PoolShoreEnd, pool.SignedDistance(x, z));
                if (mask <= 0f)
                {
                    continue;
                }

                float[] layer = LayerOf(pool.Kind);
                layer[i] = Mathf.Max(layer[i], mask);
            }
        }

        private float[] LayerOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return _water;
                case TerrainKind.Lava: return _lava;
                case TerrainKind.Ice: return _ice;
                case TerrainKind.Bog: return _bog;
                default: return _sand;
            }
        }

        // Distance from a point inside a cell to the nearest outline of its kind: the sides and the corner points
        // that border a cell of another kind. Deep inside a large patch it is simply the half cell.
        private float InsideDistance(GridMap grid, TerrainMap map, Vector2Int cell, TerrainKind kind, float x, float z)
        {
            Vector3 centre = grid.CellToWorld(cell);
            float half = CellSize * 0.5f;
            float localX = x - centre.x;
            float localZ = z - centre.z;
            float depth = half;
            for (int s = 0; s < Sides.Length; s++)
            {
                Vector2Int side = Sides[s];
                Vector2Int next = Sides[(s + 1) % Sides.Length];
                bool sideSame = map.KindAt(cell + side) == kind;
                bool nextSame = map.KindAt(cell + next) == kind;
                float u = side.x != 0 ? localX * side.x : localZ * side.y;
                float v = next.x != 0 ? localX * next.x : localZ * next.y;
                if (!sideSame)
                {
                    depth = Mathf.Min(depth, half - u);
                }

                // An outside corner is rounded off: inside the corner square the outline is a quarter circle.
                float inset = half - CornerRadiusCells * CellSize;
                if (!sideSame && !nextSame && u > inset && v > inset)
                {
                    depth = Mathf.Min(depth, CornerRadiusCells * CellSize - Mathf.Sqrt((u - inset) * (u - inset) + (v - inset) * (v - inset)));
                }

                if (sideSame && nextSame && map.KindAt(cell + side + next) != kind)
                {
                    float cx = (side.x + next.x) * half;
                    float cz = (side.y + next.y) * half;
                    depth = Mathf.Min(depth, Mathf.Sqrt((localX - cx) * (localX - cx) + (localZ - cz) * (localZ - cz)));
                }
            }

            return depth;
        }

        // Raised ground stays inside its own cells: a hill never spills onto the flat cell beside it, so a tower on
        // the cell next to it (even shifted away from the road inside its cell) stands on level ground.
        private void ConfineToCells(GridMap grid, TerrainMap map)
        {
            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    if (map.KindAt(grid.WorldToCell(new Vector3(WorldX(ix), 0f, WorldZ(iz)))) != TerrainKind.Raised)
                    {
                        _plateau[Index(ix, iz)] = 0f;
                    }
                }
            }
        }

        private void FillHeights(RoadShape road)
        {
            float halfWidth = road.HalfWidth;
            float shoulder = RoadShoulderCells * CellSize;

            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    float x = WorldX(ix);
                    float z = WorldZ(iz);
                    int i = Index(ix, iz);
                    float outside = OutsideAt(x, z);

                    float h = _plateau[i] * TerrainMap.PlateauHeight;

                    // Every pool sinks below its liquid just inside its smoothed shore, under the swept bank; lava
                    // lies as deep as water (the bank is its rim now, a thrown-up crater rim would poke through it).
                    h -= TerrainNoise.SmoothStep(BasinStart, BasinFull, _water[i]) * BasinDepth;
                    h -= TerrainNoise.SmoothStep(BasinStart, BasinFull, _sand[i]) * SandDepth;
                    // A frozen lake sits in a shallow dish; a bog sinks a little deeper than sand into soft ground.
                    h -= TerrainNoise.SmoothStep(BasinStart, BasinFull, _ice[i]) * IceDepth;
                    h -= TerrainNoise.SmoothStep(BasinStart, BasinFull, _bog[i]) * BogDepth;
                    h -= TerrainNoise.SmoothStep(RiftWallStart, RiftWallFull, _rift[i]) * RiftDepth;
                    h -= TerrainNoise.SmoothStep(BasinStart, BasinFull, _lava[i]) * BasinDepth;

                    // Low hills rise towards the edge of the map and hide where the world stops.
                    float rim = TerrainNoise.SmoothStep(RimStartCells, RimEndCells, outside);
                    h += rim * RimHeight * (0.55f + 0.9f * TerrainNoise.Fbm(x * 0.17f, z * 0.17f, 3, 11f));

                    // Never negative outside the basins, so the liquid planes cannot show through dry ground.
                    float bumps = TerrainNoise.SmoothStep(OuterBumpStartCells, OuterBumpEndCells, outside);
                    h += bumps * OuterBumpHeight * TerrainNoise.Fbm(x * 0.6f, z * 0.6f, 2, 3f);

                    float distance = road.DistanceTo(x, z);
                    float mask = 1f - TerrainNoise.SmoothStep(halfWidth, halfWidth + shoulder, distance);
                    _roadMask[i] = mask;
                    _height[i] = h * (1f - mask);
                }
            }
        }

        // Round 41: the colony rig stands just outside the field, where the rim starts to climb, so its deck hung over the
        // downhill side. The ground under its footprint is cut down to the footprint's lowest point and
        // blends back into the hills round it (a filled mesa stood on a cliff wall; a notch dug into the rim reads as
        // ground the colony cleared); the road and the playing field are left as they are.
        private void LevelColonyRigPad(LevelDefinition level)
        {
            ColonyRigDefinition rig = level.Theme != null && level.Theme.Environment != null ? level.Theme.Environment.ColonyRig : null;
            if (rig == null || rig.Drill == null)
            {
                return;
            }

            var clearance = new StructureClearance(level);
            if (!clearance.HasColonyRig)
            {
                return;
            }

            Vector3 centre = clearance.ColonyRigPosition;
            float flat = rig.FootprintRadius;
            float outer = flat + rig.GroundBlend;
            int minX = Mathf.Max(0, Mathf.FloorToInt((centre.x - outer - MinX) / Step));
            int maxX = Mathf.Min(Nx - 1, Mathf.CeilToInt((centre.x + outer - MinX) / Step));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((centre.z - outer - MinZ) / Step));
            int maxZ = Mathf.Min(Nz - 1, Mathf.CeilToInt((centre.z + outer - MinZ) / Step));

            float padHeight = float.PositiveInfinity;
            for (int iz = minZ; iz <= maxZ; iz++)
            {
                for (int ix = minX; ix <= maxX; ix++)
                {
                    if (DistanceTo(centre, ix, iz) <= flat)
                    {
                        padHeight = Mathf.Min(padHeight, _height[Index(ix, iz)]);
                    }
                }
            }

            if (float.IsPositiveInfinity(padHeight))
            {
                return;
            }

            for (int iz = minZ; iz <= maxZ; iz++)
            {
                for (int ix = minX; ix <= maxX; ix++)
                {
                    int i = Index(ix, iz);
                    float weight = 1f - TerrainNoise.SmoothStep(flat, outer, DistanceTo(centre, ix, iz));
                    weight *= TerrainNoise.SmoothStep(0f, RigPadEdgeFadeCells, OutsideAt(WorldX(ix), WorldZ(iz)));
                    weight *= 1f - _roadMask[i];
                    _height[i] = Mathf.Lerp(_height[i], padHeight, weight);
                }
            }
        }

        private float DistanceTo(Vector3 centre, int ix, int iz)
        {
            float dx = WorldX(ix) - centre.x;
            float dz = WorldZ(iz) - centre.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void FillSlopes()
        {
            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    int x0 = Mathf.Max(ix - 1, 0);
                    int x1 = Mathf.Min(ix + 1, Nx - 1);
                    int z0 = Mathf.Max(iz - 1, 0);
                    int z1 = Mathf.Min(iz + 1, Nz - 1);
                    float dx = (_height[Index(x1, iz)] - _height[Index(x0, iz)]) / ((x1 - x0) * Step);
                    float dz = (_height[Index(ix, z1)] - _height[Index(ix, z0)]) / ((z1 - z0) * Step);
                    _slope[Index(ix, iz)] = Mathf.Sqrt(dx * dx + dz * dz);
                }
            }
        }

        // One box blur pass of radius one point in both directions: soft edges between kinds, and the middle of
        // a one-cell patch keeps its full value.
        private void Blur(float[] data)
        {
            var temp = new float[data.Length];
            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    float sum = 0f;
                    for (int k = -1; k <= 1; k++)
                    {
                        sum += data[Index(Mathf.Clamp(ix + k, 0, Nx - 1), iz)];
                    }

                    temp[Index(ix, iz)] = sum / 3f;
                }
            }

            for (int iz = 0; iz < Nz; iz++)
            {
                for (int ix = 0; ix < Nx; ix++)
                {
                    float sum = 0f;
                    for (int k = -1; k <= 1; k++)
                    {
                        sum += temp[Index(ix, Mathf.Clamp(iz + k, 0, Nz - 1))];
                    }

                    data[Index(ix, iz)] = sum / 3f;
                }
            }
        }
    }
}
