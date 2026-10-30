using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Small tileable textures for the three liquids, generated from periodic functions so they wrap without seams.
    /// Each liquid has a base texture and a sparser overlay that drifts over it at another pace.
    /// </summary>
    public static class LiquidTextures
    {
        private const int Size = 256;
        private const int LavaCells = 6;
        private const float Tau = Mathf.PI * 2f;

        public static Texture2D Water()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float w = Mathf.Sin((3f * u + 1f * v) * Tau)
                              + Mathf.Sin((1f * u - 4f * v) * Tau) * 0.8f
                              + Mathf.Sin((2f * u + 5f * v) * Tau) * 0.6f;
                    float caustic = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(w * 0.9f)), 4f);
                    float value = 0.8f + 0.2f * caustic;
                    pixels[y * Size + x] = new Color(value, value, value, 1f);
                }
            }

            return Finish(pixels, "Water");
        }

        /// <summary>Thin bright threads of light, for the layer that glides over the water.</summary>
        public static Texture2D WaterSparkle()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float w = Mathf.Sin((2f * u - 3f * v) * Tau + 1.3f)
                              + Mathf.Sin((4f * u + 2f * v) * Tau) * 0.7f
                              + Mathf.Sin((-1f * u + 6f * v) * Tau + 0.4f) * 0.5f;
                    float thread = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(w)), 10f);
                    pixels[y * Size + x] = new Color(thread, thread, thread, 1f);
                }
            }

            return Finish(pixels, "WaterSparkle");
        }

        // A dark crust broken by a network of bright cracks: the edges between random cells that wrap around.
        public static Texture2D Lava()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    EdgeDistance(x / (float)Size, y / (float)Size, out float edge, out float tone);
                    float crack = 1f - TerrainNoise.SmoothStep(0.02f, 0.2f, edge);
                    float crust = 0.1f + 0.1f * tone;
                    Color color = Color.Lerp(new Color(crust, crust * 0.35f, crust * 0.2f), new Color(1f, 0.62f, 0.14f), crack);
                    color = Color.Lerp(color, new Color(1f, 0.9f, 0.5f), crack * crack * 0.6f);
                    pixels[y * Size + x] = color;
                }
            }

            return Finish(pixels, "Lava");
        }

        /// <summary>Soft blotches of heat that drift under the crust, so the glow seems to move inside the lava.</summary>
        public static Texture2D LavaHeat()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float w = Mathf.Sin((2f * u + 1f * v) * Tau)
                              + Mathf.Sin((-1f * u + 3f * v) * Tau + 1.7f) * 0.8f
                              + Mathf.Sin((3f * u - 2f * v) * Tau + 0.6f) * 0.6f;
                    float heat = TerrainNoise.SmoothStep(0.5f, 2.2f, w);
                    pixels[y * Size + x] = new Color(heat, heat * 0.55f, heat * 0.2f, 1f);
                }
            }

            return Finish(pixels, "LavaHeat");
        }

        public static Texture2D Sand()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float warp = Mathf.Sin((2f * u + 3f * v) * Tau) * 0.12f;
                    float swirl = 0.5f + 0.5f * Mathf.Sin((4f * u + warp + 2f * v) * Tau);
                    float grain = TerrainNoise.Hash(x, y, 9) * 0.1f;
                    float value = 0.7f + 0.22f * swirl + grain;
                    pixels[y * Size + x] = new Color(value, value, value, 1f);
                }
            }

            return Finish(pixels, "Sand");
        }

        /// <summary>Fine streaks that creep across the sand in the other direction, so the surface seems to churn.</summary>
        public static Texture2D SandStreaks()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float warp = Mathf.Sin((1f * u - 2f * v) * Tau + 0.9f) * 0.2f;
                    float streak = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((6f * u + warp + 1f * v) * Tau), 6f);
                    pixels[y * Size + x] = new Color(streak, streak * 0.95f, streak * 0.8f, 1f);
                }
            }

            return Finish(pixels, "SandStreaks");
        }

        private static void EdgeDistance(float u, float v, out float edge, out float tone)
        {
            float px = u * LavaCells;
            float pz = v * LavaCells;
            int cx = Mathf.FloorToInt(px);
            int cz = Mathf.FloorToInt(pz);
            float best = float.MaxValue;
            float second = float.MaxValue;
            int bestId = 0;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int wrapX = ((cx + dx) % LavaCells + LavaCells) % LavaCells;
                    int wrapZ = ((cz + dz) % LavaCells + LavaCells) % LavaCells;
                    float sx = cx + dx + 0.15f + 0.7f * TerrainNoise.Hash(wrapX, wrapZ, 4);
                    float sz = cz + dz + 0.15f + 0.7f * TerrainNoise.Hash(wrapX, wrapZ, 5);
                    float d = (px - sx) * (px - sx) + (pz - sz) * (pz - sz);
                    if (d < best)
                    {
                        second = best;
                        best = d;
                        bestId = wrapX * 31 + wrapZ;
                    }
                    else if (d < second)
                    {
                        second = d;
                    }
                }
            }

            edge = Mathf.Sqrt(second) - Mathf.Sqrt(best);
            tone = TerrainNoise.Hash(bestId, bestId / 7, 6);
        }

        private static Texture2D Finish(Color32[] pixels, string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false)
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
