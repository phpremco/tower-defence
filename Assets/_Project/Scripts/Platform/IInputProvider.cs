using System;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>Platform-independent view on player input. Gameplay never touches the Input System directly.</summary>
    public interface IInputProvider : IDisposable
    {
        /// <summary>Stick or d-pad direction, used to move the cursor on a gamepad.</summary>
        Vector2 CursorMove { get; }

        /// <summary>Pointer position in screen pixels, for mouse users.</summary>
        Vector2 PointerPosition { get; }

        event Action Confirmed;
        event Action Cancelled;
        event Action PauseToggled;
        event Action SpeedToggled;
        event Action NextTower;
        event Action PreviousTower;
        event Action SkipWave;
        event Action UpgradePressed;
        event Action RepairPressed;
        event Action SellPressed;
    }
}


