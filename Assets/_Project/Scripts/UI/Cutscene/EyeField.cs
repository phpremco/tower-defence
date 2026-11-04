using System.Collections.Generic;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The Burrower eyes in the cave walls (README round 26): every pair seated in rock where the cave put it, built
    /// once into one combined mesh per eye variant. Nothing moves on the CPU: each vertex carries its eye's pivot,
    /// hinge and up axis, and the <c>Game/BurrowerEye</c> shader turns the lids and the eyeball from the pair's state
    /// (blink, look, glow) that <see cref="Pose"/> writes into one global array a frame. A pair is shut until its moment,
    /// opens (the lead pairs slowly, the wave fast), blinks now and then and follows the camera. Posed from time alone.
    /// </summary>
    public sealed class EyeField
    {
        /// <summary>Where one pair sits: its two eyes' pivots on the rock, the way it faces, and when it opens.</summary>
        public struct Pair
        {
            public Vector3 Left;
            public Vector3 Right;
            public Vector3 Facing;
            public Vector3 Up;
            public float Scale;
            public float OpensAt;
            public float OpenSeconds;
            public int Variant;
        }

        public const int MaxPairs = 512;
        private const float BlinkRate = 0.19f;
        private const float BlinkSeconds = 0.16f;
        private const float GlowRampSeconds = 0.5f;
        private const float GlowPulse = 0.12f;
        private const float LookFollow = 0.8f;
        private const float BoundsSize = 200f;
        private static readonly int StateId = Shader.PropertyToID("_BurrowerEyeState");
        private static readonly int GlowId = Shader.PropertyToID("_EmissionColor");
        private static readonly int UpperId = Shader.PropertyToID("_UpperLid");
        private static readonly int LowerId = Shader.PropertyToID("_LowerLid");
        private static readonly int YawId = Shader.PropertyToID("_LookYaw");
        private static readonly int PitchId = Shader.PropertyToID("_LookPitch");

        // The imported meshes look along -Z (the 180 degree turn every FBX of ours arrives with); the README frame looks along +Z.
        private static readonly Matrix4x4 ImportTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        private readonly Pair[] _pairs;
        private readonly Vector4[] _state = new Vector4[MaxPairs];
        private readonly float[] _lookYaw;
        private readonly float[] _lookPitch;

        public EyeField(Transform parent, UndergroundArt art, IReadOnlyList<Pair> pairs, bool low)
        {
            int count = Mathf.Min(pairs.Count, MaxPairs);
            _pairs = new Pair[count];
            for (int i = 0; i < count; i++)
            {
                _pairs[i] = pairs[i];
            }

            BurrowerEyeArt[] eyes = art.Eyes;
            _lookYaw = new float[eyes.Length];
            _lookPitch = new float[eyes.Length];
            for (int v = 0; v < eyes.Length; v++)
            {
                if (eyes[v] == null || !eyes[v].IsValid)
                {
                    continue;
                }

                _lookYaw[v] = eyes[v].LookDegrees;
                _lookPitch[v] = eyes[v].LookUpDownDegrees;
                BuildVariant(parent, eyes[v], v, art.IrisGlow, low);
            }
        }

        public int Count => _pairs.Length;

        /// <summary>The pair's pivots, for probes and camera framing.</summary>
        public Pair this[int index] => _pairs[index];

        /// <summary>
        /// Opens every pair that is due at <paramref name="time"/> (a negative time keeps all shut), with a rare blink,
        /// each pair looking towards <paramref name="watched"/>.
        /// </summary>
        public void Pose(float time, Vector3 watched)
        {
            for (int i = 0; i < _pairs.Length; i++)
            {
                Pair pair = _pairs[i];
                float open = time < 0f ? 0f : CutsceneMath.Smooth(CutsceneMath.Span(time, pair.OpensAt, pair.OpensAt + pair.OpenSeconds));
                float blinkPhase = Mathf.Repeat(time * BlinkRate + CutsceneMath.Hash(i, 83), 1f);
                float blink = blinkPhase < BlinkSeconds * BlinkRate ? Mathf.Sin(blinkPhase / (BlinkSeconds * BlinkRate) * Mathf.PI) : 0f;
                float shut = Mathf.Max(1f - open, blink * open);
                float glow = time < 0f ? 0f : CutsceneMath.Span(time, pair.OpensAt, pair.OpensAt + GlowRampSeconds);
                glow *= 1f + GlowPulse * Mathf.Sin(time * 2.3f + i);
                Look(pair, watched, out float yaw, out float pitch);
                _state[i] = new Vector4(shut, yaw * LookFollow, pitch * LookFollow, glow);
            }

            Shader.SetGlobalVectorArray(StateId, _state);
        }

        // Towards the watched point in the pair's own frame, as a share of the eye's look range.
        private void Look(Pair pair, Vector3 watched, out float yaw, out float pitch)
        {
            Vector3 centre = (pair.Left + pair.Right) * 0.5f;
            Quaternion frame = Quaternion.LookRotation(pair.Facing, pair.Up);
            Vector3 local = Quaternion.Inverse(frame) * (watched - centre);
            float yawDegrees = Mathf.Atan2(local.x, Mathf.Max(0.01f, local.z)) * Mathf.Rad2Deg;
            float pitchDegrees = Mathf.Atan2(local.y, Mathf.Max(0.01f, new Vector2(local.x, local.z).magnitude)) * Mathf.Rad2Deg;
            int variant = Mathf.Clamp(pair.Variant, 0, _lookYaw.Length - 1);
            yaw = Mathf.Clamp(yawDegrees / Mathf.Max(1f, _lookYaw[variant]), -1f, 1f);
            // A positive turn about the hinge moves the front of the eye down, so looking up is a negative turn.
            pitch = -Mathf.Clamp(pitchDegrees / Mathf.Max(1f, _lookPitch[variant]), -1f, 1f);
        }

        private void BuildVariant(Transform parent, BurrowerEyeArt eye, int variant, float irisGlow, bool low)
        {
            Mesh source = eye.Mesh;
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uv = source.uv;
            Color[] colours = source.colors;
            int[][] subMeshes = { source.GetTriangles(0), source.subMeshCount > 1 ? source.GetTriangles(1) : new int[0] };
            Matrix4x4 local = ImportTurn * eye.Node;

            var positions = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTangents = new List<Vector4>();
            var outUv = new List<Vector2>();
            var outColours = new List<Color>();
            var pivots = new List<Vector4>();
            var hinges = new List<Vector4>();
            var ups = new List<Vector4>();
            var triangles = new[] { new List<int>(), new List<int>() };
            for (int p = 0; p < _pairs.Length; p++)
            {
                Pair pair = _pairs[p];
                if (pair.Variant != variant)
                {
                    continue;
                }

                for (int side = 0; side < 2; side++)
                {
                    bool mirrored = side == 1;
                    Vector3 pivot = mirrored ? pair.Right : pair.Left;
                    Quaternion rotation = Quaternion.LookRotation(pair.Facing, pair.Up);
                    Matrix4x4 place = Matrix4x4.TRS(pivot, rotation, new Vector3(mirrored ? -pair.Scale : pair.Scale, pair.Scale, pair.Scale));
                    Matrix4x4 full = place * local;
                    Vector3 hinge = place.MultiplyVector(eye.Axis).normalized;
                    Vector3 up = rotation * Vector3.up;
                    float mirror = mirrored ? -1f : 1f;
                    int start = positions.Count;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        positions.Add(full.MultiplyPoint3x4(vertices[i]));
                        outNormals.Add(full.MultiplyVector(normals[i]).normalized);
                        Vector3 tangent = full.MultiplyVector(tangents.Length > i ? (Vector3)tangents[i] : Vector3.right).normalized;
                        outTangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, (tangents.Length > i ? tangents[i].w : 1f) * mirror));
                        outUv.Add(uv.Length > i ? uv[i] : Vector2.zero);
                        outColours.Add(colours.Length > i ? colours[i] : Color.black);
                        pivots.Add(new Vector4(pivot.x, pivot.y, pivot.z, p));
                        hinges.Add(new Vector4(hinge.x, hinge.y, hinge.z, mirror));
                        ups.Add(new Vector4(up.x, up.y, up.z, 0f));
                    }

                    for (int s = 0; s < 2; s++)
                    {
                        int[] indices = subMeshes[s];
                        for (int t = 0; t < indices.Length; t += 3)
                        {
                            // A mirrored eye turns its triangles inside out; swapping two corners keeps them facing out.
                            triangles[s].Add(start + indices[t]);
                            triangles[s].Add(start + indices[mirrored ? t + 2 : t + 1]);
                            triangles[s].Add(start + indices[mirrored ? t + 1 : t + 2]);
                        }
                    }
                }
            }

            if (positions.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = $"Eyes_{eye.Name}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(positions);
            mesh.SetNormals(outNormals);
            mesh.SetTangents(outTangents);
            mesh.SetUVs(0, outUv);
            mesh.SetUVs(1, pivots);
            mesh.SetUVs(2, hinges);
            mesh.SetUVs(3, ups);
            mesh.SetColors(outColours);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles[0], 0);
            mesh.SetTriangles(triangles[1], 1);
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(mesh.bounds.center, Vector3.one * BoundsSize);

            var go = new GameObject($"Eyes_{eye.Name}");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            Material material = Configure(new Material(eye.EyeFor(low)), eye, irisGlow);
            // The Low profile leaves the wet cornea out: one material draws the first sub-mesh only.
            renderer.sharedMaterials = low || eye.Cornea == null ? new[] { material } : new[] { material, Configure(new Material(eye.Cornea), eye, 0f) };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static Material Configure(Material material, BurrowerEyeArt eye, float glow)
        {
            material.SetColor(GlowId, Color.white * glow);
            material.SetFloat(UpperId, eye.UpperLidDegrees);
            material.SetFloat(LowerId, eye.LowerLidDegrees);
            material.SetFloat(YawId, eye.LookDegrees);
            material.SetFloat(PitchId, eye.LookUpDownDegrees);
            return material;
        }
    }
}
