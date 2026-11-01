using UnityEngine;

namespace Game.Core
{
    /// <summary>Small generated textures shared by the effects: a soft ring, a ragged puff and tileable noise.</summary>
    public static class SpriteKit
    {
        private const int Size = 128;
        private const float Tau = Mathf.PI * 2f;

        private static Texture2D _ring;
        private static Texture2D _puff;
        private static Texture2D _noise;

        /// <summary>A soft ring with a transparent centre and edge, for ripples and shock waves seen from above.</summary>
        public static Texture2D Ring()
        {
            if (_ring != null)
            {
                return _ring;
            }

            const float radius = 0.42f;
            const float width = 0.05f;
            _ring = Make("Ring", TextureWrapMode.Clamp, (u, v) =>
            {
                float d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - radius);
                float a = Mathf.Clamp01(1f - d / width);
                return new Color(1f, 1f, 1f, a * a);
            });
            return _ring;
        }

        /// <summary>A ragged puff of vapour: a soft disc broken up by noise, so smoke and steam have body instead of a blur.</summary>
        public static Texture2D Puff()
        {
            if (_puff != null)
            {
                return _puff;
            }

            _puff = Make("Puff", TextureWrapMode.Clamp, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float x = (u + 0.5f) * Size;
                float y = (v + 0.5f) * Size;
                float noise = Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.07f + 7.7f) * 0.7f + Mathf.PerlinNoise(x * 0.16f, y * 0.16f) * 0.3f;
                float edge = Mathf.Clamp01(1f - (r - 0.25f) / 0.7f);
                return new Color(1f, 1f, 1f, edge * edge * Mathf.Clamp01(noise * 1.6f - 0.2f));
            });
            return _puff;
        }

        /// <summary>Seamless cloudy noise from summed sines, for shaders that churn a surface.</summary>
        public static Texture2D Noise()
        {
            if (_noise != null)
            {
                return _noise;
            }

            _noise = Make("Noise", TextureWrapMode.Repeat, (u, v) =>
            {
                float w = Mathf.Sin((3f * u + 2f * v) * Tau) + Mathf.Sin((1f * u - 4f * v) * Tau + 1.7f) * 0.8f
                          + Mathf.Sin((5f * u + 1f * v) * Tau + 0.6f) * 0.5f + Mathf.Sin((2f * u + 6f * v) * Tau + 2.9f) * 0.4f;
                float value = Mathf.Clamp01(0.5f + w * 0.18f);
                return new Color(value, value, value, 1f);
            });
            return _noise;
        }

        // Shader samples u and v centred on zero, from -0.5 to 0.5.
        private static Texture2D Make(string name, TextureWrapMode wrap, System.Func<float, float, Color> shade)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = shade((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false) { name = name, wrapMode = wrap };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
