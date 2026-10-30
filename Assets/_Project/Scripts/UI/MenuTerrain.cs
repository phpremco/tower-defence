using System.Collections.Generic;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The ground of the menu stage: a floating island whose surface is one height function (basins for the pools, a
    /// mound for the hive with a flat porch at its mouth, a ragged cliff all round), painted by the same function so
    /// each corner takes the colour and the glow of the theme that lives there.
    /// </summary>
    public sealed class MenuTerrain
    {
        private const float Spacing = 0.35f;
        private const float Roughness = 0.1f;
        private const float RoughnessScale = 0.45f;
        private const float RimWidth = 2.4f;
        private const float RimDrop = 4.5f;
        private const float PoolDepth = 0.8f;
        private const float PoolShore = 0.5f;
        private const float MouthPorchRadius = 1.9f;
        private const float MouthPorchBlend = 1.1f;
        private const int TextureSize = 512;
        private const float ThemeReach = 3.2f;
        private const float BaseDarkness = 0.55f;
        private const float BasinDarkness = 0.45f;
        private const float LavaCrackPower = 13f;
        private const float LavaCrackGlow = 1.1f;
        private const float NestVeinPower = 11f;
        private const float NestVeinGlow = 0.65f;
        private const float PondSpeckleChance = 0.012f;
        private const float PondSpeckleGlow = 1.4f;
        private const float Smoothness = 0.22f;

        public readonly struct Pool
        {
            public readonly Vector3 Centre;
            public readonly float Radius;
            public readonly TerrainKind Kind;
            public readonly VisualTheme Theme;

            public Pool(Vector3 centre, float radius, TerrainKind kind, VisualTheme theme)
            {
                Centre = centre;
                Radius = radius;
                Kind = kind;
                Theme = theme;
            }
        }

        private readonly Vector3 _islandCentre;
        private readonly float _islandRadius;
        private readonly Vector3 _nestCentre;
        private readonly float _nestRadius;
        private readonly float _nestHeight;
        private readonly Vector3 _mouth;
        private readonly float _porchHeight;
        private readonly Pool[] _pools;
        private readonly VisualTheme _nestTheme;
        private readonly float _seed;

        public MenuTerrain(Vector3 islandCentre, float islandRadius, Vector3 nestCentre, float nestRadius, float nestHeight, Vector3 mouth, float porchHeight, Pool[] pools, VisualTheme nestTheme, float seed)
        {
            _islandCentre = islandCentre;
            _islandRadius = islandRadius;
            _nestCentre = nestCentre;
            _nestRadius = nestRadius;
            _nestHeight = nestHeight;
            _mouth = mouth;
            _porchHeight = porchHeight;
            _pools = pools;
            _nestTheme = nestTheme;
            _seed = seed;
        }

        /// <summary>The ground height at a point, before the porch flattening is applied.</summary>
        public float Height(float x, float z)
        {
            float height = (TerrainNoise.Fbm(x * RoughnessScale, z * RoughnessScale, 3, _seed) - 0.5f) * 2f * Roughness;
            float rim = TerrainNoise.SmoothStep(_islandRadius - RimWidth, _islandRadius, Flat(x, z, _islandCentre) + RimNoise(x, z));
            height -= rim * RimDrop;

            foreach (Pool pool in _pools)
            {
                height -= Basin(x, z, pool) * PoolDepth;
            }

            float mound = TerrainNoise.SmoothStep(0f, 1f, 1f - Flat(x, z, _nestCentre) / _nestRadius);
            height += mound * mound * _nestHeight * (1f + (TerrainNoise.Fbm(x * 0.9f, z * 0.9f, 2, _seed + 5f) - 0.5f) * 0.3f);

            // The porch in front of the mouth is flat, so the guardian stands level and the crawlers step out cleanly.
            float porch = 1f - TerrainNoise.SmoothStep(MouthPorchRadius, MouthPorchRadius + MouthPorchBlend, Flat(x, z, _mouth));
            return Mathf.Lerp(height, _porchHeight, porch);
        }

        /// <summary>How deep inside a pool a point lies, 0 on the bank and 1 at the middle.</summary>
        public float Basin(float x, float z, Pool pool) =>
            1f - TerrainNoise.SmoothStep(pool.Radius * PoolShore, pool.Radius, Flat(x, z, pool.Centre));

        /// <summary>The surface normal, for things that lean against the slope such as the hive mouth.</summary>
        public Vector3 Normal(float x, float z)
        {
            const float step = 0.15f;
            float dx = Height(x + step, z) - Height(x - step, z);
            float dz = Height(x, z + step) - Height(x, z - step);
            return new Vector3(-dx, 2f * step, -dz).normalized;
        }

        /// <summary>A point on the ground, lifted a little so feet and roots do not sink into it.</summary>
        public Vector3 OnGround(float x, float z, float lift = 0f) => new Vector3(x, Height(x, z) + lift, z);

        /// <summary>Builds the island mesh with its painted and glowing surface and returns the object.</summary>
        public GameObject Build(Transform parent, VisualTheme neutral)
        {
            var go = new GameObject("Island");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.sharedMaterial = BuildMaterial(neutral);
            return go;
        }

        /// <summary>The centres of the ground cells inside a pool, for the liquid builder.</summary>
        public List<Vector3> PoolCells(Pool pool, float cellSize)
        {
            var cells = new List<Vector3>();
            int steps = Mathf.CeilToInt(pool.Radius / cellSize);
            for (int ix = -steps; ix <= steps; ix++)
            {
                for (int iz = -steps; iz <= steps; iz++)
                {
                    float x = pool.Centre.x + ix * cellSize;
                    float z = pool.Centre.z + iz * cellSize;
                    if (Flat(x, z, pool.Centre) <= pool.Radius)
                    {
                        cells.Add(new Vector3(x, 0f, z));
                    }
                }
            }

            return cells;
        }

        private Mesh BuildMesh()
        {
            float extent = _islandRadius + 0.5f;
            int n = Mathf.CeilToInt(extent * 2f / Spacing) + 1;
            var vertices = new Vector3[n * n];
            var uvs = new Vector2[vertices.Length];
            for (int iz = 0; iz < n; iz++)
            {
                for (int ix = 0; ix < n; ix++)
                {
                    float x = _islandCentre.x - extent + ix * Spacing;
                    float z = _islandCentre.z - extent + iz * Spacing;
                    int i = iz * n + ix;
                    vertices[i] = new Vector3(x, Height(x, z), z);
                    uvs[i] = Uv(x, z);
                }
            }

            // Cells wholly beyond the cliff are dropped, so the island ends in a ragged edge over the stars.
            var triangles = new List<int>(n * n * 6);
            for (int iz = 0; iz < n - 1; iz++)
            {
                for (int ix = 0; ix < n - 1; ix++)
                {
                    int a = iz * n + ix;
                    int b = a + 1;
                    int c = a + n;
                    int d = c + 1;
                    if (Outside(vertices[a]) && Outside(vertices[b]) && Outside(vertices[c]) && Outside(vertices[d]))
                    {
                        continue;
                    }

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(b);
                }
            }

            var mesh = new Mesh { name = "MenuIsland", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private bool Outside(Vector3 vertex) => Flat(vertex.x, vertex.z, _islandCentre) > _islandRadius + 0.2f;

        private Vector2 Uv(float x, float z)
        {
            float extent = _islandRadius + 0.5f;
            return new Vector2((x - _islandCentre.x + extent) / (extent * 2f), (z - _islandCentre.z + extent) / (extent * 2f));
        }

        private Material BuildMaterial(VisualTheme neutral)
        {
            var albedo = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, true) { name = "MenuIslandAlbedo", wrapMode = TextureWrapMode.Clamp };
            var emission = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, true) { name = "MenuIslandEmission", wrapMode = TextureWrapMode.Clamp };
            var albedoPixels = new Color[TextureSize * TextureSize];
            var emissionPixels = new Color[TextureSize * TextureSize];
            float extent = _islandRadius + 0.5f;
            Color baseColour = neutral.GroundColor * BaseDarkness;

            for (int py = 0; py < TextureSize; py++)
            {
                for (int px = 0; px < TextureSize; px++)
                {
                    float x = _islandCentre.x - extent + (px + 0.5f) / TextureSize * extent * 2f;
                    float z = _islandCentre.z - extent + (py + 0.5f) / TextureSize * extent * 2f;
                    Paint(x, z, baseColour, out Color colour, out Color glow);
                    int i = py * TextureSize + px;
                    albedoPixels[i] = colour;
                    emissionPixels[i] = glow;
                }
            }

            albedo.SetPixels(albedoPixels);
            albedo.Apply();
            emission.SetPixels(emissionPixels);
            emission.Apply();

            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial) { name = "MenuIsland" };
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", Smoothness);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", Color.white);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return material;
        }

        // Each theme owns the ground around its pool; the hive owns its mound. Basins darken towards the middle.
        private void Paint(float x, float z, Color baseColour, out Color colour, out Color glow)
        {
            colour = baseColour;
            glow = Color.black;
            float grain = 0.85f + TerrainNoise.Fbm(x * 1.7f, z * 1.7f, 2, _seed + 11f) * 0.3f;

            foreach (Pool pool in _pools)
            {
                float distance = Flat(x, z, pool.Centre);
                float weight = 1f - TerrainNoise.SmoothStep(pool.Radius, pool.Radius + ThemeReach, distance);
                colour = Color.Lerp(colour, pool.Theme.GroundColor * BaseDarkness, weight);
                colour = Color.Lerp(colour, colour * BasinDarkness, Basin(x, z, pool));

                switch (pool.Kind)
                {
                    case TerrainKind.Lava:
                        float crack = Mathf.Pow(TerrainNoise.Ridge(x * 1.1f, z * 1.1f, _seed + 3f), LavaCrackPower);
                        glow += pool.Theme.LavaColor * (crack * weight * weight * LavaCrackGlow);
                        break;
                    case TerrainKind.Water:
                        if (weight > 0.1f && TerrainNoise.Hash(Mathf.FloorToInt(x * 14f), Mathf.FloorToInt(z * 14f), 5) < PondSpeckleChance * weight)
                        {
                            glow += pool.Theme.AccentColor * PondSpeckleGlow;
                        }

                        break;
                }
            }

            float mound = 1f - TerrainNoise.SmoothStep(_nestRadius * 0.7f, _nestRadius * 1.5f, Flat(x, z, _nestCentre));
            colour = Color.Lerp(colour, _nestTheme.GroundColor * BaseDarkness, mound);
            float vein = Mathf.Pow(TerrainNoise.Ridge(x * 0.8f, z * 0.8f, _seed + 7f), NestVeinPower);
            glow += _nestTheme.AccentColor * (vein * mound * NestVeinGlow);

            colour *= grain;
            colour.a = 1f;
            glow.a = 1f;
        }

        private float RimNoise(float x, float z) => (TerrainNoise.Fbm(x * 0.6f, z * 0.6f, 2, _seed + 2f) - 0.5f) * 2.2f;

        private static float Flat(float x, float z, Vector3 centre)
        {
            float dx = x - centre.x;
            float dz = z - centre.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
