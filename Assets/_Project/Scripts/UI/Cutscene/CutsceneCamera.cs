using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The perspective camera of a cutscene: takes the sampled pose of the camera move, breathes a little like a
    /// hand-held camera so a held shot never freezes, and shakes on cues with the same feel as the level camera.
    /// </summary>
    public sealed class CutsceneCamera
    {
        private const float BreathAmplitude = 0.035f;
        private const float BreathSpeed = 0.35f;
        private const float BreathSeed = 7.3f;
        private const float ShakeScale = 2.2f;

        private readonly Camera _camera;
        private float _shake;
        private float _shakeSeed;
        private float _clock;

        public CutsceneCamera(Camera camera)
        {
            _camera = camera;
        }

        public Camera Camera => _camera;

        /// <summary>Kicks the camera; one is the hardest. A stronger kick replaces a weaker one still running.</summary>
        public void Shake(float strength)
        {
            if (strength > _shake)
            {
                _shake = Mathf.Min(1f, strength);
                _shakeSeed = Random.Range(0f, 100f);
            }
        }

        /// <summary>
        /// Forgets the running shake and sets the breathing clock to a moment, so a sought frame looks the same
        /// however the cutscene got there.
        /// </summary>
        public void ResetClock(float clock)
        {
            _clock = clock;
            _shake = 0f;
        }

        /// <summary>Places the camera at the sample, plus the breathing and any shake, after <paramref name="deltaTime"/> seconds of cutscene time.</summary>
        public void Apply(CameraSample sample, float deltaTime)
        {
            _clock += deltaTime;
            Vector3 forward = sample.LookAt - sample.Position;
            Quaternion rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : _camera.transform.rotation;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;

            float distance = forward.magnitude;
            Vector2 breath = ShakeCurve.Offset(_clock * BreathSpeed, BreathSeed) / ShakeCurve.Amplitude * (BreathAmplitude * Mathf.Sqrt(Mathf.Max(distance, 1f)));
            Vector3 offset = right * breath.x + up * breath.y;
            if (_shake > 0f)
            {
                _shake = ShakeCurve.Fade(_shake, deltaTime);
                if (_shake < ShakeCurve.Floor)
                {
                    _shake = 0f;
                }

                Vector2 kick = ShakeCurve.Offset(_clock, _shakeSeed) * (_shake * ShakeScale);
                offset += right * kick.x + up * kick.y;
            }

            _camera.transform.SetPositionAndRotation(sample.Position + offset, rotation);
            _camera.fieldOfView = sample.FieldOfView;
        }
    }
}
