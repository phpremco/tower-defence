using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Builds the procedural alien bodies: every species has its own silhouette, so the player reads it at a glance:
    /// the ant's three beads, the scorpion's raised tail, the bat's wide wings, the upright shield bearer, the lean
    /// runner, the floating orb, the flat ray and the soft slug, plus the roller and the queen. A creature is one
    /// static body mesh plus a list of moving parts that the view hangs on their own pivots and animates, so nothing
    /// is baked in a pose. Bosses are the same builders with more bulk, crowns and extra weapons.
    /// Submesh slots: body, eyes, glow (the species colour), dark, accent (plates, blades, bolts).
    /// </summary>
    public static class EnemyModelFactory
    {
        public const int Body = 0;
        public const int Eyes = 1;
        public const int Glow = 2;
        public const int Dark = 3;
        public const int Accent = 4;
        public const int SlotCount = 5;

        private const float BossScaleFactor = 0.6f;
        private const float LegReachShare = 0.75f;
        private const float LegThickness = 1.7f;
        private const float BodyShade = 0.6f;
        private const float DarkShade = 0.22f;
        private const float ChitinGloss = 0.68f;
        private const float EyeEmission = 3.2f;
        private const float GlowEmission = 2.2f;
        private const float AccentEmission = 2.8f;
        private const float SkinnedBodyShade = 0.9f;
        private const float SkinnedDarkShade = 0.4f;
        private const float SeamGlow = 0.9f;
        private const float DarkSeamGlow = 0.5f;
        private static readonly Color DefaultEyeColor = new Color(1f, 0.9f, 0.35f);

        public sealed class Parts
        {
            public Mesh Body;

            /// <summary>Every part that moves, with the mesh it wears and where it hangs on the body.</summary>
            public readonly List<CreaturePart> Moving = new List<CreaturePart>();

            /// <summary>Height of the top of the creature at scale one; the health bar floats above it.</summary>
            public float Height;

            /// <summary>How large the creature is drawn per unit of the definition scale; bosses are modelled big, so they use less.</summary>
            public float ScaleFactor = 1.4f;

            /// <summary>Radius of the ball a roller curls into, at scale one. Zero for creatures that never roll.</summary>
            public float BallRadius;

            /// <summary>True when the legs hang under a flying body instead of walking.</summary>
            public bool LegsDangle;

            /// <summary>True for legs that hang straight down and swing fore and aft, like a runner's, not out to the side.</summary>
            public bool StraightLegs;

            /// <summary>True when the creature sheds embers as it walks.</summary>
            public bool Embers;

            /// <summary>True when the creature drips when hurt.</summary>
            public bool Bleeds;

            /// <summary>How high above the ground a floating creature hangs, at scale one. Zero for walkers.</summary>
            public float HoverHeight;

            /// <summary>The surface the body and dark parts wear: chitin, hide, slime, scales or plasma.</summary>
            public CreatureSkin.Kind Skin = CreatureSkin.Kind.Chitin;
        }

        // The two segments of a leg and where the lower one hinges on the upper one.
        private readonly struct LegSet
        {
            public readonly Mesh Upper;
            public readonly Mesh Lower;
            public readonly Vector3 Knee;

            public LegSet(Mesh upper, Mesh lower, Vector3 knee)
            {
                Upper = upper;
                Lower = lower;
                Knee = knee;
            }
        }

        /// <summary>The four-slot palette shared with gates and other structures that borrow the creature look.</summary>
        public static Material[] CreateMaterials(Color color) => CreateMaterials(color, DefaultEyeColor);

        public static Material[] CreateMaterials(Color color, Color eyes, float glow = 1f)
        {
            Material[] all = CreateMaterials(color, eyes, Color.Lerp(color, Color.white, 0.4f), glow);
            return new[] { all[Body], all[Eyes], all[Glow], all[Dark] };
        }

        // Dark glossy chitin with the creature's colour in its seams: menace comes from the dark base and the glow,
        // which a boss asks for more of. The accent is a second light, for the parts that mark the species' skill.
        public static Material[] CreateMaterials(Color color, Color eyes, Color accent, float glow) => CreateMaterials(color, eyes, accent, glow, CreatureSkin.Kind.None);

        // With a skin the body is painted: the species colour tints the plates or fur, the grooves carry its glow.
        public static Material[] CreateMaterials(Color color, Color eyes, Color accent, float glow, CreatureSkin.Kind skin)
        {
            bool skinned = skin != CreatureSkin.Kind.None;
            float bodyShade = skinned ? SkinnedBodyShade : BodyShade;
            float darkShade = skinned ? SkinnedDarkShade : DarkShade;
            Color seam = Color.Lerp(color, Color.white, 0.2f) * glow;
            return new[]
            {
                CreatureSkin.Dress(SurfaceMaterials.Create(color * bodyShade, 0f, ChitinGloss, 0.1f), skin, seam * SeamGlow),
                SurfaceMaterials.Create(eyes, EyeEmission * glow, 0.7f),
                SurfaceMaterials.Create(Color.Lerp(color, Color.white, 0.15f), GlowEmission * glow, 0.5f),
                CreatureSkin.Dress(SurfaceMaterials.Create(color * darkShade, 0f, ChitinGloss, 0.15f), skin, seam * DarkSeamGlow),
                SurfaceMaterials.Create(Color.Lerp(accent, Color.white, 0.1f), AccentEmission * glow, 0.8f),
            };
        }

        public static Parts Create(EnemyShape shape)
        {
            var body = new PropMeshBuilder(SlotCount);
            var parts = new Parts();
            switch (shape)
            {
                case EnemyShape.Skitter:
                    Skitter(body, parts, false);
                    break;
                case EnemyShape.BroodMother:
                    Skitter(body, parts, true);
                    break;
                case EnemyShape.Stinger:
                    Stinger(body, parts, false);
                    break;
                case EnemyShape.AcidEmpress:
                    Stinger(body, parts, true);
                    break;
                case EnemyShape.Bat:
                    Bat(body, parts, false);
                    break;
                case EnemyShape.StormMatriarch:
                    Bat(body, parts, true);
                    break;
                case EnemyShape.Bulwark:
                    Bulwark(body, parts, false);
                    break;
                case EnemyShape.CrystalWarlord:
                    Bulwark(body, parts, true);
                    break;
                case EnemyShape.Prowler:
                    Prowler(body, parts, false);
                    break;
                case EnemyShape.PackAlpha:
                    Prowler(body, parts, true);
                    break;
                case EnemyShape.Wisp:
                    Wisp(body, parts, false);
                    break;
                case EnemyShape.CoreWisp:
                    Wisp(body, parts, true);
                    break;
                case EnemyShape.Skyray:
                    Skyray(body, parts, false);
                    break;
                case EnemyShape.SkySovereign:
                    Skyray(body, parts, true);
                    break;
                case EnemyShape.Oozer:
                    Oozer(body, parts, false);
                    break;
                case EnemyShape.SlimeMonarch:
                    Oozer(body, parts, true);
                    break;
                case EnemyShape.Roller:
                    Roller(body, parts, false);
                    break;
                case EnemyShape.RollerKing:
                    Roller(body, parts, true);
                    break;
                default:
                    Queen(body, parts);
                    break;
            }

            parts.Body = body.ToMesh($"{shape}_body");
            if (parts.ScaleFactor == 1.4f && IsBoss(shape))
            {
                parts.ScaleFactor = BossScaleFactor;
            }

            return parts;
        }

        private static bool IsBoss(EnemyShape shape) => (int)shape >= (int)EnemyShape.BroodMother || shape == EnemyShape.Queen;

        // The ant: three beads on a thread, a round head with big jaws, elbowed feelers and six quick legs. Small
        // and bright, so a crowd of them reads as a crowd. The brood mother is the same ant swollen to a hill with an
        // egg-filled abdomen and a crest.
        private static void Skitter(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.6f : 1f;
            b.AddBlob(new Vector3(0f, 0.2f * k, 0.02f * k), new Vector3(0.09f, 0.08f, 0.13f) * k, 4, 8, 0.03f, 1, Body, false);
            b.AddBlob(new Vector3(0f, 0.25f * k, 0.02f * k), new Vector3(0.08f, 0.04f, 0.1f) * k, 3, 8, 0.02f, 2, Dark, true, Mathf.PI * 0.5f);
            b.AddTube(new[] { new Vector3(0f, 0.2f * k, -0.1f * k), new Vector3(0f, 0.21f * k, -0.2f * k) }, new[] { 0.03f * k, 0.03f * k }, 5, Dark, false);
            b.AddBlob(new Vector3(0f, 0.22f * k, 0.22f * k), new Vector3(0.1f, 0.09f, 0.1f) * k, 4, 8, 0.02f, 3, Body, false);
            b.AddBlob(new Vector3(0f, 0.27f * k, 0.22f * k), new Vector3(0.09f, 0.04f, 0.08f) * k, 3, 8, 0.02f, 4, Dark, true, Mathf.PI * 0.5f);
            EyeCluster(b, new Vector3(0f, 0.24f * k, 0.3f * k), 0.028f * k);
            b.AddBlob(new Vector3(0f, 0.18f * k, 0.3f * k), new Vector3(0.05f, 0.025f, 0.03f) * k, 3, 6, 0f, 0, Accent, false);
            if (boss)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = (i - 2) * 0.45f;
                    var root = new Vector3(Mathf.Sin(angle) * 0.2f, 0.32f, 0.2f) * k;
                    Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.2f, 0.3f, -0.05f) * k, 0.03f * k, Accent);
                }

                b.AddTorus(new Vector3(0f, 0.2f * k, -0.14f * k), Vector3.forward, 0.06f * k, 0.012f * k, 12, 4, Glow);
            }

            var abdomen = new PropMeshBuilder(SlotCount);
            float ab = boss ? 1.5f : 1f;
            abdomen.AddBlob(new Vector3(0f, 0f, -0.16f * k * ab), new Vector3(0.13f * ab, 0.11f * ab, 0.21f * ab) * k, 5, 9, 0.03f, 5, Body, false);
            for (int i = 0; i < 3; i++)
            {
                abdomen.AddTorus(new Vector3(0f, 0f, (-0.08f - i * 0.08f) * k * ab), Vector3.forward, (0.12f - i * 0.02f) * k * ab, 0.009f * k, 12, 4, Glow);
            }

            abdomen.AddTube(new[] { new Vector3(0f, -0.03f * k, -0.34f * k * ab), new Vector3(0f, -0.06f * k, -0.44f * k * ab) }, new[] { 0.025f * k, 0f }, 5, Accent, false);
            if (boss)
            {
                for (int i = 0; i < 7; i++)
                {
                    float angle = i * 0.9f;
                    abdomen.AddBlob(new Vector3(Mathf.Cos(angle) * 0.11f * ab, 0.03f + (i % 2) * 0.06f, (-0.16f + Mathf.Sin(angle) * 0.12f) * ab) * k, Vector3.one * 0.045f * k, 4, 7, 0f, 0, Accent, false);
                }
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Abdomen, abdomen.ToMesh("Abdomen"), new Vector3(0f, 0.2f * k, -0.2f * k)));

            Mandibles(parts, new Vector3(0.04f * k, 0.19f * k, 0.3f * k), 0.13f * k, 0.02f * k, Dark);
            Antennae(parts, new Vector3(0.035f * k, 0.28f * k, 0.3f * k), 0.2f * k);
            Legs(parts, 0.17f * k, 0.08f * k, 0.2f * k, 0.016f * k, 0.17f * k, 0.04f * k, -0.08f * k);
            parts.Bleeds = true;
            parts.Height = (boss ? 0.65f : 0.36f) * k;
        }

        // The scorpion: a wide flat body on eight legs, two pincers out front, a segmented tail arched over the
        // back with a glowing sting, and a row of lit plates down the spine that fall off as its armour goes.
        // The empress carries two tails and a crown of plates.
        private static void Stinger(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.4f : 1f;
            b.AddBlob(new Vector3(0f, 0.2f * k, 0.02f * k), new Vector3(0.26f, 0.11f, 0.3f) * k, 5, 10, 0.04f, 6, Body, false);
            b.AddBlob(new Vector3(0f, 0.2f * k, 0.3f * k), new Vector3(0.17f, 0.09f, 0.12f) * k, 4, 8, 0.03f, 7, Body, false);
            EyeCluster(b, new Vector3(0f, 0.25f * k, 0.38f * k), 0.03f * k);
            b.AddTube(new[] { new Vector3(-0.24f * k, 0.2f * k, 0.1f * k), new Vector3(-0.27f * k, 0.21f * k, -0.2f * k) }, new[] { 0.008f * k, 0.008f * k }, 4, Glow, false);
            b.AddTube(new[] { new Vector3(0.24f * k, 0.2f * k, 0.1f * k), new Vector3(0.27f * k, 0.21f * k, -0.2f * k) }, new[] { 0.008f * k, 0.008f * k }, 4, Glow, false);
            if (boss)
            {
                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    Spike(b, new Vector3(0.2f * side, 0.28f, 0.3f - (i / 2) * 0.1f) * k, new Vector3(0.3f * side, 0.5f, 0.28f - (i / 2) * 0.1f) * k, 0.03f * k, Accent);
                }
            }

            int plates = boss ? 6 : 4;
            for (int i = 0; i < plates; i++)
            {
                float z = (0.16f - i * (boss ? 0.075f : 0.1f)) * k;
                var plate = new PropMeshBuilder(SlotCount);
                plate.AddBlob(Vector3.zero, new Vector3(0.2f, 0.05f, 0.055f) * k, 3, 9, 0.03f, 20 + i, Dark, true, Mathf.PI * 0.5f);
                plate.AddBlob(new Vector3(0f, 0.03f * k, 0f), new Vector3(0.14f, 0.025f, 0.03f) * k, 2, 8, 0f, 0, Accent, false, Mathf.PI * 0.5f);
                parts.Moving.Add(new CreaturePart(CreatureRole.Plate, plate.ToMesh("Plate"), new Vector3(0f, 0.28f * k, z), false, 0f, i));
            }

            Pincers(parts, new Vector3(0.17f * k, 0.17f * k, 0.3f * k), 0.3f * k, 0.035f * k);
            StingTail(parts, new Vector3(0f, 0.24f * k, -0.3f * k), 0.95f * k, 0.055f * k, 0, 0f);
            if (boss)
            {
                StingTail(parts, new Vector3(0.12f * k, 0.22f * k, -0.26f * k), 0.6f * k, 0.035f * k, 1, 0.35f);
                StingTail(parts, new Vector3(-0.12f * k, 0.22f * k, -0.26f * k), 0.6f * k, 0.035f * k, 1, -0.35f);
            }

            Legs(parts, 0.17f * k, 0.22f * k, 0.3f * k, 0.022f * k, 0.22f * k, 0.08f * k, -0.06f * k, -0.2f * k);
            parts.Height = (boss ? 1.0f : 0.78f) * k;
        }

        // The element bat: a small furred body between two wide membrane wings, big ears, a wedge face and tiny
        // feet tucked up. Its element shows in the veins and the eyes. The matriarch has a second pair of wings
        // and a crown of horns.
        private static void Bat(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.8f : 1f;
            b.AddBlob(Vector3.zero, new Vector3(0.1f, 0.1f, 0.15f) * k, 5, 8, 0.05f, 8, Body, false);
            b.AddBlob(new Vector3(0f, 0.03f * k, 0.16f * k), new Vector3(0.075f, 0.07f, 0.08f) * k, 4, 8, 0.03f, 9, Body, false);
            b.AddTube(new[] { new Vector3(0f, 0.02f * k, 0.2f * k), new Vector3(0f, -0.01f * k, 0.27f * k) }, new[] { 0.04f * k, 0.01f * k }, 5, Dark, false);
            b.AddBlob(new Vector3(0.045f * k, 0.06f * k, 0.21f * k), Vector3.one * 0.022f * k, 3, 6, 0f, 0, Eyes, false);
            b.AddBlob(new Vector3(-0.045f * k, 0.06f * k, 0.21f * k), Vector3.one * 0.022f * k, 3, 6, 0f, 0, Eyes, false);
            Spike(b, new Vector3(0.05f * k, 0.09f * k, 0.16f * k), new Vector3(0.09f * k, 0.22f * k, 0.13f * k), 0.025f * k, Dark);
            Spike(b, new Vector3(-0.05f * k, 0.09f * k, 0.16f * k), new Vector3(-0.09f * k, 0.22f * k, 0.13f * k), 0.025f * k, Dark);
            Cracks(b, Vector3.zero, new Vector3(0.1f, 0.1f, 0.15f) * k, boss ? 8 : 4, 31, Accent);
            b.AddBlob(new Vector3(0f, -0.08f * k, 0.02f * k), new Vector3(0.05f, 0.03f, 0.08f) * k, 3, 7, 0f, 0, Accent, false);
            if (boss)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = (i - 1) * 0.6f;
                    var root = new Vector3(Mathf.Sin(angle) * 0.07f, 0.1f, 0.1f) * k;
                    Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.1f, 0.25f, -0.1f) * k, 0.02f * k, Accent);
                }
            }

            var tail = new PropMeshBuilder(SlotCount);
            tail.AddBlade(new[] { Vector3.zero, new Vector3(0f, -0.02f * k, -0.12f * k), new Vector3(0f, -0.03f * k, -0.24f * k) }, new[] { 0.04f * k, 0.07f * k, 0.01f * k }, Vector3.right, Body);
            parts.Moving.Add(new CreaturePart(CreatureRole.Tail, tail.ToMesh("Tail"), new Vector3(0f, 0f, -0.13f * k)));

            MembraneWings(parts, new Vector3(0.06f * k, 0.04f * k, 0.03f * k), 0.5f * k, 0.3f * k, 0);
            if (boss)
            {
                MembraneWings(parts, new Vector3(0.06f * k, 0.02f * k, -0.07f * k), 0.38f * k, 0.22f * k, 1);
            }

            Legs(parts, 0f, 0.05f * k, 0.1f * k, 0.01f * k, -0.04f * k);
            parts.LegsDangle = true;
            parts.Skin = CreatureSkin.Kind.Membrane;
            parts.Height = 0.3f * k;
        }

        // The shield bearer: a heavy upright body on two thick legs, a crested saurian head, a long tail for
        // balance, a crystal sword held high in the right hand and a shield on the left arm. Plates on the
        // shoulders, chest and back come off as it is beaten. The warlord wields two swords under a crown.
        private static void Bulwark(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.2f : 1f;
            // The body leans into its walk: hips back, chest forward, the head out in front of the shoulders.
            b.AddBlob(new Vector3(0f, 0.62f * k, 0.06f * k), new Vector3(0.21f, 0.26f, 0.17f) * k, 5, 9, 0.04f, 10, Body, false);
            b.AddBlob(new Vector3(0f, 0.4f * k, -0.03f * k), new Vector3(0.16f, 0.1f, 0.14f) * k, 4, 8, 0.03f, 11, Dark, true);
            b.AddTube(new[] { new Vector3(0f, 0.84f * k, 0.12f * k), new Vector3(0f, 0.93f * k, 0.2f * k) }, new[] { 0.07f * k, 0.06f * k }, 6, Dark, false);
            b.AddBlob(new Vector3(0f, 0.98f * k, 0.26f * k), new Vector3(0.11f, 0.1f, 0.16f) * k, 4, 8, 0.03f, 12, Body, false);
            b.AddTube(new[] { new Vector3(0f, 0.95f * k, 0.36f * k), new Vector3(0f, 0.9f * k, 0.48f * k) }, new[] { 0.07f * k, 0.03f * k }, 5, Dark, false);
            b.AddBlob(new Vector3(0.06f * k, 1.02f * k, 0.36f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            b.AddBlob(new Vector3(-0.06f * k, 1.02f * k, 0.36f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            for (int i = 0; i < (boss ? 5 : 3); i++)
            {
                float z = (0.1f - i * 0.08f) * k;
                Spike(b, new Vector3(0f, 1.04f * k, z + 0.26f * k), new Vector3(0f, (boss ? 1.36f : 1.24f) * k, z + 0.16f * k), 0.03f * k, boss ? Accent : Dark);
            }

            // The shield: a broad plate on the left forearm with a lit rim.
            b.AddBlob(new Vector3(-0.3f * k, 0.62f * k, 0.1f * k), new Vector3(0.05f, 0.24f, 0.2f) * k, 4, 9, 0.02f, 13, Dark, true);
            b.AddTorus(new Vector3(-0.34f * k, 0.62f * k, 0.1f * k), Vector3.right, 0.18f * k, 0.015f * k, 16, 4, Accent);
            b.AddTube(new[] { new Vector3(-0.18f * k, 0.72f * k, 0f), new Vector3(-0.28f * k, 0.6f * k, 0.05f * k) }, new[] { 0.05f * k, 0.04f * k }, 5, Body, false);
            b.AddBlob(new Vector3(0f, 0.7f * k, 0.14f * k), new Vector3(0.05f, 0.04f, 0.02f) * k, 3, 6, 0f, 0, Accent, false);

            var tail = new PropMeshBuilder(SlotCount);
            tail.AddTube(new[] { Vector3.zero, new Vector3(0f, -0.1f * k, -0.3f * k), new Vector3(0f, -0.12f * k, -0.6f * k) }, new[] { 0.09f * k, 0.06f * k, 0.01f * k }, 6, Body, false);
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3(0f, (0.03f - i * 0.035f) * k, (-0.1f - i * 0.14f) * k);
                tail.AddTube(new[] { at, at + new Vector3(0f, 0.08f * k, -0.03f * k) }, new[] { 0.02f * k, 0f }, 4, Dark, true);
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Tail, tail.ToMesh("Tail"), new Vector3(0f, 0.45f * k, -0.12f * k)));

            ArmorPlate(parts, 0, new Vector3(0.22f * k, 0.82f * k, 0f), new Vector3(0.11f, 0.06f, 0.12f) * k, false, 40);
            ArmorPlate(parts, 1, new Vector3(-0.22f * k, 0.82f * k, 0f), new Vector3(0.11f, 0.06f, 0.12f) * k, true, 41);
            ArmorPlate(parts, 2, new Vector3(0f, 0.66f * k, 0.15f * k), new Vector3(0.17f, 0.17f, 0.05f) * k, false, 42, Quaternion.Euler(-90f, 0f, 0f));
            ArmorPlate(parts, 3, new Vector3(0f, 0.66f * k, -0.15f * k), new Vector3(0.17f, 0.15f, 0.05f) * k, false, 43, Quaternion.Euler(90f, 0f, 0f));

            SwordArm(parts, new Vector3(0.2f * k, 0.78f * k, 0.02f * k), 0.75f * k, 0.045f * k, false);
            if (boss)
            {
                SwordArm(parts, new Vector3(-0.2f * k, 0.78f * k, 0.02f * k), 0.65f * k, 0.04f * k, true);
            }

            StraightLegs(parts, 0.42f * k, 0.16f * k, 0.44f * k, 0.055f * k, -0.02f * k);
            parts.StraightLegs = true;
            parts.Bleeds = true;
            parts.Skin = CreatureSkin.Kind.Scales;
            parts.Height = (boss ? 1.45f : 1.3f) * k;
        }

        // The pack hunter: a lean body low to the ground on four straight legs, a long head of jaws, ears laid
        // back and a row of lit spines down the back. The alpha carries a mane of spines and a heavier head.
        private static void Prowler(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.3f : 1f;
            b.AddBlob(new Vector3(0f, 0.36f * k, 0.05f * k), new Vector3(0.14f, 0.15f, 0.34f) * k, 5, 9, 0.04f, 14, Body, false);
            b.AddBlob(new Vector3(0f, 0.34f * k, -0.25f * k), new Vector3(0.13f, 0.14f, 0.16f) * k, 4, 8, 0.03f, 15, Body, false);
            b.AddBlob(new Vector3(0f, 0.42f * k, 0.3f * k), new Vector3(0.12f, 0.12f, 0.12f) * k, 4, 8, 0.03f, 16, Body, false);
            b.AddBlob(new Vector3(0f, 0.4f * k, 0.44f * k), new Vector3(0.08f, 0.07f, 0.14f) * k, 4, 8, 0.02f, 17, Dark, true);
            b.AddBlob(new Vector3(0.05f * k, 0.46f * k, 0.38f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            b.AddBlob(new Vector3(-0.05f * k, 0.46f * k, 0.38f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            Spike(b, new Vector3(0.06f * k, 0.5f * k, 0.3f * k), new Vector3(0.09f * k, 0.62f * k, 0.22f * k), 0.025f * k, Dark);
            Spike(b, new Vector3(-0.06f * k, 0.5f * k, 0.3f * k), new Vector3(-0.09f * k, 0.62f * k, 0.22f * k), 0.025f * k, Dark);
            int spines = boss ? 7 : 5;
            for (int i = 0; i < spines; i++)
            {
                float z = (0.22f - i * 0.1f) * k;
                float height = (boss ? 0.2f : 0.12f) * k * (1f - Mathf.Abs(i - spines * 0.5f + 0.5f) / spines);
                Spike(b, new Vector3(0f, 0.49f * k, z), new Vector3(0f, 0.49f * k + height, z - 0.04f * k), 0.022f * k, Accent);
            }

            b.AddTube(new[] { new Vector3(0f, 0.3f * k, 0.3f * k), new Vector3(0f, 0.29f * k, -0.25f * k) }, new[] { 0.012f * k, 0.012f * k }, 4, Glow, false);
            if (boss)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i - 2.5f) * 0.5f;
                    var root = new Vector3(Mathf.Sin(angle) * 0.14f, 0.45f + Mathf.Cos(angle) * 0.06f, 0.2f) * k;
                    Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.18f, 0.2f, -0.1f) * k, 0.03f * k, Dark);
                }
            }

            var tail = new PropMeshBuilder(SlotCount);
            tail.AddTube(new[] { Vector3.zero, new Vector3(0f, 0.08f * k, -0.2f * k), new Vector3(0f, 0.2f * k, -0.38f * k) }, new[] { 0.05f * k, 0.035f * k, 0.01f * k }, 5, Body, false);
            tail.AddBlob(new Vector3(0f, 0.2f * k, -0.38f * k), new Vector3(0.02f, 0.02f, 0.04f) * k, 3, 6, 0f, 0, Accent, false);
            parts.Moving.Add(new CreaturePart(CreatureRole.Tail, tail.ToMesh("Tail"), new Vector3(0f, 0.36f * k, -0.38f * k)));

            Mandibles(parts, new Vector3(0.05f * k, 0.36f * k, 0.5f * k), 0.12f * k, 0.022f * k, Dark);
            StraightLegs(parts, 0.3f * k, 0.1f * k, 0.3f * k, 0.03f * k, 0.22f * k, -0.22f * k);
            parts.StraightLegs = true;
            parts.Skin = CreatureSkin.Kind.Hide;
            parts.Height = (boss ? 0.85f : 0.62f) * k;
        }

        // The energy ball: a bright core with two rings turning around it and a few motes in orbit, hanging
        // above the road. The core wisp is a sun of rings with satellites.
        private static void Wisp(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.6f : 1f;
            // A plasma shell over a white-hot core, so the skin's threads show through the glow.
            b.AddBlob(Vector3.zero, Vector3.one * 0.1f * k, 6, 12, 0f, 0, Accent, false);
            b.AddBlob(Vector3.zero, Vector3.one * 0.19f * k, 6, 14, 0.1f, 18, Body, false);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 0.4f;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle * 2.3f) * 0.6f, Mathf.Sin(angle)).normalized;
                b.AddBlob(dir * 0.17f * k, Vector3.one * 0.05f * k, 3, 6, 0f, 0, Glow, false);
            }
            if (boss)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI * 0.25f;
                    var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle * 1.7f) * 0.5f, Mathf.Sin(angle)).normalized;
                    Spike(b, dir * 0.15f * k, dir * 0.32f * k, 0.03f * k, Accent);
                }
            }

            int rings = boss ? 3 : 2;
            for (int i = 0; i < rings; i++)
            {
                var ring = new PropMeshBuilder(SlotCount);
                float radius = (0.24f + i * 0.07f) * k;
                ring.AddTorus(Vector3.zero, Vector3.up, radius, 0.024f * k, 24, 6, i % 2 == 0 ? Dark : Glow);
                for (int m = 0; m < 3; m++)
                {
                    float angle = m * Mathf.PI * 2f / 3f + i;
                    ring.AddBlob(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius), Vector3.one * 0.03f * k, 3, 6, 0f, 0, Accent, false);
                }

                parts.Moving.Add(new CreaturePart(CreatureRole.Orb, ring.ToMesh("Ring"), Vector3.zero, false, i * 1.3f, i));
            }

            parts.HoverHeight = 0.45f;
            parts.Embers = true;
            parts.Skin = CreatureSkin.Kind.Energy;
            parts.Height = 0.5f * k;
        }

        // The ray: a flat diamond of a body with eyes on top, two broad fins that ripple as they beat, gill slits
        // that glow and a long spiked tail that lashes. The sovereign is wider, with blades on the tail.
        private static void Skyray(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.6f : 1f;
            b.AddBlob(Vector3.zero, new Vector3(0.14f, 0.06f, 0.28f) * k, 5, 10, 0.02f, 19, Body, false);
            b.AddBlob(new Vector3(0f, 0.03f * k, 0.05f * k), new Vector3(0.1f, 0.04f, 0.18f) * k, 3, 9, 0.02f, 20, Dark, true, Mathf.PI * 0.5f);
            b.AddBlob(new Vector3(0.05f * k, 0.05f * k, 0.16f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            b.AddBlob(new Vector3(-0.05f * k, 0.05f * k, 0.16f * k), Vector3.one * 0.025f * k, 3, 6, 0f, 0, Eyes, false);
            for (int i = 0; i < 3; i++)
            {
                float z = (0.06f - i * 0.06f) * k;
                b.AddBlob(new Vector3(0.09f * k, -0.02f * k, z), new Vector3(0.03f, 0.012f, 0.012f) * k, 2, 5, 0f, 0, Accent, false);
                b.AddBlob(new Vector3(-0.09f * k, -0.02f * k, z), new Vector3(0.03f, 0.012f, 0.012f) * k, 2, 5, 0f, 0, Accent, false);
            }

            b.AddTube(new[] { new Vector3(0f, 0.06f * k, 0.2f * k), new Vector3(0f, 0.07f * k, -0.2f * k) }, new[] { 0.012f * k, 0.008f * k }, 4, Glow, false);
            if (boss)
            {
                Spike(b, new Vector3(0f, 0.06f * k, 0.1f * k), new Vector3(0f, 0.3f * k, -0.02f * k), 0.03f * k, Accent);
            }

            Fins(parts, new Vector3(0.1f * k, 0f, 0.02f * k), (boss ? 0.8f : 0.6f) * k, 0.45f * k, 0);
            if (boss)
            {
                Fins(parts, new Vector3(0.08f * k, 0f, -0.16f * k), 0.45f * k, 0.25f * k, 1);
            }

            WhipTail(parts, new Vector3(0f, 0f, -0.26f * k), (boss ? 0.95f : 0.7f) * k, 0.03f * k, boss);
            parts.LegsDangle = true;
            parts.Skin = CreatureSkin.Kind.Scales;
            parts.Height = 0.25f * k;
        }

        // The slug: a long soft body on a wide slimy foot, a humped mantle that breathes, two eyestalks and a lit
        // underside where the slime seeps out. The monarch is a hill of a slug with spines and bubbles.
        private static void Oozer(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.5f : 1f;
            b.AddBlob(new Vector3(0f, 0.09f * k, 0f), new Vector3(0.2f, 0.07f, 0.4f) * k, 4, 10, 0.03f, 21, Dark, true);
            b.AddBlob(new Vector3(0f, 0.06f * k, 0f), new Vector3(0.22f, 0.025f, 0.42f) * k, 2, 10, 0.02f, 22, Accent, true);
            b.AddBlob(new Vector3(0f, 0.16f * k, 0.12f * k), new Vector3(0.17f, 0.13f, 0.28f) * k, 5, 10, 0.05f, 23, Body, false);
            b.AddBlob(new Vector3(0f, 0.14f * k, 0.36f * k), new Vector3(0.11f, 0.09f, 0.12f) * k, 4, 8, 0.03f, 24, Body, false);
            b.AddBlob(new Vector3(0f, 0.08f * k, 0.44f * k), new Vector3(0.07f, 0.03f, 0.05f) * k, 3, 6, 0f, 0, Glow, false);
            if (boss)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i - 2.5f) * 0.42f;
                    var root = new Vector3(Mathf.Sin(angle) * 0.12f, 0.24f, 0.14f) * k;
                    Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.14f, 0.2f, -0.02f) * k, 0.025f * k, Dark);
                }
            }

            var mantle = new PropMeshBuilder(SlotCount);
            mantle.AddBlob(new Vector3(0f, 0f, -0.12f * k), new Vector3(0.2f, 0.16f, 0.3f) * k, 5, 10, 0.06f, 25, Body, false);
            mantle.AddBlob(new Vector3(0f, 0.06f * k, -0.1f * k), new Vector3(0.16f, 0.09f, 0.24f) * k, 3, 10, 0.04f, 26, Dark, true, Mathf.PI * 0.5f);
            for (int i = 0; i < (boss ? 9 : 5); i++)
            {
                float angle = i * 1.3f;
                mantle.AddBlob(new Vector3(Mathf.Cos(angle) * 0.14f, 0.05f + (i % 3) * 0.03f, -0.12f + Mathf.Sin(angle) * 0.2f) * k, Vector3.one * (0.025f + (i % 2) * 0.012f) * k, 3, 6, 0f, 0, Accent, false);
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Abdomen, mantle.ToMesh("Mantle"), new Vector3(0f, 0.2f * k, -0.08f * k)));

            EyeStalks(parts, new Vector3(0.05f * k, 0.2f * k, 0.4f * k), 0.2f * k);
            parts.Bleeds = true;
            parts.Skin = CreatureSkin.Kind.Slime;
            parts.Height = (boss ? 0.6f : 0.4f) * k;
        }

        // The roller: an armadillo-like digger whose shell is a row of domed bands that close into a ball. The king
        // wears spikes on every band and a crest on its head.
        private static void Roller(PropMeshBuilder b, Parts parts, bool boss)
        {
            float k = boss ? 2.3f : 1f;
            b.AddBlob(new Vector3(0f, 0.22f * k, 0f), new Vector3(0.2f, 0.14f, 0.42f) * k, 5, 9, 0.04f, 12, Body, false);

            Mesh band = ShellBand(0.3f * k, 0.2f * k, 0.075f * k, 13, boss);
            var head = new PropMeshBuilder(SlotCount);
            ShellBand(head, 0.28f * k, 0.2f * k, 0.07f * k, 14, false);
            head.AddBlob(new Vector3(0f, 0.18f * k, 0.12f * k), new Vector3(0.14f, 0.1f, 0.16f) * k, 4, 8, 0.04f, 15, Body, false);
            head.AddBlob(new Vector3(0f, 0.15f * k, 0.26f * k), new Vector3(0.08f, 0.05f, 0.08f) * k, 3, 7, 0.02f, 16, Dark, true);
            EyeCluster(head, new Vector3(0f, 0.22f * k, 0.24f * k), 0.025f * k);
            if (boss)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = (i - 1) * 0.12f * k;
                    Spike(head, new Vector3(x, 0.44f * k, 0f), new Vector3(x * 1.5f, 0.62f * k, 0.04f * k), 0.03f * k, Accent);
                }
            }

            var tail = new PropMeshBuilder(SlotCount);
            ShellBand(tail, 0.26f * k, 0.2f * k, 0.06f * k, 17, boss);
            tail.AddTube(new[] { new Vector3(0f, 0.2f * k, 0f), new Vector3(0f, 0.16f * k, -0.2f * k), new Vector3(0f, 0.1f * k, -0.36f * k) }, new[] { 0.06f * k, 0.04f * k, 0f }, 6, Dark, false);

            parts.Moving.Add(new CreaturePart(CreatureRole.Band, head.ToMesh("RollerHead"), new Vector3(0f, 0f, 0.4f * k), false, 0f, 0));
            for (int i = 0; i < 5; i++)
            {
                parts.Moving.Add(new CreaturePart(CreatureRole.Band, band, new Vector3(0f, 0f, (0.28f - i * 0.14f) * k), false, 0f, i + 1));
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Band, tail.ToMesh("RollerTail"), new Vector3(0f, 0f, -0.45f * k), false, 0f, 6));
            Legs(parts, 0.2f * k, 0.16f * k, 0.24f * k, 0.03f * k, 0.22f * k, 0f, -0.22f * k);
            parts.BallRadius = 0.32f * k;
            parts.Height = (boss ? 0.7f : 0.55f) * k;
        }

        // Boss: a swollen abdomen full of glowing eggs on long legs, a crowned head, jaws and grasping arms.
        private static void Queen(PropMeshBuilder b, Parts parts)
        {
            b.AddBlob(new Vector3(0f, 1.0f, 0.05f), new Vector3(0.45f, 0.4f, 0.45f), 5, 10, 0.06f, 18, Body, false);
            b.AddBlob(new Vector3(0f, 1.25f, 0.05f), new Vector3(0.4f, 0.15f, 0.4f), 3, 10, 0.05f, 19, Dark, true, Mathf.PI * 0.5f);
            b.AddBlob(new Vector3(0f, 1.1f, 0.65f), new Vector3(0.28f, 0.24f, 0.28f), 5, 10, 0.04f, 20, Body, false);
            EyeCluster(b, new Vector3(0f, 1.16f, 0.9f), 0.055f);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 0.4f;
                var root = new Vector3(Mathf.Sin(angle) * 0.22f, 1.3f, 0.65f);
                Spike(b, root, root + new Vector3(Mathf.Sin(angle) * 0.2f, 0.45f, 0f), 0.05f, Accent);
            }

            var abdomen = new PropMeshBuilder(SlotCount);
            abdomen.AddBlob(new Vector3(0f, 0f, -0.5f), new Vector3(0.7f, 0.6f, 0.9f), 6, 12, 0.07f, 17, Body, false);
            for (int i = 0; i < 3; i++)
            {
                abdomen.AddTorus(new Vector3(0f, 0f, -0.3f - i * 0.3f), Vector3.forward, 0.66f - i * 0.1f, 0.025f, 18, 4, Dark);
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 0.8f;
                abdomen.AddBlob(new Vector3(Mathf.Cos(angle) * 0.42f, 0.2f + (i % 3) * 0.12f, -0.5f + Mathf.Sin(angle) * 0.5f), Vector3.one * 0.13f, 4, 7, 0f, 0, Glow, false);
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Abdomen, abdomen.ToMesh("Abdomen"), new Vector3(0f, 0.95f, -0.3f)));

            Mandibles(parts, new Vector3(0.12f, 1.0f, 0.9f), 0.3f, 0.045f, Dark);
            RaptorialArms(parts, new Vector3(0.4f, 1.0f, 0.3f), 0.6f, 0.045f, 1);
            Legs(parts, 0.85f, 0.42f, 0.75f, 0.07f, 0.35f, 0f, -0.4f);
            parts.Height = 2.0f;
            parts.ScaleFactor = BossScaleFactor;
        }

        // Two main eyes with a pair of smaller ones above and outside them: a cluster, not a face.
        private static void EyeCluster(PropMeshBuilder b, Vector3 centre, float size)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBlob(centre + new Vector3(side * size * 1.4f, 0f, 0f), Vector3.one * size, 3, 6, 0f, 0, Eyes, false);
                b.AddBlob(centre + new Vector3(side * size * 2.6f, size * 1.1f, -size * 0.6f), Vector3.one * size * 0.5f, 3, 5, 0f, 0, Eyes, false);
            }
        }

        private static void Spike(PropMeshBuilder b, Vector3 from, Vector3 to, float radius, int slot) =>
            b.AddTube(new[] { from, to }, new[] { radius, 0f }, 5, slot, true);

        // Glowing fissures over a rounded surface: short tubes that hug the blob's radii.
        private static void Cracks(PropMeshBuilder b, Vector3 centre, Vector3 radii, int count, int seed, int slot)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float theta = (float)random.NextDouble() * Mathf.PI * 2f;
                float phi = 0.3f + (float)random.NextDouble() * 1.2f;
                var path = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    float t = theta + (k - 1) * 0.35f;
                    float p = phi + (k - 1) * 0.15f * (i % 2 == 0 ? 1f : -1f);
                    var direction = new Vector3(Mathf.Sin(p) * Mathf.Cos(t), Mathf.Cos(p), Mathf.Sin(p) * Mathf.Sin(t));
                    path[k] = centre + Vector3.Scale(direction, radii) * 0.97f;
                }

                b.AddTube(path, new[] { radii.x * 0.08f, radii.x * 0.2f, radii.x * 0.08f }, 4, slot, false);
            }
        }

        // A domed slice of shell across the body with a glowing rim along its front edge; a king's band carries a spike.
        private static Mesh ShellBand(float radius, float lift, float thickness, int seed, bool spiked)
        {
            var b = new PropMeshBuilder(SlotCount);
            ShellBand(b, radius, lift, thickness, seed, spiked);
            return b.ToMesh("Band");
        }

        private static void ShellBand(PropMeshBuilder b, float radius, float lift, float thickness, int seed, bool spiked)
        {
            b.AddBlob(new Vector3(0f, lift, 0f), new Vector3(radius, radius, thickness), 5, 12, 0.04f, seed, Dark, true, Mathf.PI * 0.62f);
            SeamArc(b, radius * 1.02f, lift, thickness * 0.9f, radius * 0.04f);
            if (spiked)
            {
                Spike(b, new Vector3(0f, lift + radius * 0.95f, 0f), new Vector3(0f, lift + radius * 1.5f, 0f), radius * 0.1f, Accent);
            }
        }

        // A thin glowing arc over the back, from one flank to the other.
        private static void SeamArc(PropMeshBuilder b, float radius, float lift, float z, float thickness)
        {
            const int Steps = 10;
            var path = new Vector3[Steps + 1];
            var radii = new float[Steps + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float a = Mathf.Lerp(-1.75f, 1.75f, i / (float)Steps);
                path[i] = new Vector3(Mathf.Sin(a) * radius, lift + Mathf.Cos(a) * radius, z);
                radii[i] = thickness;
            }

            b.AddTube(path, radii, 4, Glow, false);
        }

        // Hip and knee segments of one leg, built for the right side and mirrored for the left. Hip, knee and foot
        // follow an insect's leg: out and up first, then down to the ground, ending in a claw.
        private static void Legs(Parts parts, float hipY, float width, float reach, float radius, params float[] zs)
        {
            LegSet set = BuildLeg(hipY, reach, radius);
            for (int i = 0; i < zs.Length; i++)
            {
                float phase = (i % 2) * Mathf.PI;
                parts.Moving.Add(new CreaturePart(CreatureRole.Leg, set.Upper, new Vector3(width, hipY, zs[i]), false, phase, i, set.Lower, set.Knee));
                parts.Moving.Add(new CreaturePart(CreatureRole.Leg, set.Upper, new Vector3(-width, hipY, zs[i]), true, phase + Mathf.PI, i, set.Lower, set.Knee));
            }
        }

        // Legs are stocky on purpose: the camera looks down at 55 degrees, so a long thin leg pointing at it reads as a stick.
        private static LegSet BuildLeg(float hipY, float reach, float radius)
        {
            reach *= LegReachShare;
            radius *= LegThickness;
            float drop = Mathf.Max(hipY, reach * 0.6f);
            var knee = new Vector3(reach * 0.6f, drop * 0.45f, 0f);

            var upper = new PropMeshBuilder(SlotCount);
            upper.AddTube(new[] { Vector3.zero, knee * 0.5f, knee }, new[] { radius, radius * 0.95f, radius * 0.8f }, 6, Dark, false);
            upper.AddBlob(knee, Vector3.one * radius * 1.1f, 3, 6, 0f, 0, Body, false);

            var lower = new PropMeshBuilder(SlotCount);
            var foot = new Vector3(reach * 0.35f, -drop * 1.45f, 0f);
            lower.AddTube(new[] { Vector3.zero, new Vector3(reach * 0.3f, -drop * 0.75f, 0f), foot }, new[] { radius * 0.8f, radius * 0.6f, radius * 0.28f }, 6, Dark, false);
            lower.AddTube(new[] { foot, foot + new Vector3(reach * 0.08f, -drop * 0.02f, reach * 0.14f) }, new[] { radius * 0.3f, 0f }, 4, Dark, true);
            return new LegSet(upper.ToMesh("LegUpper"), lower.ToMesh("LegLower"), knee);
        }

        // A runner's or a walker's leg: the thigh drops from the hip, the shin from the knee, and the paw turns forward.
        private static void StraightLegs(Parts parts, float hipY, float width, float length, float radius, params float[] zs)
        {
            radius *= LegThickness;
            var knee = new Vector3(0f, -length * 0.5f, length * 0.08f);
            var upper = new PropMeshBuilder(SlotCount);
            upper.AddTube(new[] { Vector3.zero, knee }, new[] { radius * 1.15f, radius * 0.85f }, 6, Body, false);
            upper.AddBlob(knee, Vector3.one * radius * 1.05f, 3, 6, 0f, 0, Dark, true);

            var lower = new PropMeshBuilder(SlotCount);
            var ankle = new Vector3(0f, -length * 0.48f, -length * 0.06f);
            lower.AddTube(new[] { Vector3.zero, ankle }, new[] { radius * 0.8f, radius * 0.55f }, 6, Dark, false);
            lower.AddTube(new[] { ankle, ankle + new Vector3(0f, -length * 0.02f, length * 0.22f) }, new[] { radius * 0.6f, radius * 0.1f }, 5, Dark, true);

            Mesh upperMesh = upper.ToMesh("LegUpper");
            Mesh lowerMesh = lower.ToMesh("LegLower");
            for (int i = 0; i < zs.Length; i++)
            {
                float phase = (i % 2) * Mathf.PI;
                parts.Moving.Add(new CreaturePart(CreatureRole.Leg, upperMesh, new Vector3(width, hipY, zs[i]), false, phase, i, lowerMesh, knee));
                parts.Moving.Add(new CreaturePart(CreatureRole.Leg, upperMesh, new Vector3(-width, hipY, zs[i]), true, phase + Mathf.PI, i, lowerMesh, knee));
            }
        }

        // A pair of jaws: curved pincers that meet in front of the mouth, built right and mirrored left.
        private static void Mandibles(Parts parts, Vector3 pivot, float length, float radius, int slot)
        {
            var b = new PropMeshBuilder(SlotCount);
            b.AddTube(
                new[] { Vector3.zero, new Vector3(length * 0.25f, -length * 0.05f, length * 0.55f), new Vector3(-length * 0.12f, -length * 0.12f, length) },
                new[] { radius, radius * 0.75f, 0f }, 5, slot, false);
            Mesh mesh = b.ToMesh("Mandible");
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, pivot, false, 0f, 0));
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, new Vector3(-pivot.x, pivot.y, pivot.z), true, 0f, 0));
        }

        // A scorpion's pincer: an arm forward and a heavy claw with a lit fixed finger and a hinged one.
        private static void Pincers(Parts parts, Vector3 pivot, float length, float radius)
        {
            var b = new PropMeshBuilder(SlotCount);
            var wrist = new Vector3(length * 0.2f, 0f, length * 0.45f);
            b.AddTube(new[] { Vector3.zero, wrist }, new[] { radius, radius * 0.9f }, 5, Dark, false);
            b.AddBlob(wrist + new Vector3(0f, 0f, length * 0.12f), new Vector3(radius * 1.8f, radius * 1.3f, length * 0.2f), 4, 7, 0.03f, 27, Body, false);
            b.AddTube(new[] { wrist + new Vector3(radius * 0.8f, 0f, length * 0.25f), wrist + new Vector3(radius * 0.3f, 0f, length * 0.6f) }, new[] { radius * 0.7f, 0f }, 4, Dark, false);
            b.AddTube(new[] { wrist + new Vector3(-radius * 0.8f, 0f, length * 0.25f), wrist + new Vector3(-radius * 0.3f, 0f, length * 0.6f) }, new[] { radius * 0.7f, 0f }, 4, Accent, false);
            Mesh mesh = b.ToMesh("Pincer");
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, pivot, false, 0f, 0));
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, new Vector3(-pivot.x, pivot.y, pivot.z), true, 0f, 0));
        }

        // A segmented tail that rises from the rear and arches forward over the back, ending in a glowing sting
        // above the head. Index zero snaps forward on a strike; a side tail leans outward.
        private static void StingTail(Parts parts, Vector3 pivot, float length, float radius, int index, float lean)
        {
            var b = new PropMeshBuilder(SlotCount);
            const int Segments = 6;
            var path = new Vector3[Segments + 1];
            var radii = new float[Segments + 1];
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                float angle = Mathf.Lerp(-0.15f, 2.55f, t);
                path[i] = new Vector3(lean * t * length * 0.6f, Mathf.Sin(angle) * length * 0.8f, (-0.12f + (1f - Mathf.Cos(angle)) * 0.4f) * length);
                radii[i] = radius * (1f - t * 0.45f);
                if (i > 0 && i < Segments)
                {
                    b.AddBlob(path[i], Vector3.one * radius * (1.25f - t * 0.4f), 3, 7, 0.02f, 30 + i, Body, false);
                }
            }

            b.AddTube(path, radii, 6, Dark, false);
            Vector3 tip = path[Segments];
            b.AddBlob(tip, Vector3.one * radius * 1.5f, 4, 7, 0f, 0, Body, false);
            b.AddTube(new[] { tip, tip + new Vector3(0f, -radius * 2f, radius * 3.5f) }, new[] { radius * 0.9f, 0f }, 5, Accent, false);
            parts.Moving.Add(new CreaturePart(CreatureRole.Stinger, b.ToMesh("StingTail"), pivot, false, 0f, index));
        }

        // A straight whip of a tail with spikes along it that lashes sideways on a strike.
        private static void WhipTail(Parts parts, Vector3 pivot, float length, float radius, bool bladed)
        {
            var b = new PropMeshBuilder(SlotCount);
            b.AddTube(new[] { Vector3.zero, new Vector3(0f, 0.01f * length, -length * 0.5f), new Vector3(0f, 0.03f * length, -length) }, new[] { radius, radius * 0.6f, 0f }, 5, Body, false);
            for (int i = 1; i <= 4; i++)
            {
                var at = new Vector3(0f, 0f, -length * 0.2f * i);
                float size = length * (bladed ? 0.12f : 0.07f);
                b.AddTube(new[] { at, at + new Vector3(size, size * 0.3f, 0f) }, new[] { radius * 0.5f, 0f }, 4, Accent, true);
                b.AddTube(new[] { at, at + new Vector3(-size, size * 0.3f, 0f) }, new[] { radius * 0.5f, 0f }, 4, Accent, true);
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Stinger, b.ToMesh("WhipTail"), pivot, false, 0f, 1));
        }

        // A sword arm: the upper arm reaches up and forward from the shoulder, the forearm holds a crystal blade
        // with a lit edge. Built raised, the view brings it down on a strike.
        private static void SwordArm(Parts parts, Vector3 shoulder, float length, float radius, bool mirrored)
        {
            var b = new PropMeshBuilder(SlotCount);
            var elbow = new Vector3(length * 0.18f, length * 0.2f, length * 0.12f);
            var hand = new Vector3(length * 0.2f, length * 0.42f, length * 0.25f);
            b.AddTube(new[] { Vector3.zero, elbow }, new[] { radius * 1.2f, radius }, 5, Body, false);
            b.AddTube(new[] { elbow, hand }, new[] { radius, radius * 0.9f }, 5, Dark, false);
            b.AddBlob(hand, Vector3.one * radius * 1.3f, 3, 6, 0f, 0, Dark, true);
            Vector3 up = new Vector3(0.05f, 0.75f, 0.65f).normalized;
            Vector3 tip = hand + up * length * 0.7f;
            Vector3 side = Vector3.Cross(up, Vector3.right).normalized;
            b.AddBlade(new[] { hand + up * length * 0.05f, hand + up * length * 0.25f, hand + up * length * 0.55f, tip }, new[] { radius * 1.2f, radius * 2.4f, radius * 2f, radius * 0.2f }, side, Accent);
            b.AddTube(new[] { hand + up * length * 0.05f, tip }, new[] { radius * 0.5f, radius * 0.1f }, 4, Dark, false);
            b.AddTube(new[] { hand - side * radius * 2f + up * length * 0.04f, hand + side * radius * 2f + up * length * 0.04f }, new[] { radius * 0.4f, radius * 0.4f }, 4, Dark, false);
            parts.Moving.Add(new CreaturePart(CreatureRole.Blade, b.ToMesh("Sword"), shoulder, mirrored, 0f, mirrored ? 1 : 0));
        }

        // One armour plate hung on the body: dark shell with a lit rim.
        private static void ArmorPlate(Parts parts, int index, Vector3 pivot, Vector3 radii, bool mirrored, int seed, Quaternion? turn = null)
        {
            var b = new PropMeshBuilder(SlotCount);
            b.AddBlob(Vector3.zero, radii, 4, 9, 0.03f, seed, Dark, true, Mathf.PI * 0.55f);
            SeamArc(b, Mathf.Max(radii.x, radii.z) * 0.9f, 0f, 0f, radii.y * 0.25f);
            Mesh mesh = b.ToMesh("Plate");
            if (turn.HasValue)
            {
                mesh = Turned(mesh, turn.Value);
            }

            parts.Moving.Add(new CreaturePart(CreatureRole.Plate, mesh, pivot, mirrored, 0f, index));
        }

        // A mesh rotated in place, for parts that are built upright but hang sideways.
        private static Mesh Turned(Mesh mesh, Quaternion rotation)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotation * vertices[i];
                normals[i] = rotation * normals[i];
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        // Folded grasping forelegs with spines along the inner edge, like a mantis.
        private static void RaptorialArms(Parts parts, Vector3 pivot, float length, float radius, int index = 0)
        {
            var b = new PropMeshBuilder(SlotCount);
            var elbow = new Vector3(length * 0.25f, length * 0.35f, length * 0.4f);
            var tip = new Vector3(length * 0.05f, -length * 0.1f, length * 0.95f);
            b.AddTube(new[] { Vector3.zero, elbow }, new[] { radius, radius * 0.8f }, 5, Dark, false);
            b.AddTube(new[] { elbow, Vector3.Lerp(elbow, tip, 0.5f), tip }, new[] { radius * 0.8f, radius * 0.6f, 0f }, 5, Dark, false);
            for (int i = 1; i <= 3; i++)
            {
                Vector3 at = Vector3.Lerp(elbow, tip, i * 0.22f);
                b.AddTube(new[] { at, at + new Vector3(0f, -length * 0.1f, 0f) }, new[] { radius * 0.4f, 0f }, 4, Glow, true);
            }

            Mesh mesh = b.ToMesh("Arm");
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, pivot, false, 0f, index));
            parts.Moving.Add(new CreaturePart(CreatureRole.Mandible, mesh, new Vector3(-pivot.x, pivot.y, pivot.z), true, 0f, index));
        }

        private static void Antennae(Parts parts, Vector3 pivot, float length)
        {
            var b = new PropMeshBuilder(SlotCount);
            b.AddTube(
                new[] { Vector3.zero, new Vector3(length * 0.3f, length * 0.5f, length * 0.6f), new Vector3(length * 0.55f, length * 0.6f, length * 1.1f) },
                new[] { length * 0.06f, length * 0.045f, length * 0.015f }, 4, Dark, false);
            Mesh mesh = b.ToMesh("Antenna");
            parts.Moving.Add(new CreaturePart(CreatureRole.Antenna, mesh, pivot, false, 0f, 0));
            parts.Moving.Add(new CreaturePart(CreatureRole.Antenna, mesh, new Vector3(-pivot.x, pivot.y, pivot.z), true, Mathf.PI * 0.5f, 0));
        }

        // A slug's eyestalks: soft tubes with a glowing eye on the tip, animated like feelers.
        private static void EyeStalks(Parts parts, Vector3 pivot, float length)
        {
            var b = new PropMeshBuilder(SlotCount);
            var tip = new Vector3(length * 0.3f, length * 0.9f, length * 0.4f);
            b.AddTube(new[] { Vector3.zero, new Vector3(length * 0.15f, length * 0.5f, length * 0.25f), tip }, new[] { length * 0.14f, length * 0.1f, length * 0.08f }, 5, Body, false);
            b.AddBlob(tip, Vector3.one * length * 0.14f, 4, 7, 0f, 0, Eyes, false);
            Mesh mesh = b.ToMesh("EyeStalk");
            parts.Moving.Add(new CreaturePart(CreatureRole.Antenna, mesh, pivot, false, 0f, 0));
            parts.Moving.Add(new CreaturePart(CreatureRole.Antenna, mesh, new Vector3(-pivot.x, pivot.y, pivot.z), true, Mathf.PI * 0.5f, 0));
        }

        // A bat's wing: a membrane stretched between lit finger bones, hinged at the shoulder, reaching out along +x.
        private static void MembraneWings(Parts parts, Vector3 hinge, float length, float width, int index)
        {
            var b = new PropMeshBuilder(SlotCount);
            var spine = new[] { Vector3.zero, new Vector3(length * 0.4f, 0.02f * length, 0.03f * length), new Vector3(length, 0.01f * length, -0.02f * length) };
            b.AddBlade(spine, new[] { width * 0.5f, width, width * 0.15f }, Vector3.forward, Dark);
            b.AddTube(spine, new[] { length * 0.03f, length * 0.022f, length * 0.006f }, 4, Body, false);
            for (int i = 1; i <= 3; i++)
            {
                var root = new Vector3(length * 0.4f, 0.02f * length, 0.03f * length);
                var tip = new Vector3(length * (0.55f + i * 0.14f), 0.015f * length, -width * (0.5f - i * 0.12f));
                b.AddTube(new[] { root, tip }, new[] { length * 0.014f, length * 0.004f }, 4, Accent, false);
            }

            Mesh mesh = b.ToMesh("Wing");
            float phase = index * 0.6f;
            parts.Moving.Add(new CreaturePart(CreatureRole.Wing, mesh, hinge, false, phase, index));
            parts.Moving.Add(new CreaturePart(CreatureRole.Wing, mesh, new Vector3(-hinge.x, hinge.y, hinge.z), true, phase, index));
        }

        // A ray's fin: a wide triangle of body that beats slowly, with a lit edge.
        private static void Fins(Parts parts, Vector3 hinge, float length, float width, int index)
        {
            var b = new PropMeshBuilder(SlotCount);
            var spine = new[] { Vector3.zero, new Vector3(length * 0.45f, 0f, -width * 0.05f), new Vector3(length, 0f, -width * 0.25f) };
            b.AddBlade(spine, new[] { width, width * 0.75f, width * 0.08f }, Vector3.forward, Body);
            b.AddTube(new[] { new Vector3(0f, 0.005f * length, width * 0.5f), new Vector3(length * 0.5f, 0.005f * length, width * 0.32f), new Vector3(length, 0.005f * length, -width * 0.21f) }, new[] { length * 0.015f, length * 0.012f, length * 0.004f }, 4, Accent, false);
            Mesh mesh = b.ToMesh("Fin");
            parts.Moving.Add(new CreaturePart(CreatureRole.Fin, mesh, hinge, false, index * 0.5f, index));
            parts.Moving.Add(new CreaturePart(CreatureRole.Fin, mesh, new Vector3(-hinge.x, hinge.y, hinge.z), true, index * 0.5f, index));
        }
    }
}
