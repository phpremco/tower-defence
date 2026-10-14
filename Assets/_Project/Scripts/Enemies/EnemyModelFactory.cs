using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Builds the procedural alien bodies. A creature is a body mesh, one leg mesh used for every leg (mirrored for
    /// the left side) and, for flyers, one wing mesh. The view animates legs and wings, so nothing is baked in a pose.
    /// Submesh slots: body, eyes, glow, dark.
    /// </summary>
    public static class EnemyModelFactory
    {
        public const int Body = 0;
        public const int Eyes = 1;
        public const int Glow = 2;
        public const int Dark = 3;

        private const float BossScaleFactor = 0.6f;
        private static readonly Color EyeColor = new Color(1f, 0.9f, 0.35f);

        public readonly struct Leg
        {
            public readonly Vector3 Hip;
            public readonly bool Left;
            public readonly float Phase;

            public Leg(Vector3 hip, bool left, float phase)
            {
                Hip = hip;
                Left = left;
                Phase = phase;
            }
        }

        public sealed class Parts
        {
            public Mesh Body;
            public Mesh LegMesh;
            public Mesh Wing;
            public readonly List<Leg> Legs = new List<Leg>();

            /// <summary>Shoulder positions of the right and left wing, empty for creatures that walk.</summary>
            public readonly List<Vector3> WingHinges = new List<Vector3>();

            /// <summary>Height of the top of the creature at scale one; the health bar floats above it.</summary>
            public float Height;

            /// <summary>How large the creature is drawn per unit of the definition scale; bosses are modelled big, so they use less.</summary>
            public float ScaleFactor = 1.4f;
        }

        public static Material[] CreateMaterials(Color color)
        {
            return new[]
            {
                SurfaceMaterials.Create(color * 0.8f, 0f, 0.3f),
                SurfaceMaterials.Create(EyeColor, 3f, 0.6f),
                SurfaceMaterials.Create(Color.Lerp(color, Color.white, 0.25f), 1.5f, 0.5f),
                SurfaceMaterials.Create(color * 0.3f, 0f, 0.2f),
            };
        }

        public static Parts Create(EnemyShape shape)
        {
            var body = new PropMeshBuilder();
            var parts = new Parts();
            float hipY;
            float legRadius;
            switch (shape)
            {
                case EnemyShape.Scout:
                    Scout(body, parts);
                    hipY = 0.3f;
                    legRadius = 0.022f;
                    break;
                case EnemyShape.Grunt:
                    Grunt(body, parts);
                    hipY = 0.4f;
                    legRadius = 0.038f;
                    break;
                case EnemyShape.Brute:
                    Brute(body, parts);
                    hipY = 0.58f;
                    legRadius = 0.075f;
                    break;
                case EnemyShape.Flyer:
                    Flyer(body, parts);
                    hipY = 0f;
                    legRadius = 0f;
                    break;
                case EnemyShape.Overseer:
                    Overseer(body, parts);
                    hipY = 0.75f;
                    legRadius = 0.07f;
                    break;
                case EnemyShape.Warden:
                    Warden(body, parts);
                    hipY = 0.78f;
                    legRadius = 0.1f;
                    break;
                default:
                    Queen(body, parts);
                    hipY = 0.88f;
                    legRadius = 0.085f;
                    break;
            }

            parts.Body = body.ToMesh($"{shape}_body");
            if (hipY > 0f)
            {
                parts.LegMesh = BuildLeg(hipY, legRadius);
            }

            return parts;
        }

        // Hip, knee, foot: out and up first, then down to the ground like an insect leg.
        private static Mesh BuildLeg(float hipY, float radius)
        {
            var b = new PropMeshBuilder();
            float reach = hipY * 0.95f;
            var path = new[]
            {
                Vector3.zero,
                new Vector3(reach * 0.5f, hipY * 0.22f, 0f),
                new Vector3(reach * 0.85f, -hipY * 0.45f, 0f),
                new Vector3(reach * 0.95f, -hipY, 0f),
            };
            b.AddTube(path, new[] { radius, radius * 0.8f, radius * 0.6f, radius * 0.3f }, 5, Dark, false);
            return b.ToMesh("Leg");
        }

        private static void Pairs(Parts parts, float hipY, float width, params float[] zs)
        {
            for (int i = 0; i < zs.Length; i++)
            {
                float phase = (i % 2) * Mathf.PI;
                parts.Legs.Add(new Leg(new Vector3(width, hipY, zs[i]), false, phase));
                parts.Legs.Add(new Leg(new Vector3(-width, hipY, zs[i]), true, phase + Mathf.PI));
            }
        }

        private static void Eye(PropMeshBuilder b, Vector3 centre, float size)
        {
            b.AddBlob(centre + new Vector3(size * 1.4f, 0f, 0f), Vector3.one * size, 3, 6, 0f, 0, Eyes, false);
            b.AddBlob(centre - new Vector3(size * 1.4f, 0f, 0f), Vector3.one * size, 3, 6, 0f, 0, Eyes, false);
        }

        private static void Spike(PropMeshBuilder b, Vector3 from, Vector3 to, float radius, int slot) =>
            b.AddTube(new[] { from, to }, new[] { radius, 0f }, 5, slot, true);

        // Small and quick: slim body, narrow head, a whip tail and a glowing stripe.
        private static void Scout(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.3f, 0f), new Vector3(0.17f, 0.13f, 0.3f), 5, 8, 0.08f, 1, Body, false);
            b.AddBlob(new Vector3(0f, 0.33f, 0.3f), new Vector3(0.1f, 0.09f, 0.11f), 4, 8, 0.04f, 2, Body, false);
            Eye(b, new Vector3(0f, 0.37f, 0.38f), 0.03f);
            b.AddTube(new[] { new Vector3(0f, 0.3f, -0.28f), new Vector3(0f, 0.38f, -0.5f), new Vector3(0f, 0.5f, -0.65f) }, new[] { 0.06f, 0.035f, 0f }, 5, Dark, false);
            b.AddBlob(new Vector3(0f, 0.42f, -0.05f), new Vector3(0.05f, 0.03f, 0.2f), 3, 6, 0f, 0, Glow, false);
            Pairs(parts, 0.3f, 0.12f, 0.15f, 0f, -0.15f);
            parts.Height = 0.6f;
        }

        // The common soldier: armoured shell, mandibles and glowing spots on its back.
        private static void Grunt(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.42f, 0f), new Vector3(0.27f, 0.2f, 0.4f), 5, 9, 0.1f, 3, Body, false);
            b.AddBlob(new Vector3(0f, 0.55f, -0.05f), new Vector3(0.25f, 0.1f, 0.32f), 4, 9, 0.08f, 4, Dark, true);
            b.AddBlob(new Vector3(0f, 0.45f, 0.42f), new Vector3(0.16f, 0.14f, 0.16f), 4, 8, 0.05f, 5, Body, false);
            Eye(b, new Vector3(0f, 0.5f, 0.54f), 0.04f);
            Spike(b, new Vector3(0.08f, 0.4f, 0.54f), new Vector3(0.05f, 0.36f, 0.76f), 0.04f, Glow);
            Spike(b, new Vector3(-0.08f, 0.4f, 0.54f), new Vector3(-0.05f, 0.36f, 0.76f), 0.04f, Glow);
            for (int i = 0; i < 3; i++)
            {
                b.AddBlob(new Vector3((i - 1) * 0.09f, 0.62f, -0.1f - i * 0.04f), Vector3.one * 0.04f, 3, 6, 0f, 0, Glow, false);
            }

            Pairs(parts, 0.4f, 0.2f, 0.22f, 0f, -0.22f);
            parts.Height = 0.8f;
        }

        // Heavy: broad body, horns, back spines and glowing vents.
        private static void Brute(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.34f, 0.55f), 5, 10, 0.1f, 6, Body, false);
            b.AddBlob(new Vector3(0f, 0.66f, 0.58f), new Vector3(0.22f, 0.2f, 0.2f), 4, 8, 0.05f, 7, Body, false);
            Eye(b, new Vector3(0f, 0.72f, 0.74f), 0.05f);
            Spike(b, new Vector3(0.14f, 0.8f, 0.6f), new Vector3(0.32f, 1.15f, 0.7f), 0.06f, Dark);
            Spike(b, new Vector3(-0.14f, 0.8f, 0.6f), new Vector3(-0.32f, 1.15f, 0.7f), 0.06f, Dark);
            for (int i = 0; i < 4; i++)
            {
                float z = 0.3f - i * 0.22f;
                Spike(b, new Vector3(0f, 0.9f, z), new Vector3(0f, 1.2f - i * 0.04f, z - 0.1f), 0.07f, Dark);
            }

            b.AddBlob(new Vector3(0.28f, 0.78f, 0.1f), new Vector3(0.08f, 0.05f, 0.14f), 3, 6, 0f, 0, Glow, false);
            b.AddBlob(new Vector3(-0.28f, 0.78f, 0.1f), new Vector3(0.08f, 0.05f, 0.14f), 3, 6, 0f, 0, Glow, false);
            Pairs(parts, 0.58f, 0.3f, 0.28f, -0.28f);
            parts.Height = 1.25f;
        }

        // Flyer: slender body, long tail with a glowing tip; the wings are separate so they can beat.
        private static void Flyer(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(Vector3.zero, new Vector3(0.14f, 0.12f, 0.28f), 5, 8, 0.05f, 8, Body, false);
            b.AddBlob(new Vector3(0f, 0.02f, 0.28f), new Vector3(0.09f, 0.08f, 0.1f), 4, 8, 0f, 0, Body, false);
            Eye(b, new Vector3(0f, 0.05f, 0.35f), 0.028f);
            b.AddTube(new[] { new Vector3(0f, 0f, -0.26f), new Vector3(0f, -0.04f, -0.5f), new Vector3(0f, -0.1f, -0.7f) }, new[] { 0.05f, 0.03f, 0.012f }, 5, Dark, false);
            b.AddBlob(new Vector3(0f, -0.1f, -0.72f), Vector3.one * 0.05f, 3, 6, 0f, 0, Glow, false);

            var wing = new PropMeshBuilder();
            wing.AddBlob(new Vector3(0.42f, 0f, 0f), new Vector3(0.45f, 0.015f, 0.17f), 3, 9, 0.05f, 9, Glow, false);
            wing.AddBlob(new Vector3(0.2f, 0.01f, 0.02f), new Vector3(0.2f, 0.02f, 0.06f), 3, 6, 0f, 0, Body, false);
            parts.Wing = wing.ToMesh("Wing");
            parts.WingHinges.Add(new Vector3(0.1f, 0.08f, 0.05f));
            parts.WingHinges.Add(new Vector3(-0.1f, 0.08f, 0.05f));
            parts.Height = 0.5f;
        }

        // Boss: a floating mind-orb above a mantis body with grasping claws and a spiked crown.
        private static void Overseer(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 0.4f, 0.6f), 5, 10, 0.08f, 10, Body, false);
            b.AddBlob(new Vector3(0f, 1.05f, 0.5f), new Vector3(0.3f, 0.28f, 0.3f), 5, 10, 0.05f, 11, Body, false);
            Eye(b, new Vector3(0f, 1.1f, 0.76f), 0.06f);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 0.35f;
                var root = new Vector3(Mathf.Sin(angle) * 0.2f, 1.28f, 0.5f);
                Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.18f, 0.5f, -0.05f), 0.05f, Glow);
            }

            b.AddBlob(new Vector3(0f, 1.9f, 0.35f), Vector3.one * 0.2f, 5, 10, 0f, 0, Glow, false);
            b.AddTube(new[] { new Vector3(0.4f, 0.9f, 0.4f), new Vector3(0.62f, 0.8f, 0.8f), new Vector3(0.5f, 0.55f, 1.2f) }, new[] { 0.07f, 0.06f, 0f }, 5, Dark, false);
            b.AddTube(new[] { new Vector3(-0.4f, 0.9f, 0.4f), new Vector3(-0.62f, 0.8f, 0.8f), new Vector3(-0.5f, 0.55f, 1.2f) }, new[] { 0.07f, 0.06f, 0f }, 5, Dark, false);
            Pairs(parts, 0.75f, 0.42f, 0.4f, 0f, -0.4f);
            parts.Height = 2.2f;
            parts.ScaleFactor = BossScaleFactor;
        }

        // Boss: a walking fortress with side plates, a glowing frontal shield and shoulder spikes.
        private static void Warden(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.82f, 0f), new Vector3(0.6f, 0.42f, 0.62f), 5, 10, 0.06f, 12, Body, false);
            b.AddBlob(new Vector3(0.58f, 0.88f, 0f), new Vector3(0.14f, 0.36f, 0.5f), 4, 8, 0.1f, 13, Dark, true);
            b.AddBlob(new Vector3(-0.58f, 0.88f, 0f), new Vector3(0.14f, 0.36f, 0.5f), 4, 8, 0.1f, 14, Dark, true);
            b.AddBlob(new Vector3(0f, 0.92f, 0.56f), new Vector3(0.46f, 0.4f, 0.1f), 4, 10, 0.03f, 15, Glow, false);
            b.AddBlob(new Vector3(0f, 1.0f, 0.62f), new Vector3(0.24f, 0.22f, 0.24f), 4, 8, 0.04f, 16, Body, false);
            Eye(b, new Vector3(0f, 1.05f, 0.82f), 0.06f);
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Spike(b, new Vector3(0.45f * side, 1.15f, 0.1f - (i / 2) * 0.35f), new Vector3(0.7f * side, 1.65f, 0.05f - (i / 2) * 0.35f), 0.08f, Dark);
            }

            Pairs(parts, 0.78f, 0.5f, 0.35f, -0.35f);
            parts.Height = 1.8f;
            parts.ScaleFactor = BossScaleFactor;
        }

        // Boss: a swollen abdomen full of glowing eggs, a crowned head and long legs.
        private static void Queen(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 0.95f, -0.75f), new Vector3(0.7f, 0.6f, 0.9f), 6, 10, 0.07f, 17, Body, false);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 1.1f;
                b.AddBlob(new Vector3(Mathf.Cos(angle) * 0.4f, 1.1f + (i % 3) * 0.12f, -0.75f + Mathf.Sin(angle) * 0.5f), Vector3.one * 0.13f, 4, 7, 0f, 0, Glow, false);
            }

            b.AddBlob(new Vector3(0f, 1.0f, 0.05f), new Vector3(0.48f, 0.42f, 0.48f), 5, 10, 0.06f, 18, Body, false);
            b.AddBlob(new Vector3(0f, 1.08f, 0.7f), new Vector3(0.28f, 0.25f, 0.28f), 5, 10, 0.04f, 19, Body, false);
            Eye(b, new Vector3(0f, 1.14f, 0.94f), 0.06f);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 0.4f;
                var root = new Vector3(Mathf.Sin(angle) * 0.22f, 1.3f, 0.7f);
                Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.2f, 0.45f, 0f), 0.05f, Glow);
            }

            Spike(b, new Vector3(0.12f, 1.0f, 0.95f), new Vector3(0.22f, 0.8f, 1.3f), 0.06f, Dark);
            Spike(b, new Vector3(-0.12f, 1.0f, 0.95f), new Vector3(-0.22f, 0.8f, 1.3f), 0.06f, Dark);
            Pairs(parts, 0.88f, 0.4f, 0.4f, 0f, -0.45f);
            parts.Height = 1.9f;
            parts.ScaleFactor = BossScaleFactor;
        }
    }
}
