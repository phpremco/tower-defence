using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The space behind the menus, painted once: the same galaxy band, nebula colours and star colours as the
    /// cutscene's sky (<see cref="SpaceStyle"/>), flattened into a picture. The alpha follows the light, so over a
    /// running level (the options overlay) it only adds a faint glow instead of covering the field.
    /// </summary>
    public static class StarfieldPainter
    {
        private const int Seed = 7;
        private const float StarDensity = 0.0035f;
        private const float BrightStarShare = 0.04f;
        private const float BandSlope = -0.45f;
        private const float BandCentre = 0.62f;
        private const float BandWidth = 0.16f;
        private const float GasScale = 3.2f;
        private const float AlphaGain = 3f;

        public static Texture2D Paint(int width, int height, SpaceStyle style)
        {
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Sky(x / (float)width, y / (float)height, width / (float)height, style);
                }
            }

            ScatterStars(pixels, width, height);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.a = Mathf.Clamp01(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) * AlphaGain);
                pixels[i] = c;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Starfield", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // A diagonal band of galaxy light with nebula gas and a dark dust lane through its core, a faint haze elsewhere.
        private static Color Sky(float u, float v, float aspect, SpaceStyle style)
        {
            float px = u * aspect;
            float wobble = (Fbm(px * 1.5f, v * 1.5f, 3) - 0.5f) * BandWidth;
            float across = v - (BandCentre + BandSlope * (u - 0.5f)) + wobble;
            float band = Mathf.Exp(-(across / BandWidth) * (across / BandWidth));
            float core = Mathf.Exp(-(across / (BandWidth * 0.35f)) * (across / (BandWidth * 0.35f)));
            float gas = Fbm(px * GasScale + 4f, v * GasScale, 5);
            float mixing = Mathf.SmoothStep(0f, 1f, Fbm(px * 1.6f + 9f, v * 1.6f + 2f, 4));
            Color nebula = Color.Lerp(style.NebulaA, style.NebulaB, mixing) * (Mathf.Pow(gas, 2.2f) * 2.5f);
            float dust = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.8f, Fbm(px * 6f, v * 6f + 3f, 4)));
            Color colour = (style.Band * (core * 0.8f + band * 0.4f) + nebula * band) * (1f - dust * core * 0.85f);
            colour += nebula * (Mathf.Pow(gas, 3f) * style.Haze);
            colour *= style.SkyStrength;
            colour.a = 1f;
            return colour;
        }

        // Single pixels for the faint stars, a soft plus for the bright few, in the colours of real stars.
        private static void ScatterStars(Color[] pixels, int width, int height)
        {
            var random = new System.Random(Seed);
            int count = Mathf.RoundToInt(width * height * StarDensity);
            for (int i = 0; i < count; i++)
            {
                int x = random.Next(1, width - 1);
                int y = random.Next(1, height - 1);
                float roll = (float)random.NextDouble();
                Color star = StarColour((float)random.NextDouble()) * Mathf.Lerp(0.25f, 0.9f, roll * roll);
                Add(pixels, width, x, y, star);
                if (random.NextDouble() < BrightStarShare)
                {
                    Color glow = star * 0.45f;
                    Add(pixels, width, x + 1, y, glow);
                    Add(pixels, width, x - 1, y, glow);
                    Add(pixels, width, x, y + 1, glow);
                    Add(pixels, width, x, y - 1, glow);
                }
            }
        }

        private static void Add(Color[] pixels, int width, int x, int y, Color colour) => pixels[y * width + x] += colour;

        private static Color StarColour(float temperature)
        {
            if (temperature < 0.08f)
            {
                return new Color(1f, 0.6f, 0.45f);
            }

            if (temperature < 0.3f)
            {
                return new Color(1f, 0.9f, 0.75f);
            }

            return temperature < 0.75f ? new Color(0.95f, 0.97f, 1f) : new Color(0.68f, 0.8f, 1f);
        }

        private static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x + 31.7f * i, y + 17.3f * i) * amplitude;
                norm += amplitude;
                x *= 2.03f;
                y *= 2.03f;
                amplitude *= 0.5f;
            }

            return sum / norm;
        }
    }
}
