using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Water, lava and quicksand. Each kind is one flat sheet at a fixed level under the plain: it only shows where
    /// the ground has been sunk into a basin, so the shoreline is wherever the ground dips below it.
    /// </summary>
    public static class TerrainLiquidBuilder
    {
        private const float WaterLevel = -0.1f;
        private const float LavaLevel = -0.13f;
        private const float SandLevel = -0.06f;
        private const float TextureWorldSize = 3f;
        private const float WaterAlpha = 0.68f;
        private const float LavaEmission = 1.4f;
        private const float WaterGlow = 0.35f;
        private const float PaddingCells = 1.5f;

        public static void Build(LevelDefinition level, VisualTheme theme, Transform parent)
        {
            TerrainMap map = level.Terrain;
            float cell = level.CellSize;
            GridMap grid = level.Grid;

            foreach (TerrainKind kind in new[] { TerrainKind.Water, TerrainKind.Lava, TerrainKind.Quicksand })
            {
                if (!TryBounds(map, grid, kind, cell, out Rect bounds))
                {
                    continue;
                }

                var go = new GameObject($"Liquid_{kind}");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = Sheet(bounds);

                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                Material material = MaterialFor(kind, theme);
                renderer.sharedMaterial = material;
                go.transform.position = new Vector3(0f, LevelOf(kind), 0f);
                go.AddComponent<LiquidFlow>().Initialize(material, FlowOf(kind));
            }
        }

        private static bool TryBounds(TerrainMap map, GridMap grid, TerrainKind kind, float cellSize, out Rect bounds)
        {
            bool found = false;
            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;

            foreach (var entry in map.Cells)
            {
                if (entry.Value != kind)
                {
                    continue;
                }

                Vector3 world = grid.CellToWorld(entry.Key);
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z);
                maxZ = Mathf.Max(maxZ, world.z);
                found = true;
            }

            float pad = PaddingCells * cellSize;
            bounds = found ? Rect.MinMaxRect(minX - pad, minZ - pad, maxX + pad, maxZ + pad) : default;
            return found;
        }

        private static Mesh Sheet(Rect bounds)
        {
            var mesh = new Mesh { name = "Liquid" };
            mesh.vertices = new[]
            {
                new Vector3(bounds.xMin, 0f, bounds.yMin), new Vector3(bounds.xMin, 0f, bounds.yMax),
                new Vector3(bounds.xMax, 0f, bounds.yMax), new Vector3(bounds.xMax, 0f, bounds.yMin),
            };
            mesh.uv = new[]
            {
                new Vector2(bounds.xMin, bounds.yMin) / TextureWorldSize, new Vector2(bounds.xMin, bounds.yMax) / TextureWorldSize,
                new Vector2(bounds.xMax, bounds.yMax) / TextureWorldSize, new Vector2(bounds.xMax, bounds.yMin) / TextureWorldSize,
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float LevelOf(TerrainKind kind) =>
            kind == TerrainKind.Water ? WaterLevel : kind == TerrainKind.Lava ? LavaLevel : SandLevel;

        private static Vector2 FlowOf(TerrainKind kind) =>
            kind == TerrainKind.Water ? new Vector2(0.025f, 0.015f) : kind == TerrainKind.Lava ? new Vector2(0.008f, -0.006f) : new Vector2(-0.004f, 0.003f);

        private static Material MaterialFor(TerrainKind kind, VisualTheme theme)
        {
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetFloat("_Metallic", 0f);

            if (kind == TerrainKind.Water)
            {
                material.SetTexture("_BaseMap", LiquidTextures.Water());
                material.SetColor("_BaseColor", new Color(theme.WaterColor.r, theme.WaterColor.g, theme.WaterColor.b, WaterAlpha));
                material.SetFloat("_Smoothness", 0.45f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", theme.WaterColor * WaterGlow);
                MakeTransparent(material);
            }
            else if (kind == TerrainKind.Lava)
            {
                Texture2D texture = LiquidTextures.Lava();
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.Lerp(Color.white, theme.LavaColor, 0.35f));
                material.SetFloat("_Smoothness", 0.25f);
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", texture);
                material.SetColor("_EmissionColor", Color.white * LavaEmission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetTexture("_BaseMap", LiquidTextures.Sand());
                material.SetColor("_BaseColor", theme.SandColor);
                material.SetFloat("_Smoothness", 0.05f);
            }

            return material;
        }

        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
