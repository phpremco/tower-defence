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
        private const string NextTowerAction = "NextTower";
        private const string PreviousTowerAction = "PreviousTower";
        private const string SkipWaveAction = "SkipWave";
        private const string UpgradeAction = "Upgrade";
        private const string RepairAction = "Repair";
        private const string SellAction = "Sell";

        private readonly InputActionMap _map;
        private readonly InputAction _move;
        private readonly InputAction _point;
        private readonly InputAction _confirm;
        private readonly InputAction _cancel;
        private readonly InputAction _pause;
        private readonly InputAction _speed;
        private readonly InputAction _nextTower;
        private readonly InputAction _previousTower;
        private readonly InputAction _skipWave;
        private readonly InputAction _upgrade;
        private readonly InputAction _repair;
        private readonly InputAction _sell;

        public InputSystemProvider(InputActionAsset asset)
        {
            _map = asset.FindActionMap(MapName, true);
            _move = _map.FindAction(MoveAction, true);
            _point = _map.FindAction(PointAction, true);
            _confirm = _map.FindAction(ConfirmAction, true);
            _cancel = _map.FindAction(CancelAction, true);
            _pause = _map.FindAction(PauseAction, true);
            _speed = _map.FindAction(SpeedAction, true);
            _nextTower = _map.FindAction(NextTowerAction, true);
            _previousTower = _map.FindAction(PreviousTowerAction, true);
            _skipWave = _map.FindAction(SkipWaveAction, true);
            _upgrade = _map.FindAction(UpgradeAction, true);
            _repair = _map.FindAction(RepairAction, true);
            _sell = _map.FindAction(SellAction, true);

            _confirm.performed += OnConfirm;
            _cancel.performed += OnCancel;
            _pause.performed += OnPause;
            _speed.performed += OnSpeed;
            _nextTower.performed += OnNextTower;
            _previousTower.performed += OnPreviousTower;
            _skipWave.performed += OnSkipWave;
            _upgrade.performed += OnUpgrade;
            _repair.performed += OnRepair;
            _sell.performed += OnSell;
            _map.Enable();
        }

        public Vector2 CursorMove => _move.ReadValue<Vector2>();

        public Vector2 PointerPosition => _point.ReadValue<Vector2>();

        public event Action Confirmed;
        public event Action Cancelled;
        public event Action PauseToggled;
        public event Action SpeedToggled;
        public event Action NextTower;
        public event Action PreviousTower;
        public event Action SkipWave;
        public event Action UpgradePressed;
        public event Action RepairPressed;
        public event Action SellPressed;

        public void Dispose()
        {
            _confirm.performed -= OnConfirm;
            _cancel.performed -= OnCancel;
            _pause.performed -= OnPause;
            _speed.performed -= OnSpeed;
            _nextTower.performed -= OnNextTower;
            _previousTower.performed -= OnPreviousTower;
            _skipWave.performed -= OnSkipWave;
            _upgrade.performed -= OnUpgrade;
            _repair.performed -= OnRepair;
            _sell.performed -= OnSell;
            _map.Disable();
        }

        private void OnConfirm(InputAction.CallbackContext _) => Confirmed?.Invoke();

        private void OnCancel(InputAction.CallbackContext _) => Cancelled?.Invoke();

        private void OnPause(InputAction.CallbackContext _) => PauseToggled?.Invoke();

        private void OnSpeed(InputAction.CallbackContext _) => SpeedToggled?.Invoke();

        private void OnNextTower(InputAction.CallbackContext _) => NextTower?.Invoke();

        private void OnPreviousTower(InputAction.CallbackContext _) => PreviousTower?.Invoke();

        private void OnSkipWave(InputAction.CallbackContext _) => SkipWave?.Invoke();

        private void OnUpgrade(InputAction.CallbackContext _) => UpgradePressed?.Invoke();

        private void OnRepair(InputAction.CallbackContext _) => RepairPressed?.Invoke();

        private void OnSell(InputAction.CallbackContext _) => SellPressed?.Invoke();
    }
}


