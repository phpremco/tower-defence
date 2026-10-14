using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Platform
{
    /// <summary>IInputProvider backed by the Input System action asset. Action names are defined in that asset.</summary>
    public sealed class InputSystemProvider : IInputProvider
    {
        private const string MapName = "Gameplay";
        private const string MoveAction = "Move";
        private const string PointAction = "Point";
        private const string ConfirmAction = "Confirm";
        private const string CancelAction = "Cancel";
        private const string PauseAction = "Pause";
        private const string SpeedAction = "Speed";

        private readonly InputActionMap _map;
        private readonly InputAction _move;
        private readonly InputAction _point;
        private readonly InputAction _confirm;
        private readonly InputAction _cancel;
        private readonly InputAction _pause;
        private readonly InputAction _speed;

        public InputSystemProvider(InputActionAsset asset)
        {
            _map = asset.FindActionMap(MapName, true);
            _move = _map.FindAction(MoveAction, true);
            _point = _map.FindAction(PointAction, true);
            _confirm = _map.FindAction(ConfirmAction, true);
            _cancel = _map.FindAction(CancelAction, true);
            _pause = _map.FindAction(PauseAction, true);
            _speed = _map.FindAction(SpeedAction, true);

            _confirm.performed += OnConfirm;
            _cancel.performed += OnCancel;
            _pause.performed += OnPause;
            _speed.performed += OnSpeed;
            _map.Enable();
        }

        public Vector2 CursorMove => _move.ReadValue<Vector2>();

        public Vector2 PointerPosition => _point.ReadValue<Vector2>();

        public event Action Confirmed;
        public event Action Cancelled;
        public event Action PauseToggled;
        public event Action SpeedToggled;

        public void Dispose()
        {
            _confirm.performed -= OnConfirm;
            _cancel.performed -= OnCancel;
            _pause.performed -= OnPause;
            _speed.performed -= OnSpeed;
            _map.Disable();
        }

        private void OnConfirm(InputAction.CallbackContext _) => Confirmed?.Invoke();

        private void OnCancel(InputAction.CallbackContext _) => Cancelled?.Invoke();

        private void OnPause(InputAction.CallbackContext _) => PauseToggled?.Invoke();

        private void OnSpeed(InputAction.CallbackContext _) => SpeedToggled?.Invoke();
    }
}
