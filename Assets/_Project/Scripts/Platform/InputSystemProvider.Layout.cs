using UnityEngine.InputSystem;

namespace Game.Platform
{
    /// <summary>
    /// Pad-family conventions on top of the bindings: on a Nintendo layout confirm and cancel trade places (A east
    /// confirms, B south goes back). The trade is a pair of binding overrides that belong to the layout, not to the
    /// player: they are never written to the settings file, they follow the pad in use, and they step aside as soon as
    /// the player rebinds confirm or cancel on the pad (the player's own choice always wins).
    /// </summary>
    public sealed partial class InputSystemProvider
    {
        // True while the confirm and cancel pad bindings carry the layout's overrides rather than the player's.
        private bool _layoutSwap;

        /// <summary>True while the pad's confirm and cancel are swapped for the layout in use (Nintendo).</summary>
        public bool ConfirmSwapped => _layoutSwap;

        // Called whenever the layout, the overrides or a reset changed what the pad bindings should be.
        private void ApplyLayoutDefaults()
        {
            int confirm = RebindBinding(_confirm, InputDeviceKind.Gamepad);
            int cancel = RebindBinding(_cancel, InputDeviceKind.Gamepad);
            if (confirm < 0 || cancel < 0)
            {
                return;
            }

            if (_layoutSwap)
            {
                _confirm.RemoveBindingOverride(confirm);
                _cancel.RemoveBindingOverride(cancel);
                _layoutSwap = false;
            }

            if (!PadLayoutRules.ConfirmsEast(ActivePadLayout))
            {
                return;
            }

            // Either one rebound by the player: the swap stays off, so it can never land on a control the player moved.
            if (!string.IsNullOrEmpty(_confirm.bindings[confirm].overridePath) || !string.IsNullOrEmpty(_cancel.bindings[cancel].overridePath))
            {
                return;
            }

            _confirm.ApplyBindingOverride(confirm, PadLayoutRules.ConfirmPath(ActivePadLayout));
            _cancel.ApplyBindingOverride(cancel, PadLayoutRules.CancelPath(ActivePadLayout));
            _layoutSwap = true;
        }

        // A pad rebind that moves confirm or cancel, or takes the control one of them sits on, makes the swapped pair
        // the player's: the overrides stay where they are and are saved from now on.
        private void AdoptLayoutSwap(GameAction action, InputDeviceKind device, string path)
        {
            if (!_layoutSwap || device != InputDeviceKind.Gamepad)
            {
                return;
            }

            bool touches = action == GameAction.Confirm || action == GameAction.Cancel
                || PathEquals(ControlPath(GameAction.Confirm, device), path) || PathEquals(ControlPath(GameAction.Cancel, device), path);
            if (touches)
            {
                _layoutSwap = false;
            }
        }

        // The settings file holds the player's overrides only: the layout's pair is lifted out while the JSON is written.
        private string SaveWithoutLayoutSwap()
        {
            if (!_layoutSwap)
            {
                return SaveAllOverrides();
            }

            int confirm = RebindBinding(_confirm, InputDeviceKind.Gamepad);
            int cancel = RebindBinding(_cancel, InputDeviceKind.Gamepad);
            string confirmPath = _confirm.bindings[confirm].overridePath;
            string cancelPath = _cancel.bindings[cancel].overridePath;
            _confirm.RemoveBindingOverride(confirm);
            _cancel.RemoveBindingOverride(cancel);
            string json = SaveAllOverrides();
            _confirm.ApplyBindingOverride(confirm, confirmPath);
            _cancel.ApplyBindingOverride(cancel, cancelPath);
            return json;
        }

        private string SaveAllOverrides() => _map.asset != null ? _map.asset.SaveBindingOverridesAsJson() : _map.SaveBindingOverridesAsJson();

        private static bool PathEquals(string a, string b) => !string.IsNullOrEmpty(a) && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }
}
