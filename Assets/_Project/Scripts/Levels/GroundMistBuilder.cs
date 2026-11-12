using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Two sheets of thin mist that drift over the ground in the theme's fog colour: the cheapest way to give a
    /// flat field depth and motion. Each sheet is a quad with a tileable noise texture scrolling at its own pace,
    /// alpha blended and unlit, so it costs two draw calls and no light.
    /// </summary>
    public static class GroundMistBuilder
    {
        private const int TextureSize = 256;
        private const float MarginCells = 4f;
        private const float LowHeight = 0.22f;
        private const float HighHeight = 0.5f;
        private const float LowAlpha = 0.16f;
        private const float HighAlpha = 0.08f;
        private const float LowTiling = 1.6f;
        private const float HighTiling = 2.6f;
        private const float LowSpeed = 0.012f;
        private const float HighSpeed = 0.02f;
        private const float EdgeLow = 0.4f;
        private const float EdgeHigh = 0.85f;
        // The sheets lean towards white: mist the colour of the fog itself vanished into the field on every dark level.
        private const float Whitening = 0.45f;
        private const float Glow = 0.6f;

        private static Texture2D _noise;

        public static void Build(LevelDefinition level, VisualTheme theme, Transform parent)
        {
            GridMap grid = level.Grid;
            Vector3 first = grid.CellToWorld(Vector2Int.zero);
            Vector3 last = grid.CellToWorld(grid.Size - Vector2Int.one);
            Vector3 centre = (first + last) * 0.5f;
            Vector2 size = level.WorldSize + Vector2.one * (MarginCells * 2f * grid.CellSize);
            Color tint = Color.Lerp(Color.Lerp(theme.FogColor, theme.SkyColor, 0.5f), Color.white, Whitening);

            Sheet(parent, "MistLow", centre, size, LowHeight, tint, LowAlpha, LowTiling, new Vector2(LowSpeed, LowSpeed * 0.6f));
            Sheet(parent, "MistHigh", centre, size, HighHeight, tint, HighAlpha, HighTiling, new Vector2(-HighSpeed * 0.7f, HighSpeed));
        }

        /// <summary>
        /// The mist of a definition: one sheet per layer of the Blender mist textures (the Low profile only the first
        /// <see cref="MistDefinition.LowLayers"/>, at its own alpha and without the soft fade). The shader drifts them.
        /// </summary>
        public static void Build(LevelDefinition level, MistDefinition mist, Transform parent, bool low)
        {
            GridMap grid = level.Grid;
            Vector3 centre = (grid.CellToWorld(Vector2Int.zero) + grid.CellToWorld(grid.Size - Vector2Int.one)) * 0.5f;
            Vector2 size = level.WorldSize + Vector2.one * (mist.MarginCells * 2f * grid.CellSize);
            int layers = low ? Mathf.Min(mist.LowLayers, mist.Layers.Length) : mist.Layers.Length;
            for (int i = 0; i < layers; i++)
            {
                MistLayer layer = mist.Layers[i];
                if (layer == null || layer.Density == null || mist.Shader == null)
                {
                    continue;
                }

                var material = new Material(mist.Shader) { name = $"Mist_{i}" };
                material.SetTexture("_MainTex", layer.DensityFor(low));
                material.SetColor("_Color", mist.Colour);
                material.SetFloat("_Alpha", low ? mist.LowAlpha : layer.Alpha);
                material.SetFloat("_Tile", layer.Tile);
                material.SetVector("_Scroll", layer.Scroll);
                material.SetFloat("_SoftDistance", mist.SoftDistance);
                material.SetFloat("_Glow", mist.Glow);
                if (low)
                {
                    material.DisableKeyword("_MIST_SOFT");
                }
                else
                {
                    material.EnableKeyword("_MIST_SOFT");
                }

                var go = new GameObject($"Mist_{i}");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(new Vector3(centre.x, layer.Height, centre.z), Quaternion.Euler(90f, 0f, 0f));
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = material;
            }
        }

        private static void Sheet(Transform parent, string name, Vector3 centre, Vector2 size, float height, Color tint, float alpha, float tiling, Vector2 velocity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centre.x, height, centre.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material material = Material(tint, alpha);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            renderer.sharedMaterial = material;
            go.AddComponent<GroundMist>().Initialize(material, velocity);
        }

        private static Material Material(Color tint, float alpha)
        {
            var material = PipelineMaterials.Lit();
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent - 50;
            material.SetTexture("_BaseMap", Noise());
            material.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, alpha));
            // Self-lit a little, so the mist shows over ground that lies in shadow.
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", Noise());
            material.SetColor("_EmissionColor", tint * Glow);
            return material;
        }

        // Tileable banks of mist: a few sine waves at whole-number frequencies, so the texture wraps without a seam.
        private static Texture2D Noise()
        {
            if (_noise != null)
            {
                return _noise;
            }

            _noise = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;
                    float n = Wave(u, v, 1f, 2f, 0.3f) + Wave(u, v, 3f, -1f, 1.9f) * 0.6f + Wave(u, v, -2f, 4f, 4.1f) * 0.45f
                              + Wave(u, v, 5f, 3f, 0.7f) * 0.3f + Wave(u, v, -4f, -6f, 2.6f) * 0.2f + Wave(u, v, 7f, -5f, 5.2f) * 0.15f;
                    float bank = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(EdgeLow, EdgeHigh, n * 0.5f + 0.5f));
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, bank);
                }
            }

            _noise.SetPixels(pixels);
            _noise.Apply(true, true);
            return _noise;
        }

        private static float Wave(float u, float v, float a, float b, float phase) => Mathf.Sin(Mathf.PI * 2f * (a * u + b * v) + phase);

        private static Mesh _quad;

        private static Mesh Quad()
        {
            if (_quad != null)
            {
                return _quad;
            }

            _quad = new Mesh { name = "MistQuad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            _quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            _quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.RecalculateBounds();
            return _quad;
        }
    }
}
