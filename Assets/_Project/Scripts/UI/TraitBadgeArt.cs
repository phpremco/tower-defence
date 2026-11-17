using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The plate badge of the wave preview: a small shield painted in code (white, tinted by the image), for a creature that
    /// wears armour. The flyer badge is the wing icon the tower cards already use, so a creature's mark and the mark on the
    /// gun that answers it are the same picture.
    /// </summary>
    public static class TraitBadgeArt
    {
        private const int Size = 64;
        private const int Samples = 4;
        // The shield in a square from -1 to 1: a flat top, straight shoulders, then both sides curving in to a point.
        private const float Top = 0.82f;
        private const float HalfWidth = 0.74f;
        private const float Shoulder = 0.12f;
        private const float Tip = -0.92f;
        // The face inside the rim is a shade darker, so the badge reads as a plate and not as a blob.
        private const float Rim = 0.24f;
        private const float FaceShade = 0.62f;

        private static Sprite _shield;

        /// <summary>The shield badge; painted once and kept for the session.</summary>
        public static Sprite Shield => _shield != null ? _shield : (_shield = Paint());

        private static Sprite Paint()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = Sample(x, y);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "PlateBadge",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Supersampled so the curved edge is smooth at badge size.
        private static Color32 Sample(int x, int y)
        {
            float cover = 0f;
            float shade = 0f;
            for (int sy = 0; sy < Samples; sy++)
            {
                for (int sx = 0; sx < Samples; sx++)
                {
                    float u = ((x + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                    float v = ((y + (sy + 0.5f) / Samples) / Size) * 2f - 1f;
                    if (!Inside(u, v, 1f))
                    {
                        continue;
                    }

                    cover += 1f;
                    shade += Inside(u, v, 1f - Rim) ? FaceShade : 1f;
                }
            }

            if (cover <= 0f)
            {
                return new Color32(255, 255, 255, 0);
            }

            byte value = (byte)Mathf.RoundToInt(255f * shade / cover);
            return new Color32(value, value, value, (byte)Mathf.RoundToInt(255f * cover / (Samples * Samples)));
        }

        // Whether a point lies in the shield scaled about its middle (a scale below one gives the inner face).
        private static bool Inside(float u, float v, float scale)
        {
            float middle = (Top + Tip) * 0.5f;
            u /= scale;
            v = middle + (v - middle) / scale;
            if (v > Top || v < Tip)
            {
                return false;
            }

            if (v >= Shoulder)
            {
                return Mathf.Abs(u) <= HalfWidth;
            }

            float down = (Shoulder - v) / (Shoulder - Tip);
            return Mathf.Abs(u) <= HalfWidth * Mathf.Sqrt(Mathf.Max(0f, 1f - down * down));
        }
    }
}
