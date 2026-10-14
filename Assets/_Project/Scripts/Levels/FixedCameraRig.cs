using UnityEngine;

namespace Game.Levels
{
    /// <summary>Places the orthographic camera at a fixed angle above the middle of the field.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FixedCameraRig : MonoBehaviour
    {
        private const float DistanceFromCenter = 50f;

        private Camera _camera;

        public void Apply(LevelDefinition level)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            CameraSettings settings = level.Camera;
            _camera.orthographic = true;
            _camera.orthographicSize = settings.OrthographicSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = level.Theme.BackgroundColor;

            Quaternion rotation = Quaternion.Euler(settings.PitchDegrees, settings.YawDegrees, 0f);
            transform.SetPositionAndRotation(rotation * (Vector3.back * DistanceFromCenter), rotation);
        }
    }
}
