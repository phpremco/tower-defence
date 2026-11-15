using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// The soft cone of glowing gas every rocket plume wears (the <c>Game/ExhaustPlume</c> shader on a cone that opens towards
    /// its tail), shared by the cutscene's plumes and the level's shuttle (round 63), so both burn in one style. The shader is
    /// kept in the build by the Boot scene's shader keepers.
    /// </summary>
    public static class PlumeKit
    {
        public const string ShaderName = "Game/ExhaustPlume";

        private const int Sides = 18;
        private const int Rings = 10;
        // How far the cone opens: the tail is this many nozzle radii across more than the mouth (gas spreads in vacuum).
        private const float Flare = 1.5f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");

        private static Mesh _cone;

        /// <summary>A plume material: <paramref name="core"/> at the mouth, <paramref name="edge"/> at the soft rim.</summary>
        public static Material Material(Color core, Color edge, float edgePower, float lengthFade, float coreLength, float diamonds)
        {
            Shader shader = Shader.Find(ShaderName);
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit")) { name = ShaderName };
            material.SetColor("_CoreColor", core);
            material.SetColor("_EdgeColor", edge);
            material.SetFloat("_EdgePower", edgePower);
            material.SetFloat("_LengthFade", lengthFade);
            material.SetFloat("_CoreLength", coreLength);
            material.SetFloat("_Diamonds", diamonds);
            return material;
        }

        /// <summary>The soft outer gas of a plume, as the cutscene's plumes wear it.</summary>
        public static Material Outer(Color core, Color edge) => Material(core, edge, 2.2f, 1.3f, 0.35f, 0.45f);

        /// <summary>The narrow white-hot core inside a plume.</summary>
        public static Material Core(Color core, Color edge) => Material(core, Color.Lerp(core, edge, 0.4f), 3.2f, 2.4f, 0.7f, 0.9f);

        /// <summary>Sets how bright a plume material burns and the clock its gas flows on.</summary>
        public static void Burn(Material material, float intensity, float clock)
        {
            material.SetFloat(IntensityId, intensity);
            material.SetFloat(ClockId, clock);
        }

        /// <summary>A plume cone under <paramref name="parent"/>: no shadows; scale x/y = mouth radius, z = length.</summary>
        public static Transform Cone(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        // A unit cone along +Z, its radius one at the mouth and opening towards the tail; UV.y runs along it.
        private static Mesh ConeMesh()
        {
            if (_cone != null)
            {
                return _cone;
            }

            int columns = Sides + 1;
            var vertices = new Vector3[columns * (Rings + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int r = 0; r <= Rings; r++)
            {
                float z = (float)r / Rings;
                float radius = 1f + Flare * Mathf.Pow(z, 0.7f);
                float slope = Flare * 0.7f * Mathf.Pow(Mathf.Max(z, 0.02f), -0.3f);
                for (int s = 0; s < columns; s++)
                {
                    float u = (float)s / Sides;
                    float angle = u * Mathf.PI * 2f;
                    var round = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    int i = r * columns + s;
                    vertices[i] = round * radius + Vector3.forward * z;
                    normals[i] = (round - Vector3.forward * slope).normalized;
                    uvs[i] = new Vector2(u, z);
                }
            }

            var triangles = new int[Sides * Rings * 6];
            int t = 0;
            for (int r = 0; r < Rings; r++)
            {
                for (int s = 0; s < Sides; s++)
                {
                    int a = r * columns + s;
                    int b = a + columns;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            }

            _cone = new Mesh { name = "PlumeCone", vertices = vertices, normals = normals, uv = uvs, triangles = triangles };
            _cone.RecalculateBounds();
            return _cone;
        }
    }
}
