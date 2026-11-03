using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// The owners that currently freeze time. Each overlay holds under its own key, so closing one overlay
    /// never releases a hold another overlay still needs; time runs again only when the set is empty.
    /// </summary>
    public sealed class TimeHolds
    {
        private readonly HashSet<object> _owners = new HashSet<object>();

        public bool IsHeld => _owners.Count > 0;

        public int Count => _owners.Count;

        /// <summary>Returns true when the owner did not hold before. Holding twice under one owner is one hold.</summary>
        public bool Acquire(object owner) => _owners.Add(owner);

        /// <summary>Returns true when the owner held. Releasing an owner that never held is harmless.</summary>
        public bool Release(object owner) => _owners.Remove(owner);
    }
}
