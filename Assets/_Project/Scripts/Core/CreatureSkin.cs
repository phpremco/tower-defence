using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Tiling skins for the procedural creatures, painted in code once and shared: chitin plates with lit seams,
    /// streaked hide, wet slime, overlapping scales and plasma. Each skin is an albedo with contrast, a normal map
    /// that gives the surface relief, and an emission mask that lets the species colour glow in the grooves. The
    /// body colour tints the albedo, so every species keeps its own hue.
    /// </summary>
    public static class CreatureSkin
    {
        public enum Kind
        {
            None = 0,
            Chitin = 1,
            Hide = 2,
            Slime = 3,
            Scales = 4,
            Energy = 5,
            Membrane = 6,

            /// <summary>Wet bulging tissue with glowing veins: hive mouths and nests of the brood.</summary>
            Flesh = 7,

            /// <summary>Weathered rock with grain and glowing cracks: vents, burrows, crystal beds and ore.</summary>
            Stone = 8,

            /// <summary>Ridged bark with lit grooves: hollow stumps and wooden nests.</summary>
            Bark = 9,

            /// <summary>A quilted pressure suit: padded cells between stitched seams that carry the suit's lights.</summary>
            Weave = 10,

            /// <summary>Ceramic armour: bevelled panels in offset rows with scuffs, the seams faintly lit.</summary>
            Plate = 11,

            /// <summary>Brushed gunmetal with a lattice of rivets: joints, boots, gear and weapon bodies.</summary>
            Alloy = 12,
        }

        public sealed class Surface
        {
            public Texture2D Albedo;
            public Texture2D Normal;
            public Texture2D Emission;
            public float Tiling;
            public float Smoothness;
            public float EmissionStrength;
        }

        private const int Size = 256;
        private static readonly Dictionary<Kind, Surface> Cache = new Dictionary<Kind, Surface>();

        public static Surface Get(Kind kind)
        {
            if (kind == Kind.None)
            {
                return null;
            }

            if (Cache.TryGetValue(kind, out Surface cached) && cached != null && cached.Albedo != null)
            {
                return cached;
            }

            Surface surface;
            switch (kind)
            {
                case Kind.Hide:
                    surface = Build("Hide", HideHeight, HideGlow, 2.6f, 0.7f, 0.7f, 2.5f, 0.3f, 0f);
                    break;
                case Kind.Flesh:
                    surface = Build("Flesh", FleshHeight, FleshGlow, 2.6f, 0.78f, 0.5f, 1.5f, 0.75f, 0.9f);
                    break;
                case Kind.Stone:
                    surface = Build("Stone", StoneHeight, StoneGlow, 3f, 0.7f, 0.6f, 2f, 0.25f, 0.8f);
                    break;
                case Kind.Bark:
                    surface = Build("Bark", BarkHeight, BarkGlow, 2.8f, 0.68f, 0.55f, 2f, 0.3f, 0.6f);
                    break;
                case Kind.Weave:
                    surface = Build("Weave", WeaveHeight, WeaveGlow, 2.4f, 0.8f, 0.5f, 1f, 0.45f, 0.45f);
                    break;
                case Kind.Plate:
                    surface = Build("Plate", PlateHeight, PlateGlow, 2.2f, 0.88f, 0.3f, 1f, 0.55f, 0.6f);
                    break;
                case Kind.Alloy:
                    surface = Build("Alloy", AlloyHeight, AlloyGlow, 2.5f, 0.7f, 0.35f, 3f, 0.65f, 0f);
                    break;
                case Kind.Slime:
                    surface = Build("Slime", SlimeHeight, SlimeGlow, 3f, 0.8f, 0.45f, 1.5f, 0.92f, 0.5f);
                    break;
                case Kind.Scales:
                    surface = Build("Scales", ScaleHeight, ScaleGlow, 3.5f, 0.74f, 0.55f, 2f, 0.7f, 0.6f);
                    break;
                case Kind.Energy:
                    surface = Build("Energy", EnergyHeight, EnergyGlow, 1.2f, 0.9f, 0.3f, 1f, 0.9f, 0.9f);
                    break;
                case Kind.Membrane:
                    surface = Build("Membrane", MembraneHeight, MembraneGlow, 1.6f, 0.7f, 0.4f, 1.5f, 0.55f, 0.5f);
                    break;
                default:
                    surface = Build("Chitin", ChitinHeight, ChitinGlow, 3.2f, 0.76f, 0.6f, 2f, 0.75f, 0.7f);
                    break;
            }

            Cache[kind] = surface;
            return surface;
        }

        /// <summary>Puts a skin on a lit material: albedo tinted by the material's colour, relief, and glow in the seams.</summary>
        public static Material Dress(Material material, Kind kind, Color glow)
        {
            Surface surface = Get(kind);
            if (surface == null)
            {
                return material;
            }

            material.SetTexture("_BaseMap", surface.Albedo);
            material.SetTexture("_BumpMap", surface.Normal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTextureScale("_BaseMap", Vector2.one * surface.Tiling);
            material.SetFloat("_Smoothness", surface.Smoothness);
            if (surface.EmissionStrength > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", surface.Emission);
                material.SetColor("_EmissionColor", glow * surface.EmissionStrength);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            return material;
        }

        // Hexagonal plates in offset rows, each a shallow dome, with dark grooves between them that glow.
        private static float ChitinHeight(float u, float v)
        {
            float plate = Plates(u, v, 6f, 0.5f, out _);
            float grain = Tileable(u, v, 14f, 3f) * 0.12f;
            return plate * 0.85f + grain;
        }

        private static float ChitinGlow(float u, float v)
        {
            Plates(u, v, 6f, 0.5f, out float groove);
            return Mathf.Pow(groove, 1.5f) * 0.9f;
        }

        // Fur: fine streaks that wander with the lie of the coat, clumped into raised tufts with a darker
        // undercoat showing between them.
        private static float HideHeight(float u, float v)
        {
            float wander = (Tileable(u, v, 3f, 21f) - 0.5f) * 0.3f;
            float streak = 0.5f + 0.5f * Mathf.Sin((u + wander) * Mathf.PI * 2f * 26f + Tileable(u, v, 7f, 9f) * 4f);
            float clump = Mathf.Pow(Tileable(u, v, 6f, 33f), 1.5f);
            return streak * 0.5f * (0.6f + 0.4f * clump) + clump * 0.5f;
        }

        private static float HideGlow(float u, float v) => 0f;

        // Flesh: large soft bulges with a web of veins between them that carry the glow.
        private static float FleshHeight(float u, float v)
        {
            float bulge = Plates(u, v, 3f, 0.8f, out _);
            return bulge * 0.7f + FleshVeins(u, v) * 0.35f + Tileable(u, v, 9f, 63f) * 0.15f;
        }

        private static float FleshVeins(float u, float v) => Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 61f) * 2f - 1f), 5f);

        private static float FleshGlow(float u, float v) => Mathf.Pow(FleshVeins(u, v), 1.4f) * 0.9f;

        // Stone: layered grain with deep cracks; the cracks are where the light comes through.
        private static float StoneHeight(float u, float v)
        {
            float grain = Tileable(u, v, 3f, 71f) * 0.55f + Tileable(u, v, 9f, 73f) * 0.3f + Tileable(u, v, 20f, 75f) * 0.15f;
            return grain - StoneCracks(u, v) * 0.7f;
        }

        private static float StoneCracks(float u, float v) => Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 77f) * 2f - 1f), 8f);

        private static float StoneGlow(float u, float v) => Mathf.Clamp01(StoneCracks(u, v) * 1.5f) * 0.8f;

        // Bark: wandering vertical ridges with knots; the grooves between the ridges glow faintly.
        private static float BarkRidges(float u, float v)
        {
            float wander = (Tileable(u, v, 2f, 81f) - 0.5f) * 0.4f;
            return 0.5f + 0.5f * Mathf.Sin((u + wander) * Mathf.PI * 2f * 9f);
        }

        private static float BarkHeight(float u, float v) => Mathf.Pow(BarkRidges(u, v), 1.4f) * 0.7f + Tileable(u, v, 5f, 83f) * 0.3f;

        private static float BarkGlow(float u, float v) => Mathf.Pow(1f - BarkRidges(u, v), 6f) * 0.6f;

        // Wet, bulging cells, each catching the light on top.
        private static float SlimeHeight(float u, float v)
        {
            float dome = Plates(u, v, 4f, 0.72f, out _);
            float small = Plates(u + 0.37f, v + 0.11f, 9f, 0.6f, out _) * 0.3f;
            return Mathf.Max(dome, small) * 0.85f + Tileable(u, v, 10f, 17f) * 0.15f;
        }

        private static float SlimeGlow(float u, float v)
        {
            float dome = Plates(u, v, 4f, 0.72f, out _);
            return Mathf.Pow(dome, 4f) * 0.6f;
        }

        // Overlapping scales: rows of arcs, each scale raised at its free edge.
        private static float ScaleHeight(float u, float v)
        {
            const float Rows = 7f;
            float row = v * Rows;
            int r = Mathf.FloorToInt(row);
            float offset = r % 2 == 0 ? 0f : 0.5f;
            float column = u * Rows + offset;
            float cx = Mathf.Floor(column) + 0.5f;
            float dx = (column - cx) / 0.5f;
            float dy = row - r;
            float arc = Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx));
            float scale = dy < arc * 0.9f ? 0.35f + dy * 0.65f : 0f;
            float lower = ScaleBelow(u, v, Rows);
            return Mathf.Max(scale, lower) * 0.9f + Tileable(u, v, 12f, 27f) * 0.1f;
        }

        // The row below shows through where the row above leaves gaps.
        private static float ScaleBelow(float u, float v, float rows)
        {
            float row = v * rows - 1f;
            int r = Mathf.FloorToInt(row);
            float offset = r % 2 == 0 ? 0f : 0.5f;
            float column = u * rows + offset;
            float cx = Mathf.Floor(column) + 0.5f;
            float dx = (column - cx) / 0.5f;
            float dy = row - r;
            float arc = Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx));
            return dy < arc ? 0.2f + dy * 0.4f : 0f;
        }

        private static float ScaleGlow(float u, float v)
        {
            float h = ScaleHeight(u, v);
            float above = ScaleHeight(u, v + 1f / Size * 3f);
            return Mathf.Clamp01((h - above) * 4f) * 0.8f;
        }

        // Plasma: bright fractal noise with brighter threads.
        private static float EnergyHeight(float u, float v)
        {
            float cloud = Tileable(u, v, 3f, 41f) * 0.6f + Tileable(u, v, 7f, 43f) * 0.4f;
            float thread = Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 5f, 47f) * 2f - 1f), 4f);
            return cloud * 0.7f + thread * 0.5f;
        }

        private static float EnergyGlow(float u, float v) => Mathf.Clamp01(EnergyHeight(u, v) * 1.2f);

        // Thin skin stretched over bones: faint veins in a web.
        private static float MembraneHeight(float u, float v)
        {
            float web = Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 51f) * 2f - 1f), 3f);
            float web2 = Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 8f, 53f) * 2f - 1f), 3f);
            return 0.4f + web * 0.4f + web2 * 0.2f;
        }

        private static float MembraneGlow(float u, float v) => Mathf.Pow(1f - Mathf.Abs(Tileable(u, v, 4f, 51f) * 2f - 1f), 6f) * 0.7f;

        // A padded suit: quilted cells that bulge between stitched seams, with a fine fabric weave on top; every
        // fourth seam is a panel line that carries light.
        // Quilting: a few big padded cells per tile (small cells vanish on a limb), fine thread, and one lit seam
        // in each direction.
        private static float WeaveHeight(float u, float v)
        {
            float cell = Mathf.Sin(u * Mathf.PI * 5f) * Mathf.Sin(v * Mathf.PI * 5f);
            float quilt = Mathf.Pow(Mathf.Abs(cell), 0.6f);
            float thread = (Mathf.Sin(u * Mathf.PI * 2f * 60f) + Mathf.Sin(v * Mathf.PI * 2f * 60f)) * 0.03f;
            return quilt * 0.75f + thread + Tileable(u, v, 5f, 111f) * 0.15f;
        }

        private static float WeaveGlow(float u, float v)
        {
            float seamU = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)), 12f);
            float seamV = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(v * Mathf.PI * 2f)), 12f);
            return Mathf.Clamp01(seamU + seamV) * 0.8f;
        }

        // Armour: panels in offset rows, each with a bevelled edge, scuffed across the face. The glow mask keeps a
        // floor everywhere so the plates stay self-lit in the dark, and rises in the seams.
        private static float PlateHeight(float u, float v)
        {
            float panel = Panels(u, v, 3f, 4f, out _);
            return panel * 0.8f + Tileable(u, v, 12f, 131f) * 0.12f + Tileable(u, v, 40f, 133f) * 0.06f;
        }

        private static float PlateGlow(float u, float v)
        {
            Panels(u, v, 3f, 4f, out float seam);
            return 0.25f + seam * 0.6f;
        }

        // Brushed metal: fine streaks along one axis, a wandering patina, and a hex lattice of small rivets.
        private static float AlloyHeight(float u, float v)
        {
            float streak = Mathf.Sin(v * Mathf.PI * 2f * 90f + Tileable(u, v, 8f, 141f) * 6f) * 0.04f;
            float patina = Tileable(u, v, 6f, 143f) * 0.25f + Tileable(u, v, 24f, 145f) * 0.1f;
            float rivet = Plates(u, v, 5f, 0.16f, out _);
            return 0.4f + streak + patina + rivet * 0.35f;
        }

        private static float AlloyGlow(float u, float v) => 0f;

        // Rectangular panels in rows that alternate by half a panel; the bevel runs up from the seam over the
        // outer part of each panel, and the seam is the narrow groove between them.
        private static float Panels(float u, float v, float columns, float rows, out float seam)
        {
            float row = Mathf.Floor(v * rows);
            float offset = Mathf.Repeat(row, 2f) < 0.5f ? 0f : 0.5f;
            float fu = Mathf.Repeat(u * columns + offset, 1f);
            float fv = Mathf.Repeat(v * rows, 1f);
            float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            seam = 1f - Mathf.Clamp01(edge / 0.03f);
            return Mathf.Clamp01(edge / 0.1f);
        }

        // Rows of domes on a hexagonal lattice; the groove is how far a point lies from the nearest dome.
        private static float Plates(float u, float v, float count, float radius, out float groove)
        {
            float best = 0f;
            float rows = count;
            for (int dr = -1; dr <= 1; dr++)
            {
                float row = Mathf.Floor(v * rows) + dr;
                float offset = Mathf.Abs(Mathf.Repeat(row, 2f)) < 0.5f ? 0f : 0.5f;
                float cy = (row + 0.5f) / rows;
                for (int dc = -1; dc <= 1; dc++)
                {
                    float column = Mathf.Floor(u * count - offset) + dc;
                    float cx = (column + 0.5f + offset) / count;
                    float dx = Wrap(u - cx) * count;
                    float dy = Wrap(v - cy) * rows;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (d < 1f)
                    {
                        best = Mathf.Max(best, Mathf.Sqrt(1f - d * d));
                    }
                }
            }

            groove = 1f - Mathf.Clamp01(best * 1.3f);
            return best;
        }

        private static float Wrap(float d) => d - Mathf.Round(d);

        private static Surface Build(string name, System.Func<float, float, float> height, System.Func<float, float, float> glow, float bump, float brightness, float contrast, float tiling, float smoothness, float emission)
        {
            var heights = new float[Size * Size];
            var glows = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    heights[y * Size + x] = height(u, v);
                    glows[y * Size + x] = glow(u, v);
                }
            }

            var albedo = new Color32[Size * Size];
            var normal = new Color32[Size * Size];
            var mask = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float value = Mathf.Clamp01(brightness + (heights[i] - 0.5f) * contrast);
                    albedo[i] = new Color(value, value, value, 1f);
                    float g = Mathf.Clamp01(glows[i]);
                    mask[i] = new Color(g, g, g, 1f);

                    float dx = heights[y * Size + (x + 1) % Size] - heights[y * Size + (x + Size - 1) % Size];
                    float dy = heights[((y + 1) % Size) * Size + x] - heights[((y + Size - 1) % Size) * Size + x];
                    var n = new Vector3(-dx * bump, -dy * bump, 1f).normalized;

                    // URP unpacks runtime normal maps from the alpha and green channels, so x goes in alpha.
                    normal[i] = new Color(1f, n.y * 0.5f + 0.5f, 1f, n.x * 0.5f + 0.5f);
                }
            }

            return new Surface
            {
                Albedo = Finish(albedo, name + "Albedo", false),
                Normal = Finish(normal, name + "Normal", true),
                Emission = Finish(mask, name + "Glow", false),
                Tiling = tiling,
                Smoothness = smoothness,
                EmissionStrength = emission,
            };
        }

        // Perlin noise blended across its own edges so it wraps without a seam.
        private static float Tileable(float u, float v, float scale, float seed)
        {
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);
            float a = Mathf.PerlinNoise(u * scale + seed, v * scale + seed);
            float b = Mathf.PerlinNoise((u + 1f) * scale + seed, v * scale + seed);
            float c = Mathf.PerlinNoise(u * scale + seed, (v + 1f) * scale + seed);
            float d = Mathf.PerlinNoise((u + 1f) * scale + seed, (v + 1f) * scale + seed);
            float top = Mathf.Lerp(a, b, 1f - u);
            float bottom = Mathf.Lerp(c, d, 1f - u);
            return Mathf.Lerp(top, bottom, 1f - v);
        }

        private static Texture2D Finish(Color32[] pixels, string name, bool linear)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
