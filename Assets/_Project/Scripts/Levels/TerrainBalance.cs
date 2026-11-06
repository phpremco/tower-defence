namespace Game.Levels
{
    /// <summary>
    /// What a build spot's terrain does to a tower standing on it, as both the game and the balance model must see it (round
    /// 22, Remco: a raised platform may one day lengthen a tower's reach, and the spot lens and the plan search must pick that
    /// up from data). Every kind is one today; the hook is here so the pipeline needs no new rule when a kind gets a bonus.
    /// A bonus added here must also be applied by the tower itself (Tower.Range), so the model and the game agree.
    /// </summary>
    public static class TerrainBalance
    {
        /// <summary>Reach multiplier of a tower on a cell of this kind.</summary>
        public static float RangeMultiplier(TerrainKind kind)
        {
            switch (kind)
            {
                default:
                    return 1f;
            }
        }
    }
}
