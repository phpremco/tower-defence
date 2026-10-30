using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Tileable surface detail for the decoration and the ground, generated once: a colour map that mottles the
    /// base colour, a normal map that gives the light something to catch and, for the glowing kinds, an emission
    /// map that varies the light. Rock is grainy with cracks and lichen, bark has ridges and knots, leaves have a
    /// spine and veins, crystal carries streaks and inclusions, and the ground is grit, pebbles and fissures.
    /// </summary>
    public static class PropTextures
    {
        public readonly struct Surface
        {
            public readonly Texture2D Albedo;
            public readonly Texture2D Normal;

            /// <summary>Varies the glow over the surface; null for surfaces that do not glow.</summary>
            public readonly Texture2D Emission;

            public Surface(Texture2D albedo, Texture2D normal, Texture2D emission)
            {
                Albedo = albedo;
                Normal = normal;
                Emission = emission;
            }
        }

        private const int Size = 256;
        private const float RockBump = 2.8f;
        private const float BarkBump = 2.8f;
        private const float LeafBump = 1.8f;
        private const float CrystalBump = 1.4f;
        private const float GroundBump = 2.4f;
        private static readonly Color LichenTint = new Color(0.82f, 0.95f, 0.6f);
        private static readonly Color CrystalInclusion = new Color(0.85f, 0.9f, 1f);

        private static Surface? _rock;
        private static Surface? _bark;
        private static Surface? _leaf;
        private static Surface? _crystal;
        private static Surface? _ground;

        public static Surface Rock() => _rock ??= Build("Rock", RockHeight, RockBump, 0.76f, 0.4f, null, RockTint);

        public static Surface Bark() => _bark ??= Build("Bark", BarkHeight, BarkBump, 0.7f, 0.42f, null, null);

        public static Surface Leaf() => _leaf ??= Build("Leaf", LeafHeight, LeafBump, 0.84f, 0.26f, null, null);

        /// <summary>Streaked crystal: the albedo is bright, the glow runs in veins through it.</summary>
        public static Surface Crystal() => _crystal ??= Build("Crystal", CrystalHeight, CrystalBump, 0.86f, 0.22f, CrystalGlow, CrystalTint);

        /// <summary>Ground detail, centred on mid grey so it can multiply the painted terrain.</summary>
        public static Surface Ground() => _ground ??= Build("Ground", GroundHeight, GroundBump, 0.5f, 0.42f, null, null);

        // Grain plus a network of cracks (the dark lines are low) and small pits.
        private static float RockHeight(float u, float v)
        {
            float grain = Tileable(u, v, 3f, 1f) * 0.5f + Tileable(u, v, 9f, 7f) * 0.3f + Tileable(u, v, 22f, 11f) * 0.2f;
            float pits = Mathf.Pow(Tileable(u, v, 14f, 17f), 6f);
            return grain - RockCracks(u, v) * 0.7f - pits * 0.3f + RockLichen(u, v) * 0.1f;
        }

        private static float RockCracks(float u, float v) => Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 13f) * 2f - 1f), 7f);

        // Lichen grows in rounded patches that stop at the cracks.
        private static float RockLichen(float u, float v) =>
            Smooth(0.58f, 0.72f, Tileable(u, v, 2.5f, 91f)) * Mathf.Clamp01(1f - RockCracks(u, v) * 3f);

        private static Color RockTint(float u, float v) => Color.Lerp(Color.white, LichenTint, RockLichen(u, v));

        // Vertical ridges wandering a little, with knots.
        private static float BarkHeight(float u, float v)
        {
            float wander = (Tileable(u, v, 2f, 21f) - 0.5f) * 0.4f;
            float ridges = 0.5f + 0.5f * Mathf.Sin((u + wander) * Mathf.PI * 2f * 7f);
            float knots = Tileable(u, v, 5f, 31f);
            return Mathf.Pow(ridges, 1.3f) * 0.7f + knots * 0.3f;
        }

        // A raised spine down the middle with side veins and soft mottling between them.
        private static float LeafHeight(float u, float v)
        {
            float spine = Mathf.Pow(1f - Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 10f), 2f);
            float side = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((v * 9f + Mathf.Abs(u - 0.5f) * 3f) * Mathf.PI * 2f), 8f) * (1f - spine);
            float mottle = Tileable(u, v, 6f, 41f) * 0.35f;
            return spine * 0.7f + side * 0.45f + mottle;
        }

        // Crystal: long streaks along the shaft with cloudy inclusions between them.
        private static float CrystalStreaks(float u, float v)
        {
            float wander = (Tileable(u, v, 2f, 51f) - 0.5f) * 0.3f;
            return Mathf.Pow(0.5f + 0.5f * Mathf.Sin((u + wander) * Mathf.PI * 2f * 5f), 3f);
        }

        private static float CrystalHeight(float u, float v) => 0.5f + (CrystalStreaks(u, v) - 0.5f) * 0.3f + (Tileable(u, v, 6f, 53f) - 0.5f) * 0.2f;

        private static float CrystalGlow(float u, float v) => 0.55f + 0.45f * CrystalStreaks(u, v);

        private static Color CrystalTint(float u, float v) => Color.Lerp(Color.white, CrystalInclusion, Smooth(0.6f, 0.8f, Tileable(u, v, 6f, 53f)) * 0.5f);

        // Ground: fine grit, scattered pebbles standing proud and a web of shallow fissures.
        private static float GroundHeight(float u, float v)
        {
            float grit = Tileable(u, v, 18f, 101f) * 0.3f + Tileable(u, v, 40f, 103f) * 0.15f;
            float pebbles = Smooth(0.6f, 0.72f, Tileable(u, v, 10f, 105f)) * 0.45f;
            float fissures = Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 3f, 107f) * 2f - 1f), 9f) * 0.4f;
            return 0.35f + grit + pebbles - fissures;
        }

        private static float Smooth(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static Surface Build(string name, System.Func<float, float, float> height, float bump, float brightness, float contrast, System.Func<float, float, float> glow, System.Func<float, float, Color> tint)
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
            var emission = glow != null ? new Color32[Size * Size] : null;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    float value = Mathf.Clamp01(brightness + (heights[i] - 0.5f) * contrast);
                    Color colour = new Color(value, value, value, 1f);
                    if (tint != null)
                    {
                        colour *= tint(u, v);
                        colour.a = 1f;
                    }

                    albedo[i] = colour;
                    if (emission != null)
                    {
                        float g = Mathf.Clamp01(glow(u, v));
                        emission[i] = new Color(g, g, g, 1f);
                    }

                    float dx = heights[y * Size + (x + 1) % Size] - heights[y * Size + (x + Size - 1) % Size];
                    float dy = heights[((y + 1) % Size) * Size + x] - heights[((y + Size - 1) % Size) * Size + x];
                    var n = new Vector3(-dx * bump, -dy * bump, 1f).normalized;

                    // URP unpacks runtime normal maps from the alpha and green channels, so x goes in alpha.
                    normal[i] = new Color(1f, n.y * 0.5f + 0.5f, 1f, n.x * 0.5f + 0.5f);
                }
            }

            return new Surface(Finish(albedo, name + "Albedo", false), Finish(normal, name + "Normal", true), emission != null ? Finish(emission, name + "Glow", false) : null);
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
