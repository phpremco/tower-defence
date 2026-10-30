using System;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A place where element creatures come up: a vent in the lava, a whirl in a pond, a hollow tree, a burrow in
    /// the sand. The cell must lie off the road; creatures rise there and make for the nearest road.
    /// </summary>
    [Serializable]
    public struct ElementNest
    {
        public Vector2Int Cell;
        public Element Element;
    }
}
