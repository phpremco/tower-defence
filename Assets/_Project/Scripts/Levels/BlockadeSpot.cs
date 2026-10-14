using System;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>A fixed place on the road where the level has a blockade, and which kind it is.</summary>
    [Serializable]
    public struct BlockadeSpot
    {
        public Vector2Int Cell;
        public TowerDefinition Blockade;
    }
}
