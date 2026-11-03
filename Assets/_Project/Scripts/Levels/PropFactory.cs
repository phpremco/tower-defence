using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace Game.Levels
{
    /// <summary>
    /// The alien decoration of a landscape, made from code: rocks, glowing crystals, fungus, trees, tendrils, broken
    /// columns, spikes, egg sacs and fronds. A few variants of every kind are built once per level and shared by
    /// all the places that use them. Submesh slots: rock body, first glow, second glow, dark, leaf, bright core.
    /// </summary>
    public static class PropFactory
    {
        public const int Variants = 4;
        public const int SlotCount = 6;

        private const int Body = 0;
        private const int GlowA = 1;
        private const int GlowB = 2;
        private const int Dark = 3;
        private const int Leaf = 4;
        private const int Core = 5;
        private const float GlowEmission = 1.5f;
        private const float LeafEmission = 0.5f;
        private const float CoreEmission = 3.4f;
        private const float RockSmoothness = 0.18f;
        private const float GlowSmoothness = 0.85f;
        private const float LeafSmoothness = 0.35f;
        private const float SecondGlowHueShift = 0.33f;
        private const float VariantHueShift = 0.06f;
        private const float VariantValueShift = 0.18f;
        private const float CoreWhiteness = 0.65f;

        /// <summary>Submesh slots of the glow materials, for whoever animates them.</summary>
        public const int FirstGlowSlot = GlowA;
        public const int SecondGlowSlot = GlowB;
        public const int CoreSlot = Core;

        /// <summary>How many colour variants a level mixes, so neighbouring plants are not all the same shade.</summary>
        public const int ColourVariants = 3;

        /// <summary>
        /// The materials every prop mesh uses, in submesh order. Each colour variant shifts the plant and glow hues
        /// a little, and the rock, bark and leaf surfaces get texture and relief.
        /// </summary>
        public static Material[] CreateMaterials(VisualTheme theme, int variant = 0)
        {
            float shift = (variant - (ColourVariants - 1) * 0.5f) * VariantHueShift;
            Color accent = Shift(theme.AccentColor, shift, 1f);
            Color second = Shift(theme.AccentColor, SecondGlowHueShift + shift, 1f);
            Color leaf = Shift(theme.PlantColor, shift * 1.5f, 1f + (variant % 2 == 0 ? VariantValueShift : -VariantValueShift));
            Color core = Color.Lerp(accent, Color.white, CoreWhiteness);
            PropTextures.Surface rock = PropTextures.Rock();
            PropTextures.Surface bark = PropTextures.Bark();
            PropTextures.Surface leafSurface = PropTextures.Leaf();
            PropTextures.Surface crystal = PropTextures.Crystal();
            return new[]
            {
                Dress(Lit(theme.RockColor, 0f, RockSmoothness), rock),
                Dress(Lit(accent, GlowEmission, GlowSmoothness), crystal),
                Dress(Lit(second, GlowEmission, GlowSmoothness), crystal),
                Dress(Lit(theme.RockColor * 0.45f, 0f, RockSmoothness), bark),
                Dress(Lit(leaf, LeafEmission, LeafSmoothness), leafSurface),
                Lit(core, CoreEmission, GlowSmoothness),
            };
        }

        private static Color Shift(Color colour, float hue, float value)
        {
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            return Color.HSVToRGB(Mathf.Repeat(h + hue, 1f), s, Mathf.Clamp01(v * value));
        }

        /// <summary>Puts a surface on a lit material: its colour map tinted by the material's colour, relief, and a glow map when it has one.</summary>
        public static Material Dress(Material material, PropTextures.Surface surface)
        {
            material.SetTexture("_BaseMap", surface.Albedo);
            material.SetTexture("_BumpMap", surface.Normal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            if (surface.Emission != null && material.IsKeywordEnabled("_EMISSION"))
            {
                material.SetTexture("_EmissionMap", surface.Emission);
            }

            return material;
        }

        /// <summary>Growth that bends in the wind, as opposed to stone.</summary>
        public static bool IsPlant(PropKind kind) =>
            kind == PropKind.Frond || kind == PropKind.Mushroom || kind == PropKind.GlowTree || kind == PropKind.Tendril || kind == PropKind.Reed;

        /// <summary>Kinds whose light is strong enough to throw onto the ground around them.</summary>
        public static bool IsGlowing(PropKind kind) =>
            kind == PropKind.Crystal || kind == PropKind.GlowTree || kind == PropKind.Mushroom || kind == PropKind.Egg || kind == PropKind.Spike || kind == PropKind.IceShard;

        public static Mesh Create(PropKind kind, int variant)
        {
            var rng = new Random((int)kind * 7919 + variant * 104729 + 17);
            var builder = new PropMeshBuilder(SlotCount);
            switch (kind)
            {
                case PropKind.Rock: BuildRock(builder, rng, 1f); break;
                case PropKind.Boulder: BuildRock(builder, rng, 1.6f); break;
                case PropKind.Crystal: BuildCrystals(builder, rng); break;
                case PropKind.Mushroom: BuildMushrooms(builder, rng); break;
                case PropKind.GlowTree: BuildTree(builder, rng); break;
                case PropKind.Tendril: BuildTendrils(builder, rng); break;
                case PropKind.Column: BuildColumn(builder, rng); break;
                case PropKind.Spike: BuildSpikes(builder, rng); break;
                case PropKind.Egg: BuildEggs(builder, rng); break;
                case PropKind.Frond: BuildFronds(builder, rng); break;
                case PropKind.Spire: BuildSpire(builder, rng); break;
                case PropKind.IceShard: BuildIceShards(builder, rng); break;
                case PropKind.Snowdrift: BuildSnowdrift(builder, rng); break;
                case PropKind.DeadTree: BuildDeadTree(builder, rng); break;
                case PropKind.Reed: BuildReeds(builder, rng); break;
                case PropKind.Scree: BuildScree(builder, rng); break;
            }

            return builder.ToMesh($"{kind}_{variant}");
        }

        private static Material Lit(Color color, float emission, float smoothness)
        {
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            return material;
        }

        private static float Range(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        private static Vector3 Tilted(Random rng, float maxDegrees)
        {
            float tilt = Range(rng, 0f, maxDegrees) * Mathf.Deg2Rad;
            float around = Range(rng, 0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Sin(tilt) * Mathf.Cos(around), Mathf.Cos(tilt), Mathf.Sin(tilt) * Mathf.Sin(around));
        }

        private static Vector3 Ring(Random rng, float radius)
        {
            float angle = Range(rng, 0f, Mathf.PI * 2f);
            float distance = Range(rng, 0f, radius);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        private static Vector3 Around(float angle) => new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        // Half buried, lumpy and faceted, sitting in a dark skirt of rubble so it reads as planted in the ground;
        // a few smaller stones lean against the big one, and glowing moss creeps along its foot on some.
        // Loose broken stone spilled down a rim: a low fan of flat, angular chips around one bigger slab.
        private static void BuildScree(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.08f, 0.24f), 4, 7, 0.35f, rng.Next(100), Body, true);
            int chips = rng.Next(7, 11);
            for (int i = 0; i < chips; i++)
            {
                Vector3 offset = Ring(rng, Range(rng, 0.2f, 0.5f));
                var size = new Vector3(Range(rng, 0.05f, 0.11f), Range(rng, 0.025f, 0.05f), Range(rng, 0.05f, 0.1f));
                b.AddBlob(offset + Vector3.up * size.y * 0.5f, size, 3, 5, 0.4f, rng.Next(100), i % 3 == 0 ? Body : Dark, true);
            }
        }

        private static void BuildRock(PropMeshBuilder b, Random rng, float size)
        {
            var radii = new Vector3(Range(rng, 0.3f, 0.45f), Range(rng, 0.22f, 0.36f), Range(rng, 0.3f, 0.45f)) * size;
            b.AddBlob(new Vector3(0f, radii.y * 0.1f, 0f), new Vector3(radii.x * 1.2f, radii.y * 0.35f, radii.z * 1.2f), 3, 9, 0.25f, rng.Next(100), Dark, true);
            b.AddBlob(new Vector3(0f, radii.y * 0.5f, 0f), radii, 6, 9, 0.26f, rng.Next(100), Body, true);

            int extra = rng.Next(1, 3);
            for (int i = 0; i < extra; i++)
            {
                Vector3 offset = Ring(rng, radii.x * 1.1f);
                var small = radii * Range(rng, 0.35f, 0.55f);
                b.AddBlob(offset + Vector3.up * small.y * 0.4f, small, 4, 7, 0.3f, rng.Next(100), size > 1f ? Dark : Body, true);
            }

            if (rng.Next(2) == 0)
            {
                int moss = rng.Next(2, 5);
                for (int i = 0; i < moss; i++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f);
                    var spot = new Vector3(Mathf.Cos(angle) * radii.x * 0.95f, radii.y * 0.18f, Mathf.Sin(angle) * radii.z * 0.95f);
                    b.AddBlob(spot, new Vector3(0.06f, 0.025f, 0.06f) * size, 3, 6, 0.3f, rng.Next(100), i % 2 == 0 ? GlowA : Leaf, false);
                }
            }
        }

        // One sharp hexagonal prism: a glowing shaft that ends in a bright pointed tip.
        private static void Prism(PropMeshBuilder b, Vector3 root, Vector3 axis, float length, float radius, int glow)
        {
            Vector3 shoulder = root + axis * (length * 0.72f);
            b.AddTube(new[] { root, root + axis * (length * 0.3f), shoulder }, new[] { radius * 0.8f, radius, radius }, 6, glow, true);
            b.AddTube(new[] { shoulder, root + axis * length }, new[] { radius, 0f }, 6, Core, true);
        }

        // A cluster of faceted prisms, the tallest in the middle, with shards around the foot and a pool of light
        // on the ground beneath them.
        private static void BuildCrystals(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.045f, 0f), new Vector3(0.36f, 0.035f, 0.36f), 3, 9, 0.25f, rng.Next(100), GlowA, false);
            b.AddBlob(new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.13f, 0.3f), 4, 8, 0.2f, rng.Next(100), Dark, true);
            b.AddBlob(new Vector3(0.1f, 0.06f, -0.08f), new Vector3(0.2f, 0.1f, 0.2f), 4, 7, 0.25f, rng.Next(100), Body, true);

            Prism(b, Ring(rng, 0.05f), Tilted(rng, 10f), Range(rng, 0.9f, 1.3f), Range(rng, 0.11f, 0.15f), GlowA);
            int count = rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + Range(rng, -0.4f, 0.4f);
                Vector3 root = Around(angle) * Range(rng, 0.12f, 0.22f);
                Vector3 axis = (Around(angle) * Range(rng, 0.15f, 0.5f) + Vector3.up).normalized;
                Prism(b, root, axis, Range(rng, 0.45f, 0.85f), Range(rng, 0.06f, 0.1f), i % 2 == 0 ? GlowB : GlowA);
            }

            int shards = rng.Next(4, 8);
            for (int i = 0; i < shards; i++)
            {
                Vector3 root = Ring(rng, 0.36f);
                Vector3 axis = Tilted(rng, 60f);
                float length = Range(rng, 0.1f, 0.26f);
                b.AddTube(new[] { root, root + axis * length }, new[] { Range(rng, 0.02f, 0.04f), 0f }, 5, i % 2 == 0 ? GlowB : Core, true);
            }
        }

        // Fungus: a ribbed stalk, a domed cap with a rim of light, a dark gilled underside and bright spots.
        private static void BuildMushrooms(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = i == 0 ? Vector3.zero : Ring(rng, 0.3f);
                float height = Range(rng, 0.5f, 1.05f) * (i == 0 ? 1f : 0.6f);
                var lean = new Vector3(Range(rng, -0.12f, 0.12f), 0f, Range(rng, -0.12f, 0.12f));
                var path = new[] { root, root + lean * 0.4f + Vector3.up * height * 0.5f, root + lean + Vector3.up * height };
                b.AddTube(path, new[] { 0.085f, 0.055f, 0.05f }, 8, Leaf, false);
                b.AddBlob(root + Vector3.up * 0.03f, new Vector3(0.12f, 0.04f, 0.12f), 3, 8, 0.2f, rng.Next(100), Dark, false);

                float capRadius = Range(rng, 0.22f, 0.44f) * (i == 0 ? 1f : 0.7f);
                Vector3 top = path[2];
                int material = rng.Next(2) == 0 ? GlowA : GlowB;
                b.AddBlob(top + Vector3.up * capRadius * 0.05f, new Vector3(capRadius, capRadius * 0.6f, capRadius), 6, 14, 0.07f, rng.Next(100), material, false, Mathf.PI * 0.52f);
                b.AddBlob(top, new Vector3(capRadius * 0.92f, capRadius * 0.2f, capRadius * 0.92f), 3, 12, 0f, 0, Dark, false);
                b.AddTube(new[] { top + Vector3.up * 0.01f, top + Vector3.up * 0.04f }, new[] { capRadius * 0.98f, capRadius * 0.94f }, 14, Core, false);

                int spots = rng.Next(3, 6);
                for (int s = 0; s < spots; s++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f);
                    float ring = capRadius * Range(rng, 0.15f, 0.65f);
                    var spot = top + new Vector3(Mathf.Cos(angle) * ring, capRadius * 0.58f * (1f - ring / capRadius * 0.6f), Mathf.Sin(angle) * ring);
                    b.AddBlob(spot, Vector3.one * capRadius * 0.11f, 3, 6, 0f, 0, Core, false);
                }
            }
        }

        // A twisted trunk on spreading roots, a canopy of layered leaf clumps, and glowing fruit hanging beneath.
        private static void BuildTree(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 1.1f, 1.6f);
            float sway = Range(rng, -0.2f, 0.2f);
            var trunk = new[]
            {
                Vector3.zero,
                new Vector3(sway * 0.3f, height * 0.35f, 0f),
                new Vector3(-sway * 0.2f, height * 0.7f, sway * 0.2f),
                new Vector3(sway * 0.5f, height, 0f),
            };
            b.AddTube(trunk, new[] { 0.14f, 0.1f, 0.07f, 0.045f }, 8, Dark, false);
            int roots = rng.Next(3, 5);
            for (int i = 0; i < roots; i++)
            {
                Vector3 outward = Around(i / (float)roots * Mathf.PI * 2f + Range(rng, -0.3f, 0.3f));
                b.AddTube(new[] { Vector3.up * 0.18f, outward * 0.12f + Vector3.up * 0.08f, outward * Range(rng, 0.25f, 0.4f) }, new[] { 0.07f, 0.05f, 0f }, 5, Dark, false);
            }

            int clumps = rng.Next(4, 7);
            for (int i = 0; i < clumps; i++)
            {
                float angle = i / (float)clumps * Mathf.PI * 2f + Range(rng, -0.4f, 0.4f);
                float reach = Range(rng, 0.25f, 0.5f);
                Vector3 centre = trunk[3] + Around(angle) * reach + Vector3.up * Range(rng, -0.1f, 0.2f);
                b.AddTube(new[] { Vector3.Lerp(trunk[2], trunk[3], 0.6f), centre }, new[] { 0.04f, 0.015f }, 5, Dark, false);
                float radius = Range(rng, 0.26f, 0.4f);
                b.AddBlob(centre, new Vector3(radius, radius * 0.5f, radius), 4, 9, 0.18f, rng.Next(100), Leaf, false);
                b.AddBlob(centre + Vector3.up * radius * 0.25f, new Vector3(radius * 0.55f, radius * 0.3f, radius * 0.55f), 3, 7, 0.15f, rng.Next(100), i % 2 == 0 ? GlowA : GlowB, false);

                Vector3 fruit = centre + Vector3.up * (-radius * 0.45f) + Ring(rng, radius * 0.4f);
                b.AddTube(new[] { centre, fruit }, new[] { 0.012f, 0.012f }, 3, Dark, false);
                b.AddBlob(fruit, Vector3.one * Range(rng, 0.045f, 0.07f), 4, 7, 0f, 0, Core, false);
            }

            b.AddBlob(trunk[3] + Vector3.up * 0.15f, new Vector3(0.42f, 0.22f, 0.42f), 4, 9, 0.2f, rng.Next(100), Leaf, false);
        }

        // Fleshy stalks curling out of the ground, each ending in a bright bulb.
        private static void BuildTendrils(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(3, 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = Ring(rng, 0.18f);
                Vector3 outward = Around(Range(rng, 0f, Mathf.PI * 2f));
                float height = Range(rng, 0.5f, 1.1f);
                var path = new Vector3[5];
                var radii = new float[5];
                for (int p = 0; p < 5; p++)
                {
                    float t = p / 4f;
                    path[p] = root + outward * (t * t * height * 0.55f) + Vector3.up * (Mathf.Sin(t * Mathf.PI * 0.5f) * height);
                    radii[p] = Mathf.Lerp(0.085f, 0.015f, t);
                }

                b.AddTube(path, radii, 6, Leaf, false);
                b.AddBlob(path[4] + Vector3.up * 0.03f, Vector3.one * Range(rng, 0.05f, 0.09f), 4, 7, 0f, 0, i % 2 == 0 ? Core : GlowB, false);
            }
        }

        // A weathered pillar with a broken top, glowing inlay bands and rubble at its foot.
        private static void BuildColumn(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 0.5f, 1.2f);
            float radius = Range(rng, 0.16f, 0.22f);
            var shaft = new[] { Vector3.zero, Vector3.up * height * 0.5f, new Vector3(Range(rng, -0.04f, 0.04f), height, 0f) };
            b.AddTube(shaft, new[] { radius * 1.1f, radius, radius * 0.92f }, 8, Body, true);

            for (int i = 0; i < 2; i++)
            {
                float y = height * (0.25f + i * 0.35f);
                b.AddTube(new[] { Vector3.up * y, Vector3.up * (y + 0.05f) }, new[] { radius * 1.04f, radius * 1.04f }, 8, GlowA, true);
            }

            int rubble = rng.Next(2, 5);
            for (int i = 0; i < rubble; i++)
            {
                var piece = new Vector3(Range(rng, 0.06f, 0.14f), Range(rng, 0.05f, 0.1f), Range(rng, 0.06f, 0.14f));
                b.AddBlob(Ring(rng, 0.4f) + Vector3.up * piece.y * 0.4f, piece, 4, 6, 0.3f, rng.Next(100), Dark, true);
            }
        }

        // Sharp dark cones, like basalt teeth, with a hot glow at their feet.
        private static void BuildSpikes(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(4, 8);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = Ring(rng, 0.25f);
                Vector3 axis = Tilted(rng, 25f);
                float length = Range(rng, 0.35f, 0.95f);
                float radius = Range(rng, 0.06f, 0.12f);
                var path = new[] { root, root + axis * length * 0.55f, root + axis * length };
                b.AddTube(path, new[] { radius, radius * 0.6f, 0f }, 5, Dark, true);
            }

            b.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.28f, 0.05f, 0.28f), 3, 8, 0.2f, rng.Next(100), GlowA, false);
            b.AddBlob(new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.03f, 0.12f), 3, 8, 0.2f, rng.Next(100), Core, false);
        }

        // Pale egg sacs in a nest of dark goo, with glowing veins showing through.
        private static void BuildEggs(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.38f, 0.07f, 0.38f), 3, 9, 0.2f, rng.Next(100), Dark, false);
            int count = rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                Vector3 centre = Ring(rng, 0.22f);
                float size = Range(rng, 0.13f, 0.22f);
                var radii = new Vector3(size, size * 1.35f, size);
                b.AddBlob(centre + Vector3.up * radii.y * 0.8f, radii, 6, 9, 0.04f, rng.Next(100), Body, false);

                for (int s = 0; s < 3; s++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f);
                    var spot = centre + new Vector3(Mathf.Cos(angle) * size * 0.85f, radii.y * Range(rng, 0.8f, 1.6f), Mathf.Sin(angle) * size * 0.85f);
                    b.AddBlob(spot, Vector3.one * size * 0.18f, 3, 6, 0f, 0, s == 0 ? Core : GlowA, false);
                }
            }
        }

        // A tall leaning rock needle with a glowing crystal crown: the big shapes that give the far edge of the map height.
        private static void BuildSpire(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 2.2f, 3.4f);
            float radius = Range(rng, 0.45f, 0.7f);
            var lean = new Vector3(Range(rng, -0.3f, 0.3f), 0f, Range(rng, -0.3f, 0.3f));
            var path = new[]
            {
                Vector3.zero,
                lean * 0.3f + Vector3.up * height * 0.35f,
                lean * 0.7f + Vector3.up * height * 0.7f,
                lean + Vector3.up * height,
            };
            b.AddTube(path, new[] { radius, radius * 0.7f, radius * 0.42f, 0f }, 6, Body, true);

            int crystals = rng.Next(2, 5);
            for (int i = 0; i < crystals; i++)
            {
                Vector3 baseCenter = lean * Range(rng, 0.5f, 0.9f) + Vector3.up * height * Range(rng, 0.45f, 0.8f) + Ring(rng, radius * 0.5f);
                Prism(b, baseCenter, Tilted(rng, 50f), Range(rng, 0.5f, 1f), Range(rng, 0.07f, 0.12f), i % 2 == 0 ? GlowA : GlowB);
            }

            b.AddBlob(new Vector3(0f, 0.15f, 0f), new Vector3(radius * 1.5f, 0.3f, radius * 1.5f), 4, 8, 0.25f, rng.Next(100), Dark, true);
        }

        // A tuft of broad curling leaves around a glowing bud, every other leaf tipped with light.
        // Ice: tall slim prisms leaning out of a crust of rime, a couple snapped short, lit from inside.
        private static void BuildIceShards(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.03f, 0f), new Vector3(0.34f, 0.05f, 0.34f), 3, 9, 0.2f, rng.Next(100), Body, false);
            Prism(b, Ring(rng, 0.04f), Tilted(rng, 8f), Range(rng, 1.1f, 1.6f), Range(rng, 0.07f, 0.1f), GlowA);
            int count = rng.Next(3, 6);
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + Range(rng, -0.4f, 0.4f);
                Vector3 root = Around(angle) * Range(rng, 0.1f, 0.24f);
                Vector3 axis = (Around(angle) * Range(rng, 0.2f, 0.6f) + Vector3.up).normalized;
                bool snapped = rng.Next(3) == 0;
                Prism(b, root, axis, snapped ? Range(rng, 0.15f, 0.3f) : Range(rng, 0.5f, 1f), Range(rng, 0.04f, 0.07f), i % 2 == 0 ? GlowB : GlowA);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 root = Ring(rng, 0.3f);
                b.AddTube(new[] { root, root + Tilted(rng, 70f) * Range(rng, 0.06f, 0.14f) }, new[] { 0.02f, 0f }, 4, Core, true);
            }
        }

        // Snow: two or three soft overlapping mounds, their lee side in shadow, with a hard crust of frost on top.
        private static void BuildSnowdrift(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(2, 4);
            for (int i = 0; i < count; i++)
            {
                Vector3 centre = i == 0 ? Vector3.zero : Ring(rng, 0.28f);
                float radius = Range(rng, 0.28f, 0.46f) * (i == 0 ? 1f : 0.7f);
                float height = radius * Range(rng, 0.35f, 0.5f);
                b.AddBlob(centre + Vector3.up * height * 0.3f, new Vector3(radius, height, radius * Range(rng, 0.8f, 1.2f)), 5, 11, 0.05f, rng.Next(100), Body, false);
                b.AddBlob(centre + new Vector3(0f, height * 0.75f, 0f), new Vector3(radius * 0.6f, height * 0.25f, radius * 0.6f), 3, 8, 0.1f, rng.Next(100), GlowA, false, Mathf.PI * 0.5f);
            }

            b.AddBlob(new Vector3(0f, 0.015f, 0f), new Vector3(0.5f, 0.02f, 0.5f), 2, 10, 0.15f, rng.Next(100), Dark, false);
        }

        // A dead marsh tree: a bare twisted trunk that forks into crooked branches, moss hanging from a few of them.
        private static void BuildDeadTree(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 1.2f, 1.8f);
            float lean = Range(rng, -0.25f, 0.25f);
            var trunk = new[]
            {
                Vector3.zero,
                new Vector3(lean * 0.4f, height * 0.4f, 0f),
                new Vector3(lean, height * 0.75f, lean * 0.3f),
                new Vector3(lean * 1.2f, height, lean * 0.1f),
            };
            b.AddTube(trunk, new[] { 0.13f, 0.09f, 0.06f, 0.025f }, 7, Dark, false);
            int roots = rng.Next(3, 5);
            for (int i = 0; i < roots; i++)
            {
                Vector3 outward = Around(i / (float)roots * Mathf.PI * 2f + Range(rng, -0.3f, 0.3f));
                b.AddTube(new[] { Vector3.up * 0.12f, outward * 0.14f + Vector3.up * 0.05f, outward * Range(rng, 0.25f, 0.4f) }, new[] { 0.06f, 0.04f, 0f }, 5, Dark, false);
            }

            int branches = rng.Next(3, 6);
            for (int i = 0; i < branches; i++)
            {
                float t = Range(rng, 0.45f, 0.95f);
                Vector3 from = Vector3.Lerp(trunk[1], trunk[3], t);
                Vector3 outward = Around(i / (float)branches * Mathf.PI * 2f + Range(rng, -0.5f, 0.5f));
                float length = Range(rng, 0.35f, 0.7f);
                Vector3 elbow = from + outward * length * 0.5f + Vector3.up * length * Range(rng, 0.2f, 0.5f);
                Vector3 tip = elbow + outward * length * 0.5f + Vector3.up * length * Range(rng, -0.2f, 0.3f);
                b.AddTube(new[] { from, elbow, tip }, new[] { 0.045f, 0.028f, 0f }, 5, Dark, false);
                b.AddTube(new[] { elbow, elbow + Vector3.up * length * 0.3f + outward * length * 0.1f }, new[] { 0.02f, 0f }, 4, Dark, false);
                if (i % 2 == 0)
                {
                    // Hanging moss, a little lit, so the tree reads as marsh and not as desert.
                    b.AddBlob(elbow + Vector3.down * 0.1f, new Vector3(0.05f, 0.14f, 0.05f), 3, 6, 0.1f, rng.Next(100), Leaf, false);
                }
            }
        }

        // Reeds: a tight clump of tall thin stalks leaning every which way, cattail heads on the longest.
        private static void BuildReeds(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.22f, 0.04f, 0.22f), 3, 8, 0.15f, rng.Next(100), Dark, false);
            int count = rng.Next(9, 15);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = Ring(rng, 0.14f);
                float height = Range(rng, 0.6f, 1.15f);
                Vector3 lean = new Vector3(Range(rng, -0.2f, 0.2f), 0f, Range(rng, -0.2f, 0.2f));
                Vector3 top = root + lean * height + Vector3.up * height;
                b.AddTube(new[] { root, root + lean * height * 0.4f + Vector3.up * height * 0.5f, top }, new[] { 0.018f, 0.014f, 0.006f }, 4, Leaf, false);
                if (height > 0.95f && i % 3 == 0)
                {
                    b.AddBlob(top - Vector3.up * 0.06f, new Vector3(0.028f, 0.09f, 0.028f), 4, 6, 0.02f, rng.Next(100), Dark, false);
                }
                else if (i % 4 == 1)
                {
                    b.AddBlob(top, Vector3.one * 0.016f, 3, 5, 0f, 0, Core, false);
                }
            }
        }

        private static void BuildFronds(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(5, 8);
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + Range(rng, -0.3f, 0.3f);
                Vector3 outward = Around(angle);
                Vector3 side = Vector3.Cross(Vector3.up, outward);
                float length = Range(rng, 0.35f, 0.65f);
                float droop = Range(rng, 0.1f, 0.4f);
                Vector3 root = outward * 0.03f;
                var spine = new[]
                {
                    root,
                    root + outward * length * 0.2f + Vector3.up * length * 0.5f,
                    root + outward * length * 0.5f + Vector3.up * length * 0.8f,
                    root + outward * length * 0.85f + Vector3.up * length * (0.85f - droop),
                    root + outward * length * 1.05f + Vector3.up * length * (0.7f - droop),
                };
                float width = Range(rng, 0.14f, 0.24f);
                b.AddBlade(spine, new[] { width * 0.35f, width * 0.9f, width, width * 0.6f, 0f }, side, Leaf);
                if (i % 2 == 0)
                {
                    b.AddBlob(spine[4], Vector3.one * 0.03f, 3, 6, 0f, 0, Core, false);
                }
            }

            b.AddBlob(new Vector3(0f, 0.12f, 0f), new Vector3(0.07f, 0.1f, 0.07f), 4, 7, 0f, 0, GlowA, false);
            b.AddBlob(new Vector3(0f, 0.2f, 0f), Vector3.one * 0.035f, 3, 6, 0f, 0, Core, false);
        }
    }
}
