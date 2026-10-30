using UnityEngine;

namespace Game.UI
{
    /// <summary>Lets the menu camera breathe: it circles a few degrees around its focus and rises and sinks a little, never still, never dizzying.</summary>
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        private const float YawSwingDegrees = 4.5f;
        private const float YawSeconds = 38f;
        private const float PitchSwingDegrees = 1.4f;
        private const float PitchSeconds = 27f;
        private const float BobHeight = 0.18f;
        private const float BobSeconds = 19f;

        private Vector3 _focus;
        private float _distance;
        private float _pitch;
        private float _yaw;

        public void Initialize(Vector3 focus, float distance, float pitchDegrees, float yawDegrees)
        {
            _focus = focus;
            _distance = distance;
            _pitch = pitchDegrees;
            _yaw = yawDegrees;
            Apply(0f);
        }

        private void Update() => Apply(Time.unscaledTime);

        private void Apply(float time)
        {
            float yaw = _yaw + Mathf.Sin(time / YawSeconds * Mathf.PI * 2f) * YawSwingDegrees;
            float pitch = _pitch + Mathf.Sin(time / PitchSeconds * Mathf.PI * 2f) * PitchSwingDegrees;
            float bob = Mathf.Sin(time / BobSeconds * Mathf.PI * 2f) * BobHeight;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = _focus - rotation * Vector3.forward * _distance + Vector3.up * bob;
        }
    }
}
