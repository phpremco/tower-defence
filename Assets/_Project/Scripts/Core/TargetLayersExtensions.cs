namespace Game.Core
{
    /// <summary>The one rule for whether a weapon's layers can hit a target's layers.</summary>
    public static class TargetLayersExtensions
    {
        /// <summary>
        /// True when a weapon aimed at <paramref name="mask"/> can hit a target on <paramref name="layer"/>: they share a
        /// layer. A creature on both layers (the Wisp, a ball of energy that walks the road) is a target of every gun,
        /// the air-only flak included (round 88a, Remco: "it was supposed to be both land and air"; until then an
        /// air-only gun passed it over because it does not fly).
        /// </summary>
        public static bool CanHit(this TargetLayers mask, TargetLayers layer) => (layer & mask) != 0;
    }
}
