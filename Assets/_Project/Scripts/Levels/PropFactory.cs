using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace Game.Levels
{
    /// <summary>
    /// The alien decoration of a landscape, made from code: rocks, glowing crystals, fungus, trees, tendrils, broken
    /// columns, spikes, egg sacs and fronds. A few variants of every kind are built once per level and shared by
    /// all the places that use them.
    /// </summary>
    public static class PropFactory
    {
        public const int Variants = 4;

        private const int Body = 0;
        private const int GlowA = 1;
        private const int GlowB = 2;
        private const int Dark = 3;
        private const float GlowEmission = 1.05f;
        private const float RockSmoothness = 0.12f;
        private const float GlowSmoothness = 0.55f;
        private const float SecondGlowHueShift = 0.33f;

        /// <summary>The four materials every prop mesh uses, in submesh order: body, glow, second glow, dark.</summary>
        public static Material[] CreateMaterials(VisualTheme theme)
        {
            Color.RGBToHSV(theme.AccentColor, out float h, out float s, out float v);
            Color second = Color.HSVToRGB(Mathf.Repeat(h + SecondGlowHueShift, 1f), s, v);
            return new[]
            {
                Lit(theme.RockColor, 0f, RockSmoothness),
                Lit(theme.AccentColor, GlowEmission, GlowSmoothness),
                Lit(second, GlowEmission, GlowSmoothness),
                Lit(theme.RockColor * 0.45f, 0f, RockSmoothness),
            };
        }

        public static Mesh Create(PropKind kind, int variant)
        {
            var rng = new Random((int)kind * 7919 + variant * 104729 + 17);
            var builder = new PropMeshBuilder();
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

        // Half buried, lumpy and faceted; a few smaller stones lean against the big one.
        private static void BuildRock(PropMeshBuilder b, Random rng, float size)
        {
            var radii = new Vector3(Range(rng, 0.3f, 0.45f), Range(rng, 0.22f, 0.36f), Range(rng, 0.3f, 0.45f)) * size;
            b.AddBlob(new Vector3(0f, radii.y * 0.45f, 0f), radii, 5, 8, 0.28f, rng.Next(100), Body, true);

            int extra = rng.Next(1, 3);
            for (int i = 0; i < extra; i++)
            {
                Vector3 offset = Ring(rng, radii.x * 1.1f);
                var small = radii * Range(rng, 0.35f, 0.55f);
                b.AddBlob(offset + Vector3.up * small.y * 0.4f, small, 4, 6, 0.3f, rng.Next(100), size > 1f ? Dark : Body, true);
            }
        }

        // A cluster of faceted prisms that glow, rising out of a little pile of rock.
        private static void BuildCrystals(PropMeshBuilder b, Random rng)
        {
            b.AddBlob(new Vector3(0f, 0.03f, 0f), new Vector3(0.3f, 0.12f, 0.3f), 4, 7, 0.2f, rng.Next(100), Dark, true);
            int count = rng.Next(3, 7);
            for (int i = 0; i < count; i++)
            {
                Vector3 baseCenter = Ring(rng, 0.2f);
                Vector3 axis = Tilted(rng, 28f);
                float length = Range(rng, 0.35f, 1f);
                float radius = Range(rng, 0.05f, 0.11f);
                var path = new[] { baseCenter, baseCenter + axis * length * 0.7f, baseCenter + axis * length };
                var radii = new[] { radius, radius * 0.8f, 0f };
                b.AddTube(path, radii, 6, i % 2 == 0 ? GlowA : GlowB, true);
            }
        }

        // Fungus: a bent stalk, a glowing cap with a dark underside and bright spots.
        private static void BuildMushrooms(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = i == 0 ? Vector3.zero : Ring(rng, 0.3f);
                float height = Range(rng, 0.45f, 1f) * (i == 0 ? 1f : 0.6f);
                Vector3 lean = new Vector3(Range(rng, -0.12f, 0.12f), 0f, Range(rng, -0.12f, 0.12f));
                var path = new[] { root, root + lean * 0.4f + Vector3.up * height * 0.5f, root + lean + Vector3.up * height };
                b.AddTube(path, new[] { 0.075f, 0.05f, 0.045f }, 7, Body, false);

                float capRadius = Range(rng, 0.2f, 0.42f) * (i == 0 ? 1f : 0.7f);
                Vector3 top = path[2];
                int material = rng.Next(2) == 0 ? GlowA : GlowB;
                b.AddBlob(top + Vector3.up * capRadius * 0.05f, new Vector3(capRadius, capRadius * 0.55f, capRadius), 5, 10, 0.06f, rng.Next(100), material, false, Mathf.PI * 0.52f);
                b.AddBlob(top, new Vector3(capRadius * 0.92f, capRadius * 0.2f, capRadius * 0.92f), 3, 10, 0f, 0, Dark, false);

                int spots = rng.Next(2, 5);
                for (int s = 0; s < spots; s++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f);
                    float ring = capRadius * Range(rng, 0.2f, 0.65f);
                    var spot = top + new Vector3(Mathf.Cos(angle) * ring, capRadius * 0.5f * (1f - ring / capRadius * 0.6f), Mathf.Sin(angle) * ring);
                    b.AddBlob(spot, Vector3.one * capRadius * 0.12f, 3, 6, 0f, 0, material == GlowA ? GlowB : GlowA, false);
                }
            }
        }

        // A twisted trunk with branches that end in broad glowing leaves and hanging orbs.
        private static void BuildTree(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 1f, 1.5f);
            float sway = Range(rng, -0.2f, 0.2f);
            var trunk = new[]
            {
                Vector3.zero,
                new Vector3(sway * 0.3f, height * 0.35f, 0f),
                new Vector3(-sway * 0.2f, height * 0.7f, sway * 0.2f),
                new Vector3(sway * 0.5f, height, 0f),
            };
            b.AddTube(trunk, new[] { 0.12f, 0.09f, 0.065f, 0.04f }, 7, Dark, false);

            int branches = rng.Next(3, 5);
            for (int i = 0; i < branches; i++)
            {
                float angle = i / (float)branches * Mathf.PI * 2f + Range(rng, -0.4f, 0.4f);
                Vector3 start = Vector3.Lerp(trunk[2], trunk[3], Range(rng, 0f, 1f));
                Vector3 direction = new Vector3(Mathf.Cos(angle), Range(rng, 0.2f, 0.7f), Mathf.Sin(angle)).normalized;
                float length = Range(rng, 0.35f, 0.6f);
                Vector3 end = start + direction * length;
                b.AddTube(new[] { start, Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.06f, end }, new[] { 0.04f, 0.028f, 0.012f }, 5, Dark, false);

                int leaf = i % 2 == 0 ? GlowA : GlowB;
                float leafRadius = Range(rng, 0.2f, 0.32f);
                b.AddBlob(end, new Vector3(leafRadius, leafRadius * 0.28f, leafRadius), 4, 9, 0.12f, rng.Next(100), leaf, false);
                b.AddBlob(end + new Vector3(0f, -0.15f, 0f), Vector3.one * 0.045f, 3, 6, 0f, 0, leaf == GlowA ? GlowB : GlowA, false);
            }
        }

        // Fleshy stalks curling out of the ground, each ending in a small glowing bulb.
        private static void BuildTendrils(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(3, 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 root = Ring(rng, 0.18f);
                float angle = Range(rng, 0f, Mathf.PI * 2f);
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float height = Range(rng, 0.5f, 1.1f);
                var path = new Vector3[5];
                var radii = new float[5];
                for (int p = 0; p < 5; p++)
                {
                    float t = p / 4f;
                    path[p] = root + outward * (t * t * height * 0.55f) + Vector3.up * (Mathf.Sin(t * Mathf.PI * 0.5f) * height);
                    radii[p] = Mathf.Lerp(0.085f, 0.015f, t);
                }

                b.AddTube(path, radii, 6, Body, false);
                b.AddBlob(path[4] + Vector3.up * 0.03f, Vector3.one * Range(rng, 0.05f, 0.09f), 4, 7, 0f, 0, i % 2 == 0 ? GlowA : GlowB, false);
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
                    b.AddBlob(spot, Vector3.one * size * 0.18f, 3, 6, 0f, 0, GlowA, false);
                }
            }
        }

        // A tall leaning rock needle with a glowing crystal crown: the big shapes that give the far edge of the map height.
        private static void BuildSpire(PropMeshBuilder b, Random rng)
        {
            float height = Range(rng, 2.2f, 3.4f);
            float radius = Range(rng, 0.45f, 0.7f);
            Vector3 lean = new Vector3(Range(rng, -0.3f, 0.3f), 0f, Range(rng, -0.3f, 0.3f));
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
                Vector3 axis = Tilted(rng, 50f);
                float length = Range(rng, 0.5f, 1f);
                float size = Range(rng, 0.07f, 0.12f);
                b.AddTube(new[] { baseCenter, baseCenter + axis * length }, new[] { size, 0f }, 5, i % 2 == 0 ? GlowA : GlowB, true);
            }

            b.AddBlob(new Vector3(0f, 0.15f, 0f), new Vector3(radius * 1.5f, 0.3f, radius * 1.5f), 4, 8, 0.25f, rng.Next(100), Dark, true);
        }

        // A tuft of thin curved blades, tipped with light.
        private static void BuildFronds(PropMeshBuilder b, Random rng)
        {
            int count = rng.Next(5, 9);
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + Range(rng, -0.3f, 0.3f);
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float length = Range(rng, 0.3f, 0.6f);
                Vector3 root = outward * 0.04f;
                var path = new[]
                {
                    root,
                    root + outward * length * 0.25f + Vector3.up * length * 0.55f,
                    root + outward * length * 0.6f + Vector3.up * length * 0.85f,
                };
                b.AddTube(path, new[] { 0.028f, 0.02f, 0f }, 3, i % 2 == 0 ? GlowA : GlowB, false);
            }
        }
    }
}
