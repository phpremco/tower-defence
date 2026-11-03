using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The extraction drill from code: a plated platform with a motor housing and lit vents, a lattice derrick of
    /// four leaning legs with braces and a crown block on top, and a separate drill string (pipe, collar ring and a
    /// fluted bit) that the view spins and lowers through the platform. Slots follow <see cref="ModelPalette"/>;
    /// standing on y = 0, about 4.2 tall.
    /// </summary>
    public static class DrillModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;
        private const float DerrickHeight = 4.2f;
        private const float BaseHalf = 0.8f;
        private const float TopHalf = 0.22f;
        private const int Braces = 4;

        /// <summary>The fixed rig and the turning drill string, which hangs from y = 0 downwards in its own space.</summary>
        public readonly struct Parts
        {
            public Parts(Mesh rig, Mesh bit, float bitLength)
            {
                Rig = rig;
                Bit = bit;
                BitLength = bitLength;
            }

            public Mesh Rig { get; }
            public Mesh Bit { get; }

            /// <summary>How long the drill string is, from its top to the tip of the bit.</summary>
            public float BitLength { get; }
        }

        public static Parts Create()
        {
            var b = new PropMeshBuilder();
            b.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(2.3f, 0.3f, 2.3f), Dark);
            b.AddBox(new Vector3(0f, 0.34f, 0f), new Vector3(2.0f, 0.08f, 2.0f), Body);
            b.AddBox(new Vector3(0.75f, 0.7f, -0.75f), new Vector3(0.7f, 0.7f, 0.6f), Body);
            b.AddBox(new Vector3(0.75f, 0.7f, -0.44f), new Vector3(0.5f, 0.08f, 0.02f), Glow);
            b.AddBox(new Vector3(0.75f, 0.85f, -0.44f), new Vector3(0.5f, 0.08f, 0.02f), Glow);
            b.AddTube(new[] { new Vector3(0.95f, 1.05f, -0.85f), new Vector3(0.95f, 1.6f, -0.85f) }, new[] { 0.08f, 0.06f }, 8, Metal, false);
            b.AddTorus(new Vector3(0f, 0.42f, 0f), Vector3.up, 0.32f, 0.07f, 20, 8, Glow);

            for (int leg = 0; leg < 4; leg++)
            {
                var corner = new Vector3(leg % 2 == 0 ? -1f : 1f, 0f, leg < 2 ? -1f : 1f);
                Vector3 foot = corner * BaseHalf + Vector3.up * 0.38f;
                Vector3 top = corner * TopHalf + Vector3.up * DerrickHeight;
                b.AddTube(new[] { foot, top }, new[] { 0.06f, 0.045f }, 6, Metal, true);
                b.AddBox(foot, new Vector3(0.22f, 0.12f, 0.22f), Dark);
            }

            // Cross braces on each face, every quarter of the height: what makes it read as a lattice derrick.
            for (int level = 1; level <= Braces; level++)
            {
                float h0 = Mathf.Lerp(0.38f, DerrickHeight, (level - 1) / (float)Braces);
                float h1 = Mathf.Lerp(0.38f, DerrickHeight, level / (float)Braces);
                float half0 = Mathf.Lerp(BaseHalf, TopHalf, (level - 1) / (float)Braces);
                float half1 = Mathf.Lerp(BaseHalf, TopHalf, level / (float)Braces);
                for (int face = 0; face < 4; face++)
                {
                    Vector3 a0 = FaceCorner(face, half0, h0, false);
                    Vector3 b1 = FaceCorner(face, half1, h1, true);
                    b.AddTube(new[] { a0, b1 }, new[] { 0.025f, 0.025f }, 4, Metal, true);
                    b.AddTube(new[] { FaceCorner(face, half1, h1, false), FaceCorner(face, half1, h1, true) }, new[] { 0.03f, 0.03f }, 4, Dark, true);
                }
            }

            b.AddBox(new Vector3(0f, DerrickHeight + 0.12f, 0f), new Vector3(0.7f, 0.26f, 0.7f), Body);
            b.AddBox(new Vector3(0f, DerrickHeight + 0.12f, 0.36f), new Vector3(0.5f, 0.06f, 0.02f), Glow);
            b.AddBlob(new Vector3(0f, DerrickHeight + 0.34f, 0f), Vector3.one * 0.09f, 3, 8, 0f, 0, Glow, false);

            return new Parts(b.ToMesh("DrillRig"), CreateBit(out float length), length);
        }

        // The string: a long pipe, a lit collar, then a cone bit wrapped in two spiral flutes.
        private static Mesh CreateBit(out float length)
        {
            const float Pipe = 3.6f;
            const float Bit = 1.1f;
            const int Turns = 3;
            const int Steps = 24;
            length = Pipe + Bit;

            var b = new PropMeshBuilder();
            b.AddTube(new[] { Vector3.zero, Vector3.down * Pipe }, new[] { 0.09f, 0.09f }, 10, Metal, false);
            b.AddTorus(Vector3.down * 0.4f, Vector3.up, 0.12f, 0.04f, 16, 6, Glow);
            b.AddTorus(Vector3.down * Pipe, Vector3.up, 0.2f, 0.06f, 16, 6, Glow);
            b.AddTube(new[] { Vector3.down * Pipe, Vector3.down * (Pipe + Bit) }, new[] { 0.24f, 0f }, 12, Dark, false);
            for (int flute = 0; flute < 2; flute++)
            {
                var path = new Vector3[Steps + 1];
                var radii = new float[Steps + 1];
                for (int i = 0; i <= Steps; i++)
                {
                    float t = i / (float)Steps;
                    float angle = (t * Turns + flute * 0.5f) * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(0.27f, 0.02f, t);
                    path[i] = new Vector3(Mathf.Cos(angle) * radius, -Pipe - t * Bit, Mathf.Sin(angle) * radius);
                    radii[i] = Mathf.Lerp(0.05f, 0.01f, t);
                }

                b.AddTube(path, radii, 5, Metal, false);
            }

            return b.ToMesh("DrillBit");
        }

        // The two corners of one face of the derrick at a height; the far corner when asked.
        private static Vector3 FaceCorner(int face, float half, float height, bool far)
        {
            float a = far ? 1f : -1f;
            switch (face)
            {
                case 0: return new Vector3(a * half, height, -half);
                case 1: return new Vector3(half, height, a * half);
                case 2: return new Vector3(-a * half, height, half);
                default: return new Vector3(-half, height, -a * half);
            }
        }
    }
}
