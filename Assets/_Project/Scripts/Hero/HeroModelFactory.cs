using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander from code: a tall human warrior in a steel-blue quilted pressure suit with bone-white
    /// armour on chest, forearms, thighs and shins, dark gunmetal at the joints, gloves, boots, belt and pack, blue
    /// pauldrons rimmed in the commander's colour, a short cloak off the shoulders, and a white helmet that sits
    /// in a lit neck ring on the collar. Heroic proportions: long legs, a narrow waist, wide shoulders and a small
    /// head. Everything is built from rounded forms so the figure reads as a body, not a stack of crates.
    /// The figure is jointed so the view can give it a living stance: a torso that pivots at the pelvis and carries
    /// the head, arms and cloak with it, legs with knees, arms with elbows. Weapons come from
    /// <see cref="HeroWeaponModelFactory"/> and hang from the right hand, the pack or the hip.
    /// Submesh slots: armour (white plates), trim glow, visor glow, dark (the suit), metal (joints and gear).
    /// </summary>
    public static class HeroModelFactory
    {
        public const int Armour = 0;
        public const int Trim = 1;
        public const int Visor = 2;
        public const int Dark = 3;
        public const int Metal = 4;
        public const int SlotCount = 5;

        // White plates over a steel-blue suit with gunmetal gear and the commander's own colour in the lights:
        // three clearly different values, so suit, armour and weapons read apart even at a distance.
        private static readonly Color ArmourColor = new Color(0.93f, 0.94f, 0.92f);
        private static readonly Color SuitColor = new Color(0.3f, 0.38f, 0.54f);
        private static readonly Color MetalColor = new Color(0.17f, 0.19f, 0.23f);
        private static readonly Color VisorColor = new Color(0.3f, 0.95f, 1f);
        private static readonly Color DeadArmourColor = new Color(0.45f, 0.47f, 0.5f);
        private static readonly Color DeadSuitColor = new Color(0.18f, 0.2f, 0.26f);
        private static readonly Color DeadVisorColor = new Color(0.08f, 0.1f, 0.12f);
        private const float ArmourGlow = 0.6f;
        private const float ShoulderBallRadius = 0.085f;
        // The boot hangs this far below the ankle (its blob sits 0.03 under the ankle with a half height of 0.065).
        private const float BootDepth = 0.095f;
        private const float SuitSeamGlow = 0.45f;

        /// <summary>The meshes of the figure and where its joints sit. Right-hand parts are mirrored for the left.</summary>
        public sealed class Parts
        {
            public Mesh Body;
            public Mesh Head;
            public Mesh ArmUpper;
            public Mesh ArmLower;
            public Mesh LegUpper;
            public Mesh LegLower;
            public Mesh Cloak;

            /// <summary>Where the torso bends: the pelvis, in figure space. Head, arms and cloak move with it.</summary>
            public Vector3 Pelvis;

            /// <summary>Right shoulder, in figure space; the left is mirrored in x.</summary>
            public Vector3 Shoulder;

            /// <summary>Elbow, in the space of the upper arm.</summary>
            public Vector3 Elbow;

            /// <summary>Right hip, in figure space; the left is mirrored in x.</summary>
            public Vector3 Hip;

            /// <summary>Knee, in the space of the thigh.</summary>
            public Vector3 Knee;

            /// <summary>Ankle, in the space of the shin.</summary>
            public Vector3 Ankle;

            /// <summary>Where the head turns: the base of the neck, inside the collar.</summary>
            public Vector3 Neck;

            public Vector3 Thruster;

            /// <summary>Where the cloak hangs from, in figure space.</summary>
            public Vector3 Nape;

            /// <summary>How far the figure stands above its origin so the boot soles rest on the ground instead of in it.</summary>
            public float SoleLift;

            /// <summary>Where a weapon grip sits in the hand, in the space of the forearm.</summary>
            public Vector3 Hand;

            /// <summary>Where a long weapon clips to the pack, in the space of the body.</summary>
            public Vector3 BackHolster;

            /// <summary>Where a short weapon hangs at the hip, in the space of the body.</summary>
            public Vector3 HipHolster;
        }

        /// <summary>The suit materials; powered down, the lights are out and the plates are dull.</summary>
        public static Material[] CreateMaterials(Color trim, bool powered = true)
        {
            Color seam = powered ? trim : Color.black;
            var materials = new Material[SlotCount];
            // Every surface wears a skin: panelled ceramic on the plates (self-lit a little, so they stay readable
            // in dark levels and in the shadow of a hill, with the seams in the commander's colour), quilting on
            // the suit with lit stitching, brushed riveted metal on the gear.
            Color plateGlow = powered ? Color.Lerp(ArmourColor, trim, 0.4f) * ArmourGlow : Color.black;
            materials[Armour] = CreatureSkin.Dress(SurfaceMaterials.Create(powered ? ArmourColor : DeadArmourColor, 0f, powered ? 0.55f : 0.3f, 0.25f), CreatureSkin.Kind.Plate, plateGlow);
            materials[Trim] = SurfaceMaterials.Create(powered ? trim : Color.Lerp(trim, MetalColor, 0.6f), powered ? 1.2f : 0f, 0.5f);
            materials[Visor] = SurfaceMaterials.Create(powered ? VisorColor : DeadVisorColor, powered ? 3f : 0f, 0.7f);
            materials[Dark] = CreatureSkin.Dress(SurfaceMaterials.Create(powered ? SuitColor : DeadSuitColor, 0f, 0.4f, 0.1f), CreatureSkin.Kind.Weave, seam * SuitSeamGlow);
            materials[Metal] = CreatureSkin.Dress(SurfaceMaterials.Create(MetalColor, 0f, 0.65f, 0.8f), CreatureSkin.Kind.Alloy, Color.black);
            return materials;
        }

        public static Parts Create()
        {
            var parts = new Parts
            {
                Pelvis = new Vector3(0f, 0.74f, 0f),
                Shoulder = new Vector3(0.31f, 1.24f, 0.03f),
                Elbow = new Vector3(0.03f, -0.26f, 0.02f),
                Hip = new Vector3(0.14f, 0.72f, 0f),
                Knee = new Vector3(0f, -0.36f, 0.02f),
                Ankle = new Vector3(0f, -0.34f, -0.01f),
                Neck = new Vector3(0f, 1.28f, 0.04f),
                Thruster = new Vector3(0f, 0.88f, -0.28f),
                Nape = new Vector3(0f, 1.26f, -0.1f),
                Hand = new Vector3(0f, -0.08f, 0.24f),
                BackHolster = new Vector3(0.18f, 1.08f, -0.3f),
                HipHolster = new Vector3(0.29f, 0.74f, 0.1f),
            };

            parts.SoleLift = BootDepth - (parts.Hip.y + parts.Knee.y + parts.Ankle.y);
            parts.Body = Body();
            parts.Head = Head();
            parts.ArmUpper = ArmUpper(parts.Elbow);
            parts.ArmLower = ArmLower(parts.Hand);
            parts.LegUpper = LegUpper(parts.Knee);
            parts.LegLower = LegLower(parts.Ankle);
            parts.Cloak = Cloak();
            return parts;
        }

        private static PropMeshBuilder Builder() => new PropMeshBuilder(SlotCount);

        // Pelvis, belt and pouches, a narrow suited waist, a rounded chest plate with its ridge and lights, a collar
        // with a lit neck ring, blue pauldrons rimmed in light (heavy on the sword side), the pack with nozzles and
        // antenna, and a holster.
        private static Mesh Body()
        {
            PropMeshBuilder b = Builder();
            b.AddBlob(new Vector3(0f, 0.74f, 0f), new Vector3(0.19f, 0.09f, 0.13f), 4, 9, 0.02f, 3, Dark, false);
            b.AddTorus(new Vector3(0f, 0.8f, 0f), Vector3.up, 0.18f, 0.025f, 14, 5, Metal);
            b.AddBlob(new Vector3(0f, 0.8f, 0.17f), new Vector3(0.035f, 0.025f, 0.015f), 3, 6, 0f, 0, Trim, false);
            b.AddBlob(new Vector3(0.16f, 0.75f, 0.12f), new Vector3(0.05f, 0.06f, 0.04f), 3, 7, 0.02f, 11, Metal, false);
            b.AddBlob(new Vector3(-0.16f, 0.75f, 0.12f), new Vector3(0.05f, 0.06f, 0.04f), 3, 7, 0.02f, 12, Metal, false);
            b.AddTube(new[] { new Vector3(0f, 0.8f, 0f), new Vector3(0f, 0.9f, 0.01f), new Vector3(0f, 1.02f, 0.03f) }, new[] { 0.15f, 0.15f, 0.2f }, 10, Dark, false);

            // The chest: a broad rounded plate, a lit ridge down the middle, two lights, and a back plate.
            b.AddBlob(new Vector3(0f, 1.1f, 0.04f), new Vector3(0.25f, 0.2f, 0.17f), 5, 10, 0.02f, 4, Armour, false);
            b.AddBlob(new Vector3(0f, 1.1f, 0.18f), new Vector3(0.025f, 0.14f, 0.03f), 3, 6, 0f, 0, Trim, false);
            b.AddBlob(new Vector3(0.1f, 1.17f, 0.19f), Vector3.one * 0.02f, 3, 6, 0f, 0, Visor, false);
            b.AddBlob(new Vector3(-0.1f, 1.17f, 0.19f), Vector3.one * 0.02f, 3, 6, 0f, 0, Visor, false);
            b.AddBlob(new Vector3(0f, 1.1f, -0.08f), new Vector3(0.2f, 0.17f, 0.08f), 4, 9, 0.02f, 5, Armour, false);

            // The collar: a gunmetal ring the helmet sits in, lit along its rim.
            b.AddTube(new[] { new Vector3(0f, 1.2f, 0.04f), new Vector3(0f, 1.3f, 0.04f) }, new[] { 0.13f, 0.12f }, 10, Metal, false);
            b.AddTorus(new Vector3(0f, 1.3f, 0.04f), Vector3.up, 0.115f, 0.022f, 12, 5, Trim);

            // The shoulders: a suited yoke either side of the chest that the pauldrons sit on and the arm balls seat
            // in, so the arms grow out of the body instead of hanging beside it.
            b.AddBlob(new Vector3(0.26f, 1.2f, 0.03f), new Vector3(0.11f, 0.09f, 0.11f), 4, 9, 0.02f, 18, Dark, false);
            b.AddBlob(new Vector3(-0.26f, 1.2f, 0.03f), new Vector3(0.11f, 0.09f, 0.11f), 4, 9, 0.02f, 19, Dark, false);

            // Pauldrons in the suit's blue with a lit rim: a heavy one over the sword shoulder, a smaller one opposite.
            b.AddBlob(new Vector3(0.39f, 1.27f, 0.03f), new Vector3(0.14f, 0.1f, 0.15f), 5, 10, 0.02f, 7, Dark, false, Mathf.PI * 0.62f);
            b.AddBlob(new Vector3(0.38f, 1.27f, 0.03f), new Vector3(0.12f, 0.1f, 0.13f), 4, 9, 0.02f, 8, Metal, false);
            b.AddTorus(new Vector3(0.39f, 1.3f, 0.03f), Vector3.up, 0.12f, 0.012f, 12, 4, Trim);
            b.AddBlob(new Vector3(0.39f, 1.355f, 0.03f), new Vector3(0.07f, 0.03f, 0.09f), 3, 6, 0f, 0, Armour, false);
            b.AddBlob(new Vector3(-0.37f, 1.25f, 0.03f), new Vector3(0.11f, 0.08f, 0.13f), 4, 9, 0.02f, 9, Dark, false, Mathf.PI * 0.6f);
            b.AddBlob(new Vector3(-0.36f, 1.25f, 0.03f), new Vector3(0.11f, 0.1f, 0.12f), 4, 8, 0.02f, 10, Metal, false);
            b.AddTorus(new Vector3(-0.37f, 1.27f, 0.03f), Vector3.up, 0.1f, 0.012f, 12, 4, Trim);

            // The pack: a gunmetal case, two nozzles that glow, and an antenna.
            b.AddBlob(new Vector3(0f, 1.06f, -0.2f), new Vector3(0.16f, 0.17f, 0.07f), 4, 9, 0.02f, 6, Metal, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { new Vector3(0.08f * side, 0.95f, -0.23f), new Vector3(0.08f * side, 0.87f, -0.26f) }, new[] { 0.04f, 0.05f }, 8, Metal, true);
                b.AddBlob(new Vector3(0.08f * side, 0.87f, -0.26f), new Vector3(0.035f, 0.012f, 0.035f), 3, 8, 0f, 0, Visor, false);
            }

            b.AddBlob(new Vector3(0.18f, 1.08f, -0.26f), new Vector3(0.03f, 0.06f, 0.03f), 3, 6, 0f, 0, Metal, false);
            b.AddTube(new[] { new Vector3(-0.11f, 1.2f, -0.2f), new Vector3(-0.14f, 1.6f, -0.23f) }, new[] { 0.012f, 0.008f }, 4, Metal, false);
            b.AddBlob(new Vector3(-0.14f, 1.62f, -0.23f), Vector3.one * 0.025f, 3, 6, 0f, 0, Trim, false);

            // A holster on the right hip.
            b.AddBlob(new Vector3(0.25f, 0.72f, 0.09f), new Vector3(0.045f, 0.08f, 0.055f), 3, 7, 0.02f, 13, Metal, false);
            return b.ToMesh("Hero_body");
        }

        // The head on its neck: a short gunmetal neck inside the collar ring, a white helmet that sits low in
        // that ring, a wide visor slit, a dark jaw guard, a crest fin and side lamps.
        private static Mesh Head()
        {
            PropMeshBuilder b = Builder();
            b.AddTube(new[] { new Vector3(0f, -0.06f, 0f), new Vector3(0f, 0.07f, 0.01f) }, new[] { 0.095f, 0.1f }, 8, Metal, false);
            b.AddBlob(new Vector3(0f, 0.16f, 0.01f), new Vector3(0.145f, 0.155f, 0.15f), 6, 12, 0.01f, 6, Armour, false);
            b.AddBlob(new Vector3(0f, 0.17f, 0.12f), new Vector3(0.105f, 0.03f, 0.06f), 3, 10, 0f, 0, Visor, false);
            b.AddBlob(new Vector3(0f, 0.08f, 0.08f), new Vector3(0.115f, 0.065f, 0.105f), 4, 9, 0.02f, 14, Metal, false);
            b.AddBlob(new Vector3(0f, 0.1f, 0.16f), new Vector3(0.03f, 0.02f, 0.015f), 3, 6, 0f, 0, Trim, false);
            b.AddBlade(new[] { new Vector3(0f, 0.28f, 0.1f), new Vector3(0f, 0.35f, 0.02f), new Vector3(0f, 0.3f, -0.1f) }, new[] { 0.02f, 0.03f, 0.02f }, Vector3.right, Trim);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBlob(new Vector3(0.14f * side, 0.16f, 0.05f), new Vector3(0.03f, 0.04f, 0.04f), 3, 6, 0f, 0, Metal, false);
                b.AddBlob(new Vector3(0.125f * side, 0.21f, 0.1f), Vector3.one * 0.018f, 3, 6, 0f, 0, Visor, false);
            }

            return b.ToMesh("Hero_head");
        }

        // The upper arm in the suit, from a gunmetal shoulder ball to a gunmetal elbow joint. The ball sits on the
        // pivot, so however far the arm swings or aims it stays seated in the pauldron with no cut end showing.
        private static Mesh ArmUpper(Vector3 elbow)
        {
            PropMeshBuilder b = Builder();
            b.AddBlob(Vector3.zero, Vector3.one * ShoulderBallRadius, 4, 8, 0f, 0, Metal, false);
            b.AddTube(new[] { Vector3.zero, elbow }, new[] { 0.075f, 0.065f }, 8, Dark, false);
            b.AddBlob(elbow, Vector3.one * 0.07f, 4, 8, 0f, 0, Metal, false);
            return b.ToMesh("Hero_arm_upper");
        }

        // The forearm points forward from the elbow (the bend is in the pose), wears a white bracer with a light
        // and ends in a gunmetal glove.
        private static Mesh ArmLower(Vector3 hand)
        {
            PropMeshBuilder b = Builder();
            b.AddTube(new[] { Vector3.zero, hand * 0.85f }, new[] { 0.062f, 0.055f }, 8, Dark, false);
            b.AddBlob(new Vector3(0f, -0.03f, 0.11f), new Vector3(0.085f, 0.075f, 0.09f), 4, 9, 0.02f, 15, Armour, false);
            b.AddBlob(new Vector3(0f, 0.04f, 0.11f), new Vector3(0.02f, 0.01f, 0.06f), 3, 6, 0f, 0, Trim, false);
            b.AddBlob(hand, new Vector3(0.07f, 0.055f, 0.085f), 4, 8, 0.02f, 9, Metal, false);
            return b.ToMesh("Hero_arm_lower");
        }

        // The thigh in the suit with a white plate over the front and a gunmetal knee with a small light.
        private static Mesh LegUpper(Vector3 knee)
        {
            PropMeshBuilder b = Builder();
            b.AddTube(new[] { Vector3.zero, knee }, new[] { 0.095f, 0.08f }, 8, Dark, false);
            b.AddBlob(new Vector3(0f, -0.16f, 0.05f), new Vector3(0.1f, 0.15f, 0.07f), 4, 9, 0.02f, 16, Armour, false);
            b.AddBlob(knee + new Vector3(0f, 0f, 0.05f), new Vector3(0.08f, 0.07f, 0.06f), 3, 7, 0.02f, 1, Metal, false);
            b.AddBlob(knee + new Vector3(0f, 0f, 0.1f), new Vector3(0.03f, 0.03f, 0.015f), 3, 6, 0f, 0, Trim, false);
            return b.ToMesh("Hero_leg_upper");
        }

        // The shin in the suit with a white greave, and a heavy gunmetal boot with a lit toe cap.
        private static Mesh LegLower(Vector3 ankle)
        {
            PropMeshBuilder b = Builder();
            b.AddTube(new[] { Vector3.zero, ankle }, new[] { 0.075f, 0.065f }, 8, Dark, false);
            b.AddBlob(new Vector3(0f, -0.17f, 0.05f), new Vector3(0.08f, 0.15f, 0.06f), 4, 9, 0.02f, 17, Armour, false);
            b.AddBlob(ankle + new Vector3(0f, -0.03f, 0.06f), new Vector3(0.095f, 0.065f, 0.17f), 4, 8, 0.03f, 2, Metal, false);
            b.AddBlob(ankle + new Vector3(0f, -0.03f, 0.19f), new Vector3(0.065f, 0.04f, 0.06f), 3, 6, 0f, 0, Trim, false);
            return b.ToMesh("Hero_leg_lower");
        }

        // A short cloak hung from the nape: wide at the shoulders, tapering and ragged at the hem, in the suit cloth
        // with a lit band along the shoulders.
        private static Mesh Cloak()
        {
            PropMeshBuilder b = Builder();
            var spine = new[] { Vector3.zero, new Vector3(0f, -0.22f, -0.06f), new Vector3(0f, -0.48f, -0.1f), new Vector3(0f, -0.7f, -0.12f) };
            b.AddBlade(spine, new[] { 0.44f, 0.4f, 0.34f, 0.26f }, Vector3.right, Dark);
            for (int i = -1; i <= 1; i++)
            {
                var hem = new[] { new Vector3(i * 0.09f, -0.66f, -0.12f), new Vector3(i * 0.1f, -0.82f, -0.14f) };
                b.AddBlade(hem, new[] { 0.09f, 0.03f }, Vector3.right, Dark);
            }

            b.AddBlade(new[] { new Vector3(0f, -0.01f, 0f), new Vector3(0f, -0.08f, -0.02f) }, new[] { 0.42f, 0.4f }, Vector3.right, Trim);
            return b.ToMesh("Hero_cloak");
        }
    }
}
