using System;

namespace Game.Core
{
    /// <summary>Plain event channel without payload. Systems talk through channels instead of direct references.</summary>
    public sealed class GameEvent
    {
        private event Action Raised;

        public void Subscribe(Action listener) => Raised += listener;

        public void Unsubscribe(Action listener) => Raised -= listener;

        public void Raise() => Raised?.Invoke();
    }

    /// <summary>Plain event channel with a payload.</summary>
    public sealed class GameEvent<T>
    {
        private event Action<T> Raised;

        public void Subscribe(Action<T> listener) => Raised += listener;

        public void Unsubscribe(Action<T> listener) => Raised -= listener;

        public void Raise(T value) => Raised?.Invoke(value);
    }
}
