using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What a ground boss entrance needs round it: the sealed ruin gate (about six cells wide at its scale) stands on the
    /// cell and the giant walks out of its threshold, so nothing that draws a mound of its own (a nest) may stand that close.
    /// Shared by the level validator and the generator that places the arena's ruin gate.
    /// </summary>
    public static class GroundEntranceRules
    {
        /// <summary>Cells (in either direction, diagonals included) kept free round a ground entrance: half the arch and a step.</summary>
        public const int ThresholdCells = 3;

        /// <summary>True when a cell lies inside the threshold kept free round a ground entrance.</summary>
        public static bool CrowdsThreshold(Vector2Int entrance, Vector2Int cell)
        {
            Vector2Int offset = cell - entrance;
            return Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) <= ThresholdCells;
        }
    }
}
