using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds the commander's weapons from code. A weapon is two meshes with the grip at the origin and the
    /// business end along +z: the solid part and the energy part (a blade, a muzzle glow) that lights up when it
    /// is drawn. Submesh slots match <see cref="HeroModelFactory"/>: armour, trim glow, energy glow, dark, metal.
    /// Weapons are mostly gunmetal and white with a lit edge, so they read apart from the blue suit.
    /// </summary>
    public static class HeroWeaponModelFactory
    {
        private const int Armour = HeroModelFactory.Armour;
        private const int Trim = HeroModelFactory.Trim;
        private const int Energy = HeroModelFactory.Visor;
        private const int Metal = HeroModelFactory.Metal;

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

        private static PropMeshBuilder Builder() => new PropMeshBuilder(HeroModelFactory.SlotCount);

        // A gunmetal hilt with a lit pommel, a white cross guard and a lit emitter collar; the blade is a long
        // tapered rod of light.
        private static Model EnergySword()
        {
            PropMeshBuilder hilt = Builder();
            hilt.AddTube(new[] { new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, 0.16f) }, new[] { 0.034f, 0.034f }, 8, Metal, false);
            hilt.AddBlob(new Vector3(0f, 0f, -0.11f), Vector3.one * 0.045f, 3, 6, 0f, 0, Trim, false);
            hilt.AddBlob(new Vector3(0f, 0f, 0.17f), new Vector3(0.11f, 0.035f, 0.035f), 3, 8, 0f, 0, Armour, false);
            hilt.AddTube(new[] { new Vector3(0f, 0f, 0.19f), new Vector3(0f, 0f, 0.25f) }, new[] { 0.05f, 0.046f }, 8, Trim, false);

            PropMeshBuilder blade = Builder();
            blade.AddTube(new[] { new Vector3(0f, 0f, 0.25f), new Vector3(0f, 0f, 1.05f), new Vector3(0f, 0f, 1.3f) }, new[] { 0.045f, 0.045f, 0f }, 8, Energy, false);

            return new Model(hilt.ToMesh("Sword_hilt"), blade.ToMesh("Sword_blade"), new Vector3(0f, 0f, 1.2f), true, Quaternion.Euler(-118f, 0f, 22f), Quaternion.identity);
        }

        // A compact carbine: gunmetal grip, stock and barrel, a white receiver with a lit side stripe, a muzzle
        // ring in the commander's colour and a glowing power cell. When drawn it lies along the forearm, barrel
        // forward.
        private static Model Blaster()
        {
            PropMeshBuilder body = Builder();
            body.AddBox(new Vector3(0f, -0.08f, -0.01f), new Vector3(0.034f, 0.11f, 0.055f), Metal, Quaternion.Euler(-15f, 0f, 0f));
            body.AddBox(new Vector3(0f, 0.01f, 0.08f), new Vector3(0.06f, 0.08f, 0.34f), Armour);
            body.AddBox(new Vector3(0f, 0.06f, 0.1f), new Vector3(0.028f, 0.022f, 0.22f), Metal);
            body.AddBox(new Vector3(0f, 0f, -0.1f), new Vector3(0.055f, 0.065f, 0.05f), Metal);
            body.AddTube(new[] { new Vector3(0f, 0.01f, 0.22f), new Vector3(0f, 0.01f, 0.52f) }, new[] { 0.026f, 0.022f }, 8, Metal, false);
            body.AddTube(new[] { new Vector3(0f, 0.01f, 0.46f), new Vector3(0f, 0.01f, 0.54f) }, new[] { 0.034f, 0.034f }, 8, Trim, false);
            body.AddBox(new Vector3(0.032f, 0f, 0.02f), new Vector3(0.012f, 0.03f, 0.1f), Trim);

            PropMeshBuilder energy = Builder();
            energy.AddBox(new Vector3(-0.032f, 0f, 0.02f), new Vector3(0.012f, 0.03f, 0.1f), Energy);
            energy.AddBlob(new Vector3(0f, 0.01f, 0.54f), Vector3.one * 0.02f, 3, 6, 0f, 0, Energy, false);

            return new Model(body.ToMesh("Blaster_body"), energy.ToMesh("Blaster_energy"), new Vector3(0f, 0.01f, 0.56f), false, Quaternion.Euler(90f, 0f, 0f), Quaternion.identity);
        }
    }
}
