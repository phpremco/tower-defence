using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public enum IconType
    {
        Upgrade,
        Repair,
        Sell,
        Speed,
        Pause,
        SkipWave,
        Star,
        Check,
        Hero,
        Shockwave,
        Combine,
    }

    /// <summary>Draws the simple white HUD icons into small textures at runtime, so no image files are needed.</summary>
    public static class IconFactory
    {
        private const int Size = 64;
        private static readonly Dictionary<IconType, Sprite> Cache = new Dictionary<IconType, Sprite>();

        public static Sprite Get(IconType type)
        {
            if (Cache.TryGetValue(type, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[Size * Size];
            Func<float, float, bool> shape = ShapeFor(type);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool on = shape((x + 0.5f) / Size, (y + 0.5f) / Size);
                    pixels[y * Size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            Cache[type] = sprite;
            return sprite;
        }

        private static Func<float, float, bool> ShapeFor(IconType type)
        {
            switch (type)
            {
                case IconType.Upgrade:
                    return (u, v) => Triangle(u, v, 0.15f, 0.45f, 0.85f, 0.45f, 0.5f, 0.85f) || (Mathf.Abs(u - 0.5f) < 0.11f && v > 0.15f && v <= 0.45f);
                case IconType.Repair:
                    return Wrench;
                case IconType.Sell:
                    return Coin;                case IconType.Star:
                    return Star;
                case IconType.Check:
                    return Check;
                case IconType.Hero:
                    // A shield: a flat top that narrows to a point.
                    return (u, v) => (v > 0.5f && v < 0.86f && Mathf.Abs(u - 0.5f) < 0.3f) || Triangle(u, v, 0.2f, 0.5f, 0.8f, 0.5f, 0.5f, 0.1f);
                case IconType.Combine:
                    // Two circles that overlap, joined in the middle.
                    return (u, v) => Mathf.Sqrt((u - 0.33f) * (u - 0.33f) + (v - 0.5f) * (v - 0.5f)) < 0.24f || Mathf.Sqrt((u - 0.67f) * (u - 0.67f) + (v - 0.5f) * (v - 0.5f)) < 0.24f;
                case IconType.Shockwave:
                    return (u, v) =>
                    {
                        float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
                        return d < 0.1f || (d > 0.22f && d < 0.3f) || (d > 0.4f && d < 0.47f);
                    };
                case IconType.Speed:
                    return (u, v) => Triangle(u, v, 0.15f, 0.2f, 0.15f, 0.8f, 0.5f, 0.5f) || Triangle(u, v, 0.5f, 0.2f, 0.5f, 0.8f, 0.85f, 0.5f);
                case IconType.Pause:
                    return (u, v) => v > 0.2f && v < 0.8f && ((u > 0.25f && u < 0.4f) || (u > 0.6f && u < 0.75f));
                default:
                    return (u, v) => Triangle(u, v, 0.15f, 0.2f, 0.15f, 0.8f, 0.65f, 0.5f) || (u > 0.72f && u < 0.85f && v > 0.2f && v < 0.8f);
            }
        }

        // A wrench: diagonal handle plus an open ring at the top right.
        private static bool Wrench(float u, float v)
        {
            float t = Mathf.Clamp01(((u - 0.22f) + (v - 0.22f)) / (2f * 0.46f));
            float lx = 0.22f + t * 0.46f;
            float ly = 0.22f + t * 0.46f;
            float handle = Mathf.Sqrt((u - lx) * (u - lx) + (v - ly) * (v - ly));
            if (handle < 0.075f)
            {
                return true;
            }

            float dx = u - 0.68f;
            float dy = v - 0.68f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            bool inRing = d < 0.2f && d > 0.1f;
            bool notch = dx > 0f && dy > 0f && Mathf.Abs(dx - dy) < 0.07f;
            return inRing && !notch;
        }

        // A five pointed star: the boundary radius runs from the outer radius at a tip to the inner radius in a valley.
        private static bool Star(float u, float v)
        {
            const float OuterRadius = 0.47f;
            const float InnerRadius = 0.2f;
            float dx = u - 0.5f;
            float dy = v - 0.46f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float sector = Mathf.PI * 2f / 5f;
            float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f, sector);
            float fromTip = Mathf.Min(angle, sector - angle) / (sector * 0.5f);
            return distance < Mathf.Lerp(OuterRadius, InnerRadius, fromTip);
        }
        // A tick mark made of two thick strokes.
        private static bool Check(float u, float v)
        {
            return Stroke(u, v, 0.18f, 0.5f, 0.4f, 0.28f, 0.085f) || Stroke(u, v, 0.4f, 0.28f, 0.82f, 0.74f, 0.085f);
        }

        private static bool Stroke(float px, float py, float ax, float ay, float bx, float by, float halfWidth)
        {
            float abx = bx - ax;
            float aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float cx = ax + abx * t;
            float cy = ay + aby * t;
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy < halfWidth * halfWidth;
        }

        // A coin: thick ring with a smaller solid disc inside.
        private static bool Coin(float u, float v)
        {
            float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            return (d < 0.42f && d > 0.32f) || d < 0.17f;
        }

        private static bool Triangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }
    }
}




