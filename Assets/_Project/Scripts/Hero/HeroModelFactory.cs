using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander from code: an astronaut in a bulky suit with a life-support pack and thrusters, a
    /// glowing visor and a shoulder laser cannon. The torso, the weapon arm, one leg (mirrored for the left) and
    /// the head are separate meshes so the view can swing, walk and look around. The weapon itself comes from
    /// <see cref="HeroWeaponModelFactory"/> and hangs from the hand, the pack or the hip.
    /// Submesh slots: armour, trim glow, visor glow, dark.
    /// </summary>
    public static class HeroModelFactory
    {
        public const int Armour = 0;
        public const int Trim = 1;
        public const int Visor = 2;
        public const int Dark = 3;

        // Warm pale armour with yellow pads, so the commander never reads as one more grey machine.
        private static readonly Color ArmourColor = new Color(0.95f, 0.9f, 0.72f);
        private static readonly Color DarkColor = new Color(0.1f, 0.12f, 0.18f);
        private static readonly Color VisorColor = new Color(0.3f, 0.95f, 1f);
        private static readonly Color DeadArmourColor = new Color(0.5f, 0.52f, 0.58f);
        private static readonly Color DeadVisorColor = new Color(0.08f, 0.1f, 0.12f);
        private const float ArmourGlow = 0.28f;

        public readonly struct Parts
        {
            public readonly Mesh Body;
            public readonly Mesh Arm;
            public readonly Mesh Leg;
            public readonly Mesh Head;
            public readonly Vector3 Shoulder;
            public readonly Vector3 Hip;
            public readonly Vector3 Neck;
            public readonly Vector3 Thruster;

            /// <summary>Where a weapon grip sits in the hand, in the space of the arm.</summary>
            public readonly Vector3 Hand;

            /// <summary>Where a long weapon clips to the pack, in the space of the body.</summary>
            public readonly Vector3 BackHolster;

            /// <summary>Where a short weapon hangs at the hip, in the space of the body.</summary>
            public readonly Vector3 HipHolster;

            public Parts(Mesh body, Mesh arm, Mesh leg, Mesh head, Vector3 shoulder, Vector3 hip, Vector3 neck, Vector3 thruster, Vector3 hand, Vector3 backHolster, Vector3 hipHolster)
            {
                Body = body;
                Arm = arm;
                Leg = leg;
                Head = head;
                Shoulder = shoulder;
                Hip = hip;
                Neck = neck;
                Thruster = thruster;
                Hand = hand;
                BackHolster = backHolster;
                HipHolster = hipHolster;
            }
        }

        /// <summary>The suit materials; powered down, the lights are out and the armour is dull.</summary>
        public static Material[] CreateMaterials(Color trim, bool powered = true)
        {
            return new[]
            {
                // A touch of self-light keeps the suit readable in dark levels and in the shadow of a hill.
                SurfaceMaterials.Create(powered ? ArmourColor : DeadArmourColor, powered ? ArmourGlow : 0f, powered ? 0.6f : 0.3f, 0.5f),
                SurfaceMaterials.Create(powered ? trim : Color.Lerp(trim, DarkColor, 0.6f), powered ? 1.2f : 0f, 0.5f),
                SurfaceMaterials.Create(powered ? VisorColor : DeadVisorColor, powered ? 3f : 0f, 0.7f),
                SurfaceMaterials.Create(DarkColor, 0f, 0.3f, 0.2f),
            };
        }

        public static Parts Create()
        {
            var body = new PropMeshBuilder();

            // Bulky torso with a lit control panel, a belt and a hip plate the legs hang from.
            body.AddBlob(new Vector3(0f, 0.6f, 0f), new Vector3(0.26f, 0.13f, 0.19f), 4, 9, 0.02f, 3, Dark, false);
            body.AddBlob(new Vector3(0f, 0.95f, 0f), new Vector3(0.3f, 0.34f, 0.22f), 5, 10, 0.02f, 4, Armour, false);
            body.AddBlob(new Vector3(0f, 0.98f, 0.19f), new Vector3(0.16f, 0.12f, 0.05f), 3, 8, 0f, 0, Dark, false);
            for (int i = 0; i < 3; i++)
            {
                body.AddBlob(new Vector3(-0.07f + i * 0.07f, 0.98f, 0.235f), Vector3.one * 0.022f, 3, 6, 0f, 0, i == 1 ? Visor : Trim, false);
            }

            body.AddTube(new[] { new Vector3(0f, 0.7f, 0f), new Vector3(0f, 0.74f, 0f) }, new[] { 0.26f, 0.26f }, 12, Trim, false);
            body.AddBlob(new Vector3(0f, 1.1f, 0.12f), new Vector3(0.2f, 0.08f, 0.06f), 3, 8, 0f, 0, Trim, false);

            // A hip pouch on the right, where a short weapon hangs.
            body.AddBox(new Vector3(0.3f, 0.64f, 0.02f), new Vector3(0.08f, 0.1f, 0.12f), Dark);

            // Life-support pack with two tanks, two thruster nozzles underneath, a weapon clip and an antenna.
            body.AddBlob(new Vector3(0f, 1.0f, -0.28f), new Vector3(0.22f, 0.3f, 0.12f), 4, 9, 0.02f, 5, Dark, false);
            for (int side = -1; side <= 1; side += 2)
            {
                body.AddTube(new[] { new Vector3(0.11f * side, 0.7f, -0.36f), new Vector3(0.11f * side, 1.25f, -0.36f) }, new[] { 0.075f, 0.075f }, 8, Armour, false);
                body.AddBlob(new Vector3(0.11f * side, 1.27f, -0.36f), Vector3.one * 0.075f, 3, 8, 0f, 0, Armour, false);
                body.AddTube(new[] { new Vector3(0.11f * side, 0.7f, -0.36f), new Vector3(0.11f * side, 0.6f, -0.38f) }, new[] { 0.05f, 0.065f }, 8, Dark, true);
                body.AddBlob(new Vector3(0.11f * side, 0.6f, -0.38f), new Vector3(0.045f, 0.02f, 0.045f), 3, 8, 0f, 0, Visor, false);
            }

            body.AddBox(new Vector3(0.24f, 1.02f, -0.4f), new Vector3(0.06f, 0.12f, 0.08f), Dark);
            body.AddTube(new[] { new Vector3(0.15f, 1.2f, -0.3f), new Vector3(0.18f, 1.65f, -0.34f) }, new[] { 0.015f, 0.01f }, 4, Dark, false);
            body.AddBlob(new Vector3(0.18f, 1.67f, -0.34f), Vector3.one * 0.03f, 3, 6, 0f, 0, Trim, false);

            // Rounded shoulder pads with a stripe, and a shoulder-mounted laser cannon on the left.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.4f * side;
                body.AddBlob(new Vector3(x, 1.18f, 0f), new Vector3(0.16f, 0.14f, 0.17f), 5, 10, 0.03f, 7, Trim, false);
                body.AddBlob(new Vector3(x, 1.28f, 0f), new Vector3(0.1f, 0.03f, 0.11f), 3, 8, 0f, 0, Armour, false);
            }

            body.AddBlob(new Vector3(-0.4f, 1.42f, 0.02f), new Vector3(0.09f, 0.1f, 0.2f), 4, 8, 0.02f, 8, Dark, true);
            body.AddTube(new[] { new Vector3(-0.4f, 1.43f, 0.18f), new Vector3(-0.4f, 1.43f, 0.5f) }, new[] { 0.045f, 0.035f }, 8, Dark, false);
            body.AddBlob(new Vector3(-0.4f, 1.43f, 0.52f), Vector3.one * 0.04f, 3, 6, 0f, 0, Visor, false);

            // The free arm in a glove.
            body.AddTube(new[] { new Vector3(-0.42f, 1.08f, 0f), new Vector3(-0.5f, 0.84f, 0.04f), new Vector3(-0.48f, 0.64f, 0.1f) }, new[] { 0.09f, 0.085f, 0.075f }, 8, Armour, false);
            body.AddBlob(new Vector3(-0.48f, 0.58f, 0.12f), Vector3.one * 0.085f, 4, 8, 0f, 0, Dark, false);

            // One leg, hanging from its hip: thigh, knee pad, shin and a heavy boot. Mirrored for the other side.
            var leg = new PropMeshBuilder();
            leg.AddTube(new[] { Vector3.zero, new Vector3(0f, -0.24f, 0.02f), new Vector3(0f, -0.5f, 0f) }, new[] { 0.11f, 0.1f, 0.09f }, 8, Armour, false);
            leg.AddBlob(new Vector3(0f, -0.22f, 0.1f), new Vector3(0.09f, 0.07f, 0.06f), 3, 7, 0.02f, 1, Trim, false);
            leg.AddBlob(new Vector3(0f, -0.52f, 0.06f), new Vector3(0.11f, 0.07f, 0.17f), 4, 8, 0.03f, 2, Dark, false);
            leg.AddBox(new Vector3(0f, -0.4f, 0.09f), new Vector3(0.08f, 0.14f, 0.03f), Dark);

            // Round helmet with a large glowing visor, a crest and two lamps, on a neck ring.
            var head = new PropMeshBuilder();
            head.AddTube(new[] { new Vector3(0f, -0.03f, 0f), new Vector3(0f, 0.03f, 0f) }, new[] { 0.17f, 0.17f }, 10, Dark, false);
            head.AddBlob(new Vector3(0f, 0.19f, 0f), new Vector3(0.23f, 0.23f, 0.23f), 6, 12, 0.01f, 6, Armour, false);
            head.AddBlob(new Vector3(0f, 0.19f, 0.12f), new Vector3(0.17f, 0.13f, 0.12f), 4, 10, 0f, 0, Visor, false);
            head.AddBlob(new Vector3(0f, 0.35f, 0.02f), new Vector3(0.1f, 0.03f, 0.12f), 3, 8, 0f, 0, Trim, false);
            head.AddBlob(new Vector3(0.14f, 0.3f, 0.14f), Vector3.one * 0.035f, 3, 6, 0f, 0, Visor, false);
            head.AddBlob(new Vector3(-0.14f, 0.3f, 0.14f), Vector3.one * 0.035f, 3, 6, 0f, 0, Visor, false);

            // The weapon arm pivots at the shoulder and ends in a gloved hand.
            var arm = new PropMeshBuilder();
            arm.AddTube(new[] { Vector3.zero, new Vector3(0.06f, -0.22f, 0.06f), new Vector3(0.04f, -0.32f, 0.2f) }, new[] { 0.09f, 0.085f, 0.075f }, 8, Armour, false);
            arm.AddBlob(new Vector3(0.04f, -0.33f, 0.24f), Vector3.one * 0.085f, 4, 8, 0f, 0, Dark, false);

            return new Parts(
                body.ToMesh("Hero_body"),
                arm.ToMesh("Hero_arm"),
                leg.ToMesh("Hero_leg"),
                head.ToMesh("Hero_head"),
                new Vector3(0.4f, 1.1f, 0.02f),
                new Vector3(0.12f, 0.58f, 0f),
                new Vector3(0f, 1.27f, 0f),
                new Vector3(0f, 0.58f, -0.38f),
                new Vector3(0.04f, -0.33f, 0.24f),
                new Vector3(0.24f, 1.02f, -0.46f),
                new Vector3(0.36f, 0.62f, 0.02f));
        }
    }
}
