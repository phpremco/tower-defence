using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Platform
{
    /// <summary>
    /// Rebinding: one plain binding per action and device kind (the first key, or the first pad button) can be moved by
    /// the player. The overrides live on the action asset and travel as the Input System's own JSON.
    /// </summary>
    public sealed partial class InputSystemProvider
    {
        // A short wait lets a stick or trigger settle so its strongest control wins, not the first one past the threshold.
        private const float RebindSettleSeconds = 0.1f;
        private const string CancelThrough = "<Keyboard>/escape";
        private const string ButtonControl = "Button";

        // Pad controls the cursor and the camera already use; binding a button action there would fight them.
        private static readonly string[] PadExcluded =
        {
            "<Gamepad>/leftStick", "<Gamepad>/rightStick", "<Gamepad>/dpad", "<Gamepad>/leftStick/*",
            "<Gamepad>/rightStick/*", "<Gamepad>/dpad/*",
        };

        // Keys the camera and the pointer use, and Esc, which cancels the rebind itself.
        private static readonly string[] KeyExcluded =
        {
            "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d", "<Keyboard>/upArrow", "<Keyboard>/downArrow",
            "<Keyboard>/leftArrow", "<Keyboard>/rightArrow", "<Keyboard>/minus", "<Keyboard>/equals", "<Keyboard>/anyKey",
        };

        private InputActionRebindingExtensions.RebindingOperation _rebind;

        public event Action BindingsChanged;

        public bool IsRebinding => _rebind != null;

        /// <summary>Confirm, cancel and pause carry every menu; they are never left without a control (see the swap).</summary>
        public static bool IsEssential(GameAction action)
        {
            return action == GameAction.Confirm || action == GameAction.Cancel || action == GameAction.Pause;
        }

        public bool CanRebind(GameAction action, InputDeviceKind device)
        {
            int index = (int)action;
            return index >= 0 && index < _byGameAction.Length && RebindBinding(_byGameAction[index], device) >= 0;
        }

        public bool StartRebind(GameAction action, InputDeviceKind device, Action<bool> finished)
        {
            if (_rebind != null || !CanRebind(action, device))
            {
                return false;
            }

            InputAction target = _byGameAction[(int)action];
            int binding = RebindBinding(target, device);
            bool pad = device == InputDeviceKind.Gamepad;
            string picked = null;

            // An action cannot be rebound while it listens; the whole map rests so the pressed key does nothing else.
            _map.Disable();
            InputActionRebindingExtensions.RebindingOperation operation = target.PerformInteractiveRebinding(binding)
                .WithExpectedControlType(ButtonControl)
                .WithControlsHavingToMatchPath(pad ? "<" + GamepadLayout + ">" : "<" + KeyboardLayout + ">")
                .WithCancelingThrough(CancelThrough)
                .OnMatchWaitForAnother(RebindSettleSeconds);
            foreach (string excluded in pad ? PadExcluded : KeyExcluded)
            {
                operation.WithControlsExcluding(excluded);
            }

            // The operation only reports the control; Assign applies it, so the swap rule holds for every path in.
            operation
                .OnApplyBinding((_, path) => picked = path)
                .OnComplete(_ => EndRebind(action, device, picked, finished))
                .OnCancel(_ => EndRebind(action, device, null, finished));
            _rebind = operation;
            operation.Start();
            return true;
        }

        public void CancelRebind()
        {
            _rebind?.Cancel();
        }

        public void ResetBindings()
        {
            CancelRebind();
            _map.RemoveAllBindingOverrides();
            _layoutSwap = false;
            ApplyLayoutDefaults();
            BindingsUpdated();
        }

        public string SaveOverrides()
        {
            string json = SaveWithoutLayoutSwap();
            return HasOverrides(json) ? json : string.Empty;
        }

        public bool LoadOverrides(string json)
        {
            _map.RemoveAllBindingOverrides();
            _layoutSwap = false;
            if (string.IsNullOrWhiteSpace(json))
            {
                ApplyLayoutDefaults();
                BindingsUpdated();
                return true;
            }

            bool ok = true;
            try
            {
                if (_map.asset != null)
                {
                    _map.asset.LoadBindingOverridesFromJson(json);
                }
                else
                {
                    _map.LoadBindingOverridesFromJson(json);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Saved key bindings could not be read, the defaults are used: {exception.Message}");
                ok = false;
            }

            // Hand-edited or older text may leave an essential action without a control: then the defaults are safer.
            if (ok && !EssentialsBound())
            {
                Debug.LogWarning("Saved key bindings left confirm, cancel or pause without a control; the defaults are used.");
                ok = false;
            }

            if (!ok)
            {
                _map.RemoveAllBindingOverrides();
            }

            ApplyLayoutDefaults();
            BindingsUpdated();
            return ok;
        }

        /// <summary>
        /// Binds an action on a device to a control path (as a rebind would) and swaps it away from any other action
        /// that held it. False when the action has no rebindable binding there or the path is empty or unchanged.
        /// </summary>
        public bool Assign(GameAction action, InputDeviceKind device, string path)
        {
            if (string.IsNullOrEmpty(path) || !CanRebind(action, device))
            {
                return false;
            }

            InputAction target = _byGameAction[(int)action];
            int binding = RebindBinding(target, device);
            string before = target.bindings[binding].effectivePath;
            if (string.Equals(before, path, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            AdoptLayoutSwap(action, device, path);
            target.ApplyBindingOverride(binding, path);
            SwapAway(action, device, path, before);
            BindingsUpdated();
            return true;
        }

        private void EndRebind(GameAction action, InputDeviceKind device, string picked, Action<bool> finished)
        {
            _rebind?.Dispose();
            _rebind = null;
            _map.Enable();
            bool changed = Assign(action, device, picked);
            if (!changed)
            {
                BindingsUpdated();
            }

            finished?.Invoke(changed);
        }

        // The control the player just picked may already belong to another action on the same device: that action takes
        // the old control of the one rebound, so both stay reachable and nothing is ever unbound.
        private void SwapAway(GameAction rebound, InputDeviceKind device, string taken, string freed)
        {
            for (int i = 0; i < _byGameAction.Length; i++)
            {
                if (i == (int)rebound)
                {
                    continue;
                }

                InputAction other = _byGameAction[i];
                int binding = RebindBinding(other, device);
                if (binding >= 0 && string.Equals(other.bindings[binding].effectivePath, taken, StringComparison.OrdinalIgnoreCase))
                {
                    other.ApplyBindingOverride(binding, freed);
                }
            }
        }

        private bool EssentialsBound()
        {
            for (int i = 0; i < _byGameAction.Length; i++)
            {
                if (!IsEssential((GameAction)i))
                {
                    continue;
                }

                // Judged on the original paths: an override to an empty path hides the binding from the lookups.
                foreach (InputBinding binding in _byGameAction[i].bindings)
                {
                    bool plain = !binding.isComposite && !binding.isPartOfComposite;
                    bool ours = IsOn(binding.path, GamepadLayout) || IsOn(binding.path, KeyboardLayout);
                    if (plain && ours && string.IsNullOrEmpty(binding.effectivePath))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void BindingsUpdated()
        {
            ForgetPrompts();
            BindingsChanged?.Invoke();
        }

        // The binding a player moves: the first plain key on the keyboard side (mouse buttons stay), the first pad button.
        private static int RebindBinding(InputAction action, InputDeviceKind device)
        {
            return FirstBinding(action, device == InputDeviceKind.Gamepad ? GamepadLayout : KeyboardLayout);
        }

        // The Input System writes an empty list as {"bindings":[]}; the settings keep an empty string instead.
        private static bool HasOverrides(string json)
        {
            return !string.IsNullOrEmpty(json) && json.IndexOf("\"path\"", StringComparison.Ordinal) >= 0;
        }
    }
}
