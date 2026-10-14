using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One cell of the landscape that is not plain ground. Cells outside the grid are allowed, so a river can flow out of view.</summary>
    [Serializable]
    public struct TerrainPatch
    {
        public Vector2Int Cell;
        public TerrainKind Kind;
    }
}
