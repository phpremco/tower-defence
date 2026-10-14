using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Builds the procedural models of the towers. A model is two meshes: the head, which turns towards the target,
    /// and the barrel, which kicks back when the tower fires. Higher levels add armour, barrels and glow, so an
    /// upgrade is visible without any new art. Submesh slots: body, glow, accent metal, dark.
    /// </summary>
    public static class TowerModelFactory
    {
        public const int Body = 0;
        public const int Glow = 1;
        public const int Accent = 2;
        public const int Dark = 3;


        public readonly struct Parts
        {
            public readonly Mesh Head;
            public readonly Mesh Barrel;

            public Parts(Mesh head, Mesh barrel)
            {
                Head = head;
                Barrel = barrel;
            }
        }

        /// <summary>The four materials in slot order for a tower of the given colour.</summary>
        public static Material[] CreateMaterials(Color tower) => ModelPalette.Create(tower);

        public static Parts Create(TowerShape shape, int level)
        {
            var head = new PropMeshBuilder();
            var barrel = new PropMeshBuilder();
            switch (shape)
            {
                case TowerShape.Cannon: Cannon(head, barrel, level); break;
                case TowerShape.Laser: Laser(head, barrel, level); break;
                case TowerShape.Cryo: Cryo(head, barrel, level); break;
                case TowerShape.Rocket: Rocket(head, barrel, level); break;
                case TowerShape.Tesla: Tesla(head, barrel, level); break;
                case TowerShape.Railgun: Railgun(head, barrel, level); break;
                case TowerShape.Support: Support(head, barrel, level); break;
                case TowerShape.Artillery: Artillery(head, barrel, level); break;
                case TowerShape.StormRifle: StormRifle(head, barrel, level); break;
                case TowerShape.CryoBeam: CryoBeam(head, barrel, level); break;
                case TowerShape.PulseRifle: PulseKit(head, barrel, level); break;
            }

            // A mesh without triangles would upset some renderers, so every barrel gets at least a stub.
            Stub(barrel);
            return new Parts(head.ToMesh($"{shape}_head_{level}"), barrel.ToMesh($"{shape}_barrel_{level}"));
        }

        private static void Stub(PropMeshBuilder b) => b.AddBlob(Vector3.zero, Vector3.one * 0.001f, 2, 3, 0f, 0, Dark, false);

        private static void Tube(PropMeshBuilder b, Vector3 from, Vector3 to, float r0, float r1, int sides, int slot, bool flat = false)
        {
            b.AddTube(new[] { from, to }, new[] { r0, r1 }, sides, slot, flat);
        }

        private static void Dome(PropMeshBuilder b, Vector3 centre, Vector3 radii, int slot, float bump = 0.04f, bool flat = false) =>
            b.AddBlob(centre, radii, 5, 10, bump, 3, slot, flat);

        // Add-ons for the imported machine gun: nothing at first, then ammo drums and a glowing ring, then a
        // scope, armour and antennas, so an upgrade is visible on a model that is otherwise a single mesh.
        private static void PulseKit(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            if (level < 1)
            {
                return;
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.3f * side;
                Dome(head, new Vector3(x, 0.02f, -0.05f), new Vector3(0.12f, 0.14f, 0.18f), Body, 0.04f);
                Tube(head, new Vector3(x, 0.14f, -0.12f), new Vector3(x, 0.14f, 0.1f), 0.025f, 0.025f, 6, Glow);
            }

            Tube(head, new Vector3(0f, 0.02f, 0.2f), new Vector3(0f, 0.02f, 0.24f), 0.26f, 0.26f, 14, Glow);

            if (level < 2)
            {
                return;
            }

            Tube(head, new Vector3(0f, 0.34f, -0.1f), new Vector3(0f, 0.34f, 0.5f), 0.06f, 0.05f, 8, Accent);
            head.AddBlob(new Vector3(0f, 0.34f, 0.52f), new Vector3(0.07f, 0.07f, 0.04f), 3, 8, 0f, 0, Glow, false);
            Dome(head, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.18f, 0.1f, 0.2f), Accent, 0.1f, true);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.16f;
                head.AddTube(new[] { new Vector3(x, 0.3f, -0.3f), new Vector3(x, 0.62f, -0.34f) }, new[] { 0.02f, 0f }, 5, Glow, true);
            }
        }

        // Heavy gun: squat armoured turret, thick barrel with a muzzle brake. Level 3 has twin barrels.
        private static void Cannon(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.16f, 0f), new Vector3(0.4f, 0.2f, 0.42f), Body);
            Dome(head, new Vector3(0f, 0.02f, 0f), new Vector3(0.44f, 0.08f, 0.44f), Dark);
            if (level >= 1)
            {
                Dome(head, new Vector3(0.36f, 0.14f, 0f), new Vector3(0.1f, 0.15f, 0.28f), Accent, 0.1f, true);
                Dome(head, new Vector3(-0.36f, 0.14f, 0f), new Vector3(0.1f, 0.15f, 0.28f), Accent, 0.1f, true);
            }

            float[] xs = level >= 2 ? new[] { -0.15f, 0.15f } : new[] { 0f };
            foreach (float x in xs)
            {
                Tube(barrel, new Vector3(x, 0.2f, 0.2f), new Vector3(x, 0.2f, 0.85f), 0.11f, 0.09f, 8, Accent);
                Tube(barrel, new Vector3(x, 0.2f, 0.78f), new Vector3(x, 0.2f, 0.96f), 0.14f, 0.14f, 8, Dark);
                if (level >= 1)
                {
                    Tube(barrel, new Vector3(x, 0.2f, 0.62f), new Vector3(x, 0.2f, 0.67f), 0.125f, 0.125f, 8, Glow);
                }
            }
        }

        // Precision emitter: slim housing, two rails and a glowing lens, with a thin beam barrel.
        private static void Laser(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.2f, -0.05f), new Vector3(0.26f, 0.2f, 0.34f), Body);
            Tube(head, new Vector3(0.17f, 0.28f, 0.08f), new Vector3(0.17f, 0.28f, 0.7f), 0.035f, 0.03f, 6, Accent);
            Tube(head, new Vector3(-0.17f, 0.28f, 0.08f), new Vector3(-0.17f, 0.28f, 0.7f), 0.035f, 0.03f, 6, Accent);
            head.AddBlob(new Vector3(0f, 0.28f, 0.34f), new Vector3(0.1f, 0.1f, 0.18f), 4, 8, 0f, 0, Glow, false);
            if (level >= 1)
            {
                Dome(head, new Vector3(0.2f, 0.4f, -0.1f), new Vector3(0.07f, 0.12f, 0.2f), Accent, 0.1f, true);
                Dome(head, new Vector3(-0.2f, 0.4f, -0.1f), new Vector3(0.07f, 0.12f, 0.2f), Accent, 0.1f, true);
            }

            if (level >= 2)
            {
                head.AddTube(new[] { new Vector3(0f, 0.38f, -0.1f), new Vector3(0f, 0.8f, -0.1f) }, new[] { 0.08f, 0f }, 6, Glow, true);
            }

            Tube(barrel, new Vector3(0f, 0.28f, 0.5f), new Vector3(0f, 0.28f, 1.0f), 0.055f, 0.025f, 6, Glow);
        }

        // A cold-field dish ringed with ice crystals; a short nozzle in front.
        private static void Cryo(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.22f, 0f), new Vector3(0.32f, 0.26f, 0.32f), Accent, 0.03f);
            head.AddBlob(new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.05f, 0.36f), 3, 12, 0f, 0, Glow, false);
            int crystals = 4 + level * 2;
            for (int i = 0; i < crystals; i++)
            {
                float angle = i / (float)crystals * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 root = direction * 0.3f + Vector3.up * 0.2f;
                Vector3 tip = root + direction * 0.12f + Vector3.up * (0.34f + 0.1f * (i % 2));
                head.AddTube(new[] { root, tip }, new[] { 0.06f, 0f }, 5, Glow, true);
            }

            Tube(barrel, new Vector3(0f, 0.22f, 0.2f), new Vector3(0f, 0.22f, 0.62f), 0.1f, 0.07f, 8, Dark);
            barrel.AddBlob(new Vector3(0f, 0.22f, 0.64f), Vector3.one * 0.07f, 3, 8, 0f, 0, Glow, false);
        }

        // Missile launcher: a flat housing and a bank of pods that grows with each level.
        private static void Rocket(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.14f, 0f), new Vector3(0.38f, 0.16f, 0.4f), Dark, 0.05f, true);
            Dome(head, new Vector3(0f, 0.3f, -0.15f), new Vector3(0.18f, 0.1f, 0.12f), Accent, 0.1f, true);

            int pods = level == 0 ? 2 : 4;
            for (int i = 0; i < pods; i++)
            {
                float x = pods == 2 ? (i == 0 ? -0.15f : 0.15f) : -0.27f + i * 0.18f;
                float y = pods == 4 && i % 2 == 1 ? 0.4f : 0.32f;
                Tube(barrel, new Vector3(x, y, -0.1f), new Vector3(x, y, 0.55f), 0.08f, 0.08f, 6, Body, true);
                barrel.AddBlob(new Vector3(x, y, 0.58f), Vector3.one * 0.065f, 3, 6, 0f, 0, Glow, false);
            }

            if (level >= 2)
            {
                Tube(barrel, new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0.5f, 0.5f), 0.06f, 0.06f, 6, Accent);
            }
        }

        // A tall coil with glowing rings and a crackling sphere on top.
        private static void Tesla(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Tube(head, new Vector3(0f, -0.1f, 0f), new Vector3(0f, 0.7f, 0f), 0.13f, 0.08f, 8, Body);
            int rings = 3 + level;
            for (int i = 0; i < rings; i++)
            {
                float y = 0.08f + i * 0.14f;
                Tube(head, new Vector3(0f, y, 0f), new Vector3(0f, y + 0.07f, 0f), 0.19f - i * 0.015f, 0.19f - i * 0.015f, 10, Glow);
            }

            head.AddBlob(new Vector3(0f, 0.82f, 0f), Vector3.one * (0.15f + 0.02f * level), 5, 10, 0f, 0, Glow, false);
            for (int i = 0; i < 3 + level; i++)
            {
                float angle = i / (float)(3 + level) * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                head.AddTube(new[] { direction * 0.2f + Vector3.up * 0.1f, direction * 0.32f + Vector3.up * 0.55f, direction * 0.14f + Vector3.up * 0.82f }, new[] { 0.025f, 0.02f, 0.012f }, 4, Accent, false);
            }
        }

        // Long twin rails with a glowing channel and capacitors that appear with upgrades.
        private static void Railgun(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.2f, -0.12f), new Vector3(0.24f, 0.2f, 0.42f), Body);
            if (level >= 1)
            {
                Dome(head, new Vector3(0.24f, 0.22f, -0.14f), new Vector3(0.07f, 0.1f, 0.2f), Glow, 0.05f);
                Dome(head, new Vector3(-0.24f, 0.22f, -0.14f), new Vector3(0.07f, 0.1f, 0.2f), Glow, 0.05f);
            }

            Tube(barrel, new Vector3(0.1f, 0.3f, 0.05f), new Vector3(0.1f, 0.3f, 1.1f), 0.05f, 0.05f, 4, Accent, true);
            Tube(barrel, new Vector3(-0.1f, 0.3f, 0.05f), new Vector3(-0.1f, 0.3f, 1.1f), 0.05f, 0.05f, 4, Accent, true);
            Tube(barrel, new Vector3(0f, 0.3f, 0.15f), new Vector3(0f, 0.3f, 1.05f), 0.03f, 0.02f, 6, Glow);
            if (level >= 2)
            {
                Tube(barrel, new Vector3(0f, 0.42f, 0.1f), new Vector3(0f, 0.42f, 0.9f), 0.04f, 0.04f, 4, Accent, true);
            }
        }

        // A pylon with a floating orb and angled fins; it has no barrel because it only supports.
        private static void Support(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.14f, 0.28f), Body);
            head.AddBlob(new Vector3(0f, 0.5f, 0f), Vector3.one * (0.2f + 0.02f * level), 5, 10, 0f, 0, Glow, false);
            int fins = 3 + level;
            for (int i = 0; i < fins; i++)
            {
                float angle = i / (float)fins * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                head.AddTube(new[] { direction * 0.12f + Vector3.up * 0.12f, direction * 0.32f + Vector3.up * 0.4f, direction * 0.24f + Vector3.up * 0.62f }, new[] { 0.05f, 0.035f, 0.015f }, 4, Accent, true);
            }

            if (level >= 2)
            {
                Tube(head, new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0.5f, 0f) + Vector3.up * 0.02f, 0.34f, 0.34f, 14, Glow);
            }
        }

        // Long-range gun: a heavy barrel raised at an angle on a wide carriage.
        private static void Artillery(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.14f, 0f), new Vector3(0.46f, 0.18f, 0.46f), Dark, 0.05f, true);
            Dome(head, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.26f, 0.16f, 0.3f), Body);
            if (level >= 1)
            {
                Dome(head, new Vector3(0.4f, 0.12f, -0.1f), new Vector3(0.08f, 0.1f, 0.22f), Accent, 0.1f, true);
                Dome(head, new Vector3(-0.4f, 0.12f, -0.1f), new Vector3(0.08f, 0.1f, 0.22f), Accent, 0.1f, true);
            }

            float length = 1.0f + 0.1f * level;
            Vector3 from = new Vector3(0f, 0.32f, 0f);
            Vector3 to = from + new Vector3(0f, 0.4f, 0.9f).normalized * length;
            Tube(barrel, from, to, 0.15f, 0.11f, 8, Body);
            Tube(barrel, to - (to - from).normalized * 0.12f, to + (to - from).normalized * 0.06f, 0.16f, 0.16f, 8, Dark);
            Tube(barrel, from + (to - from) * 0.55f, from + (to - from) * 0.6f, 0.17f, 0.17f, 8, Glow);
        }

        // Rapid fire with a cluster of thin barrels and glowing coils down the sides.
        private static void StormRifle(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.2f, 0.34f), Body);
            for (int i = 0; i < 3 + level; i++)
            {
                float z = -0.1f + i * 0.14f;
                head.AddBlob(new Vector3(0.27f, 0.2f, z), new Vector3(0.05f, 0.08f, 0.06f), 3, 6, 0f, 0, Glow, false);
                head.AddBlob(new Vector3(-0.27f, 0.2f, z), new Vector3(0.05f, 0.08f, 0.06f), 3, 6, 0f, 0, Glow, false);
            }

            Vector2[] offsets = level >= 1
                ? new[] { new Vector2(0f, 0.3f), new Vector2(0.11f, 0.2f), new Vector2(-0.11f, 0.2f), new Vector2(0f, 0.12f) }
                : new[] { new Vector2(0f, 0.3f), new Vector2(0.11f, 0.2f), new Vector2(-0.11f, 0.2f) };
            foreach (Vector2 o in offsets)
            {
                Tube(barrel, new Vector3(o.x, o.y, 0.1f), new Vector3(o.x, o.y, 0.85f), 0.05f, 0.04f, 6, Accent);
            }
        }

        // A cold beam projector: long glowing prism and ice crystals along the housing.
        private static void CryoBeam(PropMeshBuilder head, PropMeshBuilder barrel, int level)
        {
            Dome(head, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.22f, 0.36f), Body);
            int crystals = 4 + level * 2;
            for (int i = 0; i < crystals; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float z = -0.2f + (i / 2) * 0.15f;
                Vector3 root = new Vector3(0.22f * side, 0.2f, z);
                head.AddTube(new[] { root, root + new Vector3(0.14f * side, 0.3f, 0.05f) }, new[] { 0.055f, 0f }, 5, Glow, true);
            }

            Tube(barrel, new Vector3(0f, 0.28f, 0.25f), new Vector3(0f, 0.28f, 1.0f), 0.09f, 0.03f, 6, Glow, true);
            Tube(barrel, new Vector3(0f, 0.28f, 0.2f), new Vector3(0f, 0.28f, 0.45f), 0.12f, 0.12f, 8, Dark);
        }
    }
}
