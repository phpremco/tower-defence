using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Places the orthographic camera at a fixed angle above the middle of the field and zooms so the whole field
    /// fills the screen, whatever the window shape. Re-fits when the aspect ratio changes. Heavy hits can shake it:
    /// the shake is an offset on top of the fixed view that dies out on its own.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FixedCameraRig : MonoBehaviour
    {
        private const float DistanceFromCenter = 50f;

        /// <summary>Height of towers and enemies above the ground that must stay inside the view.</summary>
        private const float ObjectHeight = 2.4f;

        /// <summary>Share of the screen height taken by the top and bottom HUD bars.</summary>
        private const float HudReserve = 0.2f;

        private const float ShakeAmplitude = 0.18f;
        private const float ShakeDecay = 5.5f;
        private const float ShakeFrequency = 26f;
        private const float ShakeFloor = 0.01f;

        private Camera _camera;
        private LevelDefinition _level;
        private float _fittedAspect;
        private float _fitSize;
        private float _zoom = 1f;
        private Vector3 _focus;
        private Vector3 _basePosition;
        private float _shake;
        private float _shakeSeed;

        /// <summary>Zoom factor: one shows the whole field, larger values come closer.</summary>
        public float Zoom => _zoom;

        /// <summary>The point on the ground the camera looks at.</summary>
        public Vector3 Focus => _focus;

        /// <summary>Moves and zooms the view. The angle stays fixed.</summary>
        public void SetView(Vector3 focus, float zoom)
        {
            _focus = new Vector3(focus.x, 0f, focus.z);
            _zoom = zoom;
            ApplyView();
        }

        /// <summary>Kicks the camera; strength one is the hardest hit. A stronger kick replaces a weaker one still running.</summary>
        public void Shake(float strength)
        {
            if (strength > _shake)
            {
                _shake = Mathf.Min(1f, strength);
                _shakeSeed = Random.Range(0f, 100f);
            }
        }

        public void Apply(LevelDefinition level)
        {
            _level = level;
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = level.Theme.BackgroundColor;
            Fit();
        }

        private void Update()
        {
            if (_level != null && !Mathf.Approximately(_camera.aspect, _fittedAspect))
            {
                Fit();
            }
        }

        private void LateUpdate()
        {
            if (_shake <= 0f)
            {
                return;
            }

            _shake *= Mathf.Exp(-ShakeDecay * Time.deltaTime);
            if (_shake < ShakeFloor)
            {
                _shake = 0f;
                transform.position = _basePosition;
                return;
            }

            float t = Time.time * ShakeFrequency;
            float x = Mathf.PerlinNoise(t, _shakeSeed) * 2f - 1f;
            float y = Mathf.PerlinNoise(_shakeSeed, t) * 2f - 1f;
            transform.position = _basePosition + (transform.right * x + transform.up * y) * (ShakeAmplitude * _shake);
        }

        private void Fit()
        {
            CameraSettings settings = _level.Camera;
            Vector2 world = _level.WorldSize;
            float pitch = settings.PitchDegrees * Mathf.Deg2Rad;

            float halfHeight = (world.y * Mathf.Sin(pitch) + ObjectHeight * Mathf.Cos(pitch)) * 0.5f / (1f - HudReserve);
            float halfWidth = world.x * 0.5f / _camera.aspect;
            _fitSize = Mathf.Max(halfHeight, halfWidth) * (1f + settings.FitMargin);
            _fittedAspect = _camera.aspect;
            ApplyView();
        }

        private void ApplyView()
        {
            CameraSettings settings = _level.Camera;
            Quaternion rotation = Quaternion.Euler(settings.PitchDegrees, settings.YawDegrees, 0f);
            _basePosition = rotation * (Vector3.back * DistanceFromCenter) + _focus;
            transform.SetPositionAndRotation(_basePosition, rotation);
            _camera.orthographicSize = _fitSize / _zoom;
        }
    }
}
