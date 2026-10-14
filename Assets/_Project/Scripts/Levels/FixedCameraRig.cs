using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Places the orthographic camera at a fixed angle above the middle of the field and zooms so the whole field
    /// fills the screen, whatever the window shape. Re-fits when the aspect ratio changes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FixedCameraRig : MonoBehaviour
    {
        private const float DistanceFromCenter = 50f;

        /// <summary>Height of towers and enemies above the ground that must stay inside the view.</summary>
        private const float ObjectHeight = 2.4f;

        /// <summary>Share of the screen height taken by the top and bottom HUD bars.</summary>
        private const float HudReserve = 0.2f;

        private Camera _camera;
        private LevelDefinition _level;
        private float _fittedAspect;

        public void Apply(LevelDefinition level)
        {
            _level = level;
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = level.Theme.BackgroundColor;

            CameraSettings settings = level.Camera;
            Quaternion rotation = Quaternion.Euler(settings.PitchDegrees, settings.YawDegrees, 0f);
            transform.SetPositionAndRotation(rotation * (Vector3.back * DistanceFromCenter), rotation);
            Fit();
        }

        private void Update()
        {
            if (_level != null && !Mathf.Approximately(_camera.aspect, _fittedAspect))
            {
                Fit();
            }
        }

        private void Fit()
        {
            CameraSettings settings = _level.Camera;
            Vector2 world = _level.WorldSize;
            float pitch = settings.PitchDegrees * Mathf.Deg2Rad;

            float halfHeight = (world.y * Mathf.Sin(pitch) + ObjectHeight * Mathf.Cos(pitch)) * 0.5f / (1f - HudReserve);
            float halfWidth = world.x * 0.5f / _camera.aspect;
            _camera.orthographicSize = Mathf.Max(halfHeight, halfWidth) * (1f + settings.FitMargin);
            _fittedAspect = _camera.aspect;
        }
    }
}
