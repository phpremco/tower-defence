using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Draws the imported environment (kerbs, pads, banks, cliffs, decor, scatter) with GPU instancing instead of one
    /// object per piece: the pieces are grouped by mesh, material and a square of the map, and each group that the main
    /// camera can see (with a margin for shadows) is drawn in one call per frame. A level holds thousands of pieces;
    /// as objects they cost thousands of draw calls, as groups a few hundred. Nothing allocates per frame.
    /// </summary>
    public sealed class EnvironmentInstancer : MonoBehaviour
    {
        private const int MaxPerDraw = 1023;
        private const float ChunkSize = 10f;
        private const float CullMargin = 3f;

        private sealed class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public bool Shadows;
            public readonly List<Matrix4x4> Building = new List<Matrix4x4>();
            public Matrix4x4[] Matrices;
            public Bounds Bounds;
            public bool HasBounds;
        }

        private readonly Dictionary<(Mesh, Material, bool, Vector2Int), Batch> _lookup = new Dictionary<(Mesh, Material, bool, Vector2Int), Batch>();
        private readonly List<Batch> _batches = new List<Batch>();
        private readonly Plane[] _planes = new Plane[6];
        private readonly List<Material> _ownMaterials = new List<Material>();

        /// <summary>How many pieces were added, for tests and reports.</summary>
        public int Count { get; private set; }

        /// <summary>How many groups (draws when all are in view) the pieces fell into.</summary>
        public int BatchCount => _batches.Count;

        public static EnvironmentInstancer Create(Transform parent)
        {
            var go = new GameObject("Environment");
            go.transform.SetParent(parent, false);
            return go.AddComponent<EnvironmentInstancer>();
        }

        /// <summary>
        /// Adds one piece at a placement in the piece's own frame (y up, as the README's placement frames describe it). The
        /// material is the model's for the profile unless the caller passes its own copy.
        /// </summary>
        /// <param name="castShadows">False keeps a piece that would throw a shadow out of the shadow pass (far decor).</param>
        public void Add(EnvironmentMesh model, Matrix4x4 matrix, bool low, Material material = null, bool castShadows = true)
        {
            if (model == null || !model.IsValid)
            {
                return;
            }

            Material wear = material != null ? material : model.MaterialFor(low);
            bool shadows = model.CastShadows && castShadows;
            Vector3 position = matrix.GetColumn(3);
            matrix *= model.Node;
            var chunk = new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize), Mathf.FloorToInt(position.z / ChunkSize));
            var key = (model.Mesh, wear, shadows, chunk);
            if (!_lookup.TryGetValue(key, out Batch batch))
            {
                batch = new Batch { Mesh = model.Mesh, Material = wear, Shadows = shadows };
                _lookup[key] = batch;
                _batches.Add(batch);
            }

            batch.Building.Add(matrix);
            Bounds bounds = Transform(model.Mesh.bounds, matrix);
            if (batch.HasBounds)
            {
                batch.Bounds.Encapsulate(bounds);
            }
            else
            {
                batch.Bounds = bounds;
                batch.HasBounds = true;
            }

            Count++;
        }

        /// <summary>A copy of a material this level changes (a tinted glow); destroyed with the level.</summary>
        public Material Own(Material source)
        {
            var copy = new Material(source) { name = source.name + " (level)" };
            _ownMaterials.Add(copy);
            return copy;
        }

        /// <summary>Freezes the groups once everything is added.</summary>
        public void Finish()
        {
            foreach (Batch batch in _batches)
            {
                batch.Matrices = batch.Building.ToArray();
                batch.Building.Clear();
                batch.Bounds.Expand(CullMargin);
            }

            _lookup.Clear();
        }

        private void LateUpdate()
        {
            Camera view = Camera.main;
            bool cull = view != null;
            if (cull)
            {
                GeometryUtility.CalculateFrustumPlanes(view, _planes);
            }

            int layer = gameObject.layer;
            for (int i = 0; i < _batches.Count; i++)
            {
                Batch batch = _batches[i];
                if (batch.Matrices == null || (cull && !GeometryUtility.TestPlanesAABB(_planes, batch.Bounds)))
                {
                    continue;
                }

                var parameters = new RenderParams(batch.Material)
                {
                    layer = layer,
                    shadowCastingMode = batch.Shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = batch.Bounds,
                };
                for (int start = 0; start < batch.Matrices.Length; start += MaxPerDraw)
                {
                    Graphics.RenderMeshInstanced(parameters, batch.Mesh, 0, batch.Matrices, Mathf.Min(MaxPerDraw, batch.Matrices.Length - start), start);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _ownMaterials)
            {
                Destroy(material);
            }
        }

        private static Bounds Transform(Bounds local, Matrix4x4 matrix)
        {
            Vector3 centre = matrix.MultiplyPoint3x4(local.center);
            Vector3 extents = local.extents;
            Vector3 x = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 y = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 z = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
            var size = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(centre, size * 2f);
        }
    }
}
