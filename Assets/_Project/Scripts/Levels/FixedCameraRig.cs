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
        public const float DistanceFromCenter = 50f;

        /// <summary>Height of towers and enemies above the ground that must stay inside the view.</summary>
        private const float ObjectHeight = 2.4f;

        /// <summary>Share of the screen height taken by the top and bottom HUD bars.</summary>
        private const float HudReserve = 0.2f;

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

            _shake = ShakeCurve.Fade(_shake, Time.deltaTime);
            if (_shake < ShakeCurve.Floor)
            {
                _shake = 0f;
                transform.position = _basePosition;
                return;
            }

            Vector2 offset = ShakeCurve.Offset(Time.time, _shakeSeed) * _shake;
            transform.position = _basePosition + transform.right * offset.x + transform.up * offset.y;
        }

        private void Fit()
        {
            _fitSize = FitSize(_level, _camera.aspect);
            _fittedAspect = _camera.aspect;
            ApplyView();
        }

        /// <summary>
        /// Half the ground area the fully zoomed-out view shows, as half extents along x and z around the field's
        /// centre, so the landscape can be built out far enough that no screen corner shows the void behind it.
        /// </summary>
        public static Vector2 GroundFootprint(LevelDefinition level, float aspect) => GroundFootprintFor(level, FitSize(level, aspect), aspect);

        /// <summary>Half extents along x and z of the ground a view with this orthographic size covers, around its focus.</summary>
        public static Vector2 GroundFootprintFor(LevelDefinition level, float size, float aspect)
        {
            CameraSettings settings = level.Camera;
            Quaternion rotation = Quaternion.Euler(settings.PitchDegrees, settings.YawDegrees, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            // A screen step up lands on the ground further along the view direction than its own length.
            Vector3 upOnGround = up - forward * (up.y / forward.y);
            float halfWidth = size * aspect;
            return new Vector2(
                Mathf.Abs(right.x) * halfWidth + Mathf.Abs(upOnGround.x) * size,
                Mathf.Abs(right.z) * halfWidth + Mathf.Abs(upOnGround.z) * size);
        }

        // The whole field must fit the view whatever the camera's pitch and yaw: the ground rectangle (with room for
        // what stands on it) is projected onto the camera's screen axes and the larger half extent sets the size.
        public static float FitSize(LevelDefinition level, float aspect)
        {
            CameraSettings settings = level.Camera;
            Vector2 world = level.WorldSize;
            Quaternion rotation = Quaternion.Euler(settings.PitchDegrees, settings.YawDegrees, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;

            float halfWidth = 0f;
            float halfHeight = 0f;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector3((corner % 2 == 0 ? -0.5f : 0.5f) * world.x, 0f, (corner < 2 ? -0.5f : 0.5f) * world.y);
                for (int lift = 0; lift < 2; lift++)
                {
                    Vector3 p = point + Vector3.up * (lift * ObjectHeight);
                    halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(p, right)));
                    halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(p, up)));
                }
            }

            halfHeight /= 1f - HudReserve;
            return Mathf.Max(halfHeight, halfWidth / aspect) * (1f + settings.FitMargin);
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
