using System.Collections.Generic;

namespace Game.Economy
{
    /// <summary>The weapons the player currently owns, keyed by definition id.</summary>
    public sealed class UnlockState
    {
        private readonly HashSet<string> _towerIds;

        public UnlockState(IEnumerable<string> towerIds)
        {
            _towerIds = new HashSet<string>(towerIds);
        }

        public bool IsTowerUnlocked(string towerId) => _towerIds.Contains(towerId);

        public void UnlockTower(string towerId) => _towerIds.Add(towerId);
    }
}
