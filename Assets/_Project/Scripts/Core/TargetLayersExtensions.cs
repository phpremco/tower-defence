namespace Game.Core
{
    /// <summary>The one rule for whether a weapon's layers can hit a target's layers.</summary>
    public static class TargetLayersExtensions
    {
        /// <summary>
        /// True when a weapon aimed at <paramref name="mask"/> can hit a target on <paramref name="layer"/>. A weapon that
        /// only aims at the air (flak, the air net) cannot hit a creature that also walks the ground, such as a Wisp on the
        /// road: it is in the air layer for guns that hit both, but it does not fly.
        /// </summary>
        public static bool CanHit(this TargetLayers mask, TargetLayers layer)
        {
            if ((layer & mask) == 0)
            {
                return false;
            }

            return mask != TargetLayers.Air || (layer & TargetLayers.Ground) == 0;
        }
    }
}
