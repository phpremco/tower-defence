using Game.Platform;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Lets the player move the camera over the field and zoom in and out, while the viewing angle stays fixed.
    /// The view can never leave the field. Uses unscaled time, so it also works while the game is paused or sped up.
    /// </summary>
    public sealed class CameraPanZoom : MonoBehaviour
    {
        private const float StartZoom = 1.25f;
        private const float MinZoom = 0.9f;
        private const float MaxZoom = 3f;
        private const float PanSpeedAtFullZoom = 9f;
        private const float ContinuousZoomRate = 1.2f;
        private const float ScrollStep = 0.12f;
        private const float ScrollThreshold = 1.5f;
        private const float DeadZone = 0.1f;
        private const float EdgeMargin = 1.5f;

        private FixedCameraRig _rig;
        private IInputProvider _input;
        private Vector2 _halfField;
        private Vector3 _focus;
        private float _zoom;

        public void Initialize(FixedCameraRig rig, LevelDefinition level, IInputProvider input)
        {
            _rig = rig;
            _input = input;
            _halfField = level.WorldSize * 0.5f;
            _zoom = StartZoom;
            _focus = Vector3.zero;
            Apply();
        }

        private void Update()
        {
            if (_rig == null)
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

            Vector2 pan = _input.CameraPan;
            if (pan.sqrMagnitude > DeadZone * DeadZone)
            {
                // Closer in covers less ground per screen, so panning slows down with the zoom.
                float speed = PanSpeedAtFullZoom / _zoom;
                _focus += new Vector3(pan.x, 0f, pan.y) * (speed * dt);
                changed = true;
            }

            if (changed)
            {
                Apply();
            }
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
