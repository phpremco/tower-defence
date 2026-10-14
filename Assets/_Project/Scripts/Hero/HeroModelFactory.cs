using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander from code: an armoured soldier with a crested helmet, shoulder plates, a cape and an
    /// energy blade. The body and the sword arm are separate meshes so the arm can swing.
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

            // Legs and boots.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.1f * side;
                body.AddTube(new[] { new Vector3(x, 0.08f, 0f), new Vector3(x, 0.3f, 0.01f), new Vector3(x, 0.55f, 0f) }, new[] { 0.075f, 0.085f, 0.09f }, 7, Dark, false);
                body.AddBlob(new Vector3(x, 0.06f, 0.05f), new Vector3(0.09f, 0.07f, 0.15f), 4, 8, 0.03f, 1, Armour, false);
                body.AddBlob(new Vector3(x, 0.32f, 0.07f), new Vector3(0.075f, 0.11f, 0.05f), 3, 7, 0.03f, 2, Armour, false);
            }

            // Pelvis, torso and chest core.
            body.AddBlob(new Vector3(0f, 0.58f, 0f), new Vector3(0.21f, 0.12f, 0.16f), 4, 9, 0.02f, 3, Dark, false);
            body.AddBlob(new Vector3(0f, 0.9f, 0f), new Vector3(0.27f, 0.32f, 0.19f), 5, 10, 0.02f, 4, Armour, false);
            body.AddBlob(new Vector3(0f, 0.95f, 0.15f), new Vector3(0.12f, 0.1f, 0.06f), 3, 8, 0f, 0, Trim, false);
            body.AddBlob(new Vector3(0f, 0.7f, 0.12f), new Vector3(0.2f, 0.06f, 0.08f), 3, 8, 0.02f, 5, Trim, false);

            // Shoulder plates.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.36f * side;
                body.AddBlob(new Vector3(x, 1.12f, 0f), new Vector3(0.17f, 0.12f, 0.17f), 4, 9, 0.04f, 6, Armour, false);
                body.AddBlob(new Vector3(x, 1.2f, 0f), new Vector3(0.12f, 0.04f, 0.12f), 3, 8, 0f, 0, Trim, false);
            }

            // Helmet with visor, a crest and a short cape.
            body.AddBlob(new Vector3(0f, 1.4f, 0f), new Vector3(0.16f, 0.18f, 0.17f), 5, 10, 0.02f, 7, Armour, false);
            body.AddBlob(new Vector3(0f, 1.38f, 0.12f), new Vector3(0.12f, 0.05f, 0.07f), 3, 8, 0f, 0, Visor, false);
            body.AddTube(new[] { new Vector3(0f, 1.52f, 0.05f), new Vector3(0f, 1.66f, -0.08f), new Vector3(0f, 1.6f, -0.24f) }, new[] { 0.035f, 0.03f, 0.01f }, 5, Trim, false);
            body.AddBlob(new Vector3(0f, 0.9f, -0.2f), new Vector3(0.27f, 0.5f, 0.05f), 4, 9, 0.04f, 8, Dark, false);
            body.AddBlob(new Vector3(0f, 0.45f, -0.22f), new Vector3(0.25f, 0.04f, 0.06f), 3, 9, 0f, 0, Trim, false);

            // The free arm hangs by the side.
            body.AddTube(new[] { new Vector3(-0.38f, 1.05f, 0f), new Vector3(-0.44f, 0.82f, 0.04f), new Vector3(-0.42f, 0.62f, 0.1f) }, new[] { 0.075f, 0.07f, 0.06f }, 7, Armour, false);
            body.AddBlob(new Vector3(-0.42f, 0.56f, 0.12f), Vector3.one * 0.07f, 4, 8, 0f, 0, Dark, false);

            // The sword arm: built around its shoulder so it can pivot there.
            var arm = new PropMeshBuilder();
            arm.AddTube(new[] { Vector3.zero, new Vector3(0.05f, -0.2f, 0.06f), new Vector3(0.04f, -0.3f, 0.2f) }, new[] { 0.075f, 0.07f, 0.06f }, 7, Armour, false);
            arm.AddBlob(new Vector3(0.04f, -0.31f, 0.24f), Vector3.one * 0.07f, 4, 8, 0f, 0, Dark, false);
            arm.AddTube(new[] { new Vector3(0.04f, -0.31f, 0.2f), new Vector3(0.04f, -0.31f, 0.34f) }, new[] { 0.03f, 0.03f }, 6, Dark, false);
            arm.AddBlob(new Vector3(0.04f, -0.31f, 0.36f), new Vector3(0.12f, 0.025f, 0.03f), 3, 8, 0f, 0, Trim, false);
            arm.AddTube(new[] { new Vector3(0.04f, -0.31f, 0.38f), new Vector3(0.04f, -0.31f, 1.0f), new Vector3(0.04f, -0.31f, 1.3f) }, new[] { 0.05f, 0.045f, 0f }, 4, Visor, true);

            return new Parts(body.ToMesh("Hero_body"), arm.ToMesh("Hero_arm"), new Vector3(0.36f, 1.05f, 0.02f));
        }
    }
}
