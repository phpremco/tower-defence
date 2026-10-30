using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander's weapons from code. A weapon is two meshes with the grip at the origin and the
    /// business end along +z: the solid part and the energy part (a blade, a muzzle glow) that lights up when it
    /// is drawn. Submesh slots match <see cref="HeroModelFactory"/>: armour, trim glow, energy glow, dark.
    /// </summary>
    public static class HeroWeaponModelFactory
    {
        private const int Armour = HeroModelFactory.Armour;
        private const int Trim = HeroModelFactory.Trim;
        private const int Energy = HeroModelFactory.Visor;
        private const int Dark = HeroModelFactory.Dark;

        private const float EnergyEmission = 3f;

        public readonly struct Model
        {
            public readonly Mesh Grip;
            public readonly Mesh Energy;

            /// <summary>The far end of the weapon: where the trail or the bolt starts, in weapon space.</summary>
            public readonly Vector3 Tip;

            /// <summary>True when the weapon rests on the pack; false when it hangs at the hip.</summary>
            public readonly bool OnBack;

            /// <summary>How the weapon lies when holstered.</summary>
            public readonly Quaternion HolsterRotation;

            /// <summary>How the weapon sits in the hand when drawn.</summary>
            public readonly Quaternion GripRotation;

            public Model(Mesh grip, Mesh energy, Vector3 tip, bool onBack, Quaternion holsterRotation, Quaternion gripRotation)
            {
                Grip = grip;
                Energy = energy;
                Tip = tip;
                OnBack = onBack;
                HolsterRotation = holsterRotation;
                GripRotation = gripRotation;
            }
        }

        /// <summary>The suit materials with the energy slot in the weapon's own colour.</summary>
        public static Material[] CreateMaterials(Color trim, Color energy)
        {
            Material[] materials = HeroModelFactory.CreateMaterials(trim);
            materials[Energy] = SurfaceMaterials.Create(energy, EnergyEmission, 0.7f);
            return materials;
        }

        public static Model Create(HeroWeaponStyle style)
        {
            switch (style)
            {
                case HeroWeaponStyle.Blaster: return Blaster();
                default: return EnergySword();
            }
        }

        // A hilt with a pommel and an emitter collar; the blade is a long tapered rod of light.
        private static Model EnergySword()
        {
            var hilt = new PropMeshBuilder();
            hilt.AddTube(new[] { new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, 0.16f) }, new[] { 0.032f, 0.032f }, 8, Dark, false);
            hilt.AddBlob(new Vector3(0f, 0f, -0.11f), Vector3.one * 0.04f, 3, 6, 0f, 0, Trim, false);
            hilt.AddTube(new[] { new Vector3(0f, 0f, 0.16f), new Vector3(0f, 0f, 0.23f) }, new[] { 0.048f, 0.045f }, 8, Trim, false);
            hilt.AddBox(new Vector3(0f, 0.05f, 0.17f), new Vector3(0.02f, 0.04f, 0.05f), Armour);

            var blade = new PropMeshBuilder();
            blade.AddTube(new[] { new Vector3(0f, 0f, 0.23f), new Vector3(0f, 0f, 1.05f), new Vector3(0f, 0f, 1.3f) }, new[] { 0.04f, 0.04f, 0f }, 8, Energy, false);

            return new Model(hilt.ToMesh("Sword_hilt"), blade.ToMesh("Sword_blade"), new Vector3(0f, 0f, 1.2f), true, Quaternion.Euler(-118f, 0f, 22f), Quaternion.identity);
        }

        // A compact carbine: pistol grip, receiver with a top rail, a short barrel with a muzzle ring and a glowing
        // power cell. When drawn it lies along the forearm, barrel forward.
        private static Model Blaster()
        {
            var body = new PropMeshBuilder();
            body.AddBox(new Vector3(0f, -0.07f, -0.01f), new Vector3(0.03f, 0.1f, 0.05f), Dark, Quaternion.Euler(-15f, 0f, 0f));
            body.AddBox(new Vector3(0f, 0.01f, 0.08f), new Vector3(0.055f, 0.07f, 0.32f), Armour);
            body.AddBox(new Vector3(0f, 0.055f, 0.1f), new Vector3(0.025f, 0.02f, 0.2f), Dark);
            body.AddBox(new Vector3(0f, 0.0f, -0.09f), new Vector3(0.05f, 0.06f, 0.04f), Dark);
            body.AddTube(new[] { new Vector3(0f, 0.01f, 0.22f), new Vector3(0f, 0.01f, 0.5f) }, new[] { 0.022f, 0.02f }, 8, Dark, false);
            body.AddTube(new[] { new Vector3(0f, 0.01f, 0.44f), new Vector3(0f, 0.01f, 0.52f) }, new[] { 0.03f, 0.03f }, 8, Trim, false);
            body.AddBox(new Vector3(0.03f, 0.0f, 0.02f), new Vector3(0.012f, 0.03f, 0.1f), Trim);

            var energy = new PropMeshBuilder();
            energy.AddBox(new Vector3(-0.03f, 0.0f, 0.02f), new Vector3(0.012f, 0.03f, 0.1f), Energy);
            energy.AddBlob(new Vector3(0f, 0.01f, 0.52f), Vector3.one * 0.018f, 3, 6, 0f, 0, Energy, false);

            return new Model(body.ToMesh("Blaster_body"), energy.ToMesh("Blaster_energy"), new Vector3(0f, 0.01f, 0.54f), false, Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(90f, 0f, 0f));
        }
    }
}
