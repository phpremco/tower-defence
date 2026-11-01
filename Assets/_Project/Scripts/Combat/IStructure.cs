using UnityEngine;

namespace Game.Combat
{
    /// <summary>Something the player built that enemies may attack: towers now, blockades and path objects later.</summary>
    public interface IStructure
    {
        Vector3 Position { get; }

        bool IsAlive { get; }

        /// <summary>True while ground enemies cannot walk through this structure.</summary>
        bool BlocksGround { get; }

        /// <summary>The colony base itself, which repair crews leave for last.</summary>
        bool IsBase { get; }

        void TakeDamage(float amount);

        /// <summary>Share of maximum health left, from 0 to 1.</summary>
        float HealthFraction { get; }

        /// <summary>Restores hit points, used by repair drones.</summary>
        void Repair(float amount);
    }
}
