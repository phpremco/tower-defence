using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// Flat cards for the painted effects that are not particles (muzzle flashes, bolts, rocket flames): a shared quad
    /// mesh and the turn that lays a card along a direction while it faces the camera, so a flash or a bolt keeps its
    /// length whichever way it points.
    /// </summary>
    public static class VfxQuad
    {
        private static Mesh _fromStart;
        private static Mesh _centred;

        /// <summary>A unit quad from x 0 to 1 (a flash or flame that starts at a point and reaches out along +x).</summary>
        public static Mesh FromStart => _fromStart != null ? _fromStart : _fromStart = Build("VfxQuadFromStart", 0f);

        /// <summary>A unit quad centred on its origin, long axis x.</summary>
        public static Mesh Centred => _centred != null ? _centred : _centred = Build("VfxQuadCentred", -0.5f);

        /// <summary>The rotation whose x axis is <paramref name="axis"/> and whose face turns towards the camera.</summary>
        public static Quaternion Along(Vector3 axis, Vector3 cameraForward)
        {
            if (axis.sqrMagnitude < 1e-8f)
            {
                return Quaternion.LookRotation(cameraForward);
            }

            Vector3 x = axis.normalized;
            Vector3 z = cameraForward - Vector3.Dot(cameraForward, x) * x;
            if (z.sqrMagnitude < 1e-8f)
            {
                // Pointing straight at the camera: any face will do, the card is seen end on.
                z = Vector3.Cross(x, Vector3.up);
                if (z.sqrMagnitude < 1e-8f)
                {
                    z = Vector3.forward;
                }
            }

            z.Normalize();
            return Quaternion.LookRotation(z, Vector3.Cross(z, x));
        }

        /// <summary>A card renderer under <paramref name="parent"/>, hidden until shown.</summary>
        public static MeshRenderer Create(Transform parent, string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            go.SetActive(false);
            return renderer;
        }

        private static Mesh Build(string name, float start)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(start, -0.5f, 0f), new Vector3(start + 1f, -0.5f, 0f), new Vector3(start + 1f, 0.5f, 0f), new Vector3(start, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
