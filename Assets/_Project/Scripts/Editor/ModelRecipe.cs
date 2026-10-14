using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Describes how one untextured model is split into coloured parts. A classifier receives the centre of each
    /// triangle as a 0..1 position inside the model's bounding box and returns the index of the part it belongs to.
    /// </summary>
    public sealed class ModelRecipe
    {
        public sealed class Part
        {
            public string Name;
            public Color Color = Color.gray;
            public float Metallic = 0.6f;
            public float Smoothness = 0.5f;
            public float Emission;
        }

        private Func<Vector3, int> _classify = _ => 0;

        public float Scale { get; private set; } = 1f;

        public List<Part> Parts { get; } = new List<Part> { new Part { Name = "Body" } };

        /// <summary>Turns the model about the vertical axis so its barrel points along +Z, which is "forward" for towers.</summary>
        public float YawDegrees { get; private set; }

        public static ModelRecipe For(string modelName)
        {
            return modelName == "machinegun" ? MachineGun() : new ModelRecipe();
        }

        // The gatling turret of the pulse rifle. After the yaw its barrel runs along z (0 = rear, 1 = muzzle) and y is height.
        private static ModelRecipe MachineGun()
        {
            var recipe = new ModelRecipe { YawDegrees = -90f };
            recipe.Parts.Clear();
            recipe.Parts.Add(new Part { Name = "Base", Color = new Color(0.16f, 0.18f, 0.24f), Metallic = 0.8f, Smoothness = 0.45f });
            recipe.Parts.Add(new Part { Name = "Armour", Color = new Color(0.55f, 0.64f, 0.78f), Metallic = 0.55f, Smoothness = 0.6f });
            recipe.Parts.Add(new Part { Name = "Barrels", Color = new Color(0.82f, 0.84f, 0.9f), Metallic = 0.9f, Smoothness = 0.75f });
            recipe.Parts.Add(new Part { Name = "Muzzle", Color = new Color(0.3f, 0.9f, 1f), Metallic = 0f, Smoothness = 0.3f, Emission = 2.5f });
            recipe.Parts.Add(new Part { Name = "Underside", Color = new Color(0.24f, 0.27f, 0.36f), Metallic = 0.8f, Smoothness = 0.4f });
            recipe._classify = unit =>
            {
                if (unit.z > 0.965f)
                {
                    return 3;
                }

                if (unit.z > 0.5f)
                {
                    return 2;
                }

                if (unit.y < 0.33f)
                {
                    return 0;
                }

                // Light top and darker bottom, so the gun stands out from the ground (see the look rules in CLAUDE.md).
                return unit.y < 0.52f ? 4 : 1;
            };
            return recipe;
        }

        /// <summary>Applies the recipe's turn to a freshly read mesh.</summary>
        public void Orient(Mesh mesh)
        {
            Quaternion turn = Quaternion.Euler(0f, YawDegrees, 0f);
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = turn * vertices[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>Gives every triangle to the part its centre falls in and writes the submeshes.</summary>
        public void Apply(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Bounds bounds = mesh.bounds;
            var lists = new List<int>[Parts.Count];
            for (int i = 0; i < lists.Length; i++)
            {
                lists[i] = new List<int>();
            }

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 centre = (vertices[triangles[t]] + vertices[triangles[t + 1]] + vertices[triangles[t + 2]]) / 3f;
                Vector3 unit = new Vector3(
                    Mathf.InverseLerp(bounds.min.x, bounds.max.x, centre.x),
                    Mathf.InverseLerp(bounds.min.y, bounds.max.y, centre.y),
                    Mathf.InverseLerp(bounds.min.z, bounds.max.z, centre.z));
                int part = Mathf.Clamp(_classify(unit), 0, Parts.Count - 1);
                lists[part].Add(triangles[t]);
                lists[part].Add(triangles[t + 1]);
                lists[part].Add(triangles[t + 2]);
            }

            mesh.subMeshCount = Parts.Count;
            for (int i = 0; i < lists.Length; i++)
            {
                mesh.SetTriangles(lists[i], i);
            }

            mesh.RecalculateBounds();
        }
    }
}
