using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Paints the exhausted Earth of 2053 for a lit sphere, in the same map layout as <see cref="GlobePainter"/>:
    /// dull grey-brown continents, dead forest patches and dust belts, murky seas, terraced open-pit scars eaten
    /// into the land, shrunken grey ice, a scatter of dim amber city lights that are going out, and a brown smog
    /// layer. Sphere noise only, so no image files; the maps are painted once per size and shared.
    /// </summary>
    public static class EarthPainter
    {
        /// <summary>The surface maps plus the smog deck that drifts over them.</summary>
        public sealed class Painting
        {
            public GlobePainter.Maps Maps;
            public Texture2D Smog;
        }

        private const float NoiseScale = 2.4f;
        private const float LandLevel = 0.53f;
        private const float CoastWidth = 0.02f;
        private const int Scars = 46;
        private const float ScarMinDegrees = 1.6f;
        private const float ScarMaxDegrees = 4.2f;
        private const float ScarTerraces = 5f;
        private const float ScarDepth = 0.35f;
        private const float IceLatitude = 74f;
        private const float CityDensity = 0.6f;
        private const float CityDying = 0.45f;
        private const float NormalHeight = 0.4f;
        private const float SmogCover = 0.6f;
        private const float SeaSmoothness = 0.25f;
        private const float LandSmoothness = 0.08f;
        private static readonly Color DeepSea = new Color(0.05f, 0.075f, 0.09f);
        private static readonly Color MurkySea = new Color(0.13f, 0.15f, 0.14f);
        private static readonly Color DryLand = new Color(0.36f, 0.3f, 0.22f);
        private static readonly Color GreyLand = new Color(0.32f, 0.31f, 0.29f);
        private static readonly Color DeadForest = new Color(0.19f, 0.17f, 0.13f);
        private static readonly Color Dust = new Color(0.52f, 0.44f, 0.31f);
        private static readonly Color ScarRim = new Color(0.6f, 0.53f, 0.42f);
        private static readonly Color ScarFloor = new Color(0.16f, 0.13f, 0.11f);
        private static readonly Color DirtyIce = new Color(0.55f, 0.55f, 0.53f);
        private static readonly Color CityLight = new Color(1f, 0.62f, 0.25f);
        private static readonly Color SmogColour = new Color(0.62f, 0.5f, 0.36f);

        // The cosine of each scar's reach, so the per-pixel test is one dot product.
        private static readonly float[] ScarReach = Reaches();

        private static Painting _cached;
        private static int _cachedWidth;

        /// <summary>The Earth at this map width (the height is half); painted on first use and kept.</summary>
        public static Painting Paint(int width)
        {
            if (_cached != null && _cachedWidth == width && _cached.Maps.Albedo != null)
            {
                return _cached;
            }

            int height = width / 2;
            Vector3[] scars = ScarCentres();
            var heights = new float[width * height];
            var albedo = new Color32[width * height];
            var gloss = new Color32[width * height];
            var emission = new Color32[width * height];
            var smog = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float latitude = (y + 0.5f) / height * 180f - 90f;
                for (int x = 0; x < width; x++)
                {
                    float longitude = (x + 0.5f) / width * 360f - 180f;
                    Vector3 p = Game.Levels.PlanetCoordinates.ToDirection(longitude, latitude);
                    int i = y * width + x;
                    Shade(p, latitude, scars, out heights[i], out Color colour, out float smoothness, out Color lights);
                    albedo[i] = colour;
                    gloss[i] = new Color(0f, 0f, 0f, smoothness);
                    emission[i] = lights;
                    smog[i] = Smog(p, latitude);
                }
            }

            _cached = new Painting
            {
                Maps = new GlobePainter.Maps
                {
                    Albedo = GlobePainter.ToTexture(albedo, width, height, "EarthAlbedo", false),
                    Normal = GlobePainter.ToTexture(GlobePainter.Normals(heights, width, height), width, height, "EarthNormal", true),
                    Gloss = GlobePainter.ToTexture(gloss, width, height, "EarthGloss", true),
                    Emission = GlobePainter.ToTexture(emission, width, height, "EarthLights", false),
                },
                Smog = GlobePainter.ToTexture(smog, width, height, "EarthSmog", false),
            };
            _cachedWidth = width;
            return _cached;
        }

        private static void Shade(Vector3 p, float latitude, Vector3[] scars, out float elevation, out Color colour, out float smoothness, out Color lights)
        {
            float n = GlobePainter.SphereNoise(p, NoiseScale, 300f) * 0.65f + GlobePainter.SphereNoise(p, NoiseScale * 3.3f, 310f) * 0.25f + GlobePainter.SphereNoise(p, NoiseScale * 9f, 320f) * 0.1f;
            float land = GlobePainter.Smooth(LandLevel - CoastWidth, LandLevel + CoastWidth, n);
            float relief = GlobePainter.SphereNoise(p, NoiseScale * 6f, 330f);

            // Land: grey and brown in broad patches, dead forest in the damp parts, dust where it is dry.
            Color ground = Color.Lerp(DryLand, GreyLand, GlobePainter.Smooth(0.4f, 0.6f, GlobePainter.SphereNoise(p, NoiseScale * 2f, 340f)));
            ground = Color.Lerp(ground, DeadForest, GlobePainter.Smooth(0.55f, 0.66f, GlobePainter.SphereNoise(p, NoiseScale * 4f, 350f)) * 0.8f);
            float dry = GlobePainter.Smooth(0.5f, 0.7f, GlobePainter.SphereNoise(p, NoiseScale * 1.6f, 360f)) * (1f - Mathf.Abs(Mathf.Abs(latitude) - 25f) / 30f);
            ground = Color.Lerp(ground, Dust, Mathf.Clamp01(dry));
            ground *= 0.85f + 0.3f * relief;

            // Open-pit scars: terraced rings stepping down to a dark floor, the rims bright with exposed rock.
            float pit = 0f;
            float rings = 0f;
            for (int i = 0; i < scars.Length; i++)
            {
                float dot = Vector3.Dot(p, scars[i]);
                if (dot < ScarReach[i])
                {
                    continue;
                }

                float radius = ScarRadius(i);
                float share = 1f - Mathf.Acos(Mathf.Min(1f, dot)) / radius;
                pit = Mathf.Max(pit, share);
                rings = Mathf.Max(rings, Mathf.Abs(Mathf.Sin(share * ScarTerraces * Mathf.PI)) * (1f - share));
            }

            pit *= land;
            ground = Color.Lerp(ground, Color.Lerp(ScarRim, ScarFloor, GlobePainter.Smooth(0.3f, 0.9f, pit)), GlobePainter.Smooth(0f, 0.15f, pit));
            ground = Color.Lerp(ground, ScarRim, rings * land * 0.5f);

            Color sea = Color.Lerp(DeepSea, MurkySea, GlobePainter.Smooth(LandLevel - 0.15f, LandLevel, n));
            colour = Color.Lerp(sea, ground, land);
            float ice = GlobePainter.Smooth(IceLatitude, IceLatitude + 8f, Mathf.Abs(latitude) + (n - 0.5f) * 10f);
            colour = Color.Lerp(colour, DirtyIce * (0.85f + 0.15f * relief), ice);
            colour.a = 1f;
            smoothness = Mathf.Lerp(Mathf.Lerp(SeaSmoothness, LandSmoothness, land), 0.3f, ice);
            elevation = land * (0.2f + NormalHeight * relief) - pit * ScarDepth - rings * 0.05f;

            // Cities: clusters of warm points on the land, most of them already dark.
            float cluster = GlobePainter.Smooth(0.48f, 0.62f, GlobePainter.SphereNoise(p, NoiseScale * 2.6f, 370f));
            float speckle = GlobePainter.Smooth(CityDensity, CityDensity + 0.1f, GlobePainter.SphereNoise(p, NoiseScale * 42f, 380f));
            float alive = GlobePainter.Smooth(CityDying, CityDying + 0.15f, GlobePainter.SphereNoise(p, NoiseScale * 11f, 390f));
            lights = CityLight * (cluster * speckle * alive * land * (1f - ice) * (1f - pit));
            lights.a = 1f;
        }

        // A brown haze over everything, thicker in the belts where industry and dust storms were.
        private static Color32 Smog(Vector3 p, float latitude)
        {
            float belts = 0.6f + 0.4f * Mathf.Cos(latitude * Mathf.Deg2Rad * 2.5f);
            float sheets = GlobePainter.SphereNoise(p, NoiseScale * 1.4f, 400f) * 0.6f + GlobePainter.SphereNoise(p, NoiseScale * 5f, 410f) * 0.4f;
            float density = GlobePainter.Smooth(0.42f, 0.7f, sheets * belts + 0.12f);
            return new Color(SmogColour.r, SmogColour.g, SmogColour.b, density * SmogCover);
        }

        private static float ScarRadius(int index) => Mathf.Lerp(ScarMinDegrees, ScarMaxDegrees, CutsceneMath.Hash(index, 7)) * Mathf.Deg2Rad;

        private static Vector3[] ScarCentres()
        {
            var centres = new Vector3[Scars];
            for (int i = 0; i < Scars; i++)
            {
                float longitude = CutsceneMath.Hash(i, 3) * 360f - 180f;
                float latitude = Mathf.Lerp(-55f, 62f, CutsceneMath.Hash(i, 5));
                centres[i] = Game.Levels.PlanetCoordinates.ToDirection(longitude, latitude);
            }

            return centres;
        }

        private static float[] Reaches()
        {
            var reaches = new float[Scars];
            for (int i = 0; i < Scars; i++)
            {
                reaches[i] = Mathf.Cos(ScarRadius(i));
            }

            return reaches;
        }
    }
}
