using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The local pose of every node under a root, kept so a later pose can blend from it. A rig posed by sampling clips
    /// (a cutscene's, which must follow from the shot time alone) cannot use the clip player's cross-fades: it samples the
    /// pose it leaves, holds it here, samples the pose it goes to and blends between the two, every frame anew, so a
    /// sought moment lands on the same blend as a played one. Nothing allocates after construction.
    /// </summary>
    public sealed class PoseSnapshot
    {
        private readonly Transform[] _nodes;
        private readonly Vector3[] _positions;
        private readonly Quaternion[] _rotations;
        private readonly Vector3[] _scales;

        /// <param name="root">The root whose nodes are kept (the root itself included); the hierarchy is read once, here.</param>
        public PoseSnapshot(Transform root)
        {
            _nodes = root.GetComponentsInChildren<Transform>(true);
            _positions = new Vector3[_nodes.Length];
            _rotations = new Quaternion[_nodes.Length];
            _scales = new Vector3[_nodes.Length];
        }

        /// <summary>Keeps the pose every node shows now.</summary>
        public void Capture()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                Transform node = _nodes[i];
                if (node == null)
                {
                    continue;
                }

                _positions[i] = node.localPosition;
                _rotations[i] = node.localRotation;
                _scales[i] = node.localScale;
            }
        }

        /// <summary>
        /// Moves every node from the kept pose towards the one it shows now: <paramref name="share"/> 0 is the kept pose, 1
        /// leaves the current one as it is.
        /// </summary>
        public void BlendFromCaptured(float share)
        {
            float weight = Mathf.Clamp01(share);
            if (weight >= 1f)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                Transform node = _nodes[i];
                if (node == null)
                {
                    continue;
                }

                node.localPosition = Vector3.Lerp(_positions[i], node.localPosition, weight);
                node.localRotation = Quaternion.Slerp(_rotations[i], node.localRotation, weight);
                node.localScale = Vector3.Lerp(_scales[i], node.localScale, weight);
            }
        }
    }
}
