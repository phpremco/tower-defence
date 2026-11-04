using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Water, lava and quicksand. Each kind is one rippling sheet at a fixed level under the plain: it only shows
    /// where the ground has been sunk into a basin, so the shoreline is wherever the ground dips below it.
    /// A second glowing layer drifts over the first, motes rise from the surface (embers, dust, spray) and lava
    /// throws flickering light on its banks.
    /// </summary>
    public static class TerrainLiquidBuilder
    {
        private const float WaterLevel = -0.2f;
        private const float LavaLevel = -0.22f;
        private const float SandLevel = -0.12f;
        private const float IceLevel = -0.06f;
        private const float BogLevel = -0.14f;
        private const float RiftLevel = -0.35f;
        private const float RiftVeinGlow = 2.2f;
        private const float RiftTiling = 0.4f;
        private const float RiftBreathDepth = 0.35f;
        // One slow breath every seven seconds.
        private const float RiftBreathSpeed = Mathf.PI * 2f / 7f;
        private const float IceAlpha = 0.9f;
        private const float IceGlow = 0.12f;
        private const float BogAlpha = 0.92f;
        private const float BogGlow = 0.2f;
        private const float TextureWorldSize = 3f;
        private const float OverlayWorldSize = 4.5f;
        private const float WaterAlpha = 0.68f;
        private const float LavaEmission = 1.8f;
        private const float LavaPulseDepth = 0.4f;
        private const float LavaCrustDarkening = 0.55f;
        private const float LavaPulseSpeed = 1.1f;
        private const float WaterGlow = 0.35f;
        private const float PaddingCells = 1.5f;
        private const float VertexSpacing = 0.5f;
        private const float OverlayLift = 0.012f;
        private const float ParticleLift = 0.05f;
        // Bubbles and rings spawn on this inner part of a cell, so nothing that grows on the surface crosses the bank.
        private const float InnerCellShare = 0.35f;
        private const int LightClusterCells = 3;
        private const int MaxLavaLights = 10;
        private const float LavaLightHeight = 0.7f;
        private const float LavaLightRange = 3.4f;
        private const float LavaLightIntensity = 2.2f;
        private const float LavaLightFlicker = 0.3f;
        private const float LavaLightFlickerSpeed = 3f;

        private static readonly TerrainKind[] Kinds = { TerrainKind.Water, TerrainKind.Lava, TerrainKind.Quicksand, TerrainKind.Ice, TerrainKind.Bog, TerrainKind.Rift };

        /// <summary>
        /// Every liquid of the level. With a <see cref="LiquidSet"/> on the theme each pool is its own sheet cut along its
        /// smoothed shore and worn in the set's material; the kinds the set lacks (and the rifts) keep the painted sheet.
        /// </summary>
        public static void Build(LevelDefinition level, VisualTheme theme, Transform parent, TerrainField field, float particleScale = 1f)
        {
            TerrainMap map = level.Terrain;
            float cell = level.CellSize;
            GridMap grid = level.Grid;
            LiquidSet set = theme.Liquids;
            bool low = ModelQuality.IsLow;

            foreach (TerrainKind kind in Kinds)
            {
                if (set != null && set.For(kind) != null)
                {
                    continue;
                }

                List<Vector3> cells = CellCentres(map, grid, kind);
                if (cells.Count > 0)
                {
                    BuildPool(kind, theme, cells, cell, parent, particleScale);
                }
            }

            if (set == null)
            {
                return;
            }

            foreach (LiquidShape shape in field.Pools)
            {
                LiquidLook look = set.For(shape.Kind);
                if (look != null)
                {
                    BuildShapedPool(shape, look, theme, grid, cell, parent, particleScale, low);
                }
            }
        }

        // One pool's sheet along its smoothed shore; its motes, life and bank light as before.
        private static void BuildShapedPool(LiquidShape shape, LiquidLook look, VisualTheme theme, GridMap grid, float cellSize, Transform parent, float particleScale, bool low)
        {
            float surface = LevelOf(shape.Kind);
            var go = new GameObject($"Liquid_{shape.Kind}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, surface, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = LiquidSurfaceMesh.Build(shape);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.sharedMaterial = look.MaterialFor(low);

            var cells = new List<Vector3>(shape.Cells.Count);
            foreach (Vector2Int c in shape.Cells)
            {
                cells.Add(grid.CellToWorld(c));
            }

            if (look.BankLight.maxColorComponent > 0f)
            {
                PlaceLavaLights(cells, look.BankLight, cellSize, parent);
            }

            BuildMotes(shape.Kind, cells, theme, cellSize, surface, particleScale, parent);
        }

        /// <summary>
        /// One pool of a kind over the given cell centres: the rippling sheet, the glow drifting over it, its motes
        /// and, for lava, the flickering lights on the banks. The menu diorama builds its pools with this too.
        /// </summary>
        public static void BuildPool(TerrainKind kind, VisualTheme theme, List<Vector3> cells, float cellSize, Transform parent, float particleScale = 1f)
        {
            Rect bounds = Bounds(cells, PaddingCells * cellSize);
            float surface = LevelOf(kind);

            var go = new GameObject($"Liquid_{kind}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, surface, 0f);
            Mesh mesh = Sheet(bounds);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Material material = MaterialFor(kind, theme);
            renderer.sharedMaterial = material;

            var flow = go.AddComponent<LiquidFlow>();
            flow.Initialize(material, FlowOf(kind));
            flow.SetWaves(mesh, WaveHeightOf(kind), WaveLengthOf(kind), WaveSpeedOf(kind));

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(go.transform, false);
            overlay.transform.localPosition = new Vector3(0f, OverlayLift, 0f);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            var overlayRenderer = overlay.AddComponent<MeshRenderer>();
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            Material overlayMaterial = OverlayMaterialFor(kind, theme);
            overlayRenderer.sharedMaterial = overlayMaterial;
            flow.SetOverlay(overlayMaterial, OverlayFlowOf(kind));

            if (kind == TerrainKind.Lava)
            {
                flow.SetPulse(material.GetColor("_EmissionColor"), LavaPulseDepth, LavaPulseSpeed);
                PlaceLavaLights(cells, theme.LavaColor, cellSize, parent);
            }
            else if (kind == TerrainKind.Rift)
            {
                // The veins breathe, they do not flicker: the rock is alive, not burning.
                flow.SetPulse(material.GetColor("_EmissionColor"), RiftBreathDepth, RiftBreathSpeed, 0f);
            }

            BuildMotes(kind, cells, theme, cellSize, surface, particleScale, parent);
        }

        /// <summary>The height a liquid of this kind sits at; ground above it hides the sheet, ground below it shows a pool.</summary>
        public static float SurfaceLevel(TerrainKind kind) => LevelOf(kind);

        private static List<Vector3> CellCentres(TerrainMap map, GridMap grid, TerrainKind kind)
        {
            var result = new List<Vector3>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> entry in map.Cells)
            {
                if (entry.Value == kind)
                {
                    result.Add(grid.CellToWorld(entry.Key));
                }
            }

            return result;
        }

        private static Rect Bounds(List<Vector3> cells, float pad)
        {
            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;
            foreach (Vector3 world in cells)
            {
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z);
                maxZ = Mathf.Max(maxZ, world.z);
            }

            return Rect.MinMaxRect(minX - pad, minZ - pad, maxX + pad, maxZ + pad);
        }

        // A grid of small quads rather than one big one, so waves can move its vertices.
        private static Mesh Sheet(Rect bounds)
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(bounds.width / VertexSpacing) + 1);
            int nz = Mathf.Max(2, Mathf.CeilToInt(bounds.height / VertexSpacing) + 1);
            var vertices = new Vector3[nx * nz];
            var uvs = new Vector2[vertices.Length];
            var uv2 = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = iz * nx + ix;
                    float x = Mathf.Lerp(bounds.xMin, bounds.xMax, ix / (float)(nx - 1));
                    float z = Mathf.Lerp(bounds.yMin, bounds.yMax, iz / (float)(nz - 1));
                    vertices[i] = new Vector3(x, 0f, z);
                    uvs[i] = new Vector2(x, z) / TextureWorldSize;
                    uv2[i] = new Vector2(x, z) / OverlayWorldSize;
                    normals[i] = Vector3.up;
                }
            }

            var triangles = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int iz = 0; iz < nz - 1; iz++)
            {
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = iz * nx + ix;
                    int b = a + 1;
                    int c = a + nx;
                    int d = c + 1;
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = d;
                    triangles[t++] = a;
                    triangles[t++] = d;
                    triangles[t++] = b;
                }
            }

            var mesh = new Mesh { name = "Liquid" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = uv2;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.MarkDynamic();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Water ripples, lava heaves slowly, sand creeps, the bog barely stirs and ice lies flat and still.
        private static float LevelOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return WaterLevel;
                case TerrainKind.Lava: return LavaLevel;
                case TerrainKind.Ice: return IceLevel;
                case TerrainKind.Bog: return BogLevel;
                case TerrainKind.Rift: return RiftLevel;
                default: return SandLevel;
            }
        }

        private static Vector2 FlowOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return new Vector2(0.025f, 0.015f);
                case TerrainKind.Lava: return new Vector2(0.008f, -0.006f);
                case TerrainKind.Ice: return Vector2.zero;
                case TerrainKind.Rift: return Vector2.zero;
                case TerrainKind.Bog: return new Vector2(0.003f, -0.002f);
                default: return new Vector2(-0.004f, 0.003f);
            }
        }

        private static Vector2 OverlayFlowOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return new Vector2(-0.02f, 0.03f);
                case TerrainKind.Lava: return new Vector2(-0.012f, 0.009f);
                case TerrainKind.Ice: return new Vector2(0.004f, 0.002f);
                case TerrainKind.Rift: return new Vector2(0.002f, 0.001f);
                case TerrainKind.Bog: return new Vector2(-0.005f, 0.004f);
                default: return new Vector2(0.006f, -0.005f);
            }
        }

        private static float WaveHeightOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return 0.04f;
                case TerrainKind.Lava: return 0.035f;
                case TerrainKind.Ice: return 0f;
                case TerrainKind.Rift: return 0f;
                case TerrainKind.Bog: return 0.008f;
                default: return 0.012f;
            }
        }

        private static float WaveLengthOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return 1.4f;
                case TerrainKind.Lava: return 2.6f;
                case TerrainKind.Bog: return 2.4f;
                default: return 2f;
            }
        }

        private static float WaveSpeedOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return 1.6f;
                case TerrainKind.Lava: return 0.45f;
                case TerrainKind.Bog: return 0.2f;
                default: return 0.3f;
            }
        }

        private static Material MaterialFor(TerrainKind kind, VisualTheme theme)
        {
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetFloat("_Metallic", 0f);

            if (kind == TerrainKind.Water)
            {
                material.SetTexture("_BaseMap", LiquidTextures.Water());
                material.SetColor("_BaseColor", new Color(theme.WaterColor.r, theme.WaterColor.g, theme.WaterColor.b, WaterAlpha));
                material.SetFloat("_Smoothness", 0.85f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", theme.WaterColor * WaterGlow);
                MakeTransparent(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }
            else if (kind == TerrainKind.Lava)
            {
                Texture2D texture = LiquidTextures.Lava();
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.Lerp(Color.white, theme.LavaColor, 0.35f) * LavaCrustDarkening);
                material.SetFloat("_Smoothness", 0.25f);
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", texture);
                material.SetColor("_EmissionColor", Color.white * LavaEmission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else if (kind == TerrainKind.Ice)
            {
                // A hard glossy sheet, nearly opaque, with a faint cold light of its own.
                material.SetTexture("_BaseMap", LiquidTextures.Ice());
                material.SetColor("_BaseColor", new Color(theme.IceColor.r, theme.IceColor.g, theme.IceColor.b, IceAlpha));
                material.SetFloat("_Smoothness", 0.9f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", theme.IceColor * IceGlow);
                MakeTransparent(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }
            else if (kind == TerrainKind.Rift)
            {
                // Rock at the bottom of the fissure, opaque and matte, lit only by its veins.
                material.SetTexture("_BaseMap", LiquidTextures.RiftRock());
                material.SetColor("_BaseColor", theme.RockColor * 0.55f);
                material.SetFloat("_Smoothness", 0.1f);
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", LiquidTextures.RiftVeins());
                material.SetColor("_EmissionColor", theme.RiftColor * RiftVeinGlow);
                // The veins run long: one tile spans several cells, so the pattern never reads as a grid.
                material.SetTextureScale("_BaseMap", Vector2.one * RiftTiling);
                material.SetTextureScale("_EmissionMap", Vector2.one * RiftTiling);
            }
            else if (kind == TerrainKind.Bog)
            {
                // Dark still water under a skin of scum: dull, almost opaque, a little green light in the murk.
                material.SetTexture("_BaseMap", LiquidTextures.Bog());
                material.SetColor("_BaseColor", new Color(theme.BogColor.r, theme.BogColor.g, theme.BogColor.b, BogAlpha));
                material.SetFloat("_Smoothness", 0.35f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", theme.BogColor * BogGlow);
                MakeTransparent(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }
            else
            {
                material.SetTexture("_BaseMap", LiquidTextures.Sand());
                material.SetColor("_BaseColor", theme.SandColor);
                material.SetFloat("_Smoothness", 0.05f);
            }

            return material;
        }

        // The drifting layer adds light only: black where there is nothing, so it never darkens the sheet below.
        private static Material OverlayMaterialFor(TerrainKind kind, VisualTheme theme)
        {
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_EMISSION");

            Texture2D texture;
            Color glow;
            if (kind == TerrainKind.Water)
            {
                texture = LiquidTextures.WaterSparkle();
                glow = Color.Lerp(theme.WaterColor, Color.white, 0.7f) * 0.4f;
            }
            else if (kind == TerrainKind.Lava)
            {
                texture = LiquidTextures.LavaHeat();
                glow = theme.LavaColor * 1.3f;
            }
            else if (kind == TerrainKind.Ice)
            {
                texture = LiquidTextures.IceCracks();
                glow = Color.Lerp(theme.IceColor, Color.white, 0.5f) * 0.7f;
            }
            else if (kind == TerrainKind.Rift)
            {
                texture = LiquidTextures.RiftVeins();
                glow = theme.RiftColor * 0.25f;
            }
            else if (kind == TerrainKind.Bog)
            {
                texture = LiquidTextures.BogScum();
                glow = Color.Lerp(theme.BogColor, theme.AccentColor, 0.5f) * 0.5f;
            }
            else
            {
                texture = LiquidTextures.SandStreaks();
                glow = theme.SandColor * 0.45f;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.black);
            material.SetTexture("_EmissionMap", texture);
            if (kind == TerrainKind.Rift)
            {
                material.SetTextureScale("_BaseMap", Vector2.one * RiftTiling * 0.7f);
                material.SetTextureScale("_EmissionMap", Vector2.one * RiftTiling * 0.7f);
            }

            material.SetColor("_EmissionColor", glow);
            MakeTransparent(material, BlendMode.One, BlendMode.One);
            return material;
        }

        private static void MakeTransparent(Material material, BlendMode source, BlendMode destination)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", source == BlendMode.One ? 2f : 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)source);
            material.SetFloat("_DstBlend", (float)destination);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        // One flickering point light per cluster of lava cells, the biggest clusters first, so the banks glow.
        private static void PlaceLavaLights(List<Vector3> cells, Color colour, float cellSize, Transform parent)
        {
            var clusters = new Dictionary<Vector2Int, (Vector3 Sum, int Count)>();
            float clusterSize = LightClusterCells * cellSize;
            foreach (Vector3 world in cells)
            {
                var key = new Vector2Int(Mathf.FloorToInt(world.x / clusterSize), Mathf.FloorToInt(world.z / clusterSize));
                clusters.TryGetValue(key, out (Vector3 Sum, int Count) cluster);
                clusters[key] = (cluster.Sum + world, cluster.Count + 1);
            }

            var ordered = new List<(Vector3 Sum, int Count)>(clusters.Values);
            ordered.Sort((a, b) => b.Count.CompareTo(a.Count));
            int lights = Mathf.Min(MaxLavaLights, ordered.Count);
            int fullCluster = LightClusterCells * LightClusterCells;
            for (int i = 0; i < lights; i++)
            {
                Vector3 centre = ordered[i].Sum / ordered[i].Count;
                float fill = Mathf.Clamp(ordered[i].Count / (float)fullCluster, 0.4f, 1f);
                var go = new GameObject("LavaLight");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(centre.x, LavaLightHeight, centre.z);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = colour;
                light.range = LavaLightRange;
                light.shadows = LightShadows.None;
                go.AddComponent<LightFlicker>().Initialize(LavaLightIntensity * fill, LavaLightFlicker, LavaLightFlickerSpeed);
            }
        }

        // Motes are emitted from the surface of the liquid cells themselves, so nothing rises from the banks.
        private static void BuildMotes(TerrainKind kind, List<Vector3> cells, VisualTheme theme, float cellSize, float surface, float particleScale, Transform parent)
        {
            if (particleScale <= 0f)
            {
                return;
            }

            Mesh emitter = CellsMesh(cells, cellSize);
            float area = cells.Count;
            switch (kind)
            {
                case TerrainKind.Lava:
                    Motes(parent, "LavaEmbers", emitter, surface, theme.LavaColor * 2.2f, 1f, area * 2.5f * particleScale, 1.2f, 2.4f, 0.03f, 0.08f, 0.3f, 0.9f, -0.05f, 0.25f, 0.15f);
                    Motes(parent, "LavaSparks", emitter, surface, Color.Lerp(theme.LavaColor, Color.white, 0.4f) * 2.5f, 1f, area * 0.5f * particleScale, 0.5f, 0.9f, 0.03f, 0.05f, 1.6f, 3f, 1f, 0f, 0.1f);
                    break;
                case TerrainKind.Quicksand:
                    break;
                case TerrainKind.Ice:
                    break;
                case TerrainKind.Rift:
                    // Sparse dust hanging in the fissure; the Low profile goes without.
                    if (particleScale >= 1f)
                    {
                        Motes(parent, "RiftDust", emitter, surface, new Color(0.75f, 0.68f, 0.58f, 0.6f), 0.35f, area * 0.15f * particleScale, 3f, 5f, 0.03f, 0.06f, 0.03f, 0.08f, -0.01f, 0.2f, 0.4f);
                    }

                    break;
                case TerrainKind.Bog:
                    // Marsh gas: dim green bubbles rising slowly and wisps of vapour hanging over the mire.
                    Motes(parent, "BogBubbles", emitter, surface, Color.Lerp(theme.BogColor, theme.AccentColor, 0.6f) * 1.4f, 0.8f, area * 0.6f * particleScale, 1.2f, 2.2f, 0.03f, 0.07f, 0.1f, 0.25f, 0f, 0.05f, 0.3f);
                    break;
                default:
                    Motes(parent, "WaterMist", emitter, surface, Color.Lerp(theme.WaterColor, Color.white, 0.5f), 0.12f, area * 0.3f * particleScale, 3f, 5f, 0.4f, 0.8f, 0.04f, 0.08f, 0f, 0.1f, 1.6f);
                    break;
            }

            LiquidLifeBuilder.Build(kind, emitter, CellsMesh(cells, cellSize * InnerCellShare), theme, cellSize, surface, area, particleScale, parent);
        }

        private static Mesh CellsMesh(List<Vector3> cells, float cellSize)
        {
            var vertices = new Vector3[cells.Count * 4];
            var triangles = new int[cells.Count * 6];
            float half = cellSize * 0.5f;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 c = cells[i];
                int v = i * 4;
                vertices[v] = new Vector3(c.x - half, 0f, c.z - half);
                vertices[v + 1] = new Vector3(c.x - half, 0f, c.z + half);
                vertices[v + 2] = new Vector3(c.x + half, 0f, c.z + half);
                vertices[v + 3] = new Vector3(c.x + half, 0f, c.z - half);
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            var mesh = new Mesh { name = "LiquidCells" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Motes(
            Transform parent, string name, Mesh emitter, float surface, Color color, float alpha, float rate,
            float minLife, float maxLife, float minSize, float maxSize, float minRise, float maxRise, float gravity, float drift, float endSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, surface + ParticleLift, 0f);

            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minRise, maxRise);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            color.a = alpha;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(rate * maxLife) + 8;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = emitter;
            shape.alignToDirection = false;

            if (drift > 0f)
            {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = drift;
                noise.frequency = 0.5f;
            }

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Play();
        }
    }
}
