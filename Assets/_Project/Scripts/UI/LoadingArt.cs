using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The pictures of the loading screen, painted in code from the level's palette: the planet's limb the level lies on,
    /// the scan rings around it and a soft glow. Small textures made once per screen; every edge fades out over a few
    /// pixels, so nothing ends on a hard line whatever the resolution scales them to.
    /// </summary>
    public static class LoadingArt
    {
        private const float EdgePixels = 2.5f;
        private const float BandScale = 5.5f;
        private const float BandWarp = 1.7f;
        private const float NightShare = 0.12f;
        private const float RimPower = 3f;

        /// <summary>A lit sphere: banded surface between the two palette colours, a night side, and a thin bright rim of air.</summary>
        public static Texture2D Planet(int size, Color low, Color high, Color air, Vector3 sun)
        {
            var texture = NewTexture(size);
            var pixels = new Color[size * size];
            float radius = size * 0.5f - EdgePixels - 1f;
            Vector3 light = sun.normalized;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((radius - distance) / EdgePixels);
                    if (alpha <= 0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float nx = dx / radius;
                    float ny = dy / radius;
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                    // Bands that bend with the sphere and wander a little, like weather seen from orbit.
                    float latitude = ny * BandScale + Mathf.PerlinNoise(nx * BandWarp + 11.3f, ny * BandWarp + 4.1f) * 2.2f;
                    float band = Mathf.PerlinNoise(nx * 2.4f + 3.7f, latitude);
                    float detail = Mathf.PerlinNoise(nx * 9f + 1.2f, ny * 9f + 8.8f);
                    Color surface = Color.Lerp(low, high, Mathf.SmoothStep(0.25f, 0.8f, band * 0.75f + detail * 0.25f));
                    float lit = Mathf.Clamp01(Vector3.Dot(new Vector3(nx, ny, nz), light));
                    float shade = NightShare + (1f - NightShare) * Mathf.SmoothStep(0f, 1f, lit);
                    float rim = Mathf.Pow(1f - nz, RimPower) * Mathf.SmoothStep(-0.2f, 0.6f, lit);
                    Color colour = surface * shade + air * rim;
                    colour.a = alpha;
                    pixels[y * size + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        /// <summary>
        /// A thin white ring, soft on both sides, brighter over one arc so its turning shows. The arc fades in and out along
        /// the ring; the rest of the ring keeps <paramref name="restAlpha"/>.
        /// </summary>
        public static Texture2D Ring(int size, float thickness, float arcShare, float restAlpha)
        {
            var texture = NewTexture(size);
            var pixels = new Color[size * size];
            float radius = size * 0.5f - thickness - EdgePixels - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float off = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius);
                    float line = Mathf.Clamp01((thickness * 0.5f + EdgePixels - off) / EdgePixels);
                    float turn = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f;
                    float arc = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(turn - 0.5f) / Mathf.Max(0.01f, arcShare * 0.5f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, line * Mathf.Lerp(restAlpha, 1f, arc));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        /// <summary>A round white glow that falls off to nothing at the texture's edge.</summary>
        public static Texture2D Glow(int size)
        {
            var texture = NewTexture(size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float fall = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, fall * fall);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D NewTexture(int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }
}
