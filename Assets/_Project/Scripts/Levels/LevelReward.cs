using System;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>What finishing a level gives. Scaled by the difficulty profile; the weapon is given the first time only.</summary>
    [Serializable]
    public struct LevelReward
    {
        [Min(0)] public int ResearchPoints;
        [Min(0)] public int PowerCurrency;
        public TowerDefinition NewTower;
        public UnlockSource Source;
        [TextArea] public string Story;
    }
}
