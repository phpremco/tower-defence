using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Tileable surface detail for the decoration, generated once: a colour map that mottles the base colour and
    /// a normal map that gives the light something to catch. Rock is grainy with cracks, bark has ridges, leaves
    /// have a spine and veins.
    /// </summary>
    public static class PropTextures
    {
        public readonly struct Surface
        {
            public readonly Texture2D Albedo;
            public readonly Texture2D Normal;

            public Surface(Texture2D albedo, Texture2D normal)
            {
                Albedo = albedo;
                Normal = normal;
            }
        }

        private const int Size = 128;
        private const float RockBump = 2.2f;
        private const float BarkBump = 2.6f;
        private const float LeafBump = 1.6f;

        private static Surface? _rock;
        private static Surface? _bark;
        private static Surface? _leaf;

        public static Surface Rock() => _rock ??= Build("Rock", RockHeight, RockBump, 0.78f, 0.28f);

        public static Surface Bark() => _bark ??= Build("Bark", BarkHeight, BarkBump, 0.72f, 0.32f);

        public static Surface Leaf() => _leaf ??= Build("Leaf", LeafHeight, LeafBump, 0.86f, 0.18f);

        // Grain plus a network of cracks (the dark lines are low).
        private static float RockHeight(float u, float v)
        {
            float grain = Tileable(u, v, 3f, 1f) * 0.6f + Tileable(u, v, 9f, 7f) * 0.4f;
            float cracks = Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 13f) * 2f - 1f), 6f);
            return grain - cracks * 0.6f;
        }

        // Vertical ridges wandering a little, with knots.
        private static float BarkHeight(float u, float v)
        {
            float wander = (Tileable(u, v, 2f, 21f) - 0.5f) * 0.4f;
            float ridges = 0.5f + 0.5f * Mathf.Sin((u + wander) * Mathf.PI * 2f * 7f);
            float knots = Tileable(u, v, 5f, 31f);
            return ridges * 0.7f + knots * 0.3f;
        }

        // A raised spine down the middle with side veins and soft mottling between them.
        private static float LeafHeight(float u, float v)
        {
            float spine = Mathf.Pow(1f - Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 10f), 2f);
            float side = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((v * 9f + Mathf.Abs(u - 0.5f) * 3f) * Mathf.PI * 2f), 8f) * (1f - spine);
            float mottle = Tileable(u, v, 6f, 41f) * 0.35f;
            return spine * 0.7f + side * 0.45f + mottle;
        }

        private static Surface Build(string name, System.Func<float, float, float> height, float bump, float brightness, float contrast)
        {
            var heights = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    heights[y * Size + x] = height((x + 0.5f) / Size, (y + 0.5f) / Size);
                }
            }

            var albedo = new Color32[Size * Size];
            var normal = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float value = Mathf.Clamp01(brightness + (heights[i] - 0.5f) * contrast);
                    albedo[i] = new Color(value, value, value, 1f);

                    float dx = heights[y * Size + (x + 1) % Size] - heights[y * Size + (x + Size - 1) % Size];
                    float dy = heights[((y + 1) % Size) * Size + x] - heights[((y + Size - 1) % Size) * Size + x];
                    var n = new Vector3(-dx * bump, -dy * bump, 1f).normalized;

                    // URP unpacks runtime normal maps from the alpha and green channels, so x goes in alpha.
                    normal[i] = new Color(1f, n.y * 0.5f + 0.5f, 1f, n.x * 0.5f + 0.5f);
                }
            }

            return new Surface(Finish(albedo, name + "Albedo", false), Finish(normal, name + "Normal", true));
        }

        // Perlin noise blended across its own edges so it wraps without a seam.
        private static float Tileable(float u, float v, float scale, float seed)
        {
            float a = Mathf.PerlinNoise(u * scale + seed, v * scale + seed);
            float b = Mathf.PerlinNoise((u + 1f) * scale + seed, v * scale + seed);
            float c = Mathf.PerlinNoise(u * scale + seed, (v + 1f) * scale + seed);
            float d = Mathf.PerlinNoise((u + 1f) * scale + seed, (v + 1f) * scale + seed);
            float top = Mathf.Lerp(a, b, 1f - u);
            float bottom = Mathf.Lerp(c, d, 1f - u);
            return Mathf.Lerp(top, bottom, 1f - v);
        }

        private static Texture2D Finish(Color32[] pixels, string name, bool linear)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
