using Game.Combat;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>The base as a place for the hero to stand. Hits on the base are not shared, but repairs are.</summary>
    public sealed class BasePost : IGuardable
    {
        private readonly Health _health;

        public BasePost(Vector3 position, Health health)
        {
            Position = position;
            _health = health;
        }

        public Vector3 Position { get; }

        public IDamageGuard Guard { get; set; }

        public void Restore(float fraction) => _health.Heal(_health.Max * fraction);
    }
}
