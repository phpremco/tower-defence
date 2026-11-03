using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The expedition's mothership from code: a long spine with a rounded command module and its lit bridge band in
    /// front, cargo containers clamped along the middle, a capsule bay underneath, broad radiator fins, and a heavy
    /// engine block with three glowing nozzles at the back. The habitat ring is a separate mesh so it can turn
    /// around the spine. Submesh slots follow <see cref="ModelPalette"/>; nose towards +z, about 12 units long.
    /// </summary>
    public static class MothershipModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;
        private const int Nozzles = 3;
        private const int Containers = 6;
        private const int Spokes = 4;
        private const int RingWindows = 16;
        private const float SpineFront = 4.6f;
        private const float SpineBack = -4.6f;

        /// <summary>The hull, the turning ring and the points the set lights or launches from.</summary>
        public readonly struct Parts
        {
            public Parts(Mesh hull, Mesh ring, Vector3 ringCentre, Vector3[] nozzles, Vector3 bay)
            {
                Hull = hull;
                Ring = ring;
                RingCentre = ringCentre;
                Nozzles = nozzles;
                Bay = bay;
            }

            public Mesh Hull { get; }

            /// <summary>The habitat ring with its spokes, built around <see cref="RingCentre"/>; it turns about z.</summary>
            public Mesh Ring { get; }

            public Vector3 RingCentre { get; }

            /// <summary>The exits of the engine nozzles, where the exhaust glow sits.</summary>
            public Vector3[] Nozzles { get; }

            /// <summary>The opening of the capsule bay under the hull.</summary>
            public Vector3 Bay { get; }
        }

        public static Parts Create()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0f, SpineBack), new Vector3(0f, 0f, SpineFront) }, new[] { 0.3f, 0.3f }, 10, Metal, false);
            AddCommandModule(b);
            AddCargo(b);
            Vector3 bay = AddBay(b);
            AddRadiators(b);
            Vector3[] nozzles = AddEngines(b);
            for (int i = 0; i < 7; i++)
            {
                // Running lights along the spine, top and bottom.
                float z = Mathf.Lerp(SpineBack + 1f, SpineFront - 1f, i / 6f);
                b.AddBlob(new Vector3(0f, 0.32f, z), Vector3.one * 0.05f, 3, 6, 0f, i, Glow, false);
                b.AddBlob(new Vector3(0f, -0.32f, z), Vector3.one * 0.05f, 3, 6, 0f, i + 7, Glow, false);
            }

            Vector3 ringCentre = new Vector3(0f, 0f, 2.1f);
            return new Parts(b.ToMesh("MothershipHull"), BuildRing(ringCentre), ringCentre, nozzles, bay);
        }

        private static void AddCommandModule(PropMeshBuilder b)
        {
            b.AddBlob(new Vector3(0f, 0f, SpineFront + 0.4f), new Vector3(0.95f, 0.75f, 1.4f), 10, 18, 0f, 1, Body, false);
            b.AddTorus(new Vector3(0f, 0f, SpineFront + 0.2f), Vector3.forward, 0.92f, 0.07f, 24, 6, Metal);
            b.AddBox(new Vector3(0f, 0.32f, SpineFront + 1.2f), new Vector3(0.9f, 0.14f, 0.5f), Glow);
            b.AddBox(new Vector3(0f, -0.55f, SpineFront), new Vector3(0.7f, 0.3f, 1.4f), Dark);
            b.AddTube(new[] { new Vector3(0.3f, 0.6f, SpineFront), new Vector3(0.36f, 1.4f, SpineFront - 0.2f) }, new[] { 0.035f, 0.02f }, 5, Metal, false);
            b.AddBlob(new Vector3(0.36f, 1.43f, SpineFront - 0.2f), Vector3.one * 0.06f, 3, 6, 0f, 2, Glow, false);
        }

        private static void AddCargo(PropMeshBuilder b)
        {
            for (int i = 0; i < Containers; i++)
            {
                float angle = (i / (float)Containers) * 360f + 30f;
                Quaternion turn = Quaternion.AngleAxis(angle, Vector3.forward);
                for (int row = 0; row < 2; row++)
                {
                    Vector3 centre = turn * new Vector3(0f, 0.62f, 0f) + Vector3.forward * (-0.9f - row * 1.35f);
                    b.AddBox(centre, new Vector3(0.55f, 0.6f, 1.2f), (i + row) % 2 == 0 ? Body : Dark, turn);
                }
            }

            b.AddTorus(new Vector3(0f, 0f, -0.2f), Vector3.forward, 0.95f, 0.06f, 24, 6, Metal);
            b.AddTorus(new Vector3(0f, 0f, -2.95f), Vector3.forward, 0.95f, 0.06f, 24, 6, Metal);
        }

        private static Vector3 AddBay(PropMeshBuilder b)
        {
            var bay = new Vector3(0f, -1.15f, 0.9f);
            b.AddBox(bay + Vector3.up * 0.25f, new Vector3(1f, 0.5f, 1.3f), Dark);
            b.AddBox(bay + Vector3.up * 0.01f, new Vector3(0.8f, 0.03f, 1.1f), Glow);
            return bay;
        }

        private static void AddRadiators(PropMeshBuilder b)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { Vector3.forward * -3.3f, new Vector3(side * 1.1f, 0f, -3.3f) }, new[] { 0.06f, 0.06f }, 6, Metal, false);
                b.AddBox(new Vector3(side * 2.2f, 0f, -3.3f), new Vector3(2.2f, 0.05f, 1.3f), Dark);
                b.AddBox(new Vector3(side * 2.2f, 0.035f, -3.3f), new Vector3(2.05f, 0.02f, 0.08f), Glow);
            }
        }

        private static Vector3[] AddEngines(PropMeshBuilder b)
        {
            b.AddBlob(new Vector3(0f, 0f, SpineBack - 0.2f), new Vector3(1.15f, 1.15f, 0.95f), 10, 18, 0f, 3, Dark, false);
            var nozzles = new Vector3[Nozzles];
            for (int i = 0; i < Nozzles; i++)
            {
                float angle = (i / (float)Nozzles) * Mathf.PI * 2f + Mathf.PI * 0.5f;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.55f;
                Vector3 start = offset + Vector3.forward * (SpineBack - 0.8f);
                Vector3 exit = offset + Vector3.forward * (SpineBack - 1.6f);
                b.AddTube(new[] { start, exit }, new[] { 0.22f, 0.36f }, 12, Metal, false);
                b.AddBlob(exit + Vector3.forward * 0.05f, new Vector3(0.3f, 0.3f, 0.06f), 4, 12, 0f, i, Glow, false);
                nozzles[i] = exit;
            }

            return nozzles;
        }

        private static Mesh BuildRing(Vector3 centre)
        {
            var b = new PropMeshBuilder();
            b.AddTorus(centre, Vector3.forward, 2.3f, 0.26f, 40, 10, Body);
            b.AddTorus(centre + Vector3.forward * 0.22f, Vector3.forward, 2.3f, 0.06f, 40, 6, Metal);
            for (int i = 0; i < Spokes; i++)
            {
                float angle = (i / (float)Spokes) * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                b.AddTube(new[] { centre + direction * 0.35f, centre + direction * 2.1f }, new[] { 0.08f, 0.08f }, 6, Metal, false);
            }

            for (int i = 0; i < RingWindows; i++)
            {
                float angle = (i / (float)RingWindows) * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                b.AddBlob(centre + direction * 2.3f - Vector3.forward * 0.25f, new Vector3(0.09f, 0.09f, 0.04f), 3, 6, 0f, i, Glow, false);
            }

            return b.ToMesh("MothershipRing");
        }
    }
}
