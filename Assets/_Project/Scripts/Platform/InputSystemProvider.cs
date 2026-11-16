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
    public sealed partial class InputSystemProvider : IInputProvider, IInputBindings
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

        // Indexed by GameAction; the prompt cache by action and device kind, filled on first use and emptied when the
        // bindings or the pad layout change.
        private readonly InputAction[] _byGameAction;
        private readonly string[,] _prompts;
        private readonly string[,] _paths;
        private readonly Func<PadLayout, ButtonGlyphTable> _padTables;
        private readonly PadLayout _defaultLayout;
        private ButtonGlyphTable _padGlyphs;
        private int _resolvedPadId = ActiveDeviceTracker.NoDevice;
        private readonly ButtonGlyphTable _keyGlyphs;
        private readonly ActiveDeviceTracker _devices;
        private readonly IDisposable _anyButton;

        /// <param name="padGlyphs">Names of the pad buttons for this platform's layout; null prints the Input System's names.</param>
        /// <param name="keyGlyphs">Short names of keys ("Esc"); null prints the Input System's names.</param>
        public InputSystemProvider(InputActionAsset asset, ButtonGlyphTable padGlyphs = null, ButtonGlyphTable keyGlyphs = null)
            : this(asset, _ => padGlyphs, padGlyphs != null ? padGlyphs.Layout : PadLayout.Xbox, keyGlyphs)
        {
        }

        /// <param name="padTables">The names table of each pad layout; the pad in the player's hands picks one.</param>
        /// <param name="defaultLayout">The layout before any pad was used, and for pads of no known family.</param>
        /// <param name="keyGlyphs">Short names of keys ("Esc"); null prints the Input System's names.</param>
        public InputSystemProvider(InputActionAsset asset, Func<PadLayout, ButtonGlyphTable> padTables, PadLayout defaultLayout, ButtonGlyphTable keyGlyphs)
        {
            _padTables = padTables;
            _defaultLayout = defaultLayout;
            ActivePadLayout = Gamepad.current != null ? PadLayoutResolver.ForDeviceLayout(Gamepad.current.layout, defaultLayout) : defaultLayout;
            _padGlyphs = padTables?.Invoke(ActivePadLayout);
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
            _paths = new string[_byGameAction.Length, DeviceKinds];

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
            CancelRebind();
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

        public PadLayout ActivePadLayout { get; private set; }

        public string ControlPath(GameAction action, InputDeviceKind device)
        {
            int index = (int)action;
            if (index < 0 || index >= _byGameAction.Length)
            {
                return string.Empty;
            }

            int kind = (int)device;
            if (_paths[index, kind] == null)
            {
                int binding = PromptBinding(_byGameAction[index], device);
                _paths[index, kind] = binding < 0 ? string.Empty : _byGameAction[index].bindings[binding].effectivePath;
            }

            return _paths[index, kind];
        }

        // The first plain binding on the device: on the keyboard side a key before a mouse button, so Confirm reads "Enter".
        private string Describe(InputAction action, InputDeviceKind device)
        {
            int binding = PromptBinding(action, device);
            if (binding < 0)
            {
                return string.Empty;
            }

            ButtonGlyphTable glyphs = device == InputDeviceKind.Gamepad ? _padGlyphs : _keyGlyphs;
            if (glyphs != null && glyphs.TryGet(action.bindings[binding].effectivePath, out string label))
            {
                return label;
            }

            return action.GetBindingDisplayString(binding, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        private static int PromptBinding(InputAction action, InputDeviceKind device)
        {
            if (device == InputDeviceKind.Gamepad)
            {
                return FirstBinding(action, GamepadLayout);
            }

            int key = FirstBinding(action, KeyboardLayout);
            if (key < 0)
            {
                key = FirstBinding(action, MouseLayout);
            }

            return key < 0 ? FirstBinding(action, PointerLayout) : key;
        }

        private static int FirstBinding(InputAction action, string layout)
        {
            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite && IsOn(binding.effectivePath, layout))
                {
                    return i;
                }
            }

            return -1;
        }

        // A rebind on a DualSense may store "<DualShockGamepad>/touchpadButton": still a pad binding.
        private static bool IsOn(string path, string layout)
        {
            string device = InputControlPath.TryGetDeviceLayout(path);
            return !string.IsNullOrEmpty(device) && (device == layout || InputSystem.IsFirstLayoutBasedOnSecond(device, layout));
        }

        // Bindings or the pad layout changed: every printed name and path is worked out again on its next use.
        private void ForgetPrompts()
        {
            for (int action = 0; action < _byGameAction.Length; action++)
            {
                for (int kind = 0; kind < DeviceKinds; kind++)
                {
                    _prompts[action, kind] = null;
                    _paths[action, kind] = null;
                }
            }
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

            bool changed = _devices.Used(kind, device.deviceId);
            if (kind == InputDeviceKind.Gamepad)
            {
                changed |= FollowPad(device);
            }

            if (changed)
            {
                ActiveDeviceChanged?.Invoke();
            }
        }

        // A DualSense picked up after an Xbox pad swaps the names and glyphs at once; prompts hear it as a device change.
        private bool FollowPad(InputDevice pad)
        {
            // The sticks report every frame they move: the layout is only worked out when another pad takes over.
            if (pad.deviceId == _resolvedPadId)
            {
                return false;
            }

            _resolvedPadId = pad.deviceId;
            PadLayout layout = PadLayoutResolver.ForDeviceLayout(pad.layout, _defaultLayout);
            if (layout == ActivePadLayout)
            {
                return false;
            }

            ActivePadLayout = layout;
            _padGlyphs = _padTables?.Invoke(layout);
            ForgetPrompts();
            return true;
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
