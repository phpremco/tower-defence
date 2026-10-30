using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// Puts a drone model together under a parent: hull, one rotor object per pivot, the cargo pod and the muzzle
    /// points. The in-level view animates it; the HUD renders the same thing into a card picture.
    /// </summary>
    public static class DroneModelBuilder
    {
        public sealed class Rig
        {
            public GameObject Root;
            public Material[] Materials;
            public Transform[] Rotors;
            public Vector3 RotorAxis;
            public Renderer Cargo;
            public Vector3[] Muzzles;
        }

        public static Rig Build(Transform parent, DroneDefinition definition)
        {
            DroneModelFactory.Parts parts = DroneModelFactory.For((int)definition.Kind);
            Material[] materials = Materials(definition.Color);

            var root = new GameObject("DroneModel");
            root.transform.SetParent(parent, false);
            Part("Hull", root.transform, parts.Hull, materials, Vector3.zero);

            var rotors = new Transform[parts.RotorPivots.Length];
            for (int i = 0; i < rotors.Length; i++)
            {
                Transform rotor = Part("Rotor", root.transform, parts.Rotor, materials, parts.RotorPivots[i]).transform;
                // The blade mesh lies flat (spinning around +Y); a tail fan spins around its own axis instead.
                rotor.localRotation = Quaternion.FromToRotation(Vector3.up, parts.RotorAxis);
                rotors[i] = rotor;
            }

            Renderer cargo = null;
            if (parts.Cargo != null)
            {
                cargo = Part("Cargo", root.transform, parts.Cargo, new[] { materials[ModelPalette.Dark] }, parts.CargoPivot);
            }

            return new Rig { Root = root, Materials = materials, Rotors = rotors, RotorAxis = parts.RotorAxis, Cargo = cargo, Muzzles = parts.Muzzles };
        }

        // Drones wear their own colour on the hull, not just on the lights, so the three kinds read apart at a glance.
        private static Material[] Materials(Color color)
        {
            return new[]
            {
                SurfaceMaterials.Create(Color.Lerp(new Color(0.7f, 0.74f, 0.82f), color, 0.6f), 0.12f, 0.5f, 0.2f),
                SurfaceMaterials.Create(color, 2.2f, 0.6f),
                SurfaceMaterials.Create(new Color(0.8f, 0.84f, 0.92f), 0f, 0.5f, 0.2f),
                SurfaceMaterials.Create(new Color(0.08f, 0.09f, 0.12f), 0f, 0.3f),
            };
        }

        private static Renderer Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
    }
}
