using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One blend shape of a face over time: keys of (seconds, weight 0..1), eased between them (smoothstep), held flat
    /// before the first and after the last. The Blender exports keep the face tracks out of the FBX and list them in the
    /// report, so the code drives the shapes (after any sampled or played clip has written its own zero curves).
    /// </summary>
    [System.Serializable]
    public sealed class FaceCurve
    {
        [Tooltip("The blend shape's name on the face meshes (Fear, Surprise, Determined, Breath, LookL, LookR, Blink).")]
        [SerializeField] private string _shape = string.Empty;

        [Tooltip("x: seconds into the clip; y: the shape's weight 0..1. In time order.")]
        [SerializeField] private Vector2[] _keys = System.Array.Empty<Vector2>();

        public FaceCurve()
        {
        }

        public FaceCurve(string shape, params Vector2[] keys)
        {
            _shape = shape;
            _keys = keys;
        }

        public string Shape => _shape;

        public Vector2[] Keys => _keys ?? System.Array.Empty<Vector2>();

        /// <summary>The weight 0..1 at a moment: eased between the keys around it, the nearest key's outside them, zero without keys.</summary>
        public float Evaluate(float seconds)
        {
            Vector2[] keys = Keys;
            if (keys.Length == 0)
            {
                return 0f;
            }

            if (seconds <= keys[0].x)
            {
                return keys[0].y;
            }

            for (int i = 1; i < keys.Length; i++)
            {
                if (seconds <= keys[i].x)
                {
                    float span = keys[i].x - keys[i - 1].x;
                    float t = span > 0f ? (seconds - keys[i - 1].x) / span : 1f;
                    return Mathf.Lerp(keys[i - 1].y, keys[i].y, t * t * (3f - 2f * t));
                }
            }

            return keys[keys.Length - 1].y;
        }

        /// <summary>True when the keys are in time order and every weight lies in 0..1 (for OnValidate).</summary>
        public bool IsValid()
        {
            Vector2[] keys = Keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].y < 0f || keys[i].y > 1f || (i > 0 && keys[i].x < keys[i - 1].x))
                {
                    return false;
                }
            }

            return !string.IsNullOrEmpty(_shape);
        }
    }
}
