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
        SpeedDouble,
        SpeedTriple,
        Pause,
        SkipWave,
        Star,
        Check,
        Hero,
        Shockwave,
        Combine,
        Ground,
        Air,
        Slow,
        Arrow,
        Disc,
        Ring,
        Beacon,
        Blast,
        Roll,
        Net,
        Trap,
        Patrol,

        /// <summary>An open book: the codex.</summary>
        Book,

        /// <summary>A flat-topped hexagon plate: a region on the planet map.</summary>
        Hex,

        /// <summary>A padlock: a region that is not open yet.</summary>
        Lock,
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
                case IconType.Book:
                    // An open book: two pages either side of a spine, with a line of text on each.
                    return (u, v) => (Mathf.Abs(u - 0.5f) < 0.035f && v > 0.14f && v < 0.86f)
                        || (u > 0.1f && u < 0.44f && v > 0.2f && v < 0.8f && !(u > 0.16f && u < 0.38f && Mathf.Abs(v - 0.5f) < 0.04f))
                        || (u > 0.56f && u < 0.9f && v > 0.2f && v < 0.8f && !(u > 0.62f && u < 0.84f && Mathf.Abs(v - 0.5f) < 0.04f));
                case IconType.Hero:
                    // A shield: a flat top that narrows to a point.
                    return (u, v) => (v > 0.5f && v < 0.86f && Mathf.Abs(u - 0.5f) < 0.3f) || Triangle(u, v, 0.2f, 0.5f, 0.8f, 0.5f, 0.5f, 0.1f);
                case IconType.Ground:
                    // Two mountain peaks on a baseline: what walks.
                    return (u, v) => Triangle(u, v, 0.08f, 0.28f, 0.62f, 0.28f, 0.36f, 0.82f) || Triangle(u, v, 0.45f, 0.28f, 0.92f, 0.28f, 0.7f, 0.64f) || (v > 0.16f && v < 0.28f && u > 0.08f && u < 0.92f);
                case IconType.Air:
                    // A pair of raised wings: what flies.
                    return (u, v) => Stroke(u, v, 0.08f, 0.42f, 0.5f, 0.7f, 0.075f) || Stroke(u, v, 0.5f, 0.7f, 0.92f, 0.42f, 0.075f) || Stroke(u, v, 0.5f, 0.62f, 0.5f, 0.3f, 0.07f);
                case IconType.Blast:
                    // A burst: eight spokes from a bright core.
                    return (u, v) => Radius(u, v) < 0.16f || Spokes(u, v, 8, 0.44f, 0.045f);
                case IconType.Roll:
                    // A wheel on the right with motion lines trailing it.
                    return (u, v) => Ring(u, v, 0.64f, 0.5f, 0.17f, 0.3f) || Stroke(u, v, 0.06f, 0.36f, 0.3f, 0.36f, 0.04f) || Stroke(u, v, 0.02f, 0.5f, 0.26f, 0.5f, 0.04f) || Stroke(u, v, 0.06f, 0.64f, 0.3f, 0.64f, 0.04f);
                case IconType.Net:
                    // A round net: a ring with a mesh inside.
                    return (u, v) => (Radius(u, v) > 0.37f && Radius(u, v) < 0.46f) || (Radius(u, v) < 0.4f && (Mathf.Abs(u - 0.35f) < 0.03f || Mathf.Abs(u - 0.5f) < 0.03f || Mathf.Abs(u - 0.65f) < 0.03f || Mathf.Abs(v - 0.35f) < 0.03f || Mathf.Abs(v - 0.5f) < 0.03f || Mathf.Abs(v - 0.65f) < 0.03f));
                case IconType.Trap:
                    // A base with three spikes: what waits in the road.
                    return (u, v) => (v > 0.14f && v < 0.3f && u > 0.1f && u < 0.9f) || Triangle(u, v, 0.14f, 0.3f, 0.38f, 0.3f, 0.26f, 0.8f) || Triangle(u, v, 0.38f, 0.3f, 0.62f, 0.3f, 0.5f, 0.9f) || Triangle(u, v, 0.62f, 0.3f, 0.86f, 0.3f, 0.74f, 0.8f);
                case IconType.Patrol:
                    // A buggy: body, cabin and two wheels.
                    return (u, v) => (u > 0.18f && u < 0.82f && v > 0.4f && v < 0.64f) || (u > 0.34f && u < 0.66f && v >= 0.64f && v < 0.82f) || Ring(u, v, 0.32f, 0.32f, 0f, 0.13f) || Ring(u, v, 0.68f, 0.32f, 0f, 0.13f);
                case IconType.Hex:
                    return Hexagon;
                case IconType.Lock:
                    // A padlock: the body below, the shackle as a ring open at the bottom.
                    return (u, v) => (u > 0.22f && u < 0.78f && v > 0.1f && v < 0.5f) || (Ring(u, v, 0.5f, 0.56f, 0.14f, 0.26f) && v > 0.56f);
                case IconType.Disc:
                    return (u, v) => Radius(u, v) < 0.48f;
                case IconType.Ring:
                    return (u, v) => Radius(u, v) > 0.38f && Radius(u, v) < 0.48f;
                case IconType.Beacon:
                    // A dot inside a ring: a place marked on the map.
                    return (u, v) => Radius(u, v) < 0.2f || (Radius(u, v) > 0.34f && Radius(u, v) < 0.45f);
                case IconType.Arrow:
                    // A wedge pointing right; the HUD turns it towards whatever it marks.
                    return (u, v) => Triangle(u, v, 0.12f, 0.18f, 0.12f, 0.82f, 0.92f, 0.5f);
                case IconType.Slow:
                    // A six-armed snowflake: slows what it hits.
                    return (u, v) => Stroke(u, v, 0.5f, 0.08f, 0.5f, 0.92f, 0.06f) || Stroke(u, v, 0.14f, 0.29f, 0.86f, 0.71f, 0.06f) || Stroke(u, v, 0.14f, 0.71f, 0.86f, 0.29f, 0.06f) || Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) < 0.13f;
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
                    return (u, v) => Triangle(u, v, 0.3f, 0.2f, 0.3f, 0.8f, 0.7f, 0.5f);
                case IconType.SpeedDouble:
                    return (u, v) => Triangle(u, v, 0.15f, 0.2f, 0.15f, 0.8f, 0.5f, 0.5f) || Triangle(u, v, 0.5f, 0.2f, 0.5f, 0.8f, 0.85f, 0.5f);
                case IconType.SpeedTriple:
                    return (u, v) => Triangle(u, v, 0.08f, 0.2f, 0.08f, 0.8f, 0.36f, 0.5f) || Triangle(u, v, 0.36f, 0.2f, 0.36f, 0.8f, 0.64f, 0.5f) || Triangle(u, v, 0.64f, 0.2f, 0.64f, 0.8f, 0.92f, 0.5f);
                case IconType.Pause:
                    return (u, v) => v > 0.2f && v < 0.8f && ((u > 0.25f && u < 0.4f) || (u > 0.6f && u < 0.75f));
                default:
                    return (u, v) => Triangle(u, v, 0.15f, 0.2f, 0.15f, 0.8f, 0.65f, 0.5f) || (u > 0.72f && u < 0.85f && v > 0.2f && v < 0.8f);
            }
        }

        // A wrench: diagonal handle plus an open ring at the top right.
        // True between two radii around a centre; an inner radius of zero makes a disc.
        // A regular hexagon with flat top and bottom, filling most of the square.
        private static bool Hexagon(float u, float v)
        {
            float x = Mathf.Abs(u - 0.5f);
            float y = Mathf.Abs(v - 0.5f);
            const float Radius = 0.46f;
            return y <= Radius * 0.866f && x <= Radius - y * 0.577f;
        }

        private static bool Ring(float u, float v, float cx, float cy, float inner, float outer)
        {
            float d = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
            return d >= inner && d < outer;
        }

        // Evenly spaced strokes out of the centre, like the rays of a burst.
        private static bool Spokes(float u, float v, int count, float length, float halfWidth)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                if (Stroke(u, v, 0.5f, 0.5f, 0.5f + Mathf.Cos(angle) * length, 0.5f + Mathf.Sin(angle) * length, halfWidth))
                {
                    return true;
                }
            }

            return false;
        }

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
        private static float Radius(float u, float v) => Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));

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




