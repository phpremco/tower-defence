using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The research economy of the permanent upgrades over the continents (round 66, Remco: ten tiers, opened per continent).
    /// Every upgrade has up to ten tiers; a continent opens them up to its cap (<see cref="BalanceRules.UpgradeTiersPerContinent"/>),
    /// for its own upgrades from the first tier and for the earlier continents' upgrades from where the last cap stopped. What
    /// a continent opens costs a sum; its research budget (what Psycho alone pays, <see cref="ContinentEconomy"/>) is a share of
    /// that sum (<see cref="BalanceRules.ResearchBudgetShare"/>), so one difficulty never buys everything and all four together
    /// pay it with a little to spare. Pure numbers: the generator prices the upgrades with it, the tests and the report read it.
    /// </summary>
    public static class UpgradeEconomy
    {
        /// <summary>The prices of an upgrade's tiers from the price of its first: the rules' cost curve, rounded per tier.</summary>
        public static int[] TierCosts(float firstCost, int tiers, BalanceRules rules)
        {
            float[] curve = rules.UpgradeTierCostCurve;
            var costs = new int[Math.Min(tiers, curve.Length)];
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = (int)Math.Round(firstCost * curve[i], MidpointRounding.AwayFromZero);
            }

            return costs;
        }

        /// <summary>What the tiers above <paramref name="fromTier"/> up to and including <paramref name="toTier"/> of an upgrade cost.</summary>
        public static int Cost(UpgradeStats upgrade, int fromTier, int toTier)
        {
            int sum = 0;
            for (int i = Math.Max(0, fromTier); i < toTier && i < upgrade.Costs.Length; i++)
            {
                sum += upgrade.Costs[i];
            }

            return sum;
        }

        /// <summary>
        /// The cost of every tier a continent opens, in research or in power: its own upgrades up to its cap, and the tiers
        /// of the earlier continents' upgrades between the cap before and its own. Dormant upgrades (continent -1) count nowhere.
        /// </summary>
        public static int Opened(IReadOnlyList<UpgradeStats> upgrades, int continent, bool power, BalanceRules rules)
        {
            int cap = rules.UpgradeTierCap(continent);
            int before = continent > 0 ? rules.UpgradeTierCap(continent - 1) : 0;
            int sum = 0;
            foreach (UpgradeStats upgrade in upgrades)
            {
                if (upgrade.PaidInPower != power || upgrade.Continent < 0 || upgrade.Continent > continent)
                {
                    continue;
                }

                sum += Cost(upgrade, upgrade.Continent == continent ? 0 : before, cap);
            }

            return sum;
        }

        /// <summary>The research budget the rule gives a continent that opens this much.</summary>
        public static int ResearchBudget(int opened, BalanceRules rules) =>
            (int)Math.Round(opened * rules.ResearchBudgetShare, MidpointRounding.AwayFromZero);

        /// <summary>What one clear of every level of a continent with this budget pays on a difficulty.</summary>
        public static int Earned(int budget, int difficulty, BalanceRules rules)
        {
            float multiplier = rules.ResearchMultipliers[Math.Min(difficulty, rules.ResearchMultipliers.Length - 1)];
            int sum = 0;
            for (int i = 0; i < rules.LevelsPerContinent; i++)
            {
                sum += (int)Math.Round(ContinentEconomy.Research(budget, i, rules) * multiplier, MidpointRounding.AwayFromZero);
            }

            return sum;
        }

        /// <summary>One continent of the projection.</summary>
        public sealed class Line
        {
            public int Continent;
            public int TierCap;

            /// <summary>Research cost of every tier this continent opens.</summary>
            public int Opened;
            public int Budget;

            /// <summary>Research one clear of all its levels pays, Easy to Psycho.</summary>
            public int[] Earned = new int[0];

            public int EarnedOnEveryDifficulty;
            public int OpenedSoFar;
            public int EarnedSoFarOnEveryDifficulty;

            /// <summary>The highest tier every upgrade open so far reaches when a path's research is spread evenly, per difficulty path (Easy, Medium, Medium+Hard, all four).</summary>
            public int[] EvenTier = new int[0];
        }

        /// <summary>
        /// The curve over the continents. The first continents use the real upgrades; a continent whose own upgrades do not
        /// exist yet is given <paramref name="plannedFirstTierCost"/> (the summed first-tier research price of what is planned
        /// to arrive there), priced with the same curve. <paramref name="budgets"/> pins a continent's budget where it is set
        /// (above zero); the others follow the share rule.
        /// </summary>
        public static List<Line> Project(IReadOnlyList<UpgradeStats> upgrades, IReadOnlyList<int> budgets, IReadOnlyList<int> plannedFirstTierCost, BalanceRules rules)
        {
            var all = new List<UpgradeStats>(upgrades);
            int continents = rules.UpgradeTiersPerContinent.Length;
            for (int c = 0; c < continents; c++)
            {
                bool hasOwn = false;
                foreach (UpgradeStats upgrade in upgrades)
                {
                    hasOwn |= upgrade.Continent == c && !upgrade.PaidInPower;
                }

                int planned = plannedFirstTierCost != null && c < plannedFirstTierCost.Count ? plannedFirstTierCost[c] : 0;
                if (!hasOwn && planned > 0)
                {
                    all.Add(new UpgradeStats { Id = "Planned_C" + (c + 1), Continent = c, Costs = TierCosts(planned, rules.UpgradeTierCostCurve.Length, rules) });
                }
            }

            var lines = new List<Line>();
            int openedSoFar = 0;
            int earnedSoFar = 0;
            var pathSoFar = new int[rules.ResearchMultipliers.Length];
            for (int c = 0; c < continents; c++)
            {
                int opened = Opened(all, c, power: false, rules);
                int budget = budgets != null && c < budgets.Count && budgets[c] > 0 ? budgets[c] : ResearchBudget(opened, rules);
                var line = new Line { Continent = c, TierCap = rules.UpgradeTierCap(c), Opened = opened, Budget = budget, Earned = new int[rules.ResearchMultipliers.Length] };
                for (int d = 0; d < line.Earned.Length; d++)
                {
                    line.Earned[d] = Earned(budget, d, rules);
                    line.EarnedOnEveryDifficulty += line.Earned[d];
                }

                openedSoFar += opened;
                earnedSoFar += line.EarnedOnEveryDifficulty;
                line.OpenedSoFar = openedSoFar;
                line.EarnedSoFarOnEveryDifficulty = earnedSoFar;
                line.EvenTier = new int[pathSoFar.Length];
                for (int path = 0; path < pathSoFar.Length; path++)
                {
                    foreach (int d in Progression.PathOf(path))
                    {
                        pathSoFar[path] += line.Earned[d];
                    }

                    line.EvenTier[path] = EvenTier(all, c, pathSoFar[path], rules);
                }

                lines.Add(line);
            }

            return lines;
        }

        // The highest tier T such that every research upgrade open by this continent can stand at min(T, its own tiers) for the purse.
        private static int EvenTier(List<UpgradeStats> upgrades, int continent, int purse, BalanceRules rules)
        {
            int cap = rules.UpgradeTierCap(continent);
            int reached = 0;
            for (int tier = 1; tier <= cap; tier++)
            {
                int cost = 0;
                foreach (UpgradeStats upgrade in upgrades)
                {
                    if (!upgrade.PaidInPower && upgrade.Continent >= 0 && upgrade.Continent <= continent)
                    {
                        cost += Cost(upgrade, 0, tier);
                    }
                }

                if (cost > purse)
                {
                    break;
                }

                reached = tier;
            }

            return reached;
        }
    }
}
