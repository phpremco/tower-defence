using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The puff the cutscene's smoke and steam sprites wear: a soft disc broken up by a few octaves of value noise, so a
    /// trail of puffs reads as billowing gas rather than a row of dots. Painted once in code and kept.
    /// </summary>
    public static class SmokeTexture
    {
        private const int Size = 64;
        private const int Octaves = 4;

        private static Texture2D _puff;

        public static Texture2D Puff
        {
            get
            {
                if (_puff == null)
                {
                    _puff = Paint();
                }

                return _puff;
            }
        }

        private static Texture2D Paint()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 2f - 1f;
                    float v = (y + 0.5f) / Size * 2f - 1f;
                    float disc = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                    float noise = 0f;
                    float amplitude = 0.5f;
                    float frequency = 3f;
                    for (int o = 0; o < Octaves; o++)
                    {
                        noise += amplitude * Mathf.PerlinNoise(u * frequency + 11.3f * o, v * frequency + 4.7f * o);
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }

                    float alpha = Mathf.Clamp01(disc * disc * (0.45f + 1.1f * noise));
                    byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(170f, 255f, noise));
                    pixels[y * Size + x] = new Color32(shade, shade, shade, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "SmokePuff", wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
