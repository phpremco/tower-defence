using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Round 48: a weighted average of headings taken over the moments around now (a window, see
    /// <see cref="CutsceneMath.WindowAt"/>), by angle rather than by adding the directions: two headings that point opposite
    /// ways (a builder that faced its module turning to face the base) average to a turn half way round instead of cancelling
    /// out and flipping in a frame. Each heading is unwrapped against the one before it, so the turn goes the short way between
    /// neighbours; feed the samples in their order in time.
    /// </summary>
    public struct HeadingBlend
    {
        private float _sum;
        private float _weight;
        private float _last;
        private bool _started;

        /// <summary>True once a heading with a weight came in.</summary>
        public bool HasHeading => _weight > 0f;

        /// <summary>The average heading, degrees of yaw (0 along +z); only valid with <see cref="HasHeading"/>.</summary>
        public float Yaw => _weight > 0f ? _sum / _weight : 0f;

        /// <summary>
        /// Adds a heading (its flat part), weighted by <paramref name="weight"/> times its length, so a firm heading (a fast flight,
        /// a hold) counts more than a slow drift. A heading without length or weight is left out.
        /// </summary>
        public void Add(Vector3 direction, float weight)
        {
            var flat = new Vector3(direction.x, 0f, direction.z);
            float strength = flat.magnitude * weight;
            if (strength <= 0f)
            {
                return;
            }

            float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            float unwrapped = _started ? _last + Mathf.DeltaAngle(_last, yaw) : yaw;
            _last = unwrapped;
            _started = true;
            _sum += unwrapped * strength;
            _weight += strength;
        }

        /// <summary>
        /// Round 57: adds a yaw (degrees) that is already unwrapped along its own track, as is: where the way round between two
        /// neighbouring headings was settled once for the whole flight (a builder's legs), the average can never take the other
        /// way round from one frame to the next. Do not mix with <see cref="Add"/> in one blend.
        /// </summary>
        public void AddYaw(float yaw, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            _last = yaw;
            _started = true;
            _sum += yaw * weight;
            _weight += weight;
        }
    }
}
