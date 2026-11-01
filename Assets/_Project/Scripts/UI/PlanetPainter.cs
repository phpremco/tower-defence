using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Paints the planet of the campaign into textures: its surface with one tinted region per level, a layer of
    /// clouds and a soft halo. Everything is noise, so no image files are needed; the map and the intro share it.
    /// </summary>
    public static class PlanetPainter
    {
        /// <summary>A level's part of the surface: where it lies in the map frame (0..1) and the colour it is tinted.</summary>
        public struct Region
        {
            public Vector2 Position;
            public Color Colour;

            /// <summary>Reach of the tint, in planet radii.</summary>
            public float Radius;
        }

        // Noise is sampled in a fixed 512-pixel frame, so a texture of any size shows the same planet.
        private const float NoiseFrame = 512f;
        private const float RimStart = 0.82f;
        private const float RimStrength = 0.8f;
        private const float DarkSide = 0.35f;
        private const float RegionCore = 0.3f;
        private const float RegionNoiseFloor = 0.55f;
        private const float CloudCover = 0.3f;
        private const float CloudEdge = 0.97f;
        private const float HaloFalloff = 1.6f;
        private static readonly Color Sea = new Color(0.08f, 0.22f, 0.3f);
        private static readonly Color Land = new Color(0.3f, 0.42f, 0.28f);
        private static readonly Color Highland = new Color(0.5f, 0.3f, 0.16f);
        private static readonly Color Atmosphere = new Color(0.5f, 0.85f, 1f);

        /// <summary>
        /// The surface: sea, land and highland from noise, each region tinted around its centre, the day side lit
        /// from the top right and the atmosphere brightening the rim. Transparent outside the disc.
        /// </summary>
        /// <param name="frame">Half extents of the map frame in planet radii: the map's width and height over the planet's diameter.</param>
        public static Texture2D PaintSurface(int size, IReadOnlyList<Region> regions, Vector2 frame)
        {
            return Paint(size, (dx, dy, d, u, v) =>
            {
                if (d > 1f)
                {
                    return Color.clear;
                }

                float n = Mathf.PerlinNoise(u * 0.012f + 3f, v * 0.012f + 7f) * 0.6f + Mathf.PerlinNoise(u * 0.04f, v * 0.04f) * 0.28f + Mathf.PerlinNoise(u * 0.11f + 8f, v * 0.11f + 2f) * 0.12f;
                Color land = Color.Lerp(Sea, Land, Smooth(0.35f, 0.65f, n));
                land = Color.Lerp(land, Highland, Smooth(0.62f, 0.8f, n) * 0.6f);
                float grain = RegionNoiseFloor + (1f - RegionNoiseFloor) * Mathf.PerlinNoise(u * 0.03f + 11f, v * 0.03f + 5f);
                for (int i = 0; i < regions.Count; i++)
                {
                    Region region = regions[i];
                    float cx = (region.Position.x - 0.5f) * 2f * frame.x;
                    float cy = (region.Position.y - 0.5f) * 2f * frame.y;
                    float dist = Mathf.Sqrt((dx - cx) * (dx - cx) + (dy - cy) * (dy - cy));
                    float weight = (1f - Smooth(region.Radius * RegionCore, region.Radius, dist)) * grain;
                    land = Color.Lerp(land, region.Colour, weight);
                }

                float shade = Mathf.Lerp(1f, DarkSide, (dx + dy + 2f) * 0.25f);
                Color final = Color.Lerp(land * shade, Atmosphere, Smooth(RimStart, 1f, d) * RimStrength);
                final.a = Mathf.Clamp01((1f - d) * 40f);
                return final;
            });
        }

        /// <summary>Wisps of white inside the disc, thinning towards the rim, transparent outside. Turn it slowly for weather.</summary>
        public static Texture2D PaintClouds(int size)
        {
            return Paint(size, (dx, dy, d, u, v) =>
            {
                if (d > CloudEdge)
                {
                    return Color.clear;
                }

                float n = Mathf.PerlinNoise(u * 0.02f + 21f, v * 0.02f + 9f) * 0.6f + Mathf.PerlinNoise(u * 0.06f + 4f, v * 0.06f + 17f) * 0.4f;
                float alpha = Smooth(0.58f, 0.8f, n) * CloudCover * (1f - Smooth(0.85f, CloudEdge, d));
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        /// <summary>A soft glow that fades from the centre to the edge, drawn larger than the planet behind it.</summary>
        public static Texture2D PaintHalo(int size)
        {
            return Paint(size, (dx, dy, d, u, v) => d > 1f ? Color.clear : new Color(1f, 1f, 1f, Mathf.Pow(1f - d, HaloFalloff)));
        }

        private delegate Color Shader(float dx, float dy, float distance, float u, float v);

        private static Texture2D Paint(int size, Shader shader)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float noiseScale = NoiseFrame / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = shader(dx, dy, d, x * noiseScale, y * noiseScale);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }
    }
}
