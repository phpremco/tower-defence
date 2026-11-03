using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The landing capsule from code: a rounded pod in plated hull with a metal belt, a lit window, three landing
    /// legs with pads, an antenna with a light, and a separate heat shield underneath so its glow can cool after
    /// re-entry. Submesh slots follow <see cref="ModelPalette"/>; standing on y = 0, window towards +z, about 1.7 tall.
    /// </summary>
    public static class CapsuleModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;
        private const int Legs = 3;

        /// <summary>The capsule hull and the shield under it, both in the same space.</summary>
        public readonly struct Parts
        {
            public Parts(Mesh hull, Mesh shield, Mesh hatch, Vector3 hatchHinge, Vector3 door)
            {
                Hull = hull;
                Shield = shield;
                Hatch = hatch;
                HatchHinge = hatchHinge;
                Door = door;
            }

            public Mesh Hull { get; }

            /// <summary>The heat shield: one submesh, for a material whose glow cools.</summary>
            public Mesh Shield { get; }

            /// <summary>The door panel, built around its hinge, so it swings open by turning about x.</summary>
            public Mesh Hatch { get; }

            public Vector3 HatchHinge { get; }

            /// <summary>Where someone stepping out stands, just in front of the open door.</summary>
            public Vector3 Door { get; }
        }

        public static Parts Create()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(new Vector3(0f, 1.0f, 0f), new Vector3(0.58f, 0.7f, 0.58f), 10, 18, 0f, 0, Body, false);
            b.AddTorus(new Vector3(0f, 0.92f, 0f), Vector3.up, 0.585f, 0.06f, 24, 8, Metal);
            b.AddTorus(new Vector3(0f, 0.62f, 0f), Vector3.up, 0.5f, 0.05f, 24, 8, Dark);
            b.AddBox(new Vector3(0f, 1.1f, 0.5f), new Vector3(0.5f, 0.62f, 0.1f), Dark);
            b.AddBox(new Vector3(0f, 1.5f, 0.42f), new Vector3(0.26f, 0.08f, 0.12f), Glow);
            for (int i = 0; i < Legs; i++)
            {
                float angle = (i / (float)Legs + 1f / 6f) * Mathf.PI * 2f;
                var around = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 hip = around * 0.42f + Vector3.up * 0.62f;
                Vector3 knee = around * 0.66f + Vector3.up * 0.32f;
                Vector3 foot = around * 0.72f + Vector3.up * 0.04f;
                b.AddTube(new[] { hip, knee, foot }, new[] { 0.045f, 0.04f, 0.035f }, 6, Metal, false);
                b.AddBox(foot, new Vector3(0.2f, 0.05f, 0.2f), Dark, angle * Mathf.Rad2Deg);
                b.AddBlob(hip, Vector3.one * 0.06f, 3, 6, 0f, i, Dark, false);
                b.AddBlob(around * 0.59f + Vector3.up * 1.2f, Vector3.one * 0.035f, 3, 6, 0f, i, Glow, false);
            }

            b.AddTube(new[] { new Vector3(0.12f, 1.6f, -0.1f), new Vector3(0.14f, 2.05f, -0.14f) }, new[] { 0.02f, 0.012f }, 5, Metal, false);
            b.AddBlob(new Vector3(0.14f, 2.08f, -0.14f), Vector3.one * 0.04f, 3, 6, 0f, 3, Glow, false);

            var shield = new PropMeshBuilder(1);
            shield.AddBlob(new Vector3(0f, 0.62f, 0f), new Vector3(0.6f, 0.3f, 0.6f), 6, 18, 0.02f, 5, 0, false, Mathf.PI);
            Vector3 hinge = new Vector3(0f, 0.8f, 0.56f);
            var hatch = new PropMeshBuilder();
            hatch.AddBox(new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.58f, 0.06f), Body);
            hatch.AddBox(new Vector3(0f, 0.38f, 0.035f), new Vector3(0.26f, 0.2f, 0.02f), Glow);
            hatch.AddBox(new Vector3(0f, 0.04f, 0.02f), new Vector3(0.36f, 0.05f, 0.05f), Metal);
            return new Parts(b.ToMesh("CapsuleHull"), shield.ToMesh("CapsuleShield"), hatch.ToMesh("CapsuleHatch"), hinge, new Vector3(0f, 0f, 1.05f));
        }
    }
}
