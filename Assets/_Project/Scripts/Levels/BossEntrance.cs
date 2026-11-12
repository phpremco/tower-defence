using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Where the bosses of a level come up. For a pool the cell must be liquid; for the ground it must be a plain cell
    /// off the road. The boss then makes for the nearest road and follows it to the base.
    /// </summary>
    [Serializable]
    public struct BossEntrance
    {
        public EntranceKind Kind;
        public Vector2Int Cell;

        [Tooltip("Cells the structure that rises at this entrance (the sealed ruin gate) will stand on: no tower may be built there (GroundEntranceRules.GateCells). Empty when nothing rises.")]
        public Vector2Int[] Footprint;
    }
}
