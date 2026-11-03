using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The modules a new base unfolds besides the command post: the energy core (a cage of pillars on a plinth with
    /// a separate crystal that lights up) and the landing platform (a plated hexagon with lit edges). Slots follow
    /// <see cref="ModelPalette"/>; standing on y = 0.
    /// </summary>
    public static class BaseModuleFactory
    {
        private const int Body = ModelPalette.Body;
        private const int Glow = ModelPalette.Glow;
        private const int Metal = ModelPalette.Metal;
        private const int Dark = ModelPalette.Dark;
        private const int Pillars = 4;

        /// <summary>The cage around the core, open so the crystal inside shows.</summary>
        public static Mesh CoreCage()
        {
            var b = new PropMeshBuilder();
            b.AddTube(new[] { Vector3.zero, Vector3.up * 0.3f }, new[] { 0.9f, 0.82f }, 8, Dark, true);
            b.AddBlob(Vector3.up * 0.3f, new Vector3(0.82f, 0.05f, 0.82f), 2, 8, 0f, 0, Body, true, Mathf.PI * 0.5f);
            b.AddTorus(Vector3.up * 0.32f, Vector3.up, 0.66f, 0.06f, 24, 6, Glow);
            for (int i = 0; i < Pillars; i++)
            {
                float angle = (i / (float)Pillars + 0.125f) * Mathf.PI * 2f;
                var around = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                b.AddTube(new[] { around * 0.7f + Vector3.up * 0.3f, around * 0.58f + Vector3.up * 1.5f, around * 0.42f + Vector3.up * 2.05f }, new[] { 0.09f, 0.07f, 0.06f }, 6, Metal, true);
                b.AddBox(around * 0.64f + Vector3.up * 0.95f, new Vector3(0.05f, 0.5f, 0.05f), Glow, -angle * Mathf.Rad2Deg);
            }

            b.AddTorus(Vector3.up * 2.08f, Vector3.up, 0.44f, 0.08f, 20, 6, Body);
            b.AddTorus(Vector3.up * 1.2f, Vector3.up, 0.6f, 0.035f, 20, 6, Dark);
            return b.ToMesh("CoreCage");
        }

        /// <summary>The energy crystal: a tall hexagonal prism, pointed at both ends; one submesh for its own glowing material.</summary>
        public static Mesh CoreCrystal()
        {
            var b = new PropMeshBuilder(1);
            b.AddTube(new[] { Vector3.up * 0.45f, Vector3.up * 0.8f, Vector3.up * 1.55f, Vector3.up * 1.9f }, new[] { 0f, 0.26f, 0.26f, 0f }, 6, 0, true);
            return b.ToMesh("CoreCrystal");
        }

        /// <summary>A landing platform: a low plated hexagon with a lit rim, a landing ring and lamps at the corners.</summary>
        public static Mesh LandingPad()
        {
            const int Sides = 6;
            const float Radius = 1.5f;
            var b = new PropMeshBuilder();
            b.AddTube(new[] { Vector3.zero, Vector3.up * 0.16f }, new[] { Radius, Radius - 0.08f }, Sides, Dark, true);
            // Tubes are open, so a flat dome closes the top as a hexagonal deck.
            b.AddBlob(Vector3.up * 0.16f, new Vector3(Radius - 0.08f, 0.04f, Radius - 0.08f), 2, Sides, 0f, 0, Body, true, Mathf.PI * 0.5f);
            b.AddTorus(Vector3.up * 0.21f, Vector3.up, Radius * 0.55f, 0.04f, 30, 5, Glow);
            for (int i = 0; i < Sides; i++)
            {
                float angle = i / (float)Sides * Mathf.PI * 2f;
                var corner = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (Radius - 0.05f);
                b.AddBox(corner + Vector3.up * 0.2f, new Vector3(0.12f, 0.1f, 0.12f), Metal, -angle * Mathf.Rad2Deg);
                b.AddBlob(corner + Vector3.up * 0.3f, Vector3.one * 0.05f, 3, 6, 0f, i, Glow, false);
            }

            return b.ToMesh("LandingPad");
        }
    }
}
