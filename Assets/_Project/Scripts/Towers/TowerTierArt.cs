using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>One look of a modelled tower: the tier's art and the level (and path) that wears it.</summary>
    [System.Serializable]
    public sealed class TowerTierArt
    {
        [Tooltip("The tier's name in the export (T1, T2, T3A, T3B, T3).")]
        [SerializeField] private string _tier = string.Empty;
        [SerializeField] [Min(0)] private int _levelIndex;
        [SerializeField] private TowerArtPath _path;
        [SerializeField] private ModelArtDefinition _art;

        public string Tier => _tier;
        public int LevelIndex => _levelIndex;
        public TowerArtPath Path => _path;
        public ModelArtDefinition Art => _art;
    }
}
