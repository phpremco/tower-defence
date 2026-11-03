using Game.Platform;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Lets the player move the camera over the field and zoom in and out, while the viewing angle stays fixed.
    /// The game is played close in, at a zoom where the hero and the creatures can be read (about fifty pixels of
    /// hero on a handheld screen); zooming all the way out still shows the whole field. The camera glides to the
    /// hero whenever it is sent somewhere, and follows the mouse at the screen edge for mouse players. The view
    /// can never leave the field. Uses unscaled time, so it also works while the game is paused or sped up.
    /// </summary>
    public sealed class CameraPanZoom : MonoBehaviour
    {
        /// <summary>Half the world height a play-zoomed screen shows; a 1.5-unit hero is about fifty pixels at 720p.</summary>
        public const float PlayOrthoSize = 7f;
        private const float MinZoom = 0.9f;
        private const float MaxZoom = 4.5f;
        private const float PanSpeedAtFullZoom = 30f;
        private const float ContinuousZoomRate = 1.2f;
        private const float ScrollStep = 0.12f;
        private const float ScrollThreshold = 1.5f;
        private const float DeadZone = 0.1f;
        private const float EdgeMargin = 1.5f;
        private const float EdgePanPixels = 36f;
        private const float EdgePanRecentSeconds = 10f;
        private const float GlideSeconds = 0.9f;

        private FixedCameraRig _rig;
        private Camera _camera;
        private IInputProvider _input;
        private LevelContext _context;
        private Vector2 _halfField;
        private Vector3 _focus;
        private float _zoom;
        private float _playZoom;
        private Vector3 _glideFrom;
        private Vector3 _glideTo;
        private float _glideTime = float.PositiveInfinity;
        private Vector2 _lastPointer;
        private float _pointerMovedAt = float.NegativeInfinity;

        /// <summary>The zoom the level is played at: close enough to read the hero.</summary>
        public float PlayZoom => _playZoom;

        /// <summary>The zoom a level opens on for a fully zoomed-out view of this orthographic size: close enough to see the commander.</summary>
        public static float PlayZoomFor(float fitSize) => Mathf.Clamp(fitSize / PlayOrthoSize, MinZoom, MaxZoom);

        /// <summary>While locked, player input is ignored; a cutscene such as the recon drives the view instead.</summary>
        public bool Locked { get; set; }

        public void Initialize(LevelContext context, FixedCameraRig rig, LevelDefinition level, IInputProvider input)
        {
            _context = context;
            _rig = rig;
            _camera = rig.GetComponent<Camera>();
            _input = input;
            _halfField = level.WorldSize * 0.5f;
            _playZoom = PlayZoomFor(FixedCameraRig.FitSize(level, _camera.aspect));
            // Where the mouse already is does not count as a move, or a mouse parked at the edge would pan the first seconds.
            _lastPointer = input.PointerPosition;
            _zoom = _playZoom;
            _focus = Vector3.zero;
            _context.CameraFocusRequested.Subscribe(GlideTo);
            Apply();
        }

        private void OnDestroy()
        {
            _context?.CameraFocusRequested.Unsubscribe(GlideTo);
        }

        /// <summary>Puts the view somewhere at once, cancelling any glide.</summary>
        public void SetView(Vector3 focus, float zoom)
        {
            _glideTime = float.PositiveInfinity;
            _focus = focus;
            _zoom = zoom;
            Apply();
        }

        /// <summary>Moves the view smoothly to a point on the ground, keeping the zoom; player input cuts it short.</summary>
        public void GlideTo(Vector3 focus)
        {
            _glideFrom = _focus;
            _glideTo = new Vector3(focus.x, 0f, focus.z);
            _glideTime = 0f;
        }

        private void Update()
        {
            if (_rig == null || Locked)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            bool changed = false;

            float zoomInput = _input.CameraZoom;
            if (Mathf.Abs(zoomInput) > ScrollThreshold)
            {
                _zoom *= 1f + Mathf.Sign(zoomInput) * ScrollStep;
                changed = true;
            }
            else if (Mathf.Abs(zoomInput) > DeadZone)
            {
                _zoom *= Mathf.Exp(zoomInput * ContinuousZoomRate * dt);
                changed = true;
            }

            Vector2 pan = _input.CameraPan + EdgePan();
            if (pan.sqrMagnitude > DeadZone * DeadZone)
            {
                // Closer in covers less ground per screen, so panning slows down with the zoom.
                float speed = PanSpeedAtFullZoom / _zoom;
                _focus += new Vector3(pan.x, 0f, pan.y) * (speed * dt);
                _glideTime = float.PositiveInfinity;
                changed = true;
            }

            if (_glideTime < GlideSeconds)
            {
                _glideTime += dt;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_glideTime / GlideSeconds));
                _focus = Vector3.Lerp(_glideFrom, _glideTo, t);
                changed = true;
            }

            if (changed)
            {
                Apply();
            }
        }

        // A mouse at the edge of the screen pans that way, unless it is there to use the HUD. A mouse that has not
        // moved for a while is parked (or left the window), and a parked mouse must never drag the view away.
        private Vector2 EdgePan()
        {
            // Without a mouse over the window the position reads as the exact corner (0, 0); that is no pointer at all.
            Vector2 pointer = _input.PointerPosition;
            if (pointer == Vector2.zero)
            {
                return Vector2.zero;
            }

            if ((pointer - _lastPointer).sqrMagnitude > 0.5f)
            {
                _lastPointer = pointer;
                _pointerMovedAt = Time.unscaledTime;
            }

            if (!Application.isFocused || _context.PointerOverUi || Time.unscaledTime - _pointerMovedAt > EdgePanRecentSeconds)
            {
                return Vector2.zero;
            }

            if (pointer.x <= 0f || pointer.y <= 0f || pointer.x >= Screen.width || pointer.y >= Screen.height)
            {
                return Vector2.zero;
            }

            var edge = Vector2.zero;
            if (pointer.x < EdgePanPixels)
            {
                edge.x = -1f;
            }
            else if (pointer.x > Screen.width - EdgePanPixels)
            {
                edge.x = 1f;
            }

            if (pointer.y < EdgePanPixels)
            {
                edge.y = -1f;
            }
            else if (pointer.y > Screen.height - EdgePanPixels)
            {
                edge.y = 1f;
            }

            return edge;
        }

        private void Apply()
        {
            _zoom = Mathf.Clamp(_zoom, MinZoom, MaxZoom);

            // Keep the focus inside the field; when zoomed out the whole field is in view anyway.
            float x = Mathf.Clamp(_focus.x, -_halfField.x + EdgeMargin, _halfField.x - EdgeMargin);
            float z = Mathf.Clamp(_focus.z, -_halfField.y + EdgeMargin, _halfField.y - EdgeMargin);
            _focus = new Vector3(x, 0f, z);
            _rig.SetView(_focus, _zoom);
        }
    }
}
