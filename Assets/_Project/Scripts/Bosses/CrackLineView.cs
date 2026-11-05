using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// The glowing crack a burrowing boss tears through the ground: a warm orange seam along its planned route that
    /// opens ahead of it (so the player sees where it is heading), bright in the middle and soft at its edges (the painted
    /// glow blade), a pool of light and a spray of grit at the open end, and a slow cooling once the boss is out. The
    /// seam's mesh is built once; opening it only changes how many of its triangles are drawn.
    /// </summary>
    public sealed class CrackLineView : MonoBehaviour
    {
        private const float Width = 0.75f;
        private const float GlowWidth = 2.2f;
        private const float Lift = 0.05f;
        private const float FadeSeconds = 4f;
        private const float HeadLightRadius = 1.4f;
        private const float GritRate = 36f;
        // The blade texture fades out at both of its ends (v 0 and 1); the seam keeps clear of them.
        private const float AlongMin = 0.2f;
        private const float AlongSpan = 0.6f;
        private static readonly Color Seam = new Color(1f, 0.42f, 0.1f, 1f) * 2.6f;
        private static readonly Color Halo = new Color(1f, 0.35f, 0.08f, 1f) * 0.7f;
        private static readonly Color HeadGlow = new Color(1f, 0.5f, 0.15f, 0.9f);
        private static readonly Color Grit = new Color(0.55f, 0.42f, 0.3f, 0.8f);

        private readonly List<float> _along = new List<float>();
        private readonly List<Material> _materials = new List<Material>(2);
        private readonly List<Mesh> _meshes = new List<Mesh>(2);
        private readonly List<int[]> _triangles = new List<int[]>(2);
        private Vector3[] _points;
        private float _length;
        private float _open = -1f;
        private float _fade = -1f;
        private Transform _head;
        private ParticleSystem _grit;

        /// <summary>Lays a crack along world points (on the ground); nothing shows until it opens.</summary>
        public static CrackLineView Create(Transform parent, IReadOnlyList<Vector3> points)
        {
            var go = new GameObject("CrackLine");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CrackLineView>();
            view.Build(points);
            return view;
        }

        public float Length => _length;

        /// <summary>Opens the crack up to a distance along it.</summary>
        public void OpenTo(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, _length);
            if (Mathf.Approximately(distance, _open))
            {
                return;
            }

            _open = distance;
            int segments = 0;
            while (segments < _points.Length - 1 && _along[segments + 1] <= distance + 0.001f)
            {
                segments++;
            }

            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].SetTriangles(_triangles[i], 0, segments * TrianglesPerSegment, 0, false);
            }

            _head.position = PointAt(distance) + Vector3.up * Lift;
            ParticleKit.SetRate(_grit, distance < _length ? GritRate : 0f);
        }

        /// <summary>Lets the crack cool down and disappear.</summary>
        public void Fade()
        {
            if (_fade < 0f)
            {
                _fade = 0f;
                ParticleKit.SetRate(_grit, 0f);
            }
        }

        private const int TrianglesPerSegment = 24;

        private void Update()
        {
            if (_fade < 0f)
            {
                return;
            }

            _fade += Time.deltaTime;
            float left = 1f - _fade / FadeSeconds;
            if (left <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _materials[0].SetColor(MarkerArt.Tint, Seam * left);
            _materials[1].SetColor(MarkerArt.Tint, Halo * left);
            _head.localScale = Vector3.one * left;
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _meshes)
            {
                Destroy(mesh);
            }

            foreach (Material material in _materials)
            {
                Destroy(material);
            }
        }

        private void Build(IReadOnlyList<Vector3> points)
        {
            _points = new Vector3[points.Count];
            _along.Clear();
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                _points[i] = points[i] + Vector3.up * Lift;
                if (i > 0)
                {
                    total += Vector3.Distance(_points[i - 1], _points[i]);
                }

                _along.Add(total);
            }

            _length = total;
            Layer("Seam", Width, Seam);
            Layer("Halo", GlowWidth, Halo);
            _head = new GameObject("Head").transform;
            _head.SetParent(transform, false);
            GlowDecal.PlacePool(_head, HeadGlow, HeadLightRadius);
            _grit = ParticleKit.Create(_head, "Grit", Vector3.up * 0.05f, Grit, 0.4f, 0.9f, 0.04f, 0.12f, 1.2f, 2.8f, 1.2f, 80, 0.3f);
            ParticleKit.SetRate(_grit, 0f);
            OpenTo(0f);
        }

        // One additive strip along the crack: three vertices across (edge, core, edge), the core bright, the edges soft.
        private void Layer(string name, float width, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            int count = _points.Length;
            var vertices = new Vector3[count * 3];
            var uvs = new Vector2[count * 3];
            var triangles = new int[Mathf.Max(0, count - 1) * TrianglesPerSegment];
            for (int i = 0; i < count; i++)
            {
                Vector3 side = Side(_points, i) * (width * 0.5f);
                float v = AlongMin + AlongSpan * (_length > 0f ? _along[i] / _length : 0f);
                vertices[i * 3] = _points[i] - side;
                vertices[i * 3 + 1] = _points[i];
                vertices[i * 3 + 2] = _points[i] + side;
                uvs[i * 3] = new Vector2(1f, v);
                uvs[i * 3 + 1] = new Vector2(0f, v);
                uvs[i * 3 + 2] = new Vector2(1f, v);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 3;
                int b = a + 3;
                int t = i * TrianglesPerSegment;
                // Left half and right half, each two triangles, wound both ways so the strip shows from above whatever its turn.
                Quad(triangles, t, a, a + 1, b + 1, b);
                Quad(triangles, t + 6, a + 1, a + 2, b + 2, b + 1);
                Quad(triangles, t + 12, b, b + 1, a + 1, a);
                Quad(triangles, t + 18, b + 1, b + 2, a + 2, a + 1);
            }

            var mesh = new Mesh { name = "CrackLine" + name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            Material material = GlowDecal.BladeMaterial(colour);
            material.SetColor(MarkerArt.Tint, colour);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _meshes.Add(mesh);
            _materials.Add(material);
            _triangles.Add(triangles);
        }

        private static void Quad(int[] triangles, int at, int a, int b, int c, int d)
        {
            triangles[at] = a;
            triangles[at + 1] = b;
            triangles[at + 2] = c;
            triangles[at + 3] = a;
            triangles[at + 4] = c;
            triangles[at + 5] = d;
        }

        private Vector3 PointAt(float distance)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_along[i] >= distance)
                {
                    float span = _along[i] - _along[i - 1];
                    float t = span > 0.0001f ? (distance - _along[i - 1]) / span : 1f;
                    return Vector3.Lerp(_points[i - 1], _points[i], t);
                }
            }

            return _points[_points.Length - 1];
        }

        private static Vector3 Side(Vector3[] points, int i)
        {
            Vector3 incoming = i > 0 ? points[i] - points[i - 1] : Vector3.zero;
            Vector3 outgoing = i < points.Length - 1 ? points[i + 1] - points[i] : Vector3.zero;
            Vector3 along = incoming + outgoing;
            along.y = 0f;
            return along.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
        }
    }
}
