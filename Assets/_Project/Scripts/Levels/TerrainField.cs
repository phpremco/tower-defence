using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The landscape as numbers on a fine grid of points that reaches well past the playing field: height, how much of
    /// each terrain kind is under every point, and how steep the ground is there. The ground mesh, its painted
    /// texture, the liquids and the decoration all read from the same field, so they always agree.
    /// </summary>
    public sealed class TerrainField
    {
        public const int SubdivisionsPerCell = 4;
        public const int MarginCells = 9;

        private const float EdgeWander = 0.7f;
        private const float BasinDepth = 0.42f;
        private const float SandDepth = 0.16f;
        private const float RimStartCells = 1.5f;
        private const float RimEndCells = 7f;
        private const float RimHeight = 2.2f;
        public const float RoadHalfWidthCells = 0.4f;
        private const float RoadShoulderCells = 0.3f;
        private const float OuterBumpHeight = 0.1f;
        private const float OuterBumpStartCells = 0.5f;
        private const float OuterBumpEndCells = 2.5f;

        private readonly float[] _plateau;
        private readonly float[] _water;
        private readonly float[] _lava;
        private readonly float[] _sand;
        private readonly float[] _height;
        private readonly float[] _slope;
        private readonly float[] _roadMask;

        public TerrainField(LevelDefinition level, RoadShape road)
        {
            CellSize = level.CellSize;
            Vector2 size = level.WorldSize;
            HalfSize = size * 0.5f;
            float margin = MarginCells * CellSize;
            MinX = -HalfSize.x - margin;
            MinZ = -HalfSize.y - margin;
            Step = CellSize / SubdivisionsPerCell;
            Nx = Mathf.RoundToInt((size.x + 2f * margin) / Step) + 1;
            Nz = Mathf.RoundToInt((size.y + 2f * margin) / Step) + 1;

            int count = Nx * Nz;
            _plateau = new float[count];
            _water = new float[count];
            _lava = new float[count];
            _sand = new float[count];
            _height = new float[count];
            _slope = new float[count];
            _roadMask = new float[count];

            FillMasks(level);
            FillHeights(road);
            FillSlopes();
        }

        public float CellSize { get; }
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
                    switch (map.KindAt(cell))
                    {
                        case TerrainKind.Raised:
                            _plateau[i] = 1f;
                            break;
                        case TerrainKind.Water:
                            _water[i] = 1f;
                            break;
                        case TerrainKind.Lava:
                            _lava[i] = 1f;
                            break;
                        case TerrainKind.Quicksand:
                            _sand[i] = 1f;
                            break;
                    }

                    // The middle of a raised cell must be exactly at plateau height: that is where the tower stands.
                    Vector2Int own = grid.WorldToCell(new Vector3(x, 0f, z));
                    Vector3 centre = grid.CellToWorld(own);
                    bool nearCentre = Mathf.Abs(x - centre.x) < Step * 1.01f && Mathf.Abs(z - centre.z) < Step * 1.01f;
                    core[i] = nearCentre && map.KindAt(own) == TerrainKind.Raised ? 1f : 0f;
                }
            }

            // Liquids get a wider blur than raised ground: rounder shores, and the plateau keeps crisp cliffs.
            Blur(_plateau);
            for (int pass = 0; pass < 2; pass++)
            {
                Blur(_water);
                Blur(_lava);
                Blur(_sand);
            }
            for (int i = 0; i < core.Length; i++)
            {
                _plateau[i] = Mathf.Max(_plateau[i], core[i]);
            }
        }

        private void FillHeights(RoadShape road)
        {
            float halfWidth = RoadHalfWidthCells * CellSize;
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
                    h -= Mathf.Clamp01(_water[i] + _lava[i]) * BasinDepth + _sand[i] * SandDepth;

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
