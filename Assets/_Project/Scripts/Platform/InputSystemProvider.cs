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
        private const string PanAction = "Pan";
        private const string ZoomAction = "Zoom";
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
        private const string HeroPostAction = "HeroPost";
        private const string ShockwaveAction = "Shockwave";
        private const string EmergencyRepairAction = "EmergencyRepair";

        private readonly InputActionMap _map;
        private readonly InputAction _heroPost;
        private readonly InputAction _shockwave;
        private readonly InputAction _emergencyRepair;
        private readonly InputAction _move;
        private readonly InputAction _pan;
        private readonly InputAction _zoom;
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
            _pan = _map.FindAction(PanAction, true);
            _zoom = _map.FindAction(ZoomAction, true);
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
            _heroPost = _map.FindAction(HeroPostAction, true);
            _shockwave = _map.FindAction(ShockwaveAction, true);
            _emergencyRepair = _map.FindAction(EmergencyRepairAction, true);

            _heroPost.performed += OnHeroPost;
            _shockwave.performed += OnShockwave;
            _emergencyRepair.performed += OnEmergencyRepair;
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

        public Vector2 CameraPan => _pan.ReadValue<Vector2>();

        public float CameraZoom => _zoom.ReadValue<float>();

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
        public event Action HeroPostPressed;
        public event Action ShockwavePressed;
        public event Action EmergencyRepairPressed;

        public void Dispose()
        {
            _heroPost.performed -= OnHeroPost;
            _shockwave.performed -= OnShockwave;
            _emergencyRepair.performed -= OnEmergencyRepair;
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

        public bool FromPointer { get; private set; }

        private void OnConfirm(InputAction.CallbackContext context)
        {
            FromPointer = context.control.device is Pointer;
            Confirmed?.Invoke();
            FromPointer = false;
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            FromPointer = context.control.device is Pointer;
            Cancelled?.Invoke();
            FromPointer = false;
        }

        private void OnPause(InputAction.CallbackContext _) => PauseToggled?.Invoke();

        private void OnSpeed(InputAction.CallbackContext _) => SpeedToggled?.Invoke();

        private void OnNextTower(InputAction.CallbackContext _) => NextTower?.Invoke();

        private void OnPreviousTower(InputAction.CallbackContext _) => PreviousTower?.Invoke();

        private void OnSkipWave(InputAction.CallbackContext _) => SkipWave?.Invoke();

        private void OnUpgrade(InputAction.CallbackContext _) => UpgradePressed?.Invoke();

        private void OnRepair(InputAction.CallbackContext _) => RepairPressed?.Invoke();

        private void OnSell(InputAction.CallbackContext _) => SellPressed?.Invoke();

        private void OnHeroPost(InputAction.CallbackContext _) => HeroPostPressed?.Invoke();

        private void OnShockwave(InputAction.CallbackContext _) => ShockwavePressed?.Invoke();

        private void OnEmergencyRepair(InputAction.CallbackContext _) => EmergencyRepairPressed?.Invoke();
    }
}


