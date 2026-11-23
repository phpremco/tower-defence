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

        /// <summary>
        /// Road still ahead of it to the end of its own route. Guns prefer the smallest: the threat nearest the base,
        /// whichever road or air route it came by.
        /// </summary>
        float DistanceToGoal { get; }

        bool IsAlive { get; }

        void TakeDamage(float amount);

        /// <summary>Slows movement to a share of normal speed for a short time. The strongest slow wins.</summary>
        void ApplySlow(float speedFactor, float seconds);
    }
}
