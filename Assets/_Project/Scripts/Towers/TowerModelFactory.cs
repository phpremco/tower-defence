using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Builds the procedural models of the towers: heavy hard-edged weapon platforms in one design language, so a
    /// tower reads as a serious gun from the fixed camera. A model is a shared pedestal plus two meshes: the head,
    /// which turns towards the target, and the barrel group, which kicks back (and spins, for rotary guns) when
    /// the tower fires. Higher levels add armour, barrels and glow, so an upgrade is visible without any new art.
    /// Submesh slots: body, glow, light metal, dark.
    /// </summary>
    public static class TowerModelFactory
    {
        public const int Body = ModelPalette.Body;
        public const int Glow = ModelPalette.Glow;
        public const int Metal = ModelPalette.Metal;
        public const int Dark = ModelPalette.Dark;

        /// <summary>How much the pedestal mesh is scaled when placed; it is modelled to nearly fill a cell.</summary>
        public const float PedestalScale = 0.66f;

        /// <summary>Height of the placed pedestal, where the head sits.</summary>
        public const float PedestalHeight = 0.5f * PedestalScale;

        // Towers stay well inside their cell: the hero on the road beside them is the yardstick.
        private const float TowerScale = 0.7f;
        private const float GateGunScale = 0.55f;
        private const float ElevationDegrees = 24f;
        private const float ArtilleryElevationDegrees = 38f;
        private const float GlowWhitening = 0.22f;
        private const float GlowEmissionBase = 2.2f;
        private const float GlowEmissionPerLevel = 0.25f;
        private const float BodyTintPerLevel = 0.18f;
        private const float BrassPerLevel = 0.3f;
        private static readonly Color BrassColor = new Color(1f, 0.85f, 0.55f);

        public readonly struct Parts
        {
            public readonly Mesh Head;
            public readonly Mesh Barrel;

            /// <summary>Where the barrel group hangs on the head; recoil and spin happen around this point.</summary>
            public readonly Vector3 BarrelPivot;

            /// <summary>Direction the barrel kicks back against, in barrel space.</summary>
            public readonly Vector3 RecoilAxis;

            /// <summary>Where the muzzle flash sits, in barrel space.</summary>
            public readonly Vector3 Muzzle;

            /// <summary>True for a gatling that spins around its own axis while firing.</summary>
            public readonly bool Rotary;

            /// <summary>Overall size of the head and barrel.</summary>
            public readonly float Scale;

            public Parts(Mesh head, Mesh barrel, Vector3 barrelPivot, Vector3 recoilAxis, Vector3 muzzle, bool rotary, float scale)
            {
                Head = head;
                Barrel = barrel;
                BarrelPivot = barrelPivot;
                RecoilAxis = recoilAxis;
                Muzzle = muzzle;
                Rotary = rotary;
                Scale = scale;
            }
        }

        /// <summary>
        /// The four materials in slot order for a tower of the given colour. Each level runs hotter: the glow gets
        /// brighter and whiter, the body takes on more of the tower colour and the metal warms towards brass, so an
        /// upgraded tower reads as a stronger machine even from far away.
        /// </summary>
        public static Material[] CreateMaterials(Color tower, int level = 0)
        {
            Material[] materials = ModelPalette.Create(tower);
            if (level <= 0)
            {
                return materials;
            }

            float t = level / (float)(TowerDefinition.LevelCount - 1);
            Color glow = Color.Lerp(tower, Color.white, GlowWhitening * t);
            materials[Glow] = SurfaceMaterials.Create(glow, GlowEmissionBase + GlowEmissionPerLevel * level, 0.6f);
            materials[Body] = SurfaceMaterials.Create(Color.Lerp(materials[Body].GetColor("_BaseColor"), tower, BodyTintPerLevel * level), 0f, 0.4f + 0.1f * level, 0.3f);
            materials[Metal] = SurfaceMaterials.Create(Color.Lerp(materials[Metal].GetColor("_BaseColor"), BrassColor, BrassPerLevel * level), 0f, 0.55f, 0.2f);
            return materials;
        }

        /// <summary>
        /// The armoured foot every tower stands on: a hexagonal plinth with lit chevrons and a glowing turret ring
        /// on top. Every level adds a lit rank ring around the plinth.
        /// </summary>
        public static Mesh Pedestal(int level = 0)
        {
            var b = new PropMeshBuilder();
            Hex(b, new Vector3(0f, 0.06f, 0f), 0.48f, 0.12f, Dark);
            Hex(b, new Vector3(0f, 0.26f, 0f), 0.41f, 0.28f, Body);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + 30f;
                Vector3 face = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.24f, 0.36f);
                b.AddBox(face, new Vector3(0.16f, 0.05f, 0.02f), Glow, angle);
                b.AddBox(face + Vector3.up * 0.1f, new Vector3(0.22f, 0.03f, 0.02f), Dark, angle);
            }

            for (int i = 0; i < level; i++)
            {
                b.AddTorus(new Vector3(0f, 0.15f + i * 0.09f, 0f), Vector3.up, 0.46f, 0.02f, 24, 6, Glow);
            }

            Cylinder(b, new Vector3(0f, 0.44f, 0f), Vector3.up, 0.08f, 0.36f, 12, Metal);
            b.AddTorus(new Vector3(0f, PedestalHeight - 0.02f, 0f), Vector3.up, 0.35f, 0.022f, 20, 6, Glow);
            Cylinder(b, new Vector3(0f, PedestalHeight - 0.02f, 0f), Vector3.up, 0.04f, 0.3f, 12, Dark);
            return b.ToMesh($"Pedestal_{level}");
        }

        public static Parts Create(TowerShape shape, int level)
        {
            var head = new PropMeshBuilder();
            var barrel = new PropMeshBuilder();
            Vector3 pivot;
            Vector3 recoil = Vector3.forward;
            Vector3 muzzle;
            bool rotary = false;
            float scale = TowerScale;
            switch (shape)
            {
                case TowerShape.PulseRifle: PulseRifle(head, barrel, level, out pivot, out muzzle); rotary = true; break;
                case TowerShape.Cannon: Cannon(head, barrel, level, out pivot, out muzzle); break;
                case TowerShape.Rocket: Rocket(head, barrel, level, out pivot, out muzzle, out recoil); break;
                case TowerShape.Laser: Laser(head, barrel, level, out pivot, out muzzle); break;
                case TowerShape.Cryo: Cryo(head, barrel, level, out pivot, out muzzle); break;
                case TowerShape.Tesla: Tesla(head, level, out pivot, out muzzle); break;
                case TowerShape.Railgun: Railgun(head, barrel, level, out pivot, out muzzle); break;
                case TowerShape.Support: Support(head, level, out pivot, out muzzle); break;
                case TowerShape.Artillery: Artillery(head, barrel, level, out pivot, out muzzle, out recoil); break;
                case TowerShape.StormRifle: StormRifle(head, barrel, level, out pivot, out muzzle); rotary = true; break;
                case TowerShape.CryoBeam: CryoBeam(head, barrel, level, out pivot, out muzzle); break;
                case TowerShape.GateGun: GateGun(head, barrel, level, out pivot, out muzzle); scale = GateGunScale; break;
                default: pivot = Vector3.zero; muzzle = Vector3.forward * 0.5f; break;
            }

            // A mesh without triangles would upset some renderers, so every barrel gets at least a stub.
            Stub(barrel);
            return new Parts(head.ToMesh($"{shape}_head_{level}"), barrel.ToMesh($"{shape}_barrel_{level}"), pivot, recoil, muzzle, rotary, scale);
        }

        private static void Stub(PropMeshBuilder b) => b.AddBlob(Vector3.zero, Vector3.one * 0.001f, 2, 3, 0f, 0, Dark, false);

        private static void Tube(PropMeshBuilder b, Vector3 from, Vector3 to, float r0, float r1, int sides, int slot, bool flat = false) =>
            b.AddTube(new[] { from, to }, new[] { r0, r1 }, sides, slot, flat);

        private static void Cylinder(PropMeshBuilder b, Vector3 centre, Vector3 axis, float length, float radius, int sides, int slot, bool flat = false)
        {
            Vector3 half = axis.normalized * (length * 0.5f);
            b.AddTube(new[] { centre - half, centre + half }, new[] { radius, radius }, sides, slot, flat);
        }

        private static void Hex(PropMeshBuilder b, Vector3 centre, float radius, float height, int slot) =>
            Cylinder(b, centre, Vector3.up, height, radius, 6, slot, true);

        private static void Sphere(PropMeshBuilder b, Vector3 centre, float radius, int slot, bool flat = false) =>
            b.AddBlob(centre, Vector3.one * radius, 5, 10, 0f, 0, slot, flat);

        // The armoured turret hull that most heads share: a dark lower hull, a lighter upper body, a chamfered
        // top plate and a sensor eye on the front edge. Width and depth are the hull size.
        private static void Hull(PropMeshBuilder b, float width, float depth, float height)
        {
            b.AddBox(new Vector3(0f, height * 0.25f, 0f), new Vector3(width, height * 0.5f, depth), Dark);
            b.AddBox(new Vector3(0f, height * 0.7f, 0f), new Vector3(width * 0.94f, height * 0.45f, depth * 0.94f), Body);
            b.AddBox(new Vector3(0f, height * 0.96f, 0f), new Vector3(width * 0.78f, height * 0.1f, depth * 0.78f), Metal);
            b.AddBox(new Vector3(0f, height * 0.6f, depth * 0.48f), new Vector3(width * 0.4f, height * 0.12f, 0.02f), Glow);
        }

        // A plate of sloped armour on one side of the hull; a positive side is the right.
        private static void Skirt(PropMeshBuilder b, float side, float x, float y, float depth)
        {
            b.AddBox(new Vector3(x * side, y, 0f), new Vector3(0.05f, 0.2f, depth), Dark, Quaternion.Euler(0f, 0f, side * 18f));
            b.AddBox(new Vector3(x * side, y + 0.04f, 0f), new Vector3(0.02f, 0.04f, depth * 0.7f), Glow, Quaternion.Euler(0f, 0f, side * 18f));
        }

        // A gun tube with a muzzle brake: the tube, a wider dark collar at the mouth with vent slots, and a glow band.
        private static void GunBarrel(PropMeshBuilder b, Vector3 from, Vector3 to, float radius, int slot, bool brake = true)
        {
            Vector3 axis = (to - from).normalized;
            Tube(b, from, to, radius, radius * 0.9f, 10, slot);
            Tube(b, from, from + axis * (radius * 1.5f), radius * 1.25f, radius * 1.25f, 10, Dark);
            b.AddTorus(from + axis * ((to - from).magnitude * 0.4f), axis, radius * 0.95f, radius * 0.3f, 12, 5, Glow);
            if (brake)
            {
                float length = radius * 2.4f;
                Tube(b, to - axis * length, to + axis * (radius * 0.3f), radius * 1.3f, radius * 1.3f, 10, Dark);
                Quaternion along = Quaternion.LookRotation(axis);
                b.AddBox(to - axis * (length * 0.5f), new Vector3(radius * 3.2f, radius * 0.5f, length * 0.5f), Dark, along);
                b.AddBox(to - axis * (length * 0.5f), new Vector3(radius * 0.5f, radius * 3.2f, length * 0.5f), Dark, along);
            }
        }

        // Rapid pulse gatling: six slim barrels around a hub in a sturdy yoke, an ammo drum per upgrade and a heat
        // shroud with a targeting mast at the top level. The whole barrel cluster spins while firing.
        private static void PulseRifle(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.62f, 0.62f, 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                head.AddBox(new Vector3(0.27f * side, 0.36f, -0.04f), new Vector3(0.07f, 0.34f, 0.3f), Metal);
                head.AddBox(new Vector3(0.27f * side, 0.46f, -0.04f), new Vector3(0.075f, 0.03f, 0.22f), Glow);
            }

            head.AddBox(new Vector3(0f, 0.44f, -0.26f), new Vector3(0.3f, 0.2f, 0.2f), Body);
            head.AddBox(new Vector3(0f, 0.44f, -0.37f), new Vector3(0.2f, 0.12f, 0.02f), Glow);
            if (level >= 1)
            {
                Cylinder(head, new Vector3(-0.33f, 0.3f, -0.2f), Vector3.right, 0.12f, 0.12f, 10, Dark);
                Cylinder(head, new Vector3(-0.4f, 0.3f, -0.2f), Vector3.right, 0.02f, 0.08f, 10, Glow);
            }

            if (level >= 2)
            {
                Cylinder(head, new Vector3(0.33f, 0.3f, -0.2f), Vector3.right, 0.12f, 0.12f, 10, Dark);
                Cylinder(head, new Vector3(0.4f, 0.3f, -0.2f), Vector3.right, 0.02f, 0.08f, 10, Glow);
                Tube(head, new Vector3(-0.2f, 0.5f, -0.3f), new Vector3(-0.2f, 0.9f, -0.3f), 0.015f, 0.008f, 4, Dark);
                Sphere(head, new Vector3(-0.2f, 0.92f, -0.3f), 0.03f, Glow);
            }

            pivot = new Vector3(0f, 0.4f, 0f);
            Cylinder(barrel, new Vector3(0f, 0f, -0.02f), Vector3.forward, 0.3f, 0.12f, 10, Dark);
            barrel.AddTorus(new Vector3(0f, 0f, 0.1f), Vector3.forward, 0.12f, 0.03f, 12, 5, Glow);
            Cylinder(barrel, new Vector3(0f, 0f, 0.58f), Vector3.forward, 0.06f, 0.12f, 10, Dark);
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.075f;
                Tube(barrel, offset + Vector3.forward * 0.1f, offset + Vector3.forward * 0.78f, 0.028f, 0.026f, 6, Metal);
            }

            if (level >= 2)
            {
                barrel.AddBox(new Vector3(0f, 0.12f, 0.35f), new Vector3(0.22f, 0.03f, 0.4f), Body);
            }

            muzzle = new Vector3(0f, 0f, 0.82f);
        }

        // Heavy cannon: a squat armoured turret with a sloped glacis, a thick gun in a mantlet and a muzzle brake.
        // Upgrades add armour skirts and a second gun.
        private static void Cannon(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.74f, 0.72f, 0.3f);
            head.AddBox(new Vector3(0f, 0.2f, 0.3f), new Vector3(0.6f, 0.04f, 0.26f), Dark, Quaternion.Euler(-35f, 0f, 0f));
            Cylinder(head, new Vector3(0.22f, 0.33f, -0.22f), Vector3.up, 0.06f, 0.1f, 8, Dark);
            Sphere(head, new Vector3(0.22f, 0.37f, -0.22f), 0.04f, Glow);
            head.AddBox(new Vector3(-0.2f, 0.34f, -0.24f), new Vector3(0.16f, 0.08f, 0.16f), Metal);
            if (level >= 1)
            {
                Skirt(head, 1f, 0.39f, 0.13f, 0.62f);
                Skirt(head, -1f, 0.39f, 0.13f, 0.62f);
            }

            pivot = new Vector3(0f, 0.3f, 0.1f);
            float[] xs = level >= 2 ? new[] { -0.13f, 0.13f } : new[] { 0f };
            float radius = level >= 2 ? 0.085f : 0.1f;
            barrel.AddBox(new Vector3(0f, 0f, 0.08f), new Vector3(level >= 2 ? 0.48f : 0.34f, 0.3f, 0.24f), Dark);
            barrel.AddBox(new Vector3(0f, 0f, 0.21f), new Vector3(level >= 2 ? 0.4f : 0.26f, 0.22f, 0.04f), Metal);
            foreach (float x in xs)
            {
                GunBarrel(barrel, new Vector3(x, 0f, 0.2f), new Vector3(x, 0f, 0.98f + 0.06f * level), radius, Metal);
                for (int i = 0; i < level; i++)
                {
                    barrel.AddTorus(new Vector3(x, 0f, 0.5f + i * 0.14f), Vector3.forward, radius * 1.15f, radius * 0.25f, 12, 5, Glow);
                }
            }

            if (level >= 2)
            {
                Tube(head, new Vector3(-0.3f, 0.38f, -0.28f), new Vector3(-0.3f, 0.85f, -0.28f), 0.015f, 0.008f, 4, Dark);
                Sphere(head, new Vector3(-0.3f, 0.87f, -0.28f), 0.035f, Glow);
            }

            muzzle = new Vector3(0f, 0f, 1.04f + 0.06f * level);
        }

        // Missile battery: a tilted box launcher packed with tube cells, more and smaller with every level, plus a
        // small tracking dish on the hull. The pod kicks back along its own tilt.
        private static void Rocket(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle, out Vector3 recoil)
        {
            Hull(head, 0.64f, 0.6f, 0.22f);
            head.AddBox(new Vector3(-0.22f, 0.34f, -0.18f), new Vector3(0.12f, 0.24f, 0.12f), Metal);
            head.AddBlob(new Vector3(-0.22f, 0.5f, -0.18f), new Vector3(0.12f, 0.05f, 0.12f), 3, 10, 0f, 0, Metal, false, Mathf.PI * 0.5f);
            Sphere(head, new Vector3(-0.22f, 0.53f, -0.18f), 0.025f, Glow);
            head.AddBox(new Vector3(0.24f, 0.3f, -0.2f), new Vector3(0.16f, 0.16f, 0.16f), Dark);
            head.AddBox(new Vector3(0.24f, 0.39f, -0.2f), new Vector3(0.12f, 0.02f, 0.12f), Glow);
            for (int side = -1; side <= 1; side += 2)
            {
                head.AddBox(new Vector3(0.2f * side, 0.3f, 0.05f), new Vector3(0.05f, 0.2f, 0.18f), Metal);
            }

            Quaternion tilt = Quaternion.Euler(-ElevationDegrees, 0f, 0f);
            recoil = tilt * Vector3.forward;
            pivot = new Vector3(0f, 0.34f, 0.02f);
            int columns = 2 + level;
            int rows = level == 0 ? 2 : 3;
            float width = 0.44f + 0.06f * level;
            float height = 0.28f + 0.04f * level;
            barrel.AddBox(new Vector3(0f, 0f, 0.05f), new Vector3(width, height, 0.62f), Body, tilt);
            barrel.AddBox(new Vector3(0f, 0f, -0.27f), new Vector3(width * 1.04f, height * 1.04f, 0.06f), Dark, tilt);
            barrel.AddBox(new Vector3(0f, height * 0.5f, 0.05f), new Vector3(width * 0.6f, 0.02f, 0.5f), Glow, tilt);
            float cell = Mathf.Min(width / columns, height / rows);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var local = new Vector3((c - (columns - 1) * 0.5f) * (width / columns), (r - (rows - 1) * 0.5f) * (height / rows), 0.36f);
                    Vector3 at = tilt * local;
                    Cylinder(barrel, at, recoil, 0.04f, cell * 0.36f, 8, Dark);
                    Sphere(barrel, at + recoil * 0.01f, cell * 0.24f, Glow);
                }
            }

            if (level >= 2)
            {
                // Side racks of standby missiles, nose glow out.
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 at = tilt * new Vector3((width * 0.5f + 0.07f) * side, -0.06f + i * 0.09f, 0.1f);
                        Cylinder(barrel, at, recoil, 0.4f, 0.03f, 6, Metal);
                        Sphere(barrel, at + recoil * 0.21f, 0.03f, Glow);
                    }
                }
            }

            muzzle = tilt * new Vector3(0f, 0f, 0.45f);
        }

        // Precision laser: a sleek emitter with glowing focus rings along the beam tube, twin capacitor cylinders and
        // cooling fins on the hull. Upgrades add fins and a larger charge ring.
        private static void Laser(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.56f, 0.7f, 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                Cylinder(head, new Vector3(0.2f * side, 0.32f, -0.15f), Vector3.forward, 0.4f, 0.075f, 10, Body);
                Cylinder(head, new Vector3(0.2f * side, 0.32f, -0.36f), Vector3.forward, 0.03f, 0.06f, 10, Glow);
                Cylinder(head, new Vector3(0.2f * side, 0.32f, -0.15f), Vector3.forward, 0.06f, 0.085f, 10, Dark);
            }

            int fins = 2 + level * 2;
            for (int i = 0; i < fins; i++)
            {
                head.AddBox(new Vector3(0f, 0.38f, -0.28f + i * 0.07f), new Vector3(0.26f, 0.12f, 0.015f), Metal);
            }

            if (level >= 2)
            {
                head.AddTorus(new Vector3(0f, 0.36f, -0.34f), Vector3.forward, 0.2f, 0.025f, 16, 6, Glow);
            }

            pivot = new Vector3(0f, 0.32f, 0.1f);
            Tube(barrel, new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, 0.45f), 0.08f, 0.065f, 10, Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                barrel.AddBox(new Vector3(0.09f * side, 0f, 0.6f), new Vector3(0.02f, 0.06f, 0.6f), Metal);
            }

            int rings = 3 + level;
            for (int i = 0; i < rings; i++)
            {
                barrel.AddTorus(new Vector3(0f, 0f, 0.5f + i * (0.4f / rings)), Vector3.forward, 0.055f, 0.018f, 12, 5, Glow);
            }

            Tube(barrel, new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, 0.98f), 0.028f + 0.008f * level, 0.028f + 0.008f * level, 8, Glow);
            Sphere(barrel, new Vector3(0f, 0f, 0.98f), 0.045f + 0.02f * level, Glow);
            if (level >= 2)
            {
                // A second pair of rails above the first and a big focus lens.
                for (int side = -1; side <= 1; side += 2)
                {
                    barrel.AddBox(new Vector3(0.05f * side, 0.1f, 0.55f), new Vector3(0.02f, 0.04f, 0.5f), Metal);
                }

                barrel.AddTorus(new Vector3(0f, 0f, 0.98f), Vector3.forward, 0.1f, 0.018f, 14, 5, Glow);
            }

            muzzle = new Vector3(0f, 0f, 1.02f);
        }

        // Cryo projector: a frosted coolant tank with glowing frost rings, a wide nozzle with vent flaps, and
        // coolant canisters that multiply with upgrades; the top level grows a crown of ice.
        private static void Cryo(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.6f, 0.62f, 0.2f);
            Cylinder(head, new Vector3(0f, 0.36f, -0.08f), Vector3.forward, 0.5f, 0.17f, 12, Body);
            Sphere(head, new Vector3(0f, 0.36f, -0.33f), 0.16f, Metal);
            for (int i = 0; i < 3; i++)
            {
                head.AddTorus(new Vector3(0f, 0.36f, -0.22f + i * 0.13f), Vector3.forward, 0.175f, 0.02f, 14, 5, Glow);
            }

            int canisters = 2 + level;
            for (int i = 0; i < canisters; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float z = -0.2f + (i / 2) * 0.2f;
                Cylinder(head, new Vector3(0.27f * side, 0.3f, z), Vector3.up, 0.26f, 0.055f, 8, Metal);
                Sphere(head, new Vector3(0.27f * side, 0.44f, z), 0.045f, Glow);
            }

            if (level >= 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = (i / 5f - 0.5f) * Mathf.PI;
                    var root = new Vector3(Mathf.Sin(angle) * 0.16f, 0.5f, -0.1f + Mathf.Cos(angle) * 0.05f);
                    head.AddTube(new[] { root, root + new Vector3(Mathf.Sin(angle) * 0.1f, 0.22f + 0.06f * (i % 2), 0f) }, new[] { 0.04f, 0f }, 5, Glow, true);
                }
            }

            pivot = new Vector3(0f, 0.36f, 0.17f);
            Tube(barrel, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.4f), 0.13f, 0.09f, 10, Dark);
            Tube(barrel, new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 0.6f), 0.09f, 0.14f, 10, Metal);
            for (int i = 0; i <= level; i++)
            {
                barrel.AddTorus(new Vector3(0f, 0f, 0.42f - i * 0.12f), Vector3.forward, 0.1f + i * 0.015f, 0.025f, 12, 5, Glow);
            }

            Sphere(barrel, new Vector3(0f, 0f, 0.58f), 0.08f + 0.015f * level, Glow);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                Vector3 at = Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, 0.16f, 0.64f);
                barrel.AddBox(at, new Vector3(0.08f, 0.015f, 0.14f), Metal, Quaternion.Euler(-20f, 0f, angle));
            }

            muzzle = new Vector3(0f, 0f, 0.68f);
        }

        // Tesla coil: a tall column of glowing toroidal coils under a caged discharge sphere, with capacitor
        // cylinders around the foot and arc rods running up to the top. More coils and rods per level.
        private static void Tesla(PropMeshBuilder head, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.6f, 0.6f, 0.16f);
            Tube(head, new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.95f, 0f), 0.13f, 0.09f, 8, Dark);
            int coils = 3 + level;
            for (int i = 0; i < coils; i++)
            {
                float y = 0.26f + i * 0.16f;
                head.AddTorus(new Vector3(0f, y, 0f), Vector3.up, 0.2f - i * 0.012f, 0.045f, 16, 6, Glow);
                head.AddTorus(new Vector3(0f, y + 0.08f, 0f), Vector3.up, 0.17f - i * 0.012f, 0.018f, 14, 5, Dark);
            }

            float top = 1.0f + 0.04f * level;
            Sphere(head, new Vector3(0f, top, 0f), 0.14f + 0.015f * level, Glow);
            for (int i = 0; i < level; i++)
            {
                head.AddTorus(new Vector3(0f, top + 0.05f - i * 0.1f, 0f), Vector3.up, 0.3f + i * 0.06f, 0.015f, 20, 5, Glow);
            }
            head.AddTorus(new Vector3(0f, top, 0f), Vector3.up, 0.19f + 0.015f * level, 0.012f, 16, 4, Metal);
            head.AddTorus(new Vector3(0f, top, 0f), Vector3.right, 0.19f + 0.015f * level, 0.012f, 16, 4, Metal);
            int rods = 3 + level;
            for (int i = 0; i < rods; i++)
            {
                float angle = i / (float)rods * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Cylinder(head, dir * 0.24f + Vector3.up * 0.26f, Vector3.up, 0.2f, 0.05f, 8, Body);
                Sphere(head, dir * 0.24f + Vector3.up * 0.37f, 0.035f, Glow);
                head.AddTube(new[] { dir * 0.24f + Vector3.up * 0.38f, dir * 0.3f + Vector3.up * (top * 0.6f), dir * 0.22f + Vector3.up * top }, new[] { 0.014f, 0.012f, 0.008f }, 4, Metal, false);
            }

            pivot = new Vector3(0f, top, 0f);
            muzzle = Vector3.zero;
        }

        // Railgun: a long pair of magnetic rails with a glowing channel, studded accelerator rings and a heavy
        // power cell behind. Upgrades add capacitor banks and bracing.
        private static void Railgun(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.56f, 0.7f, 0.2f);
            head.AddBox(new Vector3(0f, 0.36f, -0.2f), new Vector3(0.44f, 0.3f, 0.34f), Body);
            head.AddBox(new Vector3(0f, 0.36f, -0.38f), new Vector3(0.3f, 0.16f, 0.02f), Glow);
            head.AddBox(new Vector3(0f, 0.52f, -0.2f), new Vector3(0.34f, 0.03f, 0.26f), Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                head.AddBox(new Vector3(0.22f * side, 0.3f, 0.1f), new Vector3(0.05f, 0.2f, 0.24f), Metal);
            }

            if (level >= 1)
            {
                for (int i = 0; i < 2 + level; i++)
                {
                    head.AddBox(new Vector3(-0.15f + i * 0.1f, 0.58f, -0.2f), new Vector3(0.06f, 0.09f, 0.2f), Glow);
                }
            }

            pivot = new Vector3(0f, 0.34f, 0.04f);
            float length = 1.1f + 0.1f * level;
            for (int side = -1; side <= 1; side += 2)
            {
                barrel.AddBox(new Vector3(0.075f * side, 0f, length * 0.5f - 0.05f), new Vector3(0.05f, 0.11f, length), Metal);
            }

            barrel.AddBox(new Vector3(0f, 0f, length * 0.5f - 0.05f), new Vector3(0.05f, 0.05f + 0.03f * level, length * 0.95f), Glow);
            if (level >= 2)
            {
                barrel.AddBox(new Vector3(0f, 0.1f, length * 0.5f - 0.05f), new Vector3(0.03f, 0.03f, length * 0.9f), Glow);
                barrel.AddBox(new Vector3(0f, -0.1f, length * 0.5f - 0.05f), new Vector3(0.03f, 0.03f, length * 0.9f), Glow);
            }
            int rings = 4 + level;
            for (int i = 0; i < rings; i++)
            {
                float z = 0.1f + i * ((length - 0.25f) / rings);
                barrel.AddTorus(new Vector3(0f, 0f, z), Vector3.forward, 0.12f, 0.03f, 10, 5, Dark);
                Sphere(barrel, new Vector3(0f, 0.14f, z), 0.025f, Glow);
            }

            if (level >= 2)
            {
                barrel.AddBox(new Vector3(0f, -0.09f, length * 0.45f), new Vector3(0.2f, 0.025f, length * 0.7f), Metal);
            }

            muzzle = new Vector3(0f, 0f, length);
        }

        // Support projector: a mast with a floating field core, curved antenna fins, a glowing halo per upgrade and
        // emitter studs on the hull. It has no barrel because it only boosts its neighbours.
        private static void Support(PropMeshBuilder head, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.58f, 0.58f, 0.16f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = Quaternion.Euler(0f, i * 90f + 45f, 0f) * new Vector3(0f, 0.2f, 0.33f);
                Cylinder(head, corner, Vector3.up, 0.1f, 0.04f, 6, Metal);
                Sphere(head, corner + Vector3.up * 0.07f, 0.03f, Glow);
            }

            Tube(head, new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.62f, 0f), 0.08f, 0.04f, 8, Dark);
            Sphere(head, new Vector3(0f, 0.72f, 0f), 0.14f + 0.015f * level, Glow);
            int fins = 3 + level;
            for (int i = 0; i < fins; i++)
            {
                float angle = i / (float)fins * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                head.AddTube(new[] { dir * 0.14f + Vector3.up * 0.18f, dir * 0.34f + Vector3.up * 0.5f, dir * 0.28f + Vector3.up * 0.85f }, new[] { 0.035f, 0.03f, 0.012f }, 5, Metal, true);
            }

            for (int i = 0; i < level; i++)
            {
                head.AddTorus(new Vector3(0f, 0.72f + (i - 0.5f) * 0.14f, 0f), Vector3.up, 0.32f, 0.018f, 20, 6, Glow);
            }

            pivot = new Vector3(0f, 0.72f, 0f);
            muzzle = Vector3.zero;
        }

        // Artillery: a wide carriage on outriggers with a cradle holding a huge barrel at high elevation, with
        // twin recoil cylinders under it. Upgrades lengthen the gun and add shell racks.
        private static void Artillery(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle, out Vector3 recoil)
        {
            Hull(head, 0.8f, 0.76f, 0.22f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                Quaternion turn = Quaternion.Euler(0f, angle, 0f);
                head.AddBox(turn * new Vector3(0f, 0.08f, 0.5f), new Vector3(0.14f, 0.1f, 0.3f), Dark, angle);
                head.AddBox(turn * new Vector3(0f, 0.14f, 0.6f), new Vector3(0.1f, 0.03f, 0.1f), Glow, angle);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                head.AddBox(new Vector3(0.24f * side, 0.36f, -0.1f), new Vector3(0.08f, 0.3f, 0.34f), Metal);
                head.AddBox(new Vector3(0.24f * side, 0.36f, -0.28f), new Vector3(0.06f, 0.2f, 0.02f), Glow);
            }

            if (level >= 1)
            {
                for (int i = 0; i < 2 + level; i++)
                {
                    Cylinder(head, new Vector3(-0.3f + i * 0.12f, 0.3f, -0.3f), Vector3.forward, 0.16f, 0.035f, 6, Body);
                    Sphere(head, new Vector3(-0.3f + i * 0.12f, 0.3f, -0.22f), 0.03f, Glow);
                }
            }

            Quaternion tilt = Quaternion.Euler(-ArtilleryElevationDegrees, 0f, 0f);
            recoil = tilt * Vector3.forward;
            pivot = new Vector3(0f, 0.4f, -0.1f);
            float length = 1.05f + 0.12f * level;
            barrel.AddBox(new Vector3(0f, 0f, -0.1f), new Vector3(0.3f, 0.3f, 0.3f), Dark, tilt);
            barrel.AddBox(tilt * new Vector3(0f, 0f, -0.27f), new Vector3(0.24f, 0.24f, 0.06f), Metal, tilt);
            GunBarrel(barrel, tilt * new Vector3(0f, 0f, 0.05f), tilt * new Vector3(0f, 0f, length), 0.13f + 0.015f * level, Body);
            for (int i = 0; i < level; i++)
            {
                barrel.AddTorus(tilt * new Vector3(0f, 0f, 0.55f + i * 0.16f), recoil, 0.16f, 0.03f, 14, 5, Glow);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Cylinder(barrel, tilt * new Vector3(0.1f * side, -0.17f, 0.3f), recoil, 0.5f, 0.04f, 8, Metal);
            }

            muzzle = tilt * new Vector3(0f, 0f, length + 0.06f);
        }

        // Storm rifle: a four-barrel heavy gatling wrapped in lightning coils, with capacitor fins on the hull.
        private static void StormRifle(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.66f, 0.66f, 0.22f);
            for (int side = -1; side <= 1; side += 2)
            {
                head.AddBox(new Vector3(0.28f * side, 0.38f, -0.06f), new Vector3(0.08f, 0.36f, 0.32f), Metal);
                for (int i = 0; i < 2 + level; i++)
                {
                    Sphere(head, new Vector3(0.33f * side, 0.3f + i * 0.08f, -0.18f + i * 0.09f), 0.03f, Glow);
                }
            }

            head.AddBox(new Vector3(0f, 0.46f, -0.28f), new Vector3(0.34f, 0.24f, 0.2f), Body);
            head.AddTorus(new Vector3(0f, 0.46f, -0.39f), Vector3.forward, 0.09f, 0.02f, 12, 5, Glow);
            if (level >= 2)
            {
                Tube(head, new Vector3(0.2f, 0.56f, -0.3f), new Vector3(0.2f, 1f, -0.3f), 0.015f, 0.008f, 4, Dark);
                Sphere(head, new Vector3(0.2f, 1.02f, -0.3f), 0.035f, Glow);
            }

            pivot = new Vector3(0f, 0.42f, 0f);
            Cylinder(barrel, new Vector3(0f, 0f, 0f), Vector3.forward, 0.34f, 0.14f, 10, Dark);
            Cylinder(barrel, new Vector3(0f, 0f, 0.62f), Vector3.forward, 0.06f, 0.14f, 10, Dark);
            if (level >= 1)
            {
                barrel.AddBox(new Vector3(0f, 0.17f, 0.4f), new Vector3(0.26f, 0.03f, 0.44f), Body);
                barrel.AddBox(new Vector3(0f, 0.19f, 0.4f), new Vector3(0.1f, 0.01f, 0.36f), Glow);
            }
            for (int i = 0; i < 2 + level; i++)
            {
                barrel.AddTorus(new Vector3(0f, 0f, 0.22f + i * 0.12f), Vector3.forward, 0.15f, 0.025f, 12, 5, Glow);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                Vector3 offset = Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, 0.085f, 0f);
                Tube(barrel, offset + Vector3.forward * 0.1f, offset + Vector3.forward * 0.84f, 0.04f, 0.035f, 8, Metal);
                Tube(barrel, offset + Vector3.forward * 0.76f, offset + Vector3.forward * 0.86f, 0.05f, 0.05f, 8, Dark);
            }

            muzzle = new Vector3(0f, 0f, 0.9f);
        }

        // Cryo beam: a long hexagonal emitter around a glowing prism core, frost crystals along the housing and
        // coolant tanks behind. The crystals multiply with every level.
        private static void CryoBeam(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Hull(head, 0.6f, 0.66f, 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                Cylinder(head, new Vector3(0.2f * side, 0.34f, -0.2f), Vector3.forward, 0.36f, 0.08f, 10, Metal);
                Cylinder(head, new Vector3(0.2f * side, 0.34f, -0.39f), Vector3.forward, 0.03f, 0.065f, 10, Glow);
                head.AddTorus(new Vector3(0.2f * side, 0.34f, -0.1f), Vector3.forward, 0.085f, 0.015f, 12, 5, Glow);
            }

            head.AddBox(new Vector3(0f, 0.3f, 0.1f), new Vector3(0.3f, 0.2f, 0.3f), Body);
            int crystals = 3 + level * 2;
            for (int i = 0; i < crystals; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var root = new Vector3(0.14f * side, 0.4f, -0.1f + (i / 2) * 0.1f);
                head.AddTube(new[] { root, root + new Vector3(0.1f * side, 0.26f + 0.05f * (i % 3), 0.02f) }, new[] { 0.045f, 0f }, 5, Glow, true);
            }

            pivot = new Vector3(0f, 0.34f, 0.2f);
            Cylinder(barrel, new Vector3(0f, 0f, 0.3f), Vector3.forward, 0.7f, 0.09f, 6, Dark, true);
            Cylinder(barrel, new Vector3(0f, 0f, 0f), Vector3.forward, 0.12f, 0.12f, 6, Metal, true);
            for (int i = 0; i < 3; i++)
            {
                Cylinder(barrel, new Vector3(0f, 0f, 0.2f + i * 0.2f), Vector3.forward, 0.04f, 0.1f, 6, Metal, true);
            }

            float prism = 0.055f + 0.015f * level;
            barrel.AddTube(new[] { new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, 0.95f + 0.1f * level), new Vector3(0f, 0f, 1.15f + 0.1f * level) }, new[] { prism, prism, 0f }, 6, Glow, true);
            for (int i = 0; i < level; i++)
            {
                barrel.AddTorus(new Vector3(0f, 0f, 0.75f + i * 0.18f), Vector3.forward, 0.09f, 0.018f, 6, 4, Glow, true);
            }
            muzzle = new Vector3(0f, 0f, 1.0f);
        }

        // The small autocannon that a gate grows with its gun upgrades: a compact hull on the crossbeam with one
        // barrel, then two and a sensor mast.
        private static void GateGun(PropMeshBuilder head, PropMeshBuilder barrel, int level, out Vector3 pivot, out Vector3 muzzle)
        {
            Cylinder(head, new Vector3(0f, 0.04f, 0f), Vector3.up, 0.08f, 0.24f, 10, Dark);
            Hull(head, 0.42f, 0.44f, 0.22f);
            if (level >= 2)
            {
                Tube(head, new Vector3(-0.12f, 0.24f, -0.14f), new Vector3(-0.12f, 0.55f, -0.14f), 0.012f, 0.008f, 4, Dark);
                Sphere(head, new Vector3(-0.12f, 0.57f, -0.14f), 0.03f, Glow);
            }

            pivot = new Vector3(0f, 0.2f, 0.1f);
            float[] xs = level >= 2 ? new[] { -0.08f, 0.08f } : new[] { 0f };
            barrel.AddBox(new Vector3(0f, 0f, 0.04f), new Vector3(level >= 2 ? 0.3f : 0.18f, 0.16f, 0.16f), Dark);
            foreach (float x in xs)
            {
                GunBarrel(barrel, new Vector3(x, 0f, 0.1f), new Vector3(x, 0f, 0.62f), 0.045f, Metal);
            }

            muzzle = new Vector3(0f, 0f, 0.68f);
        }
    }
}
