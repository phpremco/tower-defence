using UnityEngine;

namespace Game.Combat
{
    /// <summary>A place where the hero can stand: a blockade or the base. A standing hero guards it.</summary>
    public interface IGuardable
    {
        Vector3 Position { get; }

        /// <summary>Set by the hero while it stands here, and cleared when it leaves.</summary>
        IDamageGuard Guard { get; set; }

        /// <summary>Gives back a share of the maximum health. Used by the hero's emergency repair.</summary>
        void Restore(float fraction);
    }
}
