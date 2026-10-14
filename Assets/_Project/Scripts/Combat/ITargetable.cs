using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Anything a tower or projectile can shoot at. Towers only know this interface, not enemy classes.</summary>
    public interface ITargetable
    {
        Vector3 Position { get; }

        /// <summary>Exactly one of Ground or Air.</summary>
        TargetLayers Layer { get; }

        /// <summary>Distance travelled along the path. Towers prefer the target that is furthest along.</summary>
        float PathProgress { get; }

        bool IsAlive { get; }

        void TakeDamage(float amount);

        /// <summary>Slows movement to a share of normal speed for a short time. The strongest slow wins.</summary>
        void ApplySlow(float speedFactor, float seconds);
    }
}
