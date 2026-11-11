using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Drives a model's face blend shapes by name on every mesh that has them (the near face and its far LOD). A pose
    /// adds weights (0..1, summed per shape and clamped) and <see cref="Apply"/> writes them; call it after the body was
    /// sampled or animated, because a clip writes its own constant-zero shape curves over the face. Allocation-free per frame.
    /// </summary>
    public sealed class FaceRig
    {
        private const float WeightScale = 100f;

        private readonly string[] _shapes;
        private readonly float[] _weights;
        private readonly SkinnedMeshRenderer[] _renderers;
        // Per renderer, the index of each shape on its mesh (or -1).
        private readonly int[][] _indices;

        public FaceRig(IEnumerable<Renderer> renderers, IReadOnlyList<string> shapes)
        {
            var names = new List<string>();
            foreach (string shape in shapes)
            {
                if (!string.IsNullOrEmpty(shape) && !names.Contains(shape))
                {
                    names.Add(shape);
                }
            }

            _shapes = names.ToArray();
            _weights = new float[_shapes.Length];
            var found = new List<SkinnedMeshRenderer>();
            var indices = new List<int[]>();
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer skinned) || skinned.sharedMesh == null || skinned.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                var map = new int[_shapes.Length];
                bool any = false;
                for (int i = 0; i < _shapes.Length; i++)
                {
                    map[i] = skinned.sharedMesh.GetBlendShapeIndex(_shapes[i]);
                    any |= map[i] >= 0;
                }

                if (any)
                {
                    found.Add(skinned);
                    indices.Add(map);
                }
            }

            _renderers = found.ToArray();
            _indices = indices.ToArray();
        }

        /// <summary>True when at least one mesh carries one of the shapes.</summary>
        public bool HasFace => _renderers.Length > 0;

        /// <summary>The weight added so far this pose for a shape (0 when unknown).</summary>
        public float WeightOf(string shape)
        {
            int index = IndexOf(shape);
            return index >= 0 ? _weights[index] : 0f;
        }

        public void Add(string shape, float weight)
        {
            int index = IndexOf(shape);
            if (index >= 0)
            {
                _weights[index] += weight;
            }
        }

        /// <summary>Raises a shape to at least the weight (a blink on top of a track that blinks itself).</summary>
        public void AtLeast(string shape, float weight)
        {
            int index = IndexOf(shape);
            if (index >= 0)
            {
                _weights[index] = Mathf.Max(_weights[index], weight);
            }
        }

        /// <summary>Adds every curve at a moment, scaled by <paramref name="share"/> (a cross-fade between two tracks).</summary>
        public void Add(FaceCurve[] curves, float seconds, float share)
        {
            if (curves == null || share <= 0f)
            {
                return;
            }

            foreach (FaceCurve curve in curves)
            {
                Add(curve.Shape, curve.Evaluate(seconds) * share);
            }
        }

        /// <summary>Writes the summed weights to the meshes and starts the next pose from a neutral face.</summary>
        public void Apply()
        {
            for (int r = 0; r < _renderers.Length; r++)
            {
                int[] map = _indices[r];
                for (int i = 0; i < map.Length; i++)
                {
                    if (map[i] >= 0)
                    {
                        _renderers[r].SetBlendShapeWeight(map[i], Mathf.Clamp01(_weights[i]) * WeightScale);
                    }
                }
            }

            System.Array.Clear(_weights, 0, _weights.Length);
        }

        private int IndexOf(string shape)
        {
            for (int i = 0; i < _shapes.Length; i++)
            {
                if (string.Equals(_shapes[i], shape, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
