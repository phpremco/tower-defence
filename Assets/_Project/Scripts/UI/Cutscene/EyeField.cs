using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Hundreds of glowing eye pairs in the dark, as one combined mesh (two submeshes for two warm glows) whose
    /// vertices are rewritten each frame: every pair opens by stretching its slits from a closed line, at its own
    /// moment, so the opening spreads through the cavern like a wave. Posed from time alone.
    /// </summary>
    public sealed class EyeField
    {
        private const float EyeWidth = 0.075f;
        private const float EyeHeight = 0.04f;
        private const float EyeGap = 0.11f;
        private const float OpenSeconds = 0.35f;
        private const float BlinkRate = 0.23f;
        private const float BlinkSeconds = 0.14f;
        private const float Emission = 5f;
        private const float ClosedShare = 0.02f;
        private const float SlitStart = 0.4f;
        private static readonly Color Amber = new Color(1f, 0.62f, 0.15f);
        private static readonly Color Ember = new Color(1f, 0.25f, 0.08f);

        private readonly Mesh _mesh;
        private readonly Vector3[] _centres;
        private readonly Vector3[] _rights;
        private readonly Vector3[] _ups;
        private readonly float[] _sizes;
        private readonly float[] _opensAt;
        private readonly Vector3[] _vertices;

        /// <param name="centres">Where each pair sits; every pair looks at <paramref name="watched"/>.</param>
        /// <param name="opensAt">When each pair opens, in seconds of the shot.</param>
        /// <param name="sizes">How big each pair is, one for the usual size.</param>
        public EyeField(Transform parent, IReadOnlyList<Vector3> centres, IReadOnlyList<float> opensAt, IReadOnlyList<float> sizes, Vector3 watched)
        {
            int pairs = centres.Count;
            _centres = new Vector3[pairs];
            _rights = new Vector3[pairs];
            _ups = new Vector3[pairs];
            _sizes = new float[pairs];
            _opensAt = new float[pairs];
            _vertices = new Vector3[pairs * 8];
            var uvs = new Vector2[_vertices.Length];
            var amber = new List<int>();
            var ember = new List<int>();
            for (int i = 0; i < pairs; i++)
            {
                _centres[i] = centres[i];
                _opensAt[i] = opensAt[i];
                Vector3 look = (watched - centres[i]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, look);
                _rights[i] = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
                _ups[i] = Vector3.Cross(look, _rights[i]).normalized;
                _sizes[i] = sizes[i];
                List<int> triangles = i % 3 == 0 ? ember : amber;
                for (int eye = 0; eye < 2; eye++)
                {
                    int v = i * 8 + eye * 4;
                    uvs[v] = new Vector2(0f, 0.5f);
                    uvs[v + 1] = new Vector2(0.5f, 1f);
                    uvs[v + 2] = new Vector2(1f, 0.5f);
                    uvs[v + 3] = new Vector2(0.5f, 0f);
                    // Both windings, so a pair shows whichever side the camera passes on.
                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3, v, v + 2, v + 1, v, v + 3, v + 2 });
                }
            }

            _mesh = new Mesh { name = "Eyes", indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.uv = uvs;
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(amber, 0);
            _mesh.SetTriangles(ember, 1);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            var go = new GameObject("Eyes");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { Glow(Amber), Glow(Ember) };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public int Count => _centres.Length;

        /// <summary>Opens every pair that is due, with a rare blink, at this moment of the shot.</summary>
        public void Pose(float time)
        {
            for (int i = 0; i < _centres.Length; i++)
            {
                float open = CutsceneMath.Smooth(CutsceneMath.Span(time, _opensAt[i], _opensAt[i] + OpenSeconds));
                float blinkPhase = Mathf.Repeat(time * BlinkRate + CutsceneMath.Hash(i, 83), 1f);
                if (blinkPhase < BlinkSeconds * BlinkRate)
                {
                    open *= 0.1f;
                }

                // A shut eye is no eye at all: the slit grows out of a point, so nothing glows before it opens.
                float size = open > ClosedShare ? _sizes[i] : 0f;
                Vector3 right = _rights[i] * (EyeWidth * size * Mathf.Lerp(SlitStart, 1f, open));
                Vector3 up = _ups[i] * (EyeHeight * size * Mathf.Max(0.02f, open));
                Vector3 gap = _rights[i] * (EyeGap * size * 0.5f);
                for (int eye = 0; eye < 2; eye++)
                {
                    Vector3 centre = _centres[i] + (eye == 0 ? -gap : gap);
                    // A slit slanted inward on both sides, the way a predator's eyes sit.
                    Vector3 slant = _ups[i] * (EyeHeight * size * 0.4f * (eye == 0 ? 1f : -1f));
                    int v = i * 8 + eye * 4;
                    _vertices[v] = centre - right + slant;
                    _vertices[v + 1] = centre + up;
                    _vertices[v + 2] = centre + right - slant;
                    _vertices[v + 3] = centre - up;
                }
            }

            _mesh.vertices = _vertices;
        }

        private static Material Glow(Color colour)
        {
            Material material = SurfaceMaterials.Create(colour, Emission, 0f);
            material.SetColor("_BaseColor", colour * 0.2f);
            return material;
        }
    }
}
