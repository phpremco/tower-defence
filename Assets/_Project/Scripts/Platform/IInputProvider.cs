using System;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>Platform-independent view on player input. Gameplay never touches the Input System directly.</summary>
    public interface IInputProvider : IDisposable
    {
        /// <summary>Stick or d-pad direction, used to move the cursor on a gamepad.</summary>
        Vector2 CursorMove { get; }

        /// <summary>Camera movement: WASD or the arrow keys, or the right stick.</summary>
        Vector2 CameraPan { get; }

        /// <summary>Camera zoom: scroll wheel (large steps), Q and E or the stick clicks (continuous).</summary>
        float CameraZoom { get; }

        /// <summary>Pointer position in screen pixels, for mouse users.</summary>
        Vector2 PointerPosition { get; }

        /// <summary>True while the overview button is held (Tab; the gamepad View button held a moment, so a tap stays skip-wave).</summary>
        bool OverviewHeld { get; }

        event Action Confirmed;
        event Action Cancelled;

        /// <summary>
        /// True while the Confirmed or Cancelled event being raised came from a mouse button. Menus that let the
        /// mouse click their own buttons ignore those, so one click does not also act on the keyboard focus.
        /// </summary>
        bool FromPointer { get; }
        event Action PauseToggled;
        event Action SpeedToggled;
        event Action NextTower;
        event Action PreviousTower;
        event Action SkipWave;
        event Action UpgradePressed;
        event Action RepairPressed;
        event Action SellPressed;
        event Action HeroPostPressed;
        /// <summary>A hero ability button with its slot: 0 is the first (F, left trigger), 1 the second (G, right trigger).</summary>
        event Action<int> HeroAbilityPressed;

        /// <summary>The Codex button (Y, C): a scan hint opens its creature in the Codex. Nothing else listens yet.</summary>
        event Action CodexPressed;

        /// <summary>The kind of device the player used last; prompts show its buttons.</summary>
        InputDeviceKind ActiveDevice { get; }

        /// <summary>Raised when the player switches between the keyboard and mouse and a gamepad.</summary>
        event Action ActiveDeviceChanged;

        /// <summary>The printed key or button of an action on the active device ("A", "F", "LT"); empty when it has none there.</summary>
        string PromptFor(GameAction action);

        /// <summary>The printed key or button of an action on the given device.</summary>
        string PromptFor(GameAction action, InputDeviceKind device);

        /// <summary>The gamepad in use disconnected. Gameplay pauses on it (consoles require that).</summary>
        event Action DeviceLost;

        /// <summary>The lost gamepad is connected again.</summary>
        event Action DeviceRegained;
    }
}


