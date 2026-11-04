using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The perspective camera of a cutscene: takes the sampled pose of the camera move, drifts very slowly like a
    /// camera on a soft mount (the cutscene's data sets how far, zero holds it still), and gives a short kick on an
    /// impact cue. A kick never lasts: it dies out in well under a second, and its strength is capped by the data.
    /// </summary>
    public sealed class CutsceneCamera
    {
        private const float DriftSeedX = 7.3f;
        private const float DriftSeedY = 19.1f;

        private readonly Camera _camera;
        private readonly CutsceneCameraFeel _feel;
        private float _shake;
        private float _shakeSeed;
        private float _clock;

        public CutsceneCamera(Camera camera, CutsceneCameraFeel feel)
        {
            _camera = camera;
            _feel = feel;
        }

        public Camera Camera => _camera;

        /// <summary>Kicks the camera; one is the hardest the data allows. A stronger kick replaces a weaker one still running.</summary>
        public void Shake(float strength)
        {
            float capped = Mathf.Min(strength, _feel.MaxKick);
            if (capped > _shake)
            {
                _shake = capped;
                _shakeSeed = Random.Range(0f, 100f);
            }
        }

        /// <summary>
        /// Forgets the running kick and sets the drift clock to a moment, so a sought frame looks the same however
        /// the cutscene got there.
        /// </summary>
        public void ResetClock(float clock)
        {
            _clock = clock;
            _shake = 0f;
        }

        /// <summary>Places the camera at the sample, plus the drift and any kick, after <paramref name="deltaTime"/> seconds of cutscene time.</summary>
        public void Apply(CameraSample sample, float deltaTime)
        {
            _clock += deltaTime;
            Vector3 forward = sample.LookAt - sample.Position;
            Quaternion rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : _camera.transform.rotation;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;

            Vector3 offset = Vector3.zero;
            if (_feel.Drift > 0f)
            {
                // One slow wander well under a cycle per second: felt as a living camera, never seen as a shake.
                float distance = Mathf.Sqrt(Mathf.Max(forward.magnitude, 1f));
                float t = _clock * _feel.DriftHertz;
                offset += right * ((Mathf.PerlinNoise(t, DriftSeedX) * 2f - 1f) * _feel.Drift * distance);
                offset += up * ((Mathf.PerlinNoise(DriftSeedY, t) * 2f - 1f) * _feel.Drift * distance);
            }

            if (_shake > 0f)
            {
                _shake = ShakeCurve.Fade(_shake, deltaTime);
                if (_shake < ShakeCurve.Floor)
                {
                    _shake = 0f;
                }

                Vector2 kick = ShakeCurve.Offset(_clock, _shakeSeed) * (_shake * _feel.KickScale);
                offset += right * kick.x + up * kick.y;
            }

            _camera.transform.SetPositionAndRotation(sample.Position + offset, rotation);
            _camera.fieldOfView = sample.FieldOfView;
        }
    }
}
