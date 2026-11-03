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

        /// <summary>A frozen sheet: pale with faint milky swirls and a web of darker fractures frozen into it.</summary>
        public static Texture2D Ice()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float swirl = 0.5f + 0.5f * Mathf.Sin((2f * u + 1f * v) * Tau + Mathf.Sin((1f * u - 3f * v) * Tau) * 0.6f);
                    EdgeDistance(u, v, out float edge, out float tone);
                    float fracture = Mathf.Pow(1f - Mathf.Clamp01(edge * 9f), 3f);
                    float value = 0.86f + 0.1f * swirl + 0.06f * tone - 0.25f * fracture;
                    pixels[y * Size + x] = new Color(value, value, Mathf.Min(1f, value + 0.04f), 1f);
                }
            }

            return Finish(pixels, "Ice");
        }

        /// <summary>Bright hairline cracks and frost blooms, for the layer that creeps over the ice.</summary>
        public static Texture2D IceCracks()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    EdgeDistance(u, v, out float edge, out float _);
                    float crack = Mathf.Pow(1f - Mathf.Clamp01(edge * 22f), 4f);
                    float bloom = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((5f * u - 2f * v) * Tau + Mathf.Sin((3f * u + 4f * v) * Tau)), 9f) * 0.5f;
                    float value = Mathf.Max(crack, bloom);
                    pixels[y * Size + x] = new Color(value, value, value, 1f);
                }
            }

            return Finish(pixels, "IceCracks");
        }

        /// <summary>Murky bog water: dark, with slow oily swirls and patches of lighter scum.</summary>
        public static Texture2D Bog()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float warp = Mathf.Sin((1f * u + 2f * v) * Tau) * 0.2f;
                    float swirl = 0.5f + 0.5f * Mathf.Sin((3f * u + warp - 2f * v) * Tau);
                    float scum = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((4f * u + 5f * v) * Tau + swirl * 2f), 5f);
                    float grain = TerrainNoise.Hash(x, y, 13) * 0.08f;
                    float value = 0.55f + 0.2f * swirl + 0.3f * scum + grain;
                    pixels[y * Size + x] = new Color(value * 0.95f, value, value * 0.8f, 1f);
                }
            }

            return Finish(pixels, "Bog");
        }

        /// <summary>Rings and specks of scum that drift over the bog, so the surface seems to breathe.</summary>
        public static Texture2D BogScum()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float rings = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin((Mathf.Sin(u * Tau * 2f) + Mathf.Sin(v * Tau * 3f)) * 3f)), 8f);
                    float specks = TerrainNoise.Hash(x / 3, y / 3, 17) > 0.93f ? 0.6f : 0f;
                    float value = Mathf.Max(rings * 0.6f, specks);
                    pixels[y * Size + x] = new Color(value * 0.8f, value, value * 0.5f, 1f);
                }
            }

            return Finish(pixels, "BogScum");
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

        /// <summary>The floor of a rift: dark broken basalt in layers, with grain; the veins are their own map.</summary>
        public static Texture2D RiftRock()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    float layers = 0.5f + 0.5f * Mathf.Sin((2f * u + 6f * v) * Tau + Mathf.Sin((3f * u - 1f * v) * Tau) * 0.8f);
                    float grain = TerrainNoise.Hash(x, y, 23) * 0.12f;
                    float value = 0.42f + 0.18f * layers + grain;
                    pixels[y * Size + x] = new Color(value, value * 0.95f, value * 0.9f, 1f);
                }
            }

            return Finish(pixels, "RiftRock");
        }

        /// <summary>The glowing veins at the bottom of a rift: thin branching threads, bright where two cross.</summary>
        public static Texture2D RiftVeins()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    // Two families of warped crack lines, each broken into segments by its own slow mask, so the veins
                    // wander and stop instead of forming a net; a soft halo lets the bloom catch them.
                    float a = Mathf.Sin((2f * u + 1f * v + 0.35f * Mathf.Sin((3f * u - 2f * v) * Tau) + 0.2f * Mathf.Sin((5f * u + 4f * v) * Tau)) * Tau);
                    float b = Mathf.Sin((1f * u - 2f * v + 0.3f * Mathf.Sin((4f * u + 1f * v) * Tau) + 0.25f * Mathf.Sin((2f * u - 5f * v) * Tau)) * Tau);
                    float maskA = TerrainNoise.SmoothStep(0.35f, 0.65f, 0.5f + 0.5f * Mathf.Sin((3f * u + 5f * v) * Tau + 1.5f * Mathf.Sin((2f * u - 1f * v) * Tau)));
                    float maskB = TerrainNoise.SmoothStep(0.4f, 0.7f, 0.5f + 0.5f * Mathf.Sin((4f * u - 3f * v) * Tau + 1.2f * Mathf.Sin((1f * u + 3f * v) * Tau)));
                    float lineA = Mathf.Pow(1f - Mathf.Abs(a), 60f) + Mathf.Pow(1f - Mathf.Abs(a), 12f) * 0.2f;
                    float lineB = Mathf.Pow(1f - Mathf.Abs(b), 70f) + Mathf.Pow(1f - Mathf.Abs(b), 14f) * 0.15f;
                    float value = Mathf.Clamp01(lineA * maskA + lineB * maskB * 0.8f);
                    pixels[y * Size + x] = new Color(value, value, value, 1f);
                }
            }

            return Finish(pixels, "RiftVeins");
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
