using Game.Core;
using Game.Platform;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Lets the player move the camera over the field and zoom in and out, while the viewing angle stays fixed.
    /// The game is played close in, at a zoom where the hero and the creatures can be read (about fifty pixels of
    /// hero on a handheld screen); zooming all the way out still shows the whole field. The camera glides to the
    /// hero whenever it is sent somewhere, and follows the mouse at the screen edge for mouse players. The view
    /// can never leave the field. Holding the overview button eases out to the whole field and back (OverviewFraming).
    /// Uses unscaled time, so it also works while the game is paused or sped up.
    /// </summary>
    public sealed class CameraPanZoom : MonoBehaviour
    {
        /// <summary>Half the world height a play-zoomed screen shows; a 1.5-unit hero is about fifty pixels at 720p.</summary>
        public const float PlayOrthoSize = 7f;
        private const float MinZoom = 0.9f;
        private const float MaxZoom = 4.5f;
        private const float ContinuousZoomRate = 1.2f;
        private const float ScrollStep = 0.12f;
        private const float ScrollThreshold = 1.5f;
        private const float DeadZone = 0.1f;
        private const float EdgeMargin = 1.5f;
        private const float EdgePanPixels = 36f;
        private const float EdgePanRecentSeconds = 10f;
        private const float GlideSeconds = 0.9f;
        // The overview aim moves across the whole field in this many seconds with the stick or the pan keys.
        private const float AimCrossSeconds = 2.5f;
        private const float ReticleRadius = 1.6f;
        private const float PointerMovedPixels = 2f;
        private static readonly Color ReticleAim = new Color(0.35f, 1f, 0.9f, 1f) * 0.9f;
        private static readonly Color ReticlePicked = new Color(0.45f, 1f, 0.95f, 1f) * 2.2f;

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
        private float _fieldSpan;
        private float _panHeld;
        private CameraControls _controls;
        private bool _overviewing;
        private float _overviewShare;
        private Vector3 _returnFocus;
        private float _returnZoom;
        private Vector3 _aim;
        private bool _picked;
        private Vector2 _aimPointer;
        private Transform _reticle;
        private Material _reticleMaterial;

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
            _fieldSpan = Mathf.Max(level.WorldSize.x, level.WorldSize.y);
            // A tool scene without the boot services pans at the default speed with edge scrolling on.
            _controls = Services.TryGet(out CameraControls controls) ? controls : new CameraControls();
            _playZoom = PlayZoomFor(FixedCameraRig.FitSize(level, _camera.aspect));
            // Where the mouse already is does not count as a move, or a mouse parked at the edge would pan the first seconds.
            _lastPointer = input.PointerPosition;
            _zoom = _playZoom;
            _focus = Vector3.zero;
            _context.CameraFocusRequested.Subscribe(GlideTo);
            _input.Confirmed += OnOverviewConfirmed;
            Apply();
        }

        private void OnDestroy()
        {
            _context?.CameraFocusRequested.Unsubscribe(GlideTo);
            if (_input != null)
            {
                _input.Confirmed -= OnOverviewConfirmed;
            }

            if (_reticle != null)
            {
                Destroy(_reticle.gameObject);
                Destroy(_reticleMaterial);
            }
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
            if (UpdateOverview(dt))
            {
                return;
            }

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

            Vector2 pan = Vector2.ClampMagnitude(_input.CameraPan + EdgePan(), 1f);
            if (pan.sqrMagnitude > DeadZone * DeadZone)
            {
                // Measured in screens, so a pan feels the same at every zoom; holding it speeds up across big maps.
                _panHeld += dt;
                float speed = PanSpeedCurve.Speed(_camera.orthographicSize * 2f, _fieldSpan, _panHeld, _controls.SpeedScale);
                _focus += new Vector3(pan.x, 0f, pan.y) * (speed * dt);
                _glideTime = float.PositiveInfinity;
                changed = true;
            }
            else
            {
                _panHeld = 0f;
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

        /// <summary>True while the overview is held or the camera is still easing between it and the play view.</summary>
        public bool InOverview => _overviewing || _overviewShare > 0f;

        // Holding the overview button eases the camera out to the whole field; letting go eases it back to where it was, or
        // to the spot picked meanwhile (a click, or A on the aim the stick or pan keys move). The clock may run or stand
        // still: the camera uses unscaled time either way. Returns true while the overview owns the camera.
        private bool UpdateOverview(float dt)
        {
            bool want = _input.OverviewHeld && !_context.InputBlocked && _context.Outcome == LevelOutcome.Running;
            if (want && !_overviewing)
            {
                BeginOverview();
            }
            else if (!want && _overviewing)
            {
                EndOverview();
            }

            if (!InOverview)
            {
                return false;
            }

            _overviewShare = Mathf.MoveTowards(_overviewShare, _overviewing ? 1f : 0f, dt / OverviewFraming.BlendSeconds);
            if (_overviewing)
            {
                MoveAim(dt);
            }

            OverviewFraming.Blend(_returnFocus, _returnZoom, _overviewShare, out Vector3 focus, out float zoom);
            _rig.SetView(focus, zoom);
            if (_overviewShare <= 0f)
            {
                // Back in the play view: the camera carries on from the spot it returned to.
                _focus = _returnFocus;
                _zoom = _returnZoom;
                _context.OverviewActive = false;
                Apply();
            }

            return true;
        }

        private void BeginOverview()
        {
            _overviewing = true;
            _glideTime = float.PositiveInfinity;
            if (_overviewShare <= 0f)
            {
                _returnFocus = _focus;
                _returnZoom = _zoom;
            }

            _aim = _returnFocus;
            _picked = false;
            _aimPointer = _input.PointerPosition;
            _context.OverviewActive = true;
            ShowReticle(true);
        }

        private void EndOverview()
        {
            _overviewing = false;
            if (_picked)
            {
                _returnFocus = _aim;
            }

            ShowReticle(false);
        }

        // The aim follows the mouse where it points on the ground, or moves with the stick and the pan keys.
        private void MoveAim(float dt)
        {
            if (!_picked)
            {
                Vector2 pointer = _input.PointerPosition;
                Vector2 stick = Vector2.ClampMagnitude(_input.CursorMove + _input.CameraPan, 1f);
                if (stick.sqrMagnitude > DeadZone * DeadZone)
                {
                    _aim += new Vector3(stick.x, 0f, stick.y) * (_fieldSpan / AimCrossSeconds * dt);
                }
                else if (pointer != Vector2.zero && (pointer - _aimPointer).sqrMagnitude > PointerMovedPixels * PointerMovedPixels
                    && OverviewFraming.GroundPoint(_camera.ScreenPointToRay(pointer), out Vector3 ground))
                {
                    _aimPointer = pointer;
                    _aim = ground;
                }

                _aim = OverviewFraming.KeepOnField(_aim, _halfField, EdgeMargin);
            }

            if (_reticle != null)
            {
                _reticle.position = _aim;
            }
        }

        // A click (not on the HUD) or A while the overview is held fixes the spot the camera goes to on release.
        private void OnOverviewConfirmed()
        {
            if (!_overviewing || (_input.FromPointer && _context.PointerOverUi))
            {
                return;
            }

            if (_input.FromPointer && OverviewFraming.GroundPoint(_camera.ScreenPointToRay(_input.PointerPosition), out Vector3 ground))
            {
                _aim = OverviewFraming.KeepOnField(ground, _halfField, EdgeMargin);
            }

            _picked = true;
            GlowDecal.SetColour(_reticleMaterial, ReticlePicked);
            _reticle.position = _aim;
        }

        // A soft pool of light on the ground where the camera will go; built on first use and reused.
        private void ShowReticle(bool shown)
        {
            if (_reticle == null)
            {
                _reticle = new GameObject("OverviewAim").transform;
                GameObject pool = GlowDecal.PlacePool(_reticle, ReticleAim, ReticleRadius);
                _reticleMaterial = new Material(pool.GetComponent<Renderer>().sharedMaterial);
                pool.GetComponent<Renderer>().sharedMaterial = _reticleMaterial;
            }

            GlowDecal.SetColour(_reticleMaterial, ReticleAim);
            _reticle.position = _aim;
            _reticle.gameObject.SetActive(shown);
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

            if (!_controls.EdgeScrolling || !Application.isFocused || _context.PointerOverUi || Time.unscaledTime - _pointerMovedAt > EdgePanRecentSeconds)
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
