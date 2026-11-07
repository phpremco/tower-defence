using System;

namespace Game.Balance
{
    /// <summary>What one wave's creatures that got past the defence cost the base (paper).</summary>
    public struct LeakLine
    {
        /// <summary>Health of the walkers and bosses that reach the base, and of the flyers that reach the end.</summary>
        public float GroundHealth;
        public float AirHealth;

        /// <summary>What they take off the base, scaled by the rules' <see cref="BalanceRules.LeakFactor"/>.</summary>
        public float BaseDamage;
    }

    /// <summary>
    /// The leak model (round 22b, the curator's decision on round 22): a wave over pressure one is not a lost level. The share
    /// of its creatures the towers and the hero do not stop walks on, stands at the base's doorstep and hammers the base with
    /// its structure damage until the guns that reach the doorstep and the hero have killed it; flyers that reach the end hit
    /// the base once. A plan holds when the base survives every wave; the stars follow from the base health left. The one
    /// factor the game sets (what the model leaves out: the base turret, hero abilities, repairs) is fitted on recorded runs.
    /// </summary>
    public static class Leak
    {
        /// <summary>Share of a wave that gets past the defence at a pressure: nothing at one or below, all of it with no defence.</summary>
        public static float Share(float pressure) => pressure <= 1f ? 0f : float.IsInfinity(pressure) ? 1f : 1f - 1f / pressure;

        /// <summary>
        /// The leak of a wave at its ground and air pressure. Each leaked creature hammers the base with its strike for the rules'
        /// <see cref="BalanceRules.LeakFactor"/> seconds (the measured life of a leaker at the doorstep), at most the rules' attackers at once.
        /// </summary>
        public static LeakLine Of(WaveLoad load, BalanceRules rules, float groundPressure, float airPressure)
        {
            var leak = new LeakLine();
            float health = load.StreamHealth + load.BossHealth;
            leak.GroundHealth = health * Share(groundPressure);
            leak.AirHealth = load.AirHealth * Share(airPressure);
            float damage = 0f;
            if (leak.GroundHealth > 0f)
            {
                int creatures = load.GroundCount + load.BossCount;
                float mean = creatures > 0 ? health / creatures : health;
                float strike = creatures > 0 ? (load.GroundStrike * load.GroundCount + load.BossStrike * load.BossCount) / creatures : 0f;
                damage += Siege(leak.GroundHealth, mean, strike, rules);
            }

            if (leak.AirHealth > 0f)
            {
                // Flyers lay siege like walkers: they hit gates and the base.
                damage += Siege(leak.AirHealth, load.MeanAirHealth, load.AirStrike, rules);
            }

            leak.BaseDamage = damage * rules.LeakFactor;
            return leak;
        }

        // Structure damage per second a leak of this health does to the base: at most the rules' attackers hit at once.
        private static float Siege(float leakHealth, float meanHealth, float strike, BalanceRules rules) =>
            Math.Min(leakHealth / Math.Max(1f, meanHealth), rules.AttackersPerGate) * strike;

        /// <summary>Stars for a base share left, as the game awards them: one for the win, one per threshold reached.</summary>
        public static int Stars(float baseLeft, float baseStart, float[] thresholds)
        {
            if (baseLeft <= 0f)
            {
                return 0;
            }

            int stars = 1;
            float fraction = baseStart > 0f ? baseLeft / baseStart : 0f;
            foreach (float threshold in thresholds)
            {
                stars += fraction >= threshold ? 1 : 0;
            }

            return Math.Min(3, stars);
        }

        /// <summary>
        /// The score a run or a plan is ranked by, the same on paper and in play: stars, base left and coins for a win; for a
        /// loss, how far it got.
        /// </summary>
        public static float Score(bool won, int stars, float baseLeft, int coins, int waveCount, int waveReached) =>
            won ? stars * 1000f + baseLeft + 0.1f * coins : -100f * Math.Max(0, waveCount - waveReached) - 1f;
    }
}
