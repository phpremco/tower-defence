using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Places the level's decoration: formations on the blocked cells, a growing wilderness around the field and a
    /// few small tufts between the build cells. Placement is deterministic, so a level looks the same every time.
    /// </summary>
    public static class PropScatterBuilder
    {
        private const float BlockedScaleMin = 0.95f;
        private const float BlockedScaleSpread = 0.45f;
        private const float OuterChanceNear = 0.55f;
        private const float OuterChanceFar = 0.4f;
        private const float OuterScaleGrowth = 0.07f;
        private const float TuftChance = 0.2f;
        private const float TuftScale = 0.4f;
        private const float TuftCornerOffset = 0.36f;
        private const float LiquidAvoid = 0.12f;
        private const float SinkIntoGround = 0.03f;
        private const int OuterRingCells = TerrainField.MarginCells - 1;

        public static void Build(LevelDefinition level, VisualTheme theme, TerrainField field, Transform parent)
        {
            PropKind[] kinds = theme.PropKinds;
            if (kinds == null || kinds.Length == 0)
            {
                return;
            }

            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            var placer = new Placer(theme, field, root);
            GridMap grid = level.Grid;
            TerrainMap terrain = level.Terrain;
            var road = new HashSet<Vector2Int>(LevelValidator.RoadCells(level));
            var blocked = new HashSet<Vector2Int>(level.BlockedCells);

            foreach (Vector2Int cell in blocked)
            {
                int hash = Hash(cell);
                Vector3 centre = grid.CellToWorld(cell);
                float scale = BlockedScaleMin + (hash % 100) / 100f * BlockedScaleSpread;
                placer.Place(kinds[hash % kinds.Length], hash, centre.x, centre.z, scale);
            }

            int width = level.GridSize.x;
            int height = level.GridSize.y;
            for (int x = -OuterRingCells; x < width + OuterRingCells; x++)
            {
                for (int y = -OuterRingCells; y < height + OuterRingCells; y++)
                {
                    var cell = new Vector2Int(x, y);
                    bool inside = x >= 0 && y >= 0 && x < width && y < height;
                    int hash = Hash(cell);
                    Vector3 centre = grid.CellToWorld(cell);

                    if (inside)
                    {
                        PlaceTuft(placer, kinds, cell, hash, centre, road, blocked, terrain);
                        continue;
                    }

                    float distance = field.OutsideAt(centre.x, centre.z);
                    float chance = distance < 3f ? OuterChanceNear : OuterChanceFar;
                    if ((hash % 1000) / 1000f > chance || terrain.IsLiquid(cell))
                    {
                        continue;
                    }

                    float jitterX = ((hash >> 3) % 100) / 100f - 0.5f;
                    float jitterZ = ((hash >> 11) % 100) / 100f - 0.5f;
                    float scale = 0.85f + (hash % 7) * 0.05f + distance * OuterScaleGrowth;
                    placer.Place(kinds[(hash >> 5) % kinds.Length], hash, centre.x + jitterX * 0.9f, centre.z + jitterZ * 0.9f, scale);
                }
            }
        }

        // Small growth in the corner of a free build cell: it never reaches the middle where a tower stands.
        private static void PlaceTuft(Placer placer, PropKind[] kinds, Vector2Int cell, int hash, Vector3 centre, HashSet<Vector2Int> road, HashSet<Vector2Int> blocked, TerrainMap terrain)
        {
            if ((hash % 1000) / 1000f > TuftChance || road.Contains(cell) || blocked.Contains(cell) || terrain.IsLiquid(cell))
            {
                return;
            }

            float sx = (hash & 1) == 0 ? -TuftCornerOffset : TuftCornerOffset;
            float sz = (hash & 2) == 0 ? -TuftCornerOffset : TuftCornerOffset;
            PropKind kind = (hash >> 4) % 2 == 0 ? PropKind.Frond : PropKind.Rock;
            placer.Place(kind, hash, centre.x + sx, centre.z + sz, TuftScale);
        }

        // A stable pseudo-random number per cell.
        private static int Hash(Vector2Int cell)
        {
            unchecked
            {
                int h = cell.x * 73856093 ^ cell.y * 19349663;
                h ^= h >> 13;
                return h & 0x7fffffff;
            }
        }

        // Builds each mesh once and puts instances of it on the ground.
        private sealed class Placer
        {
            private readonly TerrainField _field;
            private readonly Transform _root;
            private readonly Material[] _materials;
            private readonly Dictionary<(PropKind, int), Mesh> _meshes = new Dictionary<(PropKind, int), Mesh>();

            public Placer(VisualTheme theme, TerrainField field, Transform root)
            {
                _field = field;
                _root = root;
                _materials = PropFactory.CreateMaterials(theme);
            }

            public void Place(PropKind kind, int hash, float x, float z, float scale)
            {
                if (_field.WaterAt(x, z) > LiquidAvoid || _field.LavaAt(x, z) > LiquidAvoid || _field.SandAt(x, z) > LiquidAvoid)
                {
                    return;
                }

                int variant = (hash >> 7) % PropFactory.Variants;
                if (!_meshes.TryGetValue((kind, variant), out Mesh mesh))
                {
                    mesh = PropFactory.Create(kind, variant);
                    _meshes[(kind, variant)] = mesh;
                }

                var go = new GameObject(kind.ToString());
                go.transform.SetParent(_root, false);
                go.transform.position = new Vector3(x, _field.HeightAt(x, z) - SinkIntoGround, z);
                go.transform.rotation = Quaternion.Euler(0f, hash % 360, 0f);
                go.transform.localScale = Vector3.one * scale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = _materials;
            }
        }
    }
}
