using System;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The hand-set balance numbers the generator writes, in one pure file the BalanceLab tool reads as well: the
    /// difficulty profiles, what each level pays between levels, and the tower prices. Kill money is not here: it follows
    /// from each creature's threat (<see cref="Threat.Reward"/>). Wave toughness is not here either: the solver sets it.
    /// Docs/BALANCE.md explains how the numbers were chosen.
    /// </summary>
    public static class BalanceSheet
    {
        public static readonly BalanceRules Rules = BalanceRules.Default;

        /// <summary>A difficulty profile: style of the waves (health, numbers, tempo, bite) and what it pays.</summary>
        public readonly struct Difficulty
        {
            public readonly string Id;
            public readonly float Health;
            public readonly float Count;
            public readonly float Interval;
            public readonly float Research;
            public readonly float Power;
            public readonly float StructureDamage;

            public Difficulty(string id, float health, float count, float interval, float research, float power, float structureDamage)
            {
                Id = id;
                Health = health;
                Count = count;
                Interval = interval;
                Research = research;
                Power = power;
                StructureDamage = structureDamage;
            }
        }

        // Research and power multipliers come from the rules (Easy pays a fifth of all research upgrades over the campaign,
        // Psycho all of them; power only from Medium on). Health, numbers and tempo are tuned so each difficulty lands near
        // its share of the Medium pressure (BalanceRules.DifficultyPressure) in the ledger.
        public static Difficulty[] Difficulties => new[]
        {
            new Difficulty("Easy", 0.68f, 0.8f, 1.2f, Rules.ResearchMultipliers[0], Rules.PowerMultipliers[0], 0.6f),
            new Difficulty("Medium", 1f, 1f, 1f, Rules.ResearchMultipliers[1], Rules.PowerMultipliers[1], 0.75f),
            new Difficulty("Hard", 1.15f, 1.2f, 0.85f, Rules.ResearchMultipliers[2], Rules.PowerMultipliers[2], 1f),
            new Difficulty("Psycho", 1.65f, 1.5f, 0.7f, Rules.ResearchMultipliers[3], Rules.PowerMultipliers[3], 1.4f),
        };

        /// <summary>The campaign in play order, by level key.</summary>
        public static readonly string[] CampaignOrder = { "Landing", "Forest", "Lava", "Ruins", "Nest", "Ice", "Swamp" };

        /// <summary>
        /// Base research of a level: grows by <see cref="BalanceRules.ResearchGrowthPerLevel"/> per level and sums, over the
        /// campaign, to the Easy share of every research upgrade, so Easy everywhere pays a fifth and Psycho everywhere all.
        /// </summary>
        public static int Research(string levelKey, int researchUpgradeCost)
        {
            int index = Array.IndexOf(CampaignOrder, levelKey);
            if (index < 0)
            {
                return 0;
            }

            double growth = 1.0 + Rules.ResearchGrowthPerLevel;
            double weights = (Math.Pow(growth, CampaignOrder.Length) - 1.0) / (growth - 1.0);
            double total = researchUpgradeCost * Rules.EasyResearchShare;
            return (int)Math.Round(total * Math.Pow(growth, index) / weights, MidpointRounding.AwayFromZero);
        }

        /// <summary>Base power of a level: a fixed part plus a part per boss wave on Medium (bosses pay power).</summary>
        public static int Power(WaveScriptSpec script)
        {
            int bosses = 0;
            foreach (BossSpec boss in script.Bosses)
            {
                if (boss.MinDifficulty <= WaveSolver.SolveDifficulty)
                {
                    bosses++;
                }
            }

            return Rules.PowerPerLevel + Rules.PowerPerBoss * bosses;
        }

        /// <summary>
        /// Tower level prices (build, then each upgrade). Each upgrade buys at least as much damage per passing creature per
        /// coin as the level before it, so upgrading always pays; the long-range and single-target guns are priced by
        /// damage times range, not damage alone. A tower not listed keeps the price the generator gives it.
        /// </summary>
        public static int[] Prices(string towerId)
        {
            switch (towerId)
            {
                case "PulseRifle":
                    return new[] { 50, 60, 90 };
                case "Cannon":
                    return new[] { 100, 120, 180 };
                case "Rocket":
                    return new[] { 120, 140, 200 };
                case "Laser":
                    return new[] { 130, 90, 170 };
                case "Tesla":
                    return new[] { 140, 115, 190 };
                case "Railgun":
                    return new[] { 220, 150, 255 };
                default:
                    return null;
            }
        }
    }
}
