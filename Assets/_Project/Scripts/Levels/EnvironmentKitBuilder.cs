using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Puts a level's environment kit on the field from its set: the pieces <see cref="EnvironmentLayout"/> asks for,
    /// small scatter on the natural ground (pebbles and splinters round the field, shells round the nests), crystal
    /// veins in the open and silhouettes far behind the field in the mist. Everything goes to the instancer; the
    /// placement is deterministic, so a level looks the same every time.
    /// </summary>
    public static class EnvironmentKitBuilder
    {
        private const float ScatterScaleMin = 0.7f;
        private const float ScatterScaleSpread = 0.6f;
        private const float ScatterJitter = 0.8f;
        private const int ScatterReachCells = 6;
        private const float NestScatterMin = 0.7f;
        private const int NestScatterCount = 7;
        private const float PadHalf = 0.47f;
        private const float SilhouetteSpread = 1.4f;

        /// <summary>Lays the kit, scatter, veins and silhouettes; returns the layout (for tests and reports).</summary>
        public static EnvironmentLayout Build(LevelDefinition level, EnvironmentSet set, TerrainField field, EnvironmentInstancer instancer, bool low)
        {
            EnvironmentLayout layout = new EnvironmentLayout(level, field).Build(set.CliffInsetCells, set.CliffSideReach);
            foreach (EnvironmentPlacement placement in layout.Placements)
            {
                EnvironmentMesh piece = set.Piece(placement.Kind);
                if (piece != null)
                {
                    instancer.Add(piece, placement.Matrix, low);
                }
            }

            var natural = new NaturalGround(level);
            PlaceScatter(level, set, field, instancer, low, natural);
            PlaceNestScatter(level, set, field, instancer, low, natural);
            PlaceSilhouettes(level, set, field, instancer, low);
            return layout;
        }

        // Where small things may lie: not on the road, a pool or a build pad.
        private sealed class NaturalGround
        {
            private readonly LevelDefinition _level;
            private readonly BuildGrid _build;

            public NaturalGround(LevelDefinition level)
            {
                _level = level;
                _build = new BuildGrid(level);
            }

            public bool Free(Vector3 position)
            {
                GridMap grid = _level.Grid;
                Vector2Int cell = grid.WorldToCell(position);
                if (_level.Road.IsRoad(cell) || _level.Terrain.IsLiquid(cell))
                {
                    return false;
                }

                // On a build cell only the thin seam between the pads is free.
                Vector3 local = position - grid.CellToWorld(cell);
                bool onPad = Mathf.Abs(local.x) < PadHalf * grid.CellSize && Mathf.Abs(local.z) < PadHalf * grid.CellSize;
                return !(_build.CanBuild(cell) && onPad);
            }
        }

        // One small piece per few cells of open ground round the field and on the blocked cells, a vein now and then.
        private static void PlaceScatter(LevelDefinition level, EnvironmentSet set, TerrainField field, EnvironmentInstancer instancer, bool low, NaturalGround natural)
        {
            if (set.Scatter.Length == 0)
            {
                return;
            }

            GridMap grid = level.Grid;
            EnvironmentMesh vein = set.Piece(EnvironmentPieceKind.CrystalVein);
            int reachX = Mathf.Min(ScatterReachCells, field.MarginCellsX - 1);
            int reachZ = Mathf.Min(ScatterReachCells, field.MarginCellsZ - 1);
            for (int x = -reachX; x < grid.Size.x + reachX; x++)
            {
                for (int y = -reachZ; y < grid.Size.y + reachZ; y++)
                {
                    float roll = TerrainNoise.Hash(x, y, 11);
                    Vector3 centre = grid.CellToWorld(new Vector2Int(x, y));
                    var spot = new Vector3(
                        centre.x + (TerrainNoise.Hash(x, y, 12) - 0.5f) * ScatterJitter * grid.CellSize,
                        0f,
                        centre.z + (TerrainNoise.Hash(x, y, 13) - 0.5f) * ScatterJitter * grid.CellSize);
                    if (!natural.Free(spot))
                    {
                        continue;
                    }

                    float yaw = TerrainNoise.Hash(x, y, 14) * 360f;
                    spot.y = field.HeightAt(spot.x, spot.z);
                    if (vein != null && roll < set.VeinChance)
                    {
                        instancer.Add(vein, Matrix4x4.TRS(spot, Quaternion.Euler(0f, yaw, 0f), Vector3.one), low);
                    }
                    else if (roll < set.VeinChance + set.ScatterChance)
                    {
                        EnvironmentMesh piece = set.Scatter[(int)(TerrainNoise.Hash(x, y, 15) * set.Scatter.Length) % set.Scatter.Length];
                        float scale = ScatterScaleMin + TerrainNoise.Hash(x, y, 16) * ScatterScaleSpread;
                        instancer.Add(piece, Matrix4x4.TRS(spot, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale), low);
                    }
                }
            }
        }

        // Shell fragments in a loose ring round every nest.
        private static void PlaceNestScatter(LevelDefinition level, EnvironmentSet set, TerrainField field, EnvironmentInstancer instancer, bool low, NaturalGround natural)
        {
            if (set.NestScatter.Length == 0)
            {
                return;
            }

            foreach (ElementNest nest in level.Nests)
            {
                Vector3 centre = level.Grid.CellToWorld(nest.Cell);
                for (int i = 0; i < NestScatterCount; i++)
                {
                    float angle = TerrainNoise.Hash(nest.Cell.x, nest.Cell.y, 20 + i) * 360f;
                    float distance = Mathf.Lerp(NestScatterMin, set.NestScatterRadius, TerrainNoise.Hash(nest.Cell.x, nest.Cell.y, 40 + i));
                    Vector3 spot = centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                    if (!natural.Free(spot))
                    {
                        continue;
                    }

                    spot.y = field.HeightAt(spot.x, spot.z);
                    EnvironmentMesh piece = set.NestScatter[i % set.NestScatter.Length];
                    float scale = ScatterScaleMin + TerrainNoise.Hash(nest.Cell.x, nest.Cell.y, 60 + i) * ScatterScaleSpread;
                    instancer.Add(piece, Matrix4x4.TRS(spot, Quaternion.Euler(0f, angle * 3f, 0f), Vector3.one * scale), low);
                }
            }
        }

        // Tall shapes far behind the back wall and the flanks, half lost in the mist, so the world reads as bigger.
        private static void PlaceSilhouettes(LevelDefinition level, EnvironmentSet set, TerrainField field, EnvironmentInstancer instancer, bool low)
        {
            int count = set.SilhouetteCount;
            if (set.Silhouettes.Length == 0 || count == 0)
            {
                return;
            }

            Vector2 half = level.WorldSize * 0.5f;
            float cell = level.CellSize;
            float far = Mathf.Min(set.SilhouetteDistance.y, field.MarginCellsZ - 1);
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-half.x, half.x, (i + 0.5f) / count) * SilhouetteSpread;
                float depth = Mathf.Lerp(set.SilhouetteDistance.x, far, TerrainNoise.Hash(i, count, 30));
                var spot = new Vector3(x, 0f, half.y + depth * cell);
                spot.y = field.HeightAt(spot.x, spot.z);
                EnvironmentMesh piece = set.Silhouettes[(int)(TerrainNoise.Hash(i, 7, 31) * set.Silhouettes.Length) % set.Silhouettes.Length];
                float yaw = TerrainNoise.Hash(i, 7, 32) * 360f;
                instancer.Add(piece, Matrix4x4.TRS(spot, Quaternion.Euler(0f, yaw, 0f), Vector3.one), low);
            }
        }
    }
}
