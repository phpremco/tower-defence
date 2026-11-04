using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The splat map of the environment ground shader, built from the grid when the level loads: R road and G plaza
    /// follow the cell outline exactly (the kerbs stand on it), B marks the patches of the second ground layer away from
    /// the road, A the continent's element terrain. A few texels per cell is enough: the shader's height blend breaks the
    /// edges up, the kerbs cover the road's. Covers the whole landscape, margins included.
    /// </summary>
    public static class EnvironmentSplat
    {
        private const int TexelsPerCell = 8;
        private const float AltSoftness = 0.14f;
        private const float AltSeed = 57f;
        private const int AltOctaves = 3;
        private const float ElementGain = 4f;

        private static readonly Vector2Int[] Neighbours =
        {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public static Texture2D Build(LevelDefinition level, TerrainField field, EnvironmentSet set)
        {
            int width = Mathf.CeilToInt(field.Width / field.CellSize * TexelsPerCell);
            int height = Mathf.CeilToInt(field.Depth / field.CellSize * TexelsPerCell);
            var pixels = new Color32[width * height];
            GridMap grid = level.Grid;
            RoadCorridor road = level.Road;
            Dictionary<Vector2Int, int> distance = RoadDistance(level);
            int clearance = Mathf.CeilToInt(set.AltRoadClearance);

            for (int ty = 0; ty < height; ty++)
            {
                float z = field.MinZ + (ty + 0.5f) / height * field.Depth;
                for (int tx = 0; tx < width; tx++)
                {
                    float x = field.MinX + (tx + 0.5f) / width * field.Width;
                    Vector2Int cell = grid.WorldToCell(new Vector3(x, 0f, z));
                    bool isRoad = road.IsRoad(cell);
                    bool plaza = isRoad && road.IsPlaza(cell);
                    float alt = 0f;
                    if (!isRoad && !level.Terrain.IsLiquid(cell) && (!distance.TryGetValue(cell, out int steps) || steps > clearance))
                    {
                        float noise = TerrainNoise.Fbm(x * set.AltNoiseScale, z * set.AltNoiseScale, AltOctaves, AltSeed);
                        alt = TerrainNoise.SmoothStep(set.AltCoverage, set.AltCoverage + AltSoftness, noise);
                    }

                    float element = Mathf.Clamp01(ElementMask(field, set.ElementTerrain, x, z) * ElementGain);
                    pixels[ty * width + tx] = new Color32(
                        (byte)(isRoad && !plaza ? 255 : 0),
                        (byte)(plaza ? 255 : 0),
                        (byte)Mathf.RoundToInt(alt * 255f),
                        (byte)Mathf.RoundToInt(element * 255f));
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "EnvironmentSplat",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>The centre of every plaza (each connected group of plaza cells), so its tiles can be laid from there.</summary>
        public static List<Vector2> PlazaCentres(LevelDefinition level)
        {
            var centres = new List<Vector2>();
            var seen = new HashSet<Vector2Int>();
            GridMap grid = level.Grid;
            foreach (Vector2Int start in level.Road.PlazaCells)
            {
                if (!seen.Add(start))
                {
                    continue;
                }

                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                Vector3 sum = Vector3.zero;
                int count = 0;
                while (queue.Count > 0)
                {
                    Vector2Int cell = queue.Dequeue();
                    sum += grid.CellToWorld(cell);
                    count++;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int next = cell + Neighbours[i];
                        if (level.Road.IsPlaza(next) && seen.Add(next))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }

                centres.Add(new Vector2(sum.x / count, sum.z / count));
            }

            return centres;
        }

        private static float ElementMask(TerrainField field, TerrainKind kind, float x, float z)
        {
            switch (kind)
            {
                case TerrainKind.Rift: return field.RiftAt(x, z);
                case TerrainKind.Quicksand: return field.SandAt(x, z);
                case TerrainKind.Ice: return field.IceAt(x, z);
                case TerrainKind.Bog: return field.BogAt(x, z);
                case TerrainKind.Lava: return field.LavaAt(x, z);
                case TerrainKind.Water: return field.WaterAt(x, z);
                default: return 0f;
            }
        }

        // Steps (eight-way) from every cell of the landscape to the nearest road cell, as far as the clearance needs.
        private static Dictionary<Vector2Int, int> RoadDistance(LevelDefinition level)
        {
            var distance = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();
            foreach (Vector2Int cell in level.Road.Cells)
            {
                distance[cell] = 0;
                queue.Enqueue(cell);
            }

            const int Reach = 4;
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                int steps = distance[cell];
                if (steps >= Reach)
                {
                    continue;
                }

                foreach (Vector2Int offset in Neighbours)
                {
                    Vector2Int next = cell + offset;
                    if (!distance.ContainsKey(next))
                    {
                        distance[next] = steps + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distance;
        }
    }
}
