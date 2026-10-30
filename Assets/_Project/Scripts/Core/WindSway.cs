using UnityEngine;

namespace Game.Core
{
    /// <summary>Rocks a plant gently around its root, each with its own phase, so a field of growth never stands still. Pure view code.</summary>
    public sealed class WindSway : MonoBehaviour
    {
        private const float CrossSpeedShare = 0.7f;
        private const float CrossAmplitudeShare = 0.6f;

        private Quaternion _rest;
        private float _amplitude;
        private float _speed;
        private float _phase;

        public void Initialize(float amplitudeDegrees, float speed, float phase)
        {
            _rest = transform.localRotation;
            _amplitude = amplitudeDegrees;
            _speed = speed;
            _phase = phase;
        }

        private void Update()
        {
            float t = Time.time * _speed + _phase;
            float lean = Mathf.Sin(t) * _amplitude;
            float cross = Mathf.Sin(t * CrossSpeedShare + _phase) * _amplitude * CrossAmplitudeShare;
            transform.localRotation = Quaternion.Euler(lean, 0f, cross) * _rest;
        }
    }
}
