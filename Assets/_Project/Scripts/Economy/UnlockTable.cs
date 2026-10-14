using System.Collections.Generic;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>Which weapons the player owns at the start. Rewards per level are added with the meta milestone.</summary>
    [CreateAssetMenu(menuName = "Game/Unlock Table", fileName = "UnlockTable")]
    public sealed class UnlockTable : ScriptableObject
    {
        [SerializeField] private List<string> _startingTowerIds = new List<string>();

        public IReadOnlyList<string> StartingTowerIds => _startingTowerIds;
    }
}
