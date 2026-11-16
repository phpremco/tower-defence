using Game.Core;
using Game.Platform;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.UI
{
    /// <summary>
    /// Menu navigation follows the pad family's confirm convention: on a Nintendo layout the UI submits with A (east)
    /// and goes back with B (south), as the game's own confirm and cancel do. The Input System's default UI actions bind
    /// submit and cancel by usage ("*/{Submit}"), which is always south on a pad, so they are rebuilt once with an
    /// explicit pad binding that a layout override can move.
    /// </summary>
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public sealed class UiConfirmLayout : MonoBehaviour
    {
        private const string SubmitKey = "<Keyboard>/enter";
        private const string SubmitNumpadKey = "<Keyboard>/numpadEnter";
        private const string CancelKey = "<Keyboard>/escape";

        private InputAction _submit;
        private InputAction _cancel;
        private int _submitPad = -1;
        private int _cancelPad = -1;
        private IInputProvider _input;
        private IInputBindings _bindings;

        /// <summary>The pad button the menus submit with right now (a control path), for tests and probes.</summary>
        public string SubmitPadPath => _submitPad >= 0 ? _submit.bindings[_submitPad].effectivePath : string.Empty;

        /// <summary>The pad button the menus go back with right now (a control path).</summary>
        public string CancelPadPath => _cancelPad >= 0 ? _cancel.bindings[_cancelPad].effectivePath : string.Empty;

        /// <summary>Rebuilds the module's submit and cancel bindings and follows the pad layout from now on.</summary>
        public static UiConfirmLayout Attach(InputSystemUIInputModule module)
        {
            var layout = module.gameObject.AddComponent<UiConfirmLayout>();
            layout.Rebuild(module);
            return layout;
        }

        private void Rebuild(InputSystemUIInputModule module)
        {
            InputActionAsset asset = module.actionsAsset;
            _submit = module.submit != null ? module.submit.action : null;
            _cancel = module.cancel != null ? module.cancel.action : null;
            if (asset == null || _submit == null || _cancel == null)
            {
                return;
            }

            // Bindings can only change while their map rests; the module's handlers stay attached across this.
            bool enabled = asset.enabled;
            asset.Disable();
            _submitPad = Replace(_submit, PadLayoutRules.SouthPath, SubmitKey, SubmitNumpadKey);
            _cancelPad = Replace(_cancel, PadLayoutRules.EastPath, CancelKey);
            if (enabled)
            {
                asset.Enable();
            }

            if (Services.TryGet(out _bindings) && Services.TryGet(out _input))
            {
                _input.ActiveDeviceChanged += Follow;
            }

            Follow();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.ActiveDeviceChanged -= Follow;
            }
        }

        // Every usage binding goes: the keys are listed by name and the pad gets one binding the layout can move.
        private static int Replace(InputAction action, string padPath, params string[] keys)
        {
            for (int i = action.bindings.Count - 1; i >= 0; i--)
            {
                action.ChangeBinding(i).Erase();
            }

            foreach (string key in keys)
            {
                action.AddBinding(key);
            }

            action.AddBinding(padPath);
            return action.bindings.Count - 1;
        }

        private void Follow()
        {
            if (_submitPad < 0 || _cancelPad < 0)
            {
                return;
            }

            PadLayout layout = _bindings != null ? _bindings.ActivePadLayout : PadLayout.Xbox;
            Apply(_submit, _submitPad, PadLayoutRules.ConfirmPath(layout), PadLayoutRules.SouthPath);
            Apply(_cancel, _cancelPad, PadLayoutRules.CancelPath(layout), PadLayoutRules.EastPath);
        }

        private static void Apply(InputAction action, int binding, string wanted, string original)
        {
            if (wanted == original)
            {
                if (!string.IsNullOrEmpty(action.bindings[binding].overridePath))
                {
                    action.RemoveBindingOverride(binding);
                }
            }
            else if (action.bindings[binding].effectivePath != wanted)
            {
                action.ApplyBindingOverride(binding, wanted);
            }
        }
    }
}
