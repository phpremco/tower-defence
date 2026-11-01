using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Builds the three drones from code, each with its own silhouette: a stubby white medic quadcopter with rotor
    /// guards, a red cross and a welding arm; a heavy hazard-striped hauler on two big ducted rotors with a drill
    /// and a cargo pod; and a dart-shaped gunship with swept wings, gun pods and a glowing engine ring.
    /// Rotors are one blade mesh placed at every pivot by the view, so each spins on its own axis. Submesh slots
    /// follow <see cref="ModelPalette"/>: body, glow, metal, dark and accent (markings).
    /// </summary>
    public static class DroneModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;
        private const int Accent = ModelPalette.Accent;
        private const int Submeshes = 5;

        /// <summary>The colour family of a navigation light.</summary>
        public enum LightKind
        {
            /// <summary>Red, on the left.</summary>
            Port,

            /// <summary>Green, on the right.</summary>
            Starboard,

            /// <summary>White strobe, flashing.</summary>
            Strobe,

            /// <summary>Amber beacon, slow blink.</summary>
            Beacon,
        }

        public readonly struct NavLight
        {
            public readonly Vector3 Position;
            public readonly LightKind Kind;

            public NavLight(Vector3 position, LightKind kind)
            {
                Position = position;
                Kind = kind;
            }
        }

        public readonly struct Parts
        {
            public readonly Mesh Hull;
            public readonly Mesh Rotor;
            public readonly Vector3[] RotorPivots;
            public readonly Vector3 RotorAxis;

            /// <summary>Radius of the blur disc drawn under each spinning rotor.</summary>
            public readonly float RotorRadius;

            /// <summary>The cargo pod of the hauler, or null. Its single submesh is lit while the drone carries a load.</summary>
            public readonly Mesh Cargo;
            public readonly Vector3 CargoPivot;

            /// <summary>The hauler's drill, or null. It spins around +Z while the drone mines.</summary>
            public readonly Mesh Drill;
            public readonly Vector3 DrillPivot;

            /// <summary>Where the gunship's shots leave, in model space.</summary>
            public readonly Vector3[] Muzzles;

            /// <summary>Small blinking lights on the hull.</summary>
            public readonly NavLight[] Lights;

            /// <summary>Engine exhausts that glow brighter with speed and leave a trail, in model space.</summary>
            public readonly Vector3[] Thrusters;

            /// <summary>Where the medic's welding sparks come from, in model space.</summary>
            public readonly Vector3 Tool;

            public Parts(Mesh hull, Mesh rotor, Vector3[] rotorPivots, Vector3 rotorAxis, float rotorRadius, Mesh cargo, Vector3 cargoPivot, Mesh drill, Vector3 drillPivot, Vector3[] muzzles, NavLight[] lights, Vector3[] thrusters, Vector3 tool)
            {
                Hull = hull;
                Rotor = rotor;
                RotorPivots = rotorPivots;
                RotorAxis = rotorAxis;
                RotorRadius = rotorRadius;
                Cargo = cargo;
                CargoPivot = cargoPivot;
                Drill = drill;
                DrillPivot = drillPivot;
                Muzzles = muzzles;
                Lights = lights;
                Thrusters = thrusters;
                Tool = tool;
            }
        }

        /// <summary>The medic: a rounded white quadcopter with guarded rotors, a red cross, a belly lamp and a welding arm.</summary>
        public static Parts Repair()
        {
            var b = new PropMeshBuilder(Submeshes);
            b.AddBlob(Vector3.zero, new Vector3(0.21f, 0.1f, 0.25f), 5, 12, 0.012f, 3, Body, false);
            // Canopy at the front, a dark visor with a lit rim; a status ring around the belly.
            b.AddBlob(new Vector3(0f, 0.055f, 0.11f), new Vector3(0.1f, 0.055f, 0.11f), 4, 10, 0f, 0, Dark, false, Mathf.PI * 0.55f);
            b.AddTorus(new Vector3(0f, 0.045f, 0.11f), Vector3.up, 0.1f, 0.012f, 14, 6, Glow);
            b.AddTorus(new Vector3(0f, -0.05f, 0f), Vector3.up, 0.17f, 0.014f, 18, 6, Glow);
            // The red cross on the back, raised a little so it catches the light.
            b.AddBox(new Vector3(0f, 0.105f, -0.06f), new Vector3(0.17f, 0.018f, 0.055f), Accent);
            b.AddBox(new Vector3(0f, 0.105f, -0.06f), new Vector3(0.055f, 0.018f, 0.17f), Accent);
            // Belly lamp and the antenna on the tail.
            b.AddBlob(new Vector3(0f, -0.09f, 0.14f), new Vector3(0.045f, 0.03f, 0.045f), 3, 8, 0f, 0, Glow, false);
            b.AddTube(new[] { new Vector3(0.06f, 0.08f, -0.18f), new Vector3(0.08f, 0.2f, -0.24f) }, new[] { 0.008f, 0.005f }, 5, Dark, false);
            b.AddBlob(new Vector3(0.08f, 0.21f, -0.24f), Vector3.one * 0.014f, 3, 6, 0f, 0, Glow, false);

            var pivots = new Vector3[4];
            var lights = new NavLight[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var arm = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.31f;
                b.AddTube(new[] { arm * 0.45f, arm * 0.98f }, new[] { 0.034f, 0.026f }, 7, Metal, false);
                b.AddTube(new[] { arm, arm + Vector3.up * 0.055f }, new[] { 0.04f, 0.032f }, 8, Dark, false);
                // A guard ring around every rotor, held by two thin spokes.
                b.AddTorus(arm + Vector3.up * 0.06f, Vector3.up, 0.15f, 0.01f, 18, 5, Dark);
                b.AddTube(new[] { arm + Vector3.up * 0.06f, arm + Vector3.up * 0.06f + arm.normalized * 0.15f }, new[] { 0.007f, 0.007f }, 4, Dark, false);
                pivots[i] = arm + Vector3.up * 0.065f;
                lights[i] = new NavLight(arm + Vector3.up * 0.075f + arm.normalized * 0.15f, arm.x < 0f ? LightKind.Port : LightKind.Starboard);
            }

            lights[2] = new NavLight(new Vector3(0f, 0.11f, -0.2f), LightKind.Strobe);
            lights[3] = new NavLight(new Vector3(0f, 0.1f, 0.08f), LightKind.Beacon);

            // Landing skids under the arms.
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { new Vector3(0.16f * side, -0.08f, -0.14f), new Vector3(0.16f * side, -0.13f, -0.12f), new Vector3(0.16f * side, -0.13f, 0.12f), new Vector3(0.16f * side, -0.08f, 0.14f) }, new[] { 0.012f, 0.014f, 0.014f, 0.012f }, 5, Metal, false);
            }

            // The tool arm hangs below and ends in a two-finger claw with a welding tip between them.
            b.AddTube(new[] { new Vector3(0f, -0.06f, 0f), new Vector3(0f, -0.16f, 0.05f), new Vector3(0f, -0.25f, 0.1f) }, new[] { 0.03f, 0.024f, 0.02f }, 7, Dark, false);
            b.AddBlob(new Vector3(0f, -0.16f, 0.05f), Vector3.one * 0.03f, 3, 7, 0f, 0, Metal, false);
            b.AddBox(new Vector3(-0.032f, -0.29f, 0.12f), new Vector3(0.018f, 0.06f, 0.03f), Metal);
            b.AddBox(new Vector3(0.032f, -0.29f, 0.12f), new Vector3(0.018f, 0.06f, 0.03f), Metal);
            Vector3 tool = new Vector3(0f, -0.31f, 0.12f);
            b.AddBlob(tool, Vector3.one * 0.014f, 3, 6, 0f, 0, Glow, false);

            return new Parts(b.ToMesh("Drone_Repair"), Blades(0.13f, 3), pivots, Vector3.up, 0.135f, null, Vector3.zero, null, Vector3.zero, new Vector3[0], lights, new Vector3[0], tool);
        }

        /// <summary>The hauler: a boxy hull with hazard stripes on two big ducted rotors, a drill in front, a cargo pod below and a rear thruster.</summary>
        public static Parts Harvest()
        {
            var b = new PropMeshBuilder(Submeshes);
            b.AddBlob(Vector3.zero, new Vector3(0.17f, 0.11f, 0.3f), 5, 12, 0.014f, 5, Body, false);
            // Cab window with a lit frame, roof beacon mount, exhaust stacks.
            b.AddBlob(new Vector3(0f, 0.065f, 0.15f), new Vector3(0.09f, 0.055f, 0.1f), 4, 10, 0f, 0, Dark, false, Mathf.PI * 0.6f);
            b.AddTorus(new Vector3(0f, 0.055f, 0.15f), Vector3.up, 0.09f, 0.01f, 12, 5, Glow);
            b.AddBox(new Vector3(0f, 0.115f, -0.04f), new Vector3(0.05f, 0.02f, 0.05f), Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                // Hazard stripes along the flanks: accent and dark blocks in turn.
                for (int s = 0; s < 5; s++)
                {
                    b.AddBox(new Vector3(0.16f * side, 0.045f, -0.2f + s * 0.09f), new Vector3(0.035f, 0.075f, 0.075f), s % 2 == 0 ? Accent : Dark, side * 12f);
                }

                // Side booms to the rotor ducts, with a strut under each.
                b.AddTube(new[] { new Vector3(0.15f * side, 0.03f, 0f), new Vector3(0.38f * side, 0.05f, 0f) }, new[] { 0.04f, 0.03f }, 7, Metal, false);
                b.AddTube(new[] { new Vector3(0.12f * side, -0.05f, -0.06f), new Vector3(0.36f * side, 0.02f, -0.04f) }, new[] { 0.014f, 0.012f }, 5, Dark, false);
                b.AddBlob(new Vector3(0.38f * side, 0.05f, 0f), new Vector3(0.06f, 0.045f, 0.06f), 3, 8, 0f, 0, Dark, false);
                b.AddTorus(new Vector3(0.38f * side, 0.09f, 0f), Vector3.up, 0.22f, 0.018f, 20, 6, Dark);
                b.AddTorus(new Vector3(0.38f * side, 0.09f, 0f), Vector3.up, 0.22f, 0.006f, 20, 4, Accent);
                // Exhaust stacks behind the cab.
                b.AddTube(new[] { new Vector3(0.08f * side, 0.08f, -0.1f), new Vector3(0.09f * side, 0.17f, -0.14f) }, new[] { 0.016f, 0.014f }, 6, Metal, false);
            }

            // Drill boom forward; the drill itself is a separate spinning part. Tail fins and a thruster ring behind.
            b.AddTube(new[] { new Vector3(0f, -0.02f, 0.22f), new Vector3(0f, -0.09f, 0.34f) }, new[] { 0.04f, 0.028f }, 7, Metal, false);
            b.AddBlob(new Vector3(0f, -0.09f, 0.34f), Vector3.one * 0.032f, 3, 8, 0f, 0, Dark, false);
            b.AddBox(new Vector3(0.05f, 0.07f, -0.29f), new Vector3(0.015f, 0.11f, 0.08f), Dark);
            b.AddBox(new Vector3(-0.05f, 0.07f, -0.29f), new Vector3(0.015f, 0.11f, 0.08f), Dark);
            Vector3 thruster = new Vector3(0f, 0f, -0.31f);
            b.AddTorus(thruster, Vector3.forward, 0.055f, 0.014f, 12, 5, Dark);
            b.AddTorus(thruster + Vector3.back * 0.005f, Vector3.forward, 0.035f, 0.01f, 10, 4, Glow);

            var drill = new PropMeshBuilder(Submeshes);
            drill.AddTube(new[] { Vector3.zero, new Vector3(0f, 0f, 0.07f), new Vector3(0f, 0f, 0.13f) }, new[] { 0.035f, 0.024f, 0.004f }, 8, Metal, false);
            for (int ring = 0; ring < 3; ring++)
            {
                drill.AddTorus(new Vector3(0f, 0f, 0.02f + ring * 0.035f), Vector3.forward, 0.036f - ring * 0.009f, 0.006f, 10, 4, ring == 1 ? Glow : Dark);
            }

            var cargo = new PropMeshBuilder(Submeshes);
            cargo.AddBlob(Vector3.zero, new Vector3(0.12f, 0.085f, 0.15f), 4, 10, 0.012f, 2, Body, false);
            cargo.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(0.06f, 0.06f, 0.06f), Dark);
            cargo.AddBox(new Vector3(0f, -0.02f, 0f), new Vector3(0.25f, 0.025f, 0.03f), Glow);
            cargo.AddBox(new Vector3(0f, -0.02f, 0f), new Vector3(0.03f, 0.025f, 0.31f), Glow);

            var pivots = new[] { new Vector3(-0.38f, 0.095f, 0f), new Vector3(0.38f, 0.095f, 0f) };
            var lights = new[]
            {
                new NavLight(new Vector3(-0.6f, 0.1f, 0f), LightKind.Port),
                new NavLight(new Vector3(0.6f, 0.1f, 0f), LightKind.Starboard),
                new NavLight(new Vector3(0f, 0.135f, -0.04f), LightKind.Beacon),
                new NavLight(new Vector3(0f, 0.13f, -0.3f), LightKind.Strobe),
            };
            return new Parts(b.ToMesh("Drone_Harvest"), Blades(0.2f, 3), pivots, Vector3.up, 0.205f, cargo.ToMesh("Drone_Cargo"), new Vector3(0f, -0.17f, -0.02f), drill.ToMesh("Drone_Drill"), new Vector3(0f, -0.09f, 0.36f), new Vector3[0], lights, new[] { thruster }, Vector3.zero);
        }

        /// <summary>The gunship: a dart with swept wings and chevrons, canards, twin gun pods, a sensor eye, a ducted fan and a glowing engine ring.</summary>
        public static Parts Attack()
        {
            var b = new PropMeshBuilder(Submeshes);
            b.AddBlob(Vector3.zero, new Vector3(0.09f, 0.06f, 0.32f), 5, 12, 0.008f, 7, Body, false);
            // Sensor eye in the nose with a dark visor; a spine ridge; the engine ring at the tail.
            b.AddBlob(new Vector3(0f, 0.025f, 0.2f), new Vector3(0.06f, 0.035f, 0.1f), 4, 10, 0f, 0, Dark, false, Mathf.PI * 0.6f);
            b.AddBlob(new Vector3(0f, 0.03f, 0.29f), new Vector3(0.035f, 0.025f, 0.04f), 3, 8, 0f, 0, Glow, false);
            b.AddBox(new Vector3(0f, 0.06f, -0.08f), new Vector3(0.02f, 0.025f, 0.3f), Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                // Swept wing with a glowing leading edge and two accent chevrons.
                b.AddBox(new Vector3(0.21f * side, 0f, -0.05f), new Vector3(0.32f, 0.02f, 0.17f), Body, side * 20f);
                b.AddBox(new Vector3(0.23f * side, 0.012f, 0.01f), new Vector3(0.28f, 0.008f, 0.025f), Glow, side * 20f);
                b.AddBox(new Vector3(0.17f * side, 0.013f, -0.07f), new Vector3(0.09f, 0.008f, 0.03f), Accent, side * 20f + side * 35f);
                b.AddBox(new Vector3(0.27f * side, 0.013f, -0.1f), new Vector3(0.09f, 0.008f, 0.03f), Accent, side * 20f + side * 35f);
                // Canard, gun pod with two barrels, missile rail and a tail fin.
                b.AddBox(new Vector3(0.1f * side, 0.01f, 0.18f), new Vector3(0.1f, 0.012f, 0.06f), Dark, side * 25f);
                b.AddTube(new[] { new Vector3(0.18f * side, -0.04f, -0.1f), new Vector3(0.18f * side, -0.04f, 0.16f) }, new[] { 0.034f, 0.028f }, 8, Metal, false);
                b.AddTube(new[] { new Vector3(0.165f * side, -0.04f, 0.16f), new Vector3(0.165f * side, -0.04f, 0.25f) }, new[] { 0.009f, 0.008f }, 5, Dark, false);
                b.AddTube(new[] { new Vector3(0.195f * side, -0.04f, 0.16f), new Vector3(0.195f * side, -0.04f, 0.25f) }, new[] { 0.009f, 0.008f }, 5, Dark, false);
                b.AddBox(new Vector3(0.3f * side, -0.025f, -0.08f), new Vector3(0.02f, 0.015f, 0.12f), Dark, side * 20f);
                b.AddBox(new Vector3(0.06f * side, 0.065f, -0.27f), new Vector3(0.012f, 0.09f, 0.09f), Dark);
            }

            Vector3 fan = new Vector3(0f, 0.02f, -0.3f);
            b.AddTorus(fan, Vector3.forward, 0.1f, 0.022f, 14, 6, Dark);
            Vector3 thruster = new Vector3(0f, 0.02f, -0.33f);
            b.AddTorus(thruster, Vector3.forward, 0.07f, 0.012f, 12, 5, Glow);
            var muzzles = new[] { new Vector3(-0.18f, -0.04f, 0.26f), new Vector3(0.18f, -0.04f, 0.26f) };
            var lights = new[]
            {
                new NavLight(new Vector3(-0.36f, 0.015f, -0.11f), LightKind.Port),
                new NavLight(new Vector3(0.36f, 0.015f, -0.11f), LightKind.Starboard),
                new NavLight(new Vector3(0f, 0.11f, -0.27f), LightKind.Strobe),
            };
            return new Parts(b.ToMesh("Drone_Attack"), Blades(0.085f, 4), new[] { fan }, Vector3.forward, 0.09f, null, Vector3.zero, null, Vector3.zero, muzzles, lights, new[] { thruster }, Vector3.zero);
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

        // A hub with evenly spread tapered blades, lying in the plane the rotor spins in (around +Y).
        private static Mesh Blades(float radius, int count)
        {
            var r = new PropMeshBuilder(Submeshes);
            r.AddBlob(Vector3.zero, new Vector3(0.03f, 0.016f, 0.03f), 3, 7, 0f, 0, Dark, false);
            for (int i = 0; i < count; i++)
            {
                float yaw = i * 360f / count;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Vector3 side = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
                r.AddBlade(new[] { dir * 0.02f, dir * radius * 0.6f, dir * radius }, new[] { radius * 0.08f, radius * 0.13f, radius * 0.06f }, side, Metal);
            }

            return r.ToMesh("Drone_Rotor");
        }
    }
}
