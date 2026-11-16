using System;

namespace Game.Platform
{
    /// <summary>
    /// The bindings behind the prompts, and changing them. Prompts read which control an action sits on (to draw its
    /// glyph); the controls page rebinds actions per device and stores the overrides as text in the settings. The
    /// input provider implements it; gameplay never needs it.
    /// </summary>
    public interface IInputBindings
    {
        /// <summary>The face-button layout of the pad in use (or the platform's default before any pad was used).</summary>
        PadLayout ActivePadLayout { get; }

        /// <summary>The control path an action is bound to on a device ("&lt;Gamepad&gt;/buttonSouth"); empty when it has none there.</summary>
        string ControlPath(GameAction action, InputDeviceKind device);

        /// <summary>True when the action may be rebound on that device (it has a plain binding there).</summary>
        bool CanRebind(GameAction action, InputDeviceKind device);

        /// <summary>True while an interactive rebind waits for a key or button.</summary>
        bool IsRebinding { get; }

        /// <summary>
        /// Waits for the next key (keyboard) or pad button (gamepad) and binds the action to it. A control another action
        /// already uses is swapped: that action takes this one's old control, so no action is ever left unbound.
        /// <paramref name="finished"/> gets true when a new control was bound, false when cancelled or timed out.
        /// </summary>
        bool StartRebind(GameAction action, InputDeviceKind device, Action<bool> finished);

        void CancelRebind();

        /// <summary>Drops every override: the bindings of the action asset again.</summary>
        void ResetBindings();

        /// <summary>The overrides as JSON (empty when there are none), for the settings file.</summary>
        string SaveOverrides();

        /// <summary>Applies saved overrides; bad or incomplete text resets to the defaults. False when it had to reset.</summary>
        bool LoadOverrides(string json);

        /// <summary>Raised after a rebind, a reset or a load; prompts redraw.</summary>
        event Action BindingsChanged;
    }
}
