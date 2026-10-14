using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Procedural models of the things that are not towers, enemies or the hero: barricades, the base reactor,
    /// the enemy gate, drones, mines, emitters, loot. Each is one mesh in the shared model palette.
    /// </summary>
    public static class StructureModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;

        /// <summary>A barricade that spans the road: armoured blocks, hazard stripes on top and a light pylon at each end.</summary>
        public static Mesh Blockade(float width, float depth)
        {
            var b = new PropMeshBuilder();
            float half = width * 0.5f;
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(width, 0.36f, depth), Dark);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * width * 0.3f;
                b.AddBox(new Vector3(x, 0.5f, 0f), new Vector3(width * 0.28f, 0.34f, depth * 0.9f), Body);
            }

            int stripes = 8;
            for (int i = 0; i < stripes; i++)
            {
                float x = -half + (i + 0.5f) * width / stripes;
                b.AddBox(new Vector3(x, 0.69f, 0f), new Vector3(width / stripes * 0.5f, 0.04f, depth * 0.95f), i % 2 == 0 ? Glow : Dark, 0f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = half * side;
                b.AddBox(new Vector3(x, 0.45f, 0f), new Vector3(0.12f, 0.9f, depth * 1.2f), Metal);
                b.AddBlob(new Vector3(x, 0.98f, 0f), Vector3.one * 0.09f, 3, 8, 0f, 0, Glow, false);
            }

            return b.ToMesh("Blockade");
        }

        /// <summary>The base: a tiered platform with a glowing reactor core, four spires and a turning ring.</summary>
        public static Mesh BaseReactor(out Mesh ring)
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0.18f, 0f) }, new[] { 0.7f, 0.64f }, 10, Dark, true);
            b.AddTube(new[] { new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0.32f, 0f) }, new[] { 0.52f, 0.46f }, 10, Metal, true);
            b.AddTube(new[] { new Vector3(0f, 0.32f, 0f), new Vector3(0f, 0.9f, 0f), new Vector3(0f, 1.1f, 0f) }, new[] { 0.24f, 0.2f, 0.16f }, 8, Body, false);
            b.AddBlob(new Vector3(0f, 1.3f, 0f), Vector3.one * 0.18f, 5, 10, 0f, 0, Glow, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                b.AddTube(new[] { dir * 0.5f + Vector3.up * 0.3f, dir * 0.55f + Vector3.up * 0.8f, dir * 0.4f + Vector3.up * 1.15f }, new[] { 0.05f, 0.04f, 0f }, 5, Metal, true);
            }

            var r = new PropMeshBuilder();
            r.AddTube(new[] { new Vector3(0f, 1.0f, 0f), new Vector3(0f, 1.04f, 0f) }, new[] { 0.5f, 0.5f }, 16, Glow, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                r.AddBlob(new Vector3(Mathf.Cos(angle) * 0.5f, 1.02f, Mathf.Sin(angle) * 0.5f), new Vector3(0.1f, 0.08f, 0.1f), 3, 6, 0f, 0, Metal, false);
            }

            ring = r.ToMesh("BaseRing");
            return b.ToMesh("Base");
        }

        /// <summary>Where enemies come from: a dark pit ringed with bone spikes and a glowing rim.</summary>
        public static Mesh EnemyGate(out Mesh ring)
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0.01f, 0f), new Vector3(0f, 0.03f, 0f) }, new[] { 0.5f, 0.5f }, 16, Dark, false);
            for (int i = 0; i < 9; i++)
            {
                float angle = i / 9f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float height = 0.35f + 0.15f * (i % 3);
                b.AddTube(new[] { dir * 0.55f, dir * 0.45f + Vector3.up * height }, new[] { 0.07f, 0f }, 5, Metal, true);
            }

            var r = new PropMeshBuilder();
            r.AddTube(new[] { new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.08f, 0f) }, new[] { 0.42f, 0.42f }, 16, Glow, false);
            ring = r.ToMesh("GateRing");
            return b.ToMesh("Gate");
        }

        /// <summary>A quad-rotor drone. The four rotor discs are a separate mesh so they can spin; a mark shows its job.</summary>
        public static Mesh Drone(bool repair, out Mesh rotors)
        {
            var b = new PropMeshBuilder();
            b.AddBlob(new Vector3(0f, 0f, 0f), new Vector3(0.2f, 0.1f, 0.2f), 4, 10, 0.02f, 3, Body, false);
            b.AddBlob(new Vector3(0f, -0.04f, 0.14f), Vector3.one * 0.055f, 3, 8, 0f, 0, Glow, false);
            var r = new PropMeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var arm = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.3f;
                b.AddTube(new[] { Vector3.zero, arm * 0.9f }, new[] { 0.03f, 0.025f }, 5, Metal, false);
                r.AddTube(new[] { arm + Vector3.up * 0.04f, arm + Vector3.up * 0.05f }, new[] { 0.14f, 0.14f }, 10, Metal, false);
            }

            if (repair)
            {
                b.AddBox(new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.04f, 0.06f), Glow);
                b.AddBox(new Vector3(0f, 0.12f, 0f), new Vector3(0.06f, 0.04f, 0.18f), Glow);
            }
            else
            {
                b.AddTube(new[] { new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.16f, 0f) }, new[] { 0.1f, 0.1f }, 10, Glow, false);
            }

            rotors = r.ToMesh("DroneRotors");
            return b.ToMesh("Drone");
        }

        /// <summary>An armoured proximity mine with spikes and a warning light.</summary>
        public static Mesh Mine()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.14f, 0f) }, new[] { 0.26f, 0.22f }, 10, Dark, true);
            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                b.AddTube(new[] { dir * 0.24f + Vector3.up * 0.08f, dir * 0.36f + Vector3.up * 0.12f }, new[] { 0.035f, 0f }, 4, Metal, true);
            }

            b.AddBlob(new Vector3(0f, 0.16f, 0f), new Vector3(0.1f, 0.06f, 0.1f), 3, 8, 0f, 0, Glow, false);
            return b.ToMesh("Mine");
        }

        /// <summary>A field projector: a small pylon with three prongs; the field itself is drawn around it.</summary>
        public static Mesh FieldEmitter()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.3f, 0f) }, new[] { 0.14f, 0.07f }, 8, Dark, false);
            b.AddBlob(new Vector3(0f, 0.36f, 0f), Vector3.one * 0.1f, 4, 8, 0f, 0, Glow, false);
            for (int i = 0; i < 3; i++)
            {
                float angle = i / 3f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                b.AddTube(new[] { dir * 0.1f + Vector3.up * 0.04f, dir * 0.2f + Vector3.up * 0.24f }, new[] { 0.035f, 0.01f }, 4, Metal, true);
            }

            return b.ToMesh("Emitter");
        }

        /// <summary>A rolling ball of molten rock with glowing seams.</summary>
        public static Mesh LavaBall()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(new Vector3(0f, 0.35f, 0f), Vector3.one * 0.34f, 6, 12, 0.18f, 5, Dark, true);
            b.AddBlob(new Vector3(0f, 0.35f, 0f), Vector3.one * 0.3f, 5, 10, 0.12f, 9, Glow, false);
            return b.ToMesh("LavaBall");
        }

        /// <summary>A little wheeled combat robot with a glowing eye.</summary>
        public static Mesh GroundDrone()
        {
            var b = new PropMeshBuilder();
            b.AddBox(new Vector3(0f, 0.28f, 0f), new Vector3(0.36f, 0.2f, 0.5f), Body);
            b.AddBlob(new Vector3(0f, 0.42f, 0.05f), new Vector3(0.14f, 0.1f, 0.16f), 4, 8, 0f, 0, Metal, false);
            b.AddBlob(new Vector3(0f, 0.28f, 0.27f), Vector3.one * 0.06f, 3, 6, 0f, 0, Glow, false);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    b.AddBox(new Vector3(0.22f * side, 0.12f, -0.15f + i * 0.3f), new Vector3(0.1f, 0.24f, 0.2f), Dark);
                }
            }

            return b.ToMesh("GroundDrone");
        }

        /// <summary>An explosive barrel with glowing hazard bands.</summary>
        public static Mesh Barrel()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.55f, 0f) }, new[] { 0.22f, 0.22f }, 10, Body, false);
            for (int i = 0; i < 2; i++)
            {
                float y = 0.15f + i * 0.28f;
                b.AddTube(new[] { new Vector3(0f, y, 0f), new Vector3(0f, y + 0.07f, 0f) }, new[] { 0.235f, 0.235f }, 10, Glow, false);
            }

            b.AddTube(new[] { new Vector3(0f, 0.55f, 0f), new Vector3(0f, 0.6f, 0f) }, new[] { 0.16f, 0.12f }, 8, Dark, false);
            return b.ToMesh("Barrel");
        }

        /// <summary>An anti-air net: two posts with a glowing strand between them.</summary>
        public static Mesh AirNet()
        {
            var b = new PropMeshBuilder();
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { new Vector3(0.4f * side, 0.02f, 0f), new Vector3(0.4f * side, 0.9f, 0f) }, new[] { 0.06f, 0.035f }, 6, Metal, false);
                b.AddBlob(new Vector3(0.4f * side, 0.95f, 0f), Vector3.one * 0.06f, 3, 6, 0f, 0, Glow, false);
            }

            for (int i = 0; i < 4; i++)
            {
                float y = 0.2f + i * 0.2f;
                b.AddBox(new Vector3(0f, y, 0f), new Vector3(0.8f, 0.02f, 0.02f), Glow);
            }

            return b.ToMesh("AirNet");
        }

        /// <summary>A resource node: a boulder with a cluster of golden crystals that collector drones mine.</summary>
        public static Mesh MiningNode()
        {
            var b = new PropMeshBuilder();
            b.AddBlob(new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.2f, 0.42f), 5, 9, 0.2f, 21, Dark, true);
            var tilts = new[] { new Vector3(0f, 1f, 0f), new Vector3(0.45f, 1f, 0.1f), new Vector3(-0.4f, 1f, 0.2f), new Vector3(0.1f, 1f, -0.45f), new Vector3(-0.15f, 1f, -0.35f) };
            for (int i = 0; i < tilts.Length; i++)
            {
                Vector3 axis = tilts[i].normalized;
                float length = i == 0 ? 0.75f : 0.45f + 0.05f * i;
                Vector3 root = new Vector3(tilts[i].x * 0.25f, 0.18f, tilts[i].z * 0.25f);
                b.AddTube(new[] { root, root + axis * length }, new[] { i == 0 ? 0.11f : 0.08f, 0f }, 6, Glow, true);
            }

            return b.ToMesh("MiningNode");
        }

        /// <summary>A dropped loot crystal.</summary>
        public static Mesh LootCrystal()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { new Vector3(0f, -0.16f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0.18f, 0f) }, new[] { 0f, 0.1f, 0f }, 6, Glow, true);
            return b.ToMesh("Loot");
        }
    }
}
