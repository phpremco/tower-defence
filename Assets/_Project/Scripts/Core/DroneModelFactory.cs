using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Builds the three drones from code, each with its own silhouette: a stubby medic quadcopter with a tool arm,
    /// a heavy twin-rotor hauler with a cargo pod and a drill, and a dart-shaped gunship with a ducted fan.
    /// Rotors are one blade mesh placed at every pivot by the view, so each spins on its own axis.
    /// Submesh slots follow <see cref="ModelPalette"/>: body, glow, metal, dark.
    /// </summary>
    public static class DroneModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;

        public readonly struct Parts
        {
            public readonly Mesh Hull;
            public readonly Mesh Rotor;
            public readonly Vector3[] RotorPivots;
            public readonly Vector3 RotorAxis;

            /// <summary>The cargo pod of the hauler, or null. Its single submesh is lit while the drone carries a load.</summary>
            public readonly Mesh Cargo;
            public readonly Vector3 CargoPivot;

            /// <summary>Where the gunship's shots leave, in model space.</summary>
            public readonly Vector3[] Muzzles;

            public Parts(Mesh hull, Mesh rotor, Vector3[] rotorPivots, Vector3 rotorAxis, Mesh cargo, Vector3 cargoPivot, Vector3[] muzzles)
            {
                Hull = hull;
                Rotor = rotor;
                RotorPivots = rotorPivots;
                RotorAxis = rotorAxis;
                Cargo = cargo;
                CargoPivot = cargoPivot;
                Muzzles = muzzles;
            }
        }

        /// <summary>The medic: a rounded quadcopter with a cross on its back, a lamp and a folding tool arm below.</summary>
        public static Parts Repair()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(Vector3.zero, new Vector3(0.2f, 0.09f, 0.24f), 4, 10, 0.015f, 3, Body, false);
            b.AddBlob(new Vector3(0f, 0.05f, 0.1f), new Vector3(0.09f, 0.05f, 0.1f), 3, 8, 0f, 0, Glow, false);
            b.AddBox(new Vector3(0f, 0.1f, -0.04f), new Vector3(0.16f, 0.03f, 0.05f), Glow);
            b.AddBox(new Vector3(0f, 0.1f, -0.04f), new Vector3(0.05f, 0.03f, 0.16f), Glow);
            b.AddBlob(new Vector3(0f, -0.03f, 0.22f), Vector3.one * 0.04f, 3, 6, 0f, 0, Glow, false);

            var pivots = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var arm = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.3f;
                b.AddTube(new[] { arm * 0.3f, arm }, new[] { 0.03f, 0.025f }, 6, Metal, false);
                b.AddTube(new[] { arm, arm + Vector3.up * 0.05f }, new[] { 0.035f, 0.03f }, 6, Dark, false);
                pivots[i] = arm + Vector3.up * 0.06f;
            }

            // The tool arm hangs below and ends in a two-finger claw.
            b.AddTube(new[] { new Vector3(0f, -0.06f, 0f), new Vector3(0f, -0.16f, 0.04f), new Vector3(0f, -0.24f, 0.08f) }, new[] { 0.03f, 0.025f, 0.02f }, 6, Dark, false);
            b.AddBox(new Vector3(-0.03f, -0.28f, 0.1f), new Vector3(0.02f, 0.06f, 0.03f), Metal);
            b.AddBox(new Vector3(0.03f, -0.28f, 0.1f), new Vector3(0.02f, 0.06f, 0.03f), Metal);

            return new Parts(b.ToMesh("Drone_Repair"), Blades(0.13f, 3), pivots, Vector3.up, null, Vector3.zero, new Vector3[0]);
        }

        /// <summary>The hauler: a long body on two big side rotors, a drill in front and a cargo pod slung underneath.</summary>
        public static Parts Harvest()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(Vector3.zero, new Vector3(0.16f, 0.1f, 0.3f), 4, 10, 0.02f, 5, Body, false);
            b.AddBlob(new Vector3(0f, 0.06f, 0.14f), new Vector3(0.08f, 0.05f, 0.1f), 3, 8, 0f, 0, Dark, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBox(new Vector3(0.11f * side, 0.02f, -0.02f), new Vector3(0.03f, 0.04f, 0.26f), Glow);
                b.AddTube(new[] { new Vector3(0.14f * side, 0.02f, 0f), new Vector3(0.36f * side, 0.04f, 0f) }, new[] { 0.035f, 0.03f }, 6, Metal, false);
                b.AddBlob(new Vector3(0.36f * side, 0.04f, 0f), new Vector3(0.06f, 0.04f, 0.06f), 3, 8, 0f, 0, Dark, false);
            }

            // Drill arm forward with a glowing bit; tail fins behind.
            b.AddTube(new[] { new Vector3(0f, -0.02f, 0.2f), new Vector3(0f, -0.1f, 0.34f) }, new[] { 0.035f, 0.025f }, 6, Metal, false);
            b.AddBlob(new Vector3(0f, -0.11f, 0.36f), new Vector3(0.03f, 0.03f, 0.05f), 3, 6, 0f, 0, Glow, false);
            b.AddBox(new Vector3(0f, 0.06f, -0.28f), new Vector3(0.02f, 0.1f, 0.08f), Dark);

            var cargo = new PropMeshBuilder();
            cargo.AddBlob(Vector3.zero, new Vector3(0.11f, 0.08f, 0.14f), 4, 8, 0.015f, 2, 0, false);
            cargo.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(0.06f, 0.06f, 0.06f), 0);

            var pivots = new[] { new Vector3(-0.36f, 0.08f, 0f), new Vector3(0.36f, 0.08f, 0f) };
            return new Parts(b.ToMesh("Drone_Harvest"), Blades(0.2f, 3), pivots, Vector3.up, cargo.ToMesh("Drone_Cargo"), new Vector3(0f, -0.16f, -0.02f), new Vector3[0]);
        }

        /// <summary>The gunship: a dart with swept wings, a gun pod under each, a sensor eye and a ducted fan at the tail.</summary>
        public static Parts Attack()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(Vector3.zero, new Vector3(0.09f, 0.06f, 0.3f), 4, 10, 0.01f, 7, Body, false);
            b.AddBlob(new Vector3(0f, 0.03f, 0.26f), new Vector3(0.04f, 0.03f, 0.05f), 3, 8, 0f, 0, Glow, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBox(new Vector3(0.2f * side, 0f, -0.04f), new Vector3(0.3f, 0.02f, 0.16f), Body, side * 18f);
                b.AddBox(new Vector3(0.22f * side, 0.012f, 0.02f), new Vector3(0.26f, 0.008f, 0.03f), Glow, side * 18f);
                b.AddTube(new[] { new Vector3(0.18f * side, -0.035f, -0.08f), new Vector3(0.18f * side, -0.035f, 0.2f) }, new[] { 0.03f, 0.025f }, 6, Metal, false);
                b.AddBox(new Vector3(0.06f * side, 0.06f, -0.26f), new Vector3(0.015f, 0.08f, 0.08f), Dark);
            }

            Vector3 fan = new Vector3(0f, 0.02f, -0.3f);
            b.AddTorus(fan, Vector3.forward, 0.1f, 0.022f, 12, 6, Dark);
            var muzzles = new[] { new Vector3(-0.18f, -0.035f, 0.22f), new Vector3(0.18f, -0.035f, 0.22f) };
            return new Parts(b.ToMesh("Drone_Attack"), Blades(0.085f, 4), new[] { fan }, Vector3.forward, null, Vector3.zero, muzzles);
        }

        public static Parts For(int kind)
        {
            switch (kind)
            {
                case 0:
                    return Repair();
                case 1:
                    return Harvest();
                default:
                    return Attack();
            }
        }

        // A hub with evenly spread blades, lying in the plane the rotor spins in (around +Y).
        private static Mesh Blades(float radius, int count)
        {
            var r = new PropMeshBuilder();
            r.AddBlob(Vector3.zero, new Vector3(0.03f, 0.015f, 0.03f), 3, 6, 0f, 0, Dark, false);
            for (int i = 0; i < count; i++)
            {
                float yaw = i * 180f / count;
                r.AddBox(Vector3.zero, new Vector3(radius * 2f, 0.006f, radius * 0.22f), Metal, yaw);
            }

            return r.ToMesh("Drone_Rotor");
        }
    }
}
