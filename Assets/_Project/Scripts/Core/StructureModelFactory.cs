using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Procedural models of the things that are not towers, enemies or the hero: barricades, the base reactor,
    /// the enemy gate, mines, emitters. Each is one mesh in the shared model palette.
    /// </summary>
    public static class StructureModelFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;

        // The hive gate uses the creature palette: body, eyes, glow, dark.
        private const int HiveBody = 0;
        private const int HiveEyes = 1;
        private const int HiveGlow = 2;
        private const int HiveDark = 3;

        private static Vector3 Around(float angle) => new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        /// <summary>One pylon of a road gate: a plated column with light strips, a lamp on top and an armoured foot.</summary>
        public static Mesh GatePylon()
        {
            var b = new PropMeshBuilder();
            b.AddBox(new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.1f, 0.46f), Dark);
            b.AddBox(new Vector3(0f, 0.6f, 0f), new Vector3(0.24f, 1f, 0.3f), Metal);
            b.AddBox(new Vector3(0f, 0.95f, 0f), new Vector3(0.28f, 0.24f, 0.34f), Body);
            b.AddBox(new Vector3(0f, 0.45f, 0.16f), new Vector3(0.05f, 0.6f, 0.02f), Glow);
            b.AddBox(new Vector3(0f, 0.45f, -0.16f), new Vector3(0.05f, 0.6f, 0.02f), Glow);
            b.AddBox(new Vector3(0f, 1.12f, 0f), new Vector3(0.2f, 0.1f, 0.26f), Dark);
            b.AddBlob(new Vector3(0f, 1.2f, 0f), new Vector3(0.09f, 0.07f, 0.09f), 3, 8, 0f, 0, Glow, false);
            return b.ToMesh("GatePylon");
        }

        /// <summary>The crossbeam between the pylons: a girder with a light strip underneath and two braces.</summary>
        public static Mesh GateBeam(float width)
        {
            var b = new PropMeshBuilder();
            b.AddBox(Vector3.zero, new Vector3(width, 0.12f, 0.26f), Metal);
            b.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(width * 0.8f, 0.05f, 0.18f), Body);
            b.AddBox(new Vector3(0f, -0.075f, 0f), new Vector3(width * 0.85f, 0.03f, 0.08f), Glow);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBox(new Vector3(side * width * 0.33f, -0.1f, 0f), new Vector3(0.06f, 0.1f, 0.2f), Dark);
            }

            return b.ToMesh("GateBeam");
        }


        /// <summary>The meshes of the base and where its turning parts pivot, in base space.</summary>
        public readonly struct BaseParts
        {
            public readonly Mesh Body;
            public readonly Mesh Ring;
            public readonly Vector3 RingPivot;
            public readonly Mesh Dish;
            public readonly Vector3 DishPivot;

            public BaseParts(Mesh body, Mesh ring, Vector3 ringPivot, Mesh dish, Vector3 dishPivot)
            {
                Body = body;
                Ring = ring;
                RingPivot = ringPivot;
                Dish = dish;
                DishPivot = dishPivot;
            }
        }

        /// <summary>
        /// The base: a colony outpost on a stepped deck with a lit rim and a ramp down to the road (+z). A tiered
        /// command tower with window bands and a spire rises from the middle, orbited by a spinning energy ring;
        /// a caged reactor core glows on the right, stacked habitat cylinders with portholes sit on the left, a
        /// lattice mast carries a turning dish at the back, and a lit landing pad, shield pylons, solar panels,
        /// tanks and crates fill the deck.
        /// </summary>
        public static BaseParts BaseOutpost()
        {
            var b = new PropMeshBuilder();

            // Stepped deck: plinth, deck plate, lit rim strips, plate seams and corner shield pylons.
            b.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(2.3f, 0.12f, 1.9f), Dark);
            b.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(2.16f, 0.06f, 1.76f), Metal);
            b.AddBox(new Vector3(0f, 0.185f, 0.86f), new Vector3(2.1f, 0.02f, 0.04f), Glow);
            b.AddBox(new Vector3(0f, 0.185f, -0.86f), new Vector3(2.1f, 0.02f, 0.04f), Glow);
            b.AddBox(new Vector3(1.06f, 0.185f, 0f), new Vector3(0.04f, 0.02f, 1.7f), Glow);
            b.AddBox(new Vector3(-1.06f, 0.185f, 0f), new Vector3(0.04f, 0.02f, 1.7f), Glow);
            for (int i = -1; i <= 1; i++)
            {
                b.AddBox(new Vector3(i * 0.7f, 0.182f, 0f), new Vector3(0.012f, 0.004f, 1.7f), Dark);
                b.AddBox(new Vector3(0f, 0.182f, i * 0.55f), new Vector3(2.1f, 0.004f, 0.012f), Dark);
            }

            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? -1.02f : 1.02f, 0.18f, i < 2 ? -0.82f : 0.82f);
                b.AddBox(corner + Vector3.up * 0.2f, new Vector3(0.1f, 0.4f, 0.1f), Metal);
                b.AddBox(corner + Vector3.up * 0.44f, new Vector3(0.12f, 0.06f, 0.12f), Dark);
                b.AddBlob(corner + Vector3.up * 0.5f, Vector3.one * 0.045f, 3, 6, 0f, 0, Glow, false);
            }

            // Ramp from the deck down to the road, with lit edges.
            b.AddBox(new Vector3(0f, 0.09f, 1.12f), new Vector3(0.6f, 0.04f, 0.5f), Metal, Quaternion.Euler(22f, 0f, 0f));
            b.AddBox(new Vector3(0.3f, 0.1f, 1.12f), new Vector3(0.03f, 0.02f, 0.5f), Glow, Quaternion.Euler(22f, 0f, 0f));
            b.AddBox(new Vector3(-0.3f, 0.1f, 1.12f), new Vector3(0.03f, 0.02f, 0.5f), Glow, Quaternion.Euler(22f, 0f, 0f));

            // Command tower: three tiers of octagonal hull with window bands, a dark cap, a sensor box and a spire.
            var tower = new Vector3(0f, 0.18f, -0.15f);
            Tier(b, tower, 0f, 0.5f, 0.44f, Body);
            Tier(b, tower, 0.5f, 0.42f, 0.36f, Metal);
            Tier(b, tower, 0.92f, 0.34f, 0.26f, Body);
            b.AddTube(new[] { tower + Vector3.up * 1.26f, tower + Vector3.up * 1.32f }, new[] { 0.28f, 0.22f }, 8, Dark, true);
            b.AddBox(tower + new Vector3(0.12f, 1.37f, 0f), new Vector3(0.16f, 0.1f, 0.12f), Dark);
            b.AddBox(tower + new Vector3(0.12f, 1.43f, 0.07f), new Vector3(0.1f, 0.02f, 0.02f), Glow);
            b.AddTube(new[] { tower + Vector3.up * 1.32f, tower + Vector3.up * 1.9f }, new[] { 0.035f, 0.012f }, 5, Dark, false);
            b.AddBlob(tower + Vector3.up * 1.92f, Vector3.one * 0.04f, 3, 6, 0f, 0, Glow, false);
            b.AddBox(tower + new Vector3(0f, 0.22f, 0.44f), new Vector3(0.2f, 0.3f, 0.03f), Dark);
            b.AddBox(tower + new Vector3(0f, 0.39f, 0.45f), new Vector3(0.24f, 0.025f, 0.03f), Glow);
            b.AddBox(tower + new Vector3(0f, 0.01f, 0.56f), new Vector3(0.36f, 0.02f, 0.24f), Metal);

            // Reactor on the right: a dark base, a glowing core in a six-post cage with a metal collar, cooling
            // pipes into the tower and two round tanks beside it.
            var reactor = new Vector3(0.72f, 0.18f, -0.3f);
            b.AddTube(new[] { reactor, reactor + Vector3.up * 0.12f }, new[] { 0.32f, 0.3f }, 12, Dark, false);
            b.AddTube(new[] { reactor + Vector3.up * 0.12f, reactor + Vector3.up * 0.55f }, new[] { 0.2f, 0.19f }, 12, Glow, false);
            b.AddTorus(reactor + Vector3.up * 0.33f, Vector3.up, 0.22f, 0.025f, 16, 6, Dark);
            b.AddTube(new[] { reactor + Vector3.up * 0.55f, reactor + Vector3.up * 0.62f }, new[] { 0.24f, 0.2f }, 12, Metal, false);
            b.AddTorus(reactor + Vector3.up * 0.66f, Vector3.up, 0.26f, 0.02f, 16, 5, Metal);
            for (int i = 0; i < 6; i++)
            {
                Vector3 post = reactor + Around(i * Mathf.PI / 3f) * 0.27f;
                b.AddTube(new[] { post + Vector3.up * 0.1f, post + Vector3.up * 0.68f }, new[] { 0.022f, 0.02f }, 4, Dark, false);
            }

            b.AddTube(new[] { reactor + new Vector3(-0.2f, 0.3f, 0f), reactor + new Vector3(-0.38f, 0.3f, 0f), reactor + new Vector3(-0.38f, 0.3f, 0.12f) }, new[] { 0.035f, 0.035f, 0.035f }, 6, Metal, false);
            b.AddTube(new[] { reactor + new Vector3(-0.2f, 0.42f, 0.08f), reactor + new Vector3(-0.42f, 0.42f, 0.14f) }, new[] { 0.025f, 0.025f }, 6, Metal, false);
            b.AddBlob(reactor + new Vector3(0.1f, 0.14f, 0.5f), Vector3.one * 0.13f, 4, 8, 0f, 0, Metal, false);
            b.AddBlob(reactor + new Vector3(-0.2f, 0.14f, 0.5f), Vector3.one * 0.13f, 4, 8, 0f, 0, Metal, false);
            b.AddBox(reactor + new Vector3(-0.05f, 0.08f, 0.5f), new Vector3(0.46f, 0.08f, 0.1f), Dark);

            // Habitat on the left: two stacked cylinders along the road with porthole lights and end caps, and a
            // corridor tube into the tower.
            var hab = new Vector3(-0.74f, 0.38f, -0.1f);
            Habitat(b, hab, 0.78f, 0.19f);
            Habitat(b, hab + new Vector3(0.06f, 0.36f, 0.1f), 0.56f, 0.15f);
            b.AddTube(new[] { hab + new Vector3(0.15f, 0f, 0f), hab + new Vector3(0.48f, 0f, 0f) }, new[] { 0.1f, 0.1f }, 8, Metal, false);
            b.AddTorus(hab + new Vector3(0.3f, 0f, 0f), Vector3.right, 0.11f, 0.015f, 10, 5, Dark);
            for (int i = 0; i < 3; i++)
            {
                b.AddBox(hab + new Vector3(-0.2f + i * 0.2f, -0.2f, 0f), new Vector3(0.06f, 0.06f, 0.5f), Dark);
            }

            // Comms mast at the back left: four lattice legs, cross plates and a collar the dish turns on.
            var mastFoot = new Vector3(-0.78f, 0.18f, -0.7f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 leg = Around(i * Mathf.PI * 0.5f + Mathf.PI * 0.25f) * 0.13f;
                b.AddTube(new[] { mastFoot + leg, mastFoot + leg * 0.4f + Vector3.up * 1.5f }, new[] { 0.02f, 0.015f }, 4, Dark, false);
            }

            for (int i = 0; i < 3; i++)
            {
                b.AddBox(mastFoot + Vector3.up * (0.5f + i * 0.5f), new Vector3(0.18f - i * 0.03f, 0.02f, 0.18f - i * 0.03f), Metal);
            }

            b.AddTube(new[] { mastFoot + Vector3.up * 1.5f, mastFoot + Vector3.up * 1.66f }, new[] { 0.035f, 0.03f }, 5, Dark, false);
            b.AddBlob(mastFoot + Vector3.up * 0.25f, new Vector3(0.05f, 0.05f, 0.05f), 3, 6, 0f, 0, Glow, false);

            // Landing pad at the front right: lit ring, cross markings and two floodlight masts.
            var pad = new Vector3(0.66f, 0.18f, 0.5f);
            b.AddTube(new[] { pad, pad + Vector3.up * 0.015f }, new[] { 0.34f, 0.34f }, 20, Dark, false);
            b.AddTorus(pad + Vector3.up * 0.02f, Vector3.up, 0.32f, 0.015f, 20, 5, Glow);
            b.AddBox(pad + Vector3.up * 0.02f, new Vector3(0.3f, 0.008f, 0.05f), Glow);
            b.AddBox(pad + Vector3.up * 0.02f, new Vector3(0.05f, 0.008f, 0.3f), Glow);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 mast = pad + new Vector3(0.34f * side, 0f, 0.3f);
                b.AddTube(new[] { mast, mast + Vector3.up * 0.5f }, new[] { 0.02f, 0.015f }, 4, Dark, false);
                b.AddBox(mast + Vector3.up * 0.5f, new Vector3(0.08f, 0.05f, 0.05f), Dark);
                b.AddBox(mast + new Vector3(-0.02f * side, 0.5f, 0.03f), new Vector3(0.06f, 0.03f, 0.01f), Glow);
            }

            // Solar panels along the back, crates near the ramp.
            for (int i = 0; i < 3; i++)
            {
                var panel = new Vector3(0.2f + i * 0.26f, 0.28f, -0.78f);
                b.AddBox(panel, new Vector3(0.22f, 0.015f, 0.2f), Metal, Quaternion.Euler(-30f, 0f, 0f));
                b.AddBox(panel + new Vector3(0f, 0.012f, 0f), new Vector3(0.18f, 0.004f, 0.16f), Glow, Quaternion.Euler(-30f, 0f, 0f));
                b.AddTube(new[] { panel - Vector3.up * 0.1f, panel }, new[] { 0.015f, 0.015f }, 4, Dark, false);
            }

            b.AddBox(new Vector3(-0.5f, 0.27f, 0.62f), new Vector3(0.18f, 0.18f, 0.18f), Body, 15f);
            b.AddBox(new Vector3(-0.5f, 0.38f, 0.62f), new Vector3(0.12f, 0.02f, 0.12f), Glow, 15f);
            b.AddBox(new Vector3(-0.72f, 0.26f, 0.5f), new Vector3(0.16f, 0.16f, 0.16f), Dark, -10f);
            b.AddBox(new Vector3(-0.68f, 0.42f, 0.55f), new Vector3(0.14f, 0.14f, 0.14f), Body, 25f);

            // The energy ring that orbits the tower: a glowing torus with three field nodes.
            var r = new PropMeshBuilder();
            r.AddTorus(Vector3.zero, Vector3.up, 0.6f, 0.03f, 28, 6, Glow);
            for (int i = 0; i < 3; i++)
            {
                Vector3 node = Around(i * Mathf.PI * 2f / 3f) * 0.6f;
                r.AddBlob(node, new Vector3(0.07f, 0.05f, 0.07f), 3, 6, 0f, 0, Metal, false);
                r.AddBlob(node + Vector3.up * 0.04f, Vector3.one * 0.03f, 3, 6, 0f, 0, Glow, false);
            }

            // The dish: a pale shallow bowl on a dark hub, with a feed arm and a lit receiver.
            var d = new PropMeshBuilder();
            d.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.28f, 0.09f, 0.28f), 4, 12, 0f, 0, Metal, false, Mathf.PI * 0.5f);
            d.AddBlob(new Vector3(0f, -0.02f, 0f), new Vector3(0.1f, 0.05f, 0.1f), 3, 8, 0f, 0, Dark, false);
            d.AddTube(new[] { new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0.26f, 0.1f) }, new[] { 0.015f, 0.01f }, 4, Dark, false);
            d.AddBlob(new Vector3(0f, 0.25f, 0.1f), Vector3.one * 0.028f, 3, 6, 0f, 0, Glow, false);

            return new BaseParts(b.ToMesh("Base"), r.ToMesh("BaseRing"), tower + Vector3.up * 0.78f, d.ToMesh("BaseDish"), mastFoot + Vector3.up * 1.68f);
        }

        // One tier of the command tower: an octagonal hull with a glowing window band near its top and a dark ledge.
        private static void Tier(PropMeshBuilder b, Vector3 foot, float from, float height, float radius, int slot)
        {
            Vector3 bottom = foot + Vector3.up * from;
            b.AddTube(new[] { bottom, bottom + Vector3.up * height }, new[] { radius, radius }, 8, slot, true);
            b.AddTube(new[] { bottom + Vector3.up * (height * 0.68f), bottom + Vector3.up * (height * 0.8f) }, new[] { radius + 0.01f, radius + 0.01f }, 8, Glow, true);
            b.AddTube(new[] { bottom + Vector3.up * (height - 0.03f), bottom + Vector3.up * height }, new[] { radius + 0.03f, radius + 0.03f }, 8, Dark, true);
        }

        // A habitat cylinder along z: end caps, a seam ring and a row of lit portholes on the outer side.
        private static void Habitat(PropMeshBuilder b, Vector3 centre, float length, float radius)
        {
            b.AddTube(new[] { centre - Vector3.forward * (length * 0.5f), centre + Vector3.forward * (length * 0.5f) }, new[] { radius, radius }, 12, Body, false);
            b.AddBlob(centre - Vector3.forward * (length * 0.5f), new Vector3(radius, radius, radius * 0.5f), 4, 10, 0f, 0, Metal, false);
            b.AddBlob(centre + Vector3.forward * (length * 0.5f), new Vector3(radius, radius, radius * 0.5f), 4, 10, 0f, 0, Metal, false);
            b.AddTorus(centre, Vector3.forward, radius + 0.005f, 0.012f, 12, 5, Dark);
            int portholes = Mathf.Max(2, Mathf.RoundToInt(length / 0.2f));
            for (int i = 0; i < portholes; i++)
            {
                float z = -length * 0.38f + i * (length * 0.76f / (portholes - 1));
                b.AddBlob(centre + new Vector3(-radius * 0.9f, radius * 0.2f, z), new Vector3(0.025f, 0.035f, 0.035f), 3, 6, 0f, 0, Glow, false);
            }
        }

        /// <summary>
        /// Where enemies come from: a fleshy hive mouth. A bumpy mound with a dark maw ringed by curved teeth,
        /// glowing veins running down its flanks and pustule eyes. Uses the creature palette, not the machine one.
        /// </summary>
        public static Mesh EnemyGate(out Mesh ring)
        {
            var b = new PropMeshBuilder();
            b.AddBlob(new Vector3(0f, 0.08f, 0f), new Vector3(0.78f, 0.3f, 0.78f), 6, 14, 0.18f, 5, HiveBody, false);
            b.AddBlob(new Vector3(0f, 0.24f, 0f), new Vector3(0.44f, 0.07f, 0.44f), 3, 12, 0.08f, 2, HiveDark, false);
            b.AddBlob(new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.05f, 0.3f), 3, 10, 0f, 0, HiveDark, false);
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                Vector3 dir = Around(angle);
                float height = 0.5f + 0.12f * (i % 3);
                b.AddTube(new[] { dir * 0.46f + Vector3.up * 0.25f, dir * 0.32f + Vector3.up * (height * 0.85f), dir * 0.2f + Vector3.up * height }, new[] { 0.07f, 0.045f, 0f }, 5, HiveDark, true);
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * Mathf.PI * 2f + 0.3f;
                Vector3 dir = Around(angle);
                b.AddTube(new[] { dir * 0.4f + Vector3.up * 0.28f, dir * 0.6f + Vector3.up * 0.22f, dir * 0.82f + Vector3.up * 0.06f }, new[] { 0.035f, 0.028f, 0.012f }, 5, HiveGlow, false);
                b.AddBlob(dir * 0.62f + Vector3.up * 0.27f, new Vector3(0.06f, 0.05f, 0.06f), 3, 7, 0f, 0, HiveEyes, false);
            }

            var r = new PropMeshBuilder();
            r.AddTube(new[] { new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0.3f, 0f) }, new[] { 0.4f, 0.38f }, 16, HiveGlow, false);
            for (int i = 0; i < 4; i++)
            {
                r.AddBlob(Around(i * Mathf.PI * 0.5f) * 0.39f + Vector3.up * 0.3f, new Vector3(0.07f, 0.05f, 0.07f), 3, 6, 0f, 0, HiveGlow, false);
            }

            ring = r.ToMesh("GateRing");
            return b.ToMesh("Gate");
        }

        /// <summary>
        /// An element nest, where the element creatures rise: a fire vent, a whirl in the water, a hollow stump, a
        /// burrow in the sand or a crystal cluster, each ringed by a glowing circle that turns. Uses the creature palette.
        /// </summary>
        public static Mesh ElementNest(Element element, out Mesh ring)
        {
            var b = new PropMeshBuilder();
            switch (element)
            {
                case Element.Fire:
                    b.AddBlob(new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.3f, 0.6f), 5, 12, 0.14f, 7, HiveDark, true);
                    b.AddTube(new[] { new Vector3(0f, 0.2f, 0f), new Vector3(0f, 0.36f, 0f) }, new[] { 0.3f, 0.2f }, 12, HiveDark, true);
                    b.AddBlob(new Vector3(0f, 0.34f, 0f), new Vector3(0.17f, 0.04f, 0.17f), 3, 10, 0f, 0, HiveGlow, false);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 dir = Around(i / 6f * Mathf.PI * 2f);
                        b.AddTube(new[] { dir * 0.2f + Vector3.up * 0.3f, dir * 0.45f + Vector3.up * 0.16f, dir * 0.62f + Vector3.up * 0.04f }, new[] { 0.03f, 0.025f, 0.01f }, 4, HiveGlow, false);
                    }

                    break;
                case Element.Water:
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 dir = Around(i / 8f * Mathf.PI * 2f + 0.2f);
                        b.AddBlob(dir * 0.45f + Vector3.up * 0.03f, new Vector3(0.16f, 0.02f, 0.16f), 3, 8, 0.04f, 10 + i, HiveBody, true);
                        b.AddTube(new[] { dir * 0.52f, dir * 0.5f + Vector3.up * (0.35f + (i % 3) * 0.12f) }, new[] { 0.025f, 0f }, 4, HiveDark, false);
                        b.AddBlob(dir * 0.5f + Vector3.up * (0.33f + (i % 3) * 0.12f), new Vector3(0.035f, 0.05f, 0.035f), 3, 6, 0f, 0, HiveGlow, false);
                    }

                    b.AddTorus(new Vector3(0f, 0.03f, 0f), Vector3.up, 0.22f, 0.03f, 16, 5, HiveGlow);
                    break;
                case Element.Wood:
                    b.AddTube(new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0.3f, 0f), new Vector3(0f, 0.55f, 0f) }, new[] { 0.42f, 0.34f, 0.36f }, 10, HiveDark, false);
                    b.AddBlob(new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.06f, 0.3f), 3, 10, 0f, 0, HiveBody, false);
                    b.AddBlob(new Vector3(0f, 0.56f, 0f), new Vector3(0.2f, 0.04f, 0.2f), 3, 10, 0f, 0, HiveGlow, false);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 dir = Around(i / 5f * Mathf.PI * 2f);
                        b.AddTube(new[] { dir * 0.3f + Vector3.up * 0.12f, dir * 0.62f + Vector3.up * 0.06f, dir * 0.85f }, new[] { 0.09f, 0.06f, 0.02f }, 5, HiveDark, false);
                        b.AddBlob(dir * 0.4f + Vector3.up * (0.2f + i * 0.07f), new Vector3(0.08f, 0.03f, 0.08f), 3, 7, 0f, 0, HiveGlow, false);
                    }

                    break;
                case Element.Ice:
                    for (int i = 0; i < 7; i++)
                    {
                        Vector3 dir = Around(i / 7f * Mathf.PI * 2f);
                        float height = 0.3f + (i % 3) * 0.2f;
                        b.AddTube(new[] { dir * 0.25f, dir * 0.35f + Vector3.up * height }, new[] { 0.09f, 0f }, 5, HiveGlow, true);
                    }

                    b.AddBlob(new Vector3(0f, 0.04f, 0f), new Vector3(0.55f, 0.1f, 0.55f), 4, 12, 0.06f, 9, HiveBody, true);
                    break;
                case Element.Bog:
                    // A sunken hollow in the mire ringed by rotten stumps and bubbles of marsh gas.
                    b.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.55f, 0.08f, 0.55f), 4, 12, 0.08f, 11, HiveDark, true);
                    b.AddTorus(new Vector3(0f, 0.05f, 0f), Vector3.up, 0.26f, 0.035f, 16, 5, HiveGlow);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 dir = Around(i / 6f * Mathf.PI * 2f + 0.3f);
                        float height = 0.18f + (i % 3) * 0.14f;
                        b.AddTube(new[] { dir * 0.48f, dir * 0.5f + Vector3.up * height }, new[] { 0.07f, 0.04f }, 5, HiveDark, true);
                        b.AddBlob(dir * (0.2f + (i % 2) * 0.12f) + Vector3.up * 0.06f, Vector3.one * (0.035f + (i % 3) * 0.012f), 3, 6, 0f, 0, HiveGlow, false);
                    }

                    break;
                default:
                    b.AddBlob(new Vector3(0f, 0.02f, 0f), new Vector3(0.6f, 0.22f, 0.6f), 5, 12, 0.05f, 8, HiveBody, true);
                    b.AddBlob(new Vector3(0f, 0.2f, 0f), new Vector3(0.22f, 0.05f, 0.22f), 3, 10, 0f, 0, HiveDark, false);
                    b.AddBlob(new Vector3(0f, 0.19f, 0f), new Vector3(0.14f, 0.04f, 0.14f), 3, 10, 0f, 0, HiveGlow, false);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 dir = Around(i / 5f * Mathf.PI * 2f + 0.4f);
                        b.AddTube(new[] { dir * 0.25f + Vector3.up * 0.16f, dir * 0.62f + Vector3.up * 0.02f }, new[] { 0.05f, 0.015f }, 4, HiveDark, false);
                    }

                    break;
            }

            var r = new PropMeshBuilder();
            r.AddTorus(Vector3.zero, Vector3.up, 0.5f, 0.02f, 24, 4, HiveGlow);
            for (int i = 0; i < 3; i++)
            {
                r.AddBlob(Around(i * Mathf.PI * 2f / 3f) * 0.5f, new Vector3(0.05f, 0.04f, 0.05f), 3, 6, 0f, 0, HiveGlow, false);
            }

            ring = r.ToMesh("NestRing");
            return b.ToMesh($"Nest_{element}");
        }

        /// <summary>A quad-rotor drone. The four rotor discs are a separate mesh so they can spin; a mark shows its job.</summary>
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
    }
}
