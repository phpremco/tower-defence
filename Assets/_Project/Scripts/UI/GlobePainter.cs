using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Paints the planet of the campaign for a lit sphere: an equirectangular surface with deep sea, shallows,
    /// beaches, lowland in each region's tint, hills, bare mountains with snow and ragged polar ice; a normal map
    /// from the same relief so the sun models the mountains; a gloss map that makes the sea shine and the land
    /// matte; the colony lights of every level site as emission; and a cloud layer. Everything is sphere noise,
    /// so it has no seam and no image files.
    /// </summary>
    public static class GlobePainter
    {
        /// <summary>A continent on the globe: where it lies, how far it reaches, the colour its land is tinted and whether it carries lights.</summary>
        public struct Region
        {
            public Vector3 Direction;
            public Color Colour;

            /// <summary>Reach of the land from the middle, in degrees of arc.</summary>
            public float RadiusDegrees;

            /// <summary>True for a level site: a settlement whose lights show on the surface.</summary>
            public bool Lights;
        }

        /// <summary>The maps one lit material needs; all the same size.</summary>
        public sealed class Maps
        {
            public Texture2D Albedo;
            public Texture2D Normal;
            public Texture2D Gloss;
            public Texture2D Emission;
        }

        private const float IceStart = 66f;
        private const float IceFull = 82f;
        private const float NoiseScale = 2.6f;
        private const float ContinentCore = 0.6f;
        private const float ContinentEdge = 1.1f;
        private const float CoastNoiseWeight = 2.1f;
        private const float CoastStart = 0.5f;
        private const float CoastEnd = 0.56f;
        private const float IslandStart = 0.84f;
        private const float IslandEnd = 0.9f;
        private const float IslandShare = 0.55f;
        private const float RegionTintStrength = 0.45f;
        private const float MountainHeight = 0.7f;
        private const float HillHeight = 0.25f;
        private const float NormalStrength = 22f;
        private const float SeaSmoothness = 0.88f;
        private const float IceSmoothness = 0.5f;
        private const float LandSmoothness = 0.12f;
        /// <summary>Reach of the patch of land every level site raises, in degrees of arc.</summary>
        public const float LevelLandDegrees = 7f;
        private const float LightReachShare = 0.55f;
        private const float LightDensity = 0.62f;
        private const float CloudCover = 0.7f;
        private static readonly Color DeepSea = new Color(0.02f, 0.1f, 0.26f);
        private static readonly Color Sea = new Color(0.04f, 0.2f, 0.4f);
        private static readonly Color Shallows = new Color(0.1f, 0.42f, 0.5f);
        private static readonly Color Beach = new Color(0.72f, 0.66f, 0.48f);
        private static readonly Color Lowland = new Color(0.34f, 0.48f, 0.26f);
        private static readonly Color Hills = new Color(0.4f, 0.4f, 0.22f);
        private static readonly Color Rock = new Color(0.42f, 0.36f, 0.3f);
        private static readonly Color Snow = new Color(0.92f, 0.95f, 1f);
        private static readonly Color Ice = new Color(0.8f, 0.9f, 0.97f);
        private static readonly Color Desert = new Color(0.66f, 0.56f, 0.36f);
        private static readonly Color LightColour = new Color(1f, 0.78f, 0.45f);

        public static Maps Paint(int width, int height, IReadOnlyList<Region> regions)
        {
            var heights = new float[width * height];
            var albedo = new Color32[width * height];
            var gloss = new Color32[width * height];
            var emission = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float latitude = (y + 0.5f) / height * 180f - 90f;
                for (int x = 0; x < width; x++)
                {
                    float longitude = (x + 0.5f) / width * 360f - 180f;
                    Vector3 p = Game.Levels.PlanetCoordinates.ToDirection(longitude, latitude);
                    int i = y * width + x;
                    Shade(p, latitude, regions, out heights[i], out Color colour, out float smoothness, out Color lights);
                    albedo[i] = colour;
                    gloss[i] = new Color(0f, 0f, 0f, smoothness);
                    emission[i] = lights;
                }
            }

            return new Maps
            {
                Albedo = ToTexture(albedo, width, height, "GlobeAlbedo", false),
                Normal = ToTexture(Normals(heights, width, height), width, height, "GlobeNormal", true),
                Gloss = ToTexture(gloss, width, height, "GlobeGloss", true),
                Emission = ToTexture(emission, width, height, "GlobeLights", false),
            };
        }

        /// <summary>Cloud cover for a sphere: banks and wisps, denser at the equator and the storm belts, transparent elsewhere.</summary>
        public static Texture2D PaintClouds(int width, int height, float seed)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float latitude = (y + 0.5f) / height * 180f - 90f;
                float belts = 0.55f + 0.45f * Mathf.Cos(latitude * Mathf.Deg2Rad * 3f);
                for (int x = 0; x < width; x++)
                {
                    float longitude = (x + 0.5f) / width * 360f - 180f;
                    Vector3 p = Game.Levels.PlanetCoordinates.ToDirection(longitude, latitude);
                    float banks = SphereNoise(p, NoiseScale * 1.3f, seed) * 0.5f + SphereNoise(p, NoiseScale * 3.6f, seed + 10f) * 0.3f + SphereNoise(p, NoiseScale * 9f, seed + 20f) * 0.2f;
                    // Three-way sphere noise crowds around one half, so the banks are cut from a narrow band of it.
                    float density = Smooth(0.52f, 0.72f, banks * belts);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(density) * CloudCover);
                }
            }

            return ToTexture(pixels, width, height, "GlobeClouds", false);
        }

        private static void Shade(Vector3 p, float latitude, IReadOnlyList<Region> regions, out float heightOut, out Color colour, out float smoothness, out Color lights)
        {
            float n = SphereNoise(p, NoiseScale, 0f) * 0.6f + SphereNoise(p, NoiseScale * 3.1f, 10f) * 0.28f + SphereNoise(p, NoiseScale * 8.7f, 20f) * 0.12f;

            // The body of the nearest continent, 1 at its heart and 0 beyond its reach, with its tint; the lights
            // of the nearest settlement.
            float body = 0f;
            Color tint = Lowland;
            float settlement = 0f;
            Color settlementColour = LightColour;
            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];
                float angle = Vector3.Angle(p, region.Direction);
                float reach = 1f - Smooth(region.RadiusDegrees * ContinentCore, region.RadiusDegrees * ContinentEdge, angle);
                if (reach > body)
                {
                    body = reach;
                    tint = Color.Lerp(Lowland, region.Colour, RegionTintStrength);
                }

                if (region.Lights)
                {
                    float glow = 1f - Smooth(0f, region.RadiusDegrees * LightReachShare, angle);
                    if (glow > settlement)
                    {
                        settlement = glow;
                        settlementColour = Color.Lerp(LightColour, region.Colour, 0.3f);
                    }
                }
            }

            float shape = body + (n - 0.5f) * CoastNoiseWeight;
            float coast = Smooth(CoastStart, CoastEnd, shape);
            float islands = Smooth(IslandStart, IslandEnd, (SphereNoise(p, NoiseScale * 5.3f, 60f) - 0.5f) * 3f + 0.5f) * (1f - body) * IslandShare;
            float land = Mathf.Max(coast, islands);

            // Relief: rolling hills everywhere on land, ridged mountain chains where the mountain mask is high.
            float hills = SphereNoise(p, NoiseScale * 6f, 70f);
            float ridge = 1f - Mathf.Abs(SphereNoise(p, NoiseScale * 4.5f, 80f) * 2f - 1f);
            ridge *= ridge;
            float mountainMask = Smooth(0.46f, 0.64f, SphereNoise(p, NoiseScale * 1.7f, 90f));
            float inland = Smooth(CoastEnd, CoastEnd + 0.25f, shape);
            float elevation = land * (0.15f + HillHeight * hills + MountainHeight * ridge * mountainMask * inland);
            heightOut = elevation;

            // Sea: deep far out, lighter over the shelf, flat.
            float shelf = Smooth(CoastStart - 0.2f, CoastStart, shape);
            Color sea = Color.Lerp(Color.Lerp(DeepSea, Sea, Smooth(0.1f, 0.4f, shape)), Shallows, shelf);
            sea *= 0.92f + 0.16f * SphereNoise(p, NoiseScale * 12f, 100f);

            // Land: beach at the waterline, lowland in the region's tint, hills, bare rock and snow with height; a
            // desert belt where it is hot and dry, ice where it is cold.
            float beach = 1f - Smooth(CoastEnd, CoastEnd + 0.05f, shape);
            Color ground = Color.Lerp(tint, Hills, Smooth(0.25f, 0.45f, elevation));
            ground = Color.Lerp(ground, Rock, Smooth(0.42f, 0.6f, elevation));
            // Forest darkens the lowland in patches, so a continent is never one flat sheet of its tint.
            ground = Color.Lerp(ground, ground * 0.62f, Smooth(0.5f, 0.6f, SphereNoise(p, NoiseScale * 3.4f, 140f)) * (1f - Smooth(0.3f, 0.45f, elevation)));
            ground = Color.Lerp(ground, Beach, beach * coast);
            float heat = 1f - Mathf.Abs(Mathf.Abs(latitude) - 24f) / 16f;
            float dry = Smooth(0.55f, 0.75f, SphereNoise(p, NoiseScale * 2.2f, 110f));
            ground = Color.Lerp(ground, Desert, Mathf.Clamp01(heat) * dry * (1f - Smooth(0.3f, 0.5f, elevation)));
            float cold = Smooth(IceStart - 30f, IceFull, Mathf.Abs(latitude));
            float snowLine = Mathf.Lerp(0.62f, 0.3f, cold);
            ground = Color.Lerp(ground, Snow, Smooth(snowLine, snowLine + 0.12f, elevation));
            ground *= 0.9f + 0.2f * SphereNoise(p, NoiseScale * 14f, 120f);

            colour = Color.Lerp(sea, ground, land);
            float ice = Smooth(IceStart, IceFull, Mathf.Abs(latitude) + (n - 0.5f) * 12f);
            colour = Color.Lerp(colour, Ice * (0.85f + 0.15f * hills), ice);
            colour.a = 1f;
            smoothness = Mathf.Lerp(Mathf.Lerp(SeaSmoothness, LandSmoothness, land), IceSmoothness, ice);

            // Settlements: a scatter of warm points on the land around every level, brightest at its heart.
            float speckle = Smooth(LightDensity, LightDensity + 0.12f, SphereNoise(p, NoiseScale * 40f, 130f));
            float strength = settlement * settlement * land * (1f - ice) * (0.25f + 0.75f * speckle);
            lights = settlementColour * strength;
            lights.a = 1f;
        }

        // Tangent-space normals from the height field: x along longitude (east), y along latitude (north).
        private static Color32[] Normals(float[] heights, int width, int height)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int up = Mathf.Min(y + 1, height - 1);
                int down = Mathf.Max(y - 1, 0);
                for (int x = 0; x < width; x++)
                {
                    int east = (x + 1) % width;
                    int west = (x - 1 + width) % width;
                    float dx = (heights[y * width + east] - heights[y * width + west]) * NormalStrength;
                    float dy = (heights[up * width + x] - heights[down * width + x]) * NormalStrength;
                    Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
                    pixels[y * width + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }

            return pixels;
        }

        private static Texture2D ToTexture(Color32[] pixels, int width, int height, string name, bool linear)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, linear) { name = name, wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        // Perlin noise read three ways across the unit sphere and averaged: continuous everywhere, so the map has
        // no seam at the date line and no pinch at the poles.
        private static float SphereNoise(Vector3 p, float scale, float offset)
        {
            float a = Mathf.PerlinNoise(p.x * scale + offset + 3f, p.y * scale + offset + 7f);
            float b = Mathf.PerlinNoise(p.y * scale + offset + 11f, p.z * scale + offset + 5f);
            float c = Mathf.PerlinNoise(p.z * scale + offset + 2f, p.x * scale + offset + 13f);
            return (a + b + c) / 3f;
        }

        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }
    }
}
