using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Paints the soft halo the planet map draws behind its globe. Generated in code, so no image file is needed.
    /// </summary>
    public static class PlanetPainter
    {
        private const float HaloFalloff = 1.6f;

        /// <summary>A soft glow that fades from the centre to the edge, drawn larger than the planet behind it.</summary>
        public static Texture2D PaintHalo(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = d > 1f ? Color.clear : new Color(1f, 1f, 1f, Mathf.Pow(1f - d, HaloFalloff));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
