using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Game.Platform
{
    /// <summary>
    /// IInputProvider backed by the Input System action asset. Action names are defined in that asset. It also follows
    /// which device the player uses (any button press, or the sticks), names actions for prompts from the bindings and
    /// the glyph tables, and reports the gamepad in use dropping out.
    /// </summary>
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
        private const string HeroAbility1Action = "HeroAbility1";
        private const string HeroAbility2Action = "HeroAbility2";
        private const string OverviewAction = "Overview";
        private const string CodexAction = "Codex";
        private const string GamepadLayout = "Gamepad";
        private const string KeyboardLayout = "Keyboard";
        private const string MouseLayout = "Mouse";
        private const string PointerLayout = "Pointer";
        private const int DeviceKinds = 2;

        private readonly InputActionMap _map;
        private readonly InputAction _heroPost;
        private readonly InputAction _heroAbility1;
        private readonly InputAction _heroAbility2;
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
        private readonly InputAction _overview;
        private readonly InputAction _codex;

        // Indexed by GameAction; the prompt cache by action and device kind, filled on first use (bindings never change).
        private readonly InputAction[] _byGameAction;
        private readonly string[,] _prompts;
        private readonly ButtonGlyphTable _padGlyphs;
        private readonly ButtonGlyphTable _keyGlyphs;
        private readonly ActiveDeviceTracker _devices;
        private readonly IDisposable _anyButton;

        /// <param name="padGlyphs">Names of the pad buttons for this platform's layout; null prints the Input System's names.</param>
        /// <param name="keyGlyphs">Short names of keys ("Esc"); null prints the Input System's names.</param>
        public InputSystemProvider(InputActionAsset asset, ButtonGlyphTable padGlyphs = null, ButtonGlyphTable keyGlyphs = null)
        {
            _padGlyphs = padGlyphs;
            _keyGlyphs = keyGlyphs;
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
            _heroAbility1 = _map.FindAction(HeroAbility1Action, true);
            _heroAbility2 = _map.FindAction(HeroAbility2Action, true);
            _overview = _map.FindAction(OverviewAction, true);
            _codex = _map.FindAction(CodexAction, true);

            _byGameAction = new[]
            {
                _confirm, _cancel, _pause, _speed, _nextTower, _previousTower, _skipWave, _upgrade, _repair, _sell, _heroPost,
                _heroAbility1, _heroAbility2, _overview, _codex,
            };
            _prompts = new string[_byGameAction.Length, DeviceKinds];

            // A console has no keyboard: start on the pad there, so the first prompts are already right.
            bool padOnly = Keyboard.current == null && Gamepad.current != null;
            _devices = new ActiveDeviceTracker(padOnly ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse);

            _heroPost.performed += OnHeroPost;
            _heroAbility1.performed += OnHeroAbility1;
            _heroAbility2.performed += OnHeroAbility2;
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
            _overview.performed += OnOverviewHeld;
            _overview.canceled += OnOverviewReleased;
            _codex.performed += OnCodex;
            // The sticks press no button, so a pad player who only steers is noticed through them.
            _move.performed += OnSteered;
            _pan.performed += OnSteered;
            _anyButton = InputSystem.onAnyButtonPress.Call(OnAnyButton);
            InputSystem.onDeviceChange += OnDeviceChange;
            _map.Enable();
        }

        public Vector2 CursorMove => _move.ReadValue<Vector2>();

        public Vector2 CameraPan => _pan.ReadValue<Vector2>();

        public float CameraZoom => _zoom.ReadValue<float>();

        public Vector2 PointerPosition => _point.ReadValue<Vector2>();

        // Performed when held (at once for the key, after the hold time on the gamepad), cancelled on release.
        public bool OverviewHeld { get; private set; }

        public InputDeviceKind ActiveDevice => _devices.Kind;

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
        public event Action<int> HeroAbilityPressed;
        public event Action CodexPressed;
        public event Action ActiveDeviceChanged;
        public event Action DeviceLost;
        public event Action DeviceRegained;

        public void Dispose()
        {
            _heroPost.performed -= OnHeroPost;
            _heroAbility1.performed -= OnHeroAbility1;
            _heroAbility2.performed -= OnHeroAbility2;
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
            _overview.performed -= OnOverviewHeld;
            _overview.canceled -= OnOverviewReleased;
            _codex.performed -= OnCodex;
            _move.performed -= OnSteered;
            _pan.performed -= OnSteered;
            _anyButton.Dispose();
            InputSystem.onDeviceChange -= OnDeviceChange;
            _map.Disable();
        }

        public bool FromPointer { get; private set; }

        public string PromptFor(GameAction action) => PromptFor(action, _devices.Kind);

        public string PromptFor(GameAction action, InputDeviceKind device)
        {
            int index = (int)action;
            if (index < 0 || index >= _byGameAction.Length)
            {
                return string.Empty;
            }

            int kind = (int)device;
            if (_prompts[index, kind] == null)
            {
                _prompts[index, kind] = Describe(_byGameAction[index], device);
            }

            return _prompts[index, kind];
        }

        // The first plain binding on the device: on the keyboard side a key before a mouse button, so Confirm reads "Enter".
        private string Describe(InputAction action, InputDeviceKind device)
        {
            if (device == InputDeviceKind.Gamepad)
            {
                return Describe(action, GamepadLayout, _padGlyphs);
            }

            string key = Describe(action, KeyboardLayout, _keyGlyphs);
            if (key.Length == 0)
            {
                key = Describe(action, MouseLayout, _keyGlyphs);
            }

            return key.Length == 0 ? Describe(action, PointerLayout, _keyGlyphs) : key;
        }

        private static string Describe(InputAction action, string layout, ButtonGlyphTable glyphs)
        {
            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || InputControlPath.TryGetDeviceLayout(binding.effectivePath) != layout)
                {
                    continue;
                }

                if (glyphs != null && glyphs.TryGet(binding.effectivePath, out string label))
                {
                    return label;
                }

                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }

            return string.Empty;
        }

        private void OnAnyButton(InputControl control) => Track(control.device);

        private void OnSteered(InputAction.CallbackContext context) => Track(context.control.device);

        private void Track(InputDevice device)
        {
            InputDeviceKind kind;
            if (device is Gamepad)
            {
                kind = InputDeviceKind.Gamepad;
            }
            else if (device is Keyboard || device is Pointer)
            {
                kind = InputDeviceKind.KeyboardMouse;
            }
            else
            {
                return;
            }

            if (_devices.Used(kind, device.deviceId))
            {
                ActiveDeviceChanged?.Invoke();
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad))
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Removed:
                    if (_devices.Lost(device.deviceId))
                    {
                        DeviceLost?.Invoke();
                    }

                    break;
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Added:
                    if (_devices.Regained(device.deviceId))
                    {
                        DeviceRegained?.Invoke();
                    }

                    break;
            }
        }

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

        private void OnOverviewHeld(InputAction.CallbackContext _) => OverviewHeld = true;

        private void OnOverviewReleased(InputAction.CallbackContext _) => OverviewHeld = false;

        private void OnHeroPost(InputAction.CallbackContext _) => HeroPostPressed?.Invoke();

        private void OnHeroAbility1(InputAction.CallbackContext _) => HeroAbilityPressed?.Invoke(0);

        private void OnHeroAbility2(InputAction.CallbackContext _) => HeroAbilityPressed?.Invoke(1);

        private void OnCodex(InputAction.CallbackContext _) => CodexPressed?.Invoke();
    }
}
