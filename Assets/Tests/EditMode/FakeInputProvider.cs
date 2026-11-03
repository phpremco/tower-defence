using System;
using Game.Platform;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>An input provider the tests press by hand; only the events the tests need can be raised.</summary>
    internal sealed class FakeInputProvider : IInputProvider
    {
        public Vector2 CursorMove => Vector2.zero;
        public Vector2 CameraPan => Vector2.zero;
        public float CameraZoom => 0f;
        public Vector2 PointerPosition => Vector2.zero;
        public bool FromPointer { get; set; }

        public event Action Confirmed;
        public event Action Cancelled;
        public event Action PauseToggled;
        public event Action SpeedToggled;
#pragma warning disable 67 // Part of the interface; no test presses these.
        public event Action NextTower;
        public event Action PreviousTower;
        public event Action SkipWave;
        public event Action UpgradePressed;
        public event Action RepairPressed;
        public event Action SellPressed;
        public event Action HeroPostPressed;
#pragma warning restore 67
        public event Action<int> HeroAbilityPressed;

        public void Confirm() => Confirmed?.Invoke();

        public void Cancel() => Cancelled?.Invoke();

        public void Pause() => PauseToggled?.Invoke();

        public void Speed() => SpeedToggled?.Invoke();

        public void HeroAbility(int slot) => HeroAbilityPressed?.Invoke(slot);

        public void Dispose()
        {
        }
    }
}
