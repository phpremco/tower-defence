using System.Collections.Generic;

namespace Game.Hero
{
    /// <summary>What the hero's unlocked perks change, multiplied together; built once when the level starts.</summary>
    public readonly struct HeroPerkModifiers
    {
        public static readonly HeroPerkModifiers None = new HeroPerkModifiers(1f, 1f);

        public HeroPerkModifiers(float rangedIntervalMultiplier, float reloadMultiplier)
        {
            RangedIntervalMultiplier = rangedIntervalMultiplier;
            ReloadMultiplier = reloadMultiplier;
        }

        /// <summary>Scales the time between two shots of the ranged weapon.</summary>
        public float RangedIntervalMultiplier { get; }

        /// <summary>Scales the time the ranged weapon needs to reload.</summary>
        public float ReloadMultiplier { get; }

        public static HeroPerkModifiers From(IEnumerable<HeroPerkDefinition> perks)
        {
            float interval = 1f;
            float reload = 1f;
            if (perks != null)
            {
                foreach (HeroPerkDefinition perk in perks)
                {
                    if (perk != null)
                    {
                        interval *= perk.RangedIntervalMultiplier;
                        reload *= perk.ReloadMultiplier;
                    }
                }
            }

            return new HeroPerkModifiers(interval, reload);
        }

        /// <summary>Seconds per shot of a gun over a whole magazine and its reload: what its damage per second follows.</summary>
        public static float EffectiveInterval(float interval, int magazine, float reloadSeconds)
        {
            return magazine > 0 ? (magazine * interval + reloadSeconds) / magazine : interval;
        }
    }
}
