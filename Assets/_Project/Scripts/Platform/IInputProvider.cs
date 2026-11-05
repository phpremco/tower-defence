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
    }
}


