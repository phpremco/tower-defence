using UnityEngine;

namespace Game.Combat
{
    /// <summary>Something the player built that enemies may attack: towers now, blockades and path objects later.</summary>
    public interface IStructure
    {
        Vector3 Position { get; }

        bool IsAlive { get; }

        void TakeDamage(float amount);
    }
}
