using Game.Combat;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The base as something on the field: it stands at the end of the road and blocks it while it lives, so the
    /// enemies that get that far lay siege to it instead of slipping away. That gives the player a last chance to
    /// fight them off. Hits pass through the hero when it guards the doorstep; drones and the hero can repair it.
    /// Its health is the level's base health, so the HUD, the rewards and the defeat all read the same number.
    /// </summary>
    public sealed class BaseStructure : IStructure, IGuardable
    {
        private readonly Health _health;

        public BaseStructure(Vector3 position, Health health)
        {
            Position = position;
            _health = health;
        }

        public Vector3 Position { get; }

        public bool IsAlive => !_health.IsDead;

        public bool BlocksGround => IsAlive;

        public float HealthFraction => _health.Fraction;

        public IDamageGuard Guard { get; set; }

        public void TakeDamage(float amount)
        {
            if (_health.IsDead)
            {
                return;
            }

            _health.TakeDamage(Guard != null ? Guard.Absorb(amount) : amount);
        }

        public void Repair(float amount)
        {
            if (!_health.IsDead)
            {
                _health.Heal(amount);
            }
        }

        public void Restore(float fraction) => _health.Heal(_health.Max * fraction);
    }
}
