using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander from code: an astronaut in a bulky suit with a life-support pack, a glowing visor, a
    /// shoulder laser cannon and a laser sword. The body and the sword arm are separate meshes so the arm can swing.
    /// Submesh slots: armour, trim glow, visor and blade glow, dark.
    /// </summary>
    public static class HeroModelFactory
    {
        public const int Armour = 0;
        public const int Trim = 1;
        public const int Visor = 2;
        public const int Dark = 3;

        private static readonly Color VisorColor = new Color(0.3f, 0.95f, 1f);

        public readonly struct Parts
        {
            public readonly Mesh Body;
            public readonly Mesh Arm;
            public readonly Vector3 Shoulder;

            public Parts(Mesh body, Mesh arm, Vector3 shoulder)
            {
                Body = body;
                Arm = arm;
                Shoulder = shoulder;
            }
        }

        public static Material[] CreateMaterials(Color trim)
        {
            return new[]
            {
                SurfaceMaterials.Create(new Color(0.82f, 0.86f, 0.95f), 0f, 0.6f, 0.5f),
                SurfaceMaterials.Create(trim, 1.2f, 0.5f),
                SurfaceMaterials.Create(VisorColor, 3f, 0.7f),
                SurfaceMaterials.Create(new Color(0.1f, 0.12f, 0.18f), 0f, 0.3f, 0.2f),
            };
        }

        public static Parts Create()
        {
            var body = new PropMeshBuilder();

            // Suit legs with knee pads and heavy boots.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.12f * side;
                body.AddTube(new[] { new Vector3(x, 0.1f, 0f), new Vector3(x, 0.32f, 0.01f), new Vector3(x, 0.58f, 0f) }, new[] { 0.095f, 0.105f, 0.11f }, 8, Armour, false);
                body.AddBlob(new Vector3(x, 0.36f, 0.09f), new Vector3(0.09f, 0.07f, 0.06f), 3, 7, 0.02f, 1, Trim, false);
                body.AddBlob(new Vector3(x, 0.06f, 0.06f), new Vector3(0.11f, 0.07f, 0.17f), 4, 8, 0.03f, 2, Dark, false);
            }

            // Bulky torso with a lit control panel and a belt.
            body.AddBlob(new Vector3(0f, 0.62f, 0f), new Vector3(0.24f, 0.12f, 0.18f), 4, 9, 0.02f, 3, Dark, false);
            body.AddBlob(new Vector3(0f, 0.95f, 0f), new Vector3(0.3f, 0.34f, 0.22f), 5, 10, 0.02f, 4, Armour, false);
            body.AddBlob(new Vector3(0f, 0.98f, 0.19f), new Vector3(0.16f, 0.12f, 0.05f), 3, 8, 0f, 0, Dark, false);
            for (int i = 0; i < 3; i++)
            {
                body.AddBlob(new Vector3(-0.07f + i * 0.07f, 0.98f, 0.235f), Vector3.one * 0.022f, 3, 6, 0f, 0, i == 1 ? Visor : Trim, false);
            }

            body.AddTube(new[] { new Vector3(0f, 0.7f, 0f), new Vector3(0f, 0.74f, 0f) }, new[] { 0.26f, 0.26f }, 12, Trim, false);

            // Life-support pack with two tanks and an antenna.
            body.AddBlob(new Vector3(0f, 1.0f, -0.28f), new Vector3(0.22f, 0.3f, 0.12f), 4, 9, 0.02f, 5, Dark, false);
            for (int side = -1; side <= 1; side += 2)
            {
                body.AddTube(new[] { new Vector3(0.11f * side, 0.7f, -0.36f), new Vector3(0.11f * side, 1.25f, -0.36f) }, new[] { 0.075f, 0.075f }, 8, Armour, false);
                body.AddBlob(new Vector3(0.11f * side, 1.27f, -0.36f), Vector3.one * 0.075f, 3, 8, 0f, 0, Armour, false);
            }

            body.AddTube(new[] { new Vector3(0.15f, 1.2f, -0.3f), new Vector3(0.18f, 1.65f, -0.34f) }, new[] { 0.015f, 0.01f }, 4, Dark, false);
            body.AddBlob(new Vector3(0.18f, 1.67f, -0.34f), Vector3.one * 0.03f, 3, 6, 0f, 0, Trim, false);

            // Round helmet with a large glowing visor and a neck ring.
            body.AddTube(new[] { new Vector3(0f, 1.24f, 0f), new Vector3(0f, 1.3f, 0f) }, new[] { 0.17f, 0.17f }, 10, Dark, false);
            body.AddBlob(new Vector3(0f, 1.46f, 0f), new Vector3(0.23f, 0.23f, 0.23f), 6, 12, 0.01f, 6, Armour, false);
            body.AddBlob(new Vector3(0f, 1.46f, 0.12f), new Vector3(0.17f, 0.13f, 0.12f), 4, 10, 0f, 0, Visor, false);
            body.AddBlob(new Vector3(0f, 1.62f, 0.02f), new Vector3(0.1f, 0.03f, 0.12f), 3, 8, 0f, 0, Trim, false);

            // Rounded shoulder pads with a stripe, and a shoulder-mounted laser cannon on the left.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.4f * side;
                body.AddBlob(new Vector3(x, 1.18f, 0f), new Vector3(0.16f, 0.14f, 0.17f), 5, 10, 0.03f, 7, Armour, false);
                body.AddBlob(new Vector3(x, 1.28f, 0f), new Vector3(0.1f, 0.03f, 0.11f), 3, 8, 0f, 0, Trim, false);
            }

            body.AddBlob(new Vector3(-0.4f, 1.42f, 0.02f), new Vector3(0.09f, 0.1f, 0.2f), 4, 8, 0.02f, 8, Dark, true);
            body.AddTube(new[] { new Vector3(-0.4f, 1.43f, 0.18f), new Vector3(-0.4f, 1.43f, 0.5f) }, new[] { 0.045f, 0.035f }, 8, Dark, false);
            body.AddBlob(new Vector3(-0.4f, 1.43f, 0.52f), Vector3.one * 0.04f, 3, 6, 0f, 0, Visor, false);

            // The free arm in a glove.
            body.AddTube(new[] { new Vector3(-0.42f, 1.08f, 0f), new Vector3(-0.5f, 0.84f, 0.04f), new Vector3(-0.48f, 0.64f, 0.1f) }, new[] { 0.09f, 0.085f, 0.075f }, 8, Armour, false);
            body.AddBlob(new Vector3(-0.48f, 0.58f, 0.12f), Vector3.one * 0.085f, 4, 8, 0f, 0, Dark, false);

            // The sword arm pivots at the shoulder and holds a laser sword.
            var arm = new PropMeshBuilder();
            arm.AddTube(new[] { Vector3.zero, new Vector3(0.06f, -0.22f, 0.06f), new Vector3(0.04f, -0.32f, 0.2f) }, new[] { 0.09f, 0.085f, 0.075f }, 8, Armour, false);
            arm.AddBlob(new Vector3(0.04f, -0.33f, 0.24f), Vector3.one * 0.085f, 4, 8, 0f, 0, Dark, false);
            arm.AddTube(new[] { new Vector3(0.04f, -0.33f, 0.2f), new Vector3(0.04f, -0.33f, 0.48f) }, new[] { 0.032f, 0.032f }, 8, Dark, false);
            arm.AddTube(new[] { new Vector3(0.04f, -0.33f, 0.44f), new Vector3(0.04f, -0.33f, 0.5f) }, new[] { 0.045f, 0.045f }, 8, Trim, false);
            arm.AddTube(new[] { new Vector3(0.04f, -0.33f, 0.5f), new Vector3(0.04f, -0.33f, 1.3f), new Vector3(0.04f, -0.33f, 1.55f) }, new[] { 0.04f, 0.04f, 0f }, 8, Visor, false);

            return new Parts(body.ToMesh("Hero_body"), arm.ToMesh("Hero_arm"), new Vector3(0.4f, 1.1f, 0.02f));
        }
    }
}
