using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Lets a beam linger after its shot has landed: the line keeps its last shape and thins out to nothing over
    /// a few frames, then switches itself off. It lives on the beam object, which is not a child of the pooled
    /// projectile, so the fade keeps running after the projectile goes back to the pool.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BeamFade : MonoBehaviour
    {
        private LineRenderer _line;
        private float _width;
        private float _seconds;
        private float _age = float.PositiveInfinity;

        private void Awake() => _line = GetComponent<LineRenderer>();

        /// <summary>Shows the beam at full width and stops any fade in progress.</summary>
        public void Show(float width)
        {
            _width = width;
            _age = float.PositiveInfinity;
            _line.widthMultiplier = width;
            _line.enabled = true;
        }

        /// <summary>Starts thinning the beam out from its current shape.</summary>
        public void Begin(float seconds)
        {
            _seconds = seconds;
            _age = 0f;
        }

        public void Hide()
        {
            _age = float.PositiveInfinity;
            _line.enabled = false;
        }

        private void Update()
        {
            if (_age >= _seconds)
            {
                return;
            }

            // At x2 or x3 a fade in game time was gone before the eye caught it: the laser looked broken.
            _age += FeedbackClock.DeltaTime;
            float t = Mathf.Clamp01(_age / _seconds);
            _line.widthMultiplier = _width * (1f - t);
            if (t >= 1f)
            {
                _line.enabled = false;
            }
        }
    }
}
