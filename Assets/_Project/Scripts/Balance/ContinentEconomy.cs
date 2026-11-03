using System;

namespace Game.Balance
{
    /// <summary>
    /// What a level pays between levels, per continent (Docs/BALANCE.md, "end game per continent"). Each continent has a
    /// research budget and a power budget. Easy everywhere in a continent pays the Easy share of its research budget and
    /// Psycho everywhere all of it (research multipliers 1/2/3/5); Psycho everywhere pays its power budget. Research rises
    /// gently over the continent and its finale pays double; power pays per level and more per boss wave. Weights are
    /// fixed by a level's index against the full continent, so adding later levels never changes an earlier payout.
    /// </summary>
    public static class ContinentEconomy
    {
        /// <summary>Research weight of the level at a 0-based index of its continent.</summary>
        public static double ResearchWeight(int index, BalanceRules rules)
        {
            int last = rules.LevelsPerContinent - 1;
            if (index >= last)
            {
                return rules.ContinentFinaleWeight;
            }

            return 1.0 + rules.ResearchCurveRise * Math.Max(0, index) / Math.Max(1, last - 1);
        }

        public static double ResearchWeightSum(BalanceRules rules)
        {
            double sum = 0.0;
            for (int i = 0; i < rules.LevelsPerContinent; i++)
            {
                sum += ResearchWeight(i, rules);
            }

            return sum;
        }

        /// <summary>Base (Easy) research of the level at an index of a continent with the given research budget.</summary>
        public static int Research(int budget, int index, BalanceRules rules)
        {
            if (budget <= 0)
            {
                return 0;
            }

            double value = rules.EasyResearchShare * budget * ResearchWeight(index, rules) / ResearchWeightSum(rules);
            return Math.Max(rules.MinResearchPerLevel, (int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>Base power of a level with this many Medium boss waves, in a continent with the given power budget.</summary>
        public static int Power(int budget, int mediumBosses, BalanceRules rules)
        {
            if (budget <= 0)
            {
                return 0;
            }

            float psycho = rules.PowerMultipliers[rules.PowerMultipliers.Length - 1];
            double perWeight = budget / psycho / (rules.LevelsPerContinent * (1.0 + rules.PowerPerBossWeight * rules.ExpectedBossesPerLevel));
            return (int)Math.Round(perWeight * (1.0 + rules.PowerPerBossWeight * mediumBosses), MidpointRounding.AwayFromZero);
        }
    }
}
