using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What a build spot's terrain does to a tower standing on it, as both the game and the balance model must see it (round
    /// 22, Remco: a raised platform may one day lengthen a tower's reach, and the spot lens and the plan search must pick that
    /// up from data). Round 65: raised ground reaches further, by the one number in the level's <see cref="TerrainRules"/>;
    /// the tower (Tower.Range), the range ring, the info line and the plan model's spots all ask here, so they agree.
    /// </summary>
    public static class TerrainBalance
    {
        /// <summary>Reach multiplier of a tower on this cell of the level.</summary>
        public static float RangeMultiplier(LevelDefinition level, Vector2Int cell) =>
            level != null && level.TerrainRules != null ? level.TerrainRules.RangeMultiplier(level.Terrain.KindAt(cell)) : 1f;
    }
}
