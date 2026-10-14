using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A route enemies can follow. Road paths are axis aligned and block building; flying paths may cross the field
    /// in any direction. Spawn groups pick a path by index, so each wave can send units along different routes.
    /// </summary>
    [Serializable]
    public struct EnemyPath
    {
        public string Name;

        /// <summary>True for a route only flyers may use. Ground enemies need a road path.</summary>
        public bool Flying;

        public Vector2Int[] Waypoints;
    }
}
