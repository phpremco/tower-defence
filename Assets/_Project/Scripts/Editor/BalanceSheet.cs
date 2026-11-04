using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The hand-set balance numbers the generator writes, in one pure file the BalanceLab tool reads as well: the
    /// difficulty profiles, what each continent's levels pay between levels, and the tower prices. Kill money is not here:
    /// it follows from each creature's threat (<see cref="Threat.Reward"/>). Wave toughness is not here either: the solver
    /// sets it. Docs/BALANCE.md explains how the numbers were chosen.
    /// </summary>
    public static class BalanceSheet
    {
        public static readonly BalanceRules Rules = BalanceRules.Default;

        /// <summary>A difficulty profile: style of the waves (health, numbers, tempo, bite) and what it pays.</summary>
        public readonly struct Difficulty
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly float Health;
            public readonly float Count;
            public readonly float Interval;
            public readonly float Research;
            public readonly float Power;
            public readonly float StructureDamage;

            /// <summary>In-level income (bounties, harvested ore) as a share of what creatures and mines pay.</summary>
            public readonly float Income;

            /// <summary>Starting money as a share of the level's own.</summary>
            public readonly float StartMoney;

            public Difficulty(string id, string displayName, float health, float count, float interval, float research, float power, float structureDamage, float income, float startMoney)
            {
                Id = id;
                DisplayName = displayName;
                Health = health;
                Count = count;
                Interval = interval;
                Research = research;
                Power = power;
                StructureDamage = structureDamage;
                Income = income;
                StartMoney = startMoney;
            }
        }

        /// <summary>What one continent pays out over its twenty levels.</summary>
        public readonly struct Continent
        {
            public readonly string Key;

            /// <summary>Research all of the continent pays on Psycho; Easy pays the Easy share of it.</summary>
            public readonly int ResearchBudget;

            /// <summary>Power all of the continent pays on Psycho.</summary>
            public readonly int PowerBudget;

            public Continent(string key, int researchBudget, int powerBudget)
            {
                Key = key;
                ResearchBudget = researchBudget;
                PowerBudget = powerBudget;
            }
        }

        // Easy's in-level money (2026-11-04, Remco on C1_01 Easy: "je krijgt gewoon te veel geld"): with the full purse a
        // competent player (BalanceRules.HumanUtilisation) bought 2.3 to 2.6 times the firepower the plain waves need and
        // never needed the commander. At 85% income and 80% starting money that is 1.75 to 2.0 times, still clearly more
        // than Medium (1.45 to 1.75): Easy asks about 0.8 of Medium instead of 0.6 to 0.75. The creatures are not touched:
        // the solver sizes them against the full purse (CampaignModel.ReferenceInputFor).
        private const float EasyIncome = 0.85f;
        private const float EasyStartMoney = 0.8f;

        // Research and power multipliers come from the rules (Easy pays a fifth of a continent's research budget, Psycho all
        // of it; power only from Medium on). Health, numbers and tempo are tuned so each difficulty lands near its share of
        // the Medium pressure (BalanceRules.DifficultyPressure) in the ledger. The asset ids stay; only Psycho shows another name.
        public static Difficulty[] Difficulties => new[]
        {
            new Difficulty("Easy", "Easy", 0.68f, 0.8f, 1.2f, Rules.ResearchMultipliers[0], Rules.PowerMultipliers[0], 0.6f, EasyIncome, EasyStartMoney),
            new Difficulty("Medium", "Medium", 1f, 1f, 1f, Rules.ResearchMultipliers[1], Rules.PowerMultipliers[1], 0.75f, 1f, 1f),
            new Difficulty("Hard", "Hard", 1.15f, 1.2f, 0.85f, Rules.ResearchMultipliers[2], Rules.PowerMultipliers[2], 1f, 1f, 1f),
            new Difficulty("Psycho", "Crazy", 1.65f, 1.5f, 0.7f, Rules.ResearchMultipliers[3], Rules.PowerMultipliers[3], 1.4f, 1f, 1f),
        };

        /// <summary>
        /// The continents' budgets, by region index. Crystal Plains: its six towers' research upgrades (6 x 96) plus base,
        /// gates and commander (172) make 748 research points, and every drone upgrade makes 186 power. The later
        /// continents get their budgets with their content (the upgrades tagged to them).
        /// </summary>
        public static readonly Continent[] Continents =
        {
            new Continent("C1", 748, 186),
            new Continent("C2", 0, 0),
            new Continent("C3", 0, 0),
            new Continent("C4", 0, 0),
            new Continent("C5", 0, 0),
            new Continent("C6", 0, 0),
        };

        /// <summary>Base research of the level at an index of a continent (see <see cref="ContinentEconomy.Research"/>).</summary>
        public static int Research(int continent, int index) =>
            continent >= 0 && continent < Continents.Length ? ContinentEconomy.Research(Continents[continent].ResearchBudget, index, Rules) : 0;

        /// <summary>Base power of a level: one share per level and more per boss wave on Medium (bosses pay power).</summary>
        public static int Power(int continent, WaveScriptSpec script)
        {
            if (continent < 0 || continent >= Continents.Length)
            {
                return 0;
            }

            int bosses = 0;
            foreach (BossSpec boss in script.Bosses)
            {
                if (boss.MinDifficulty <= WaveSolver.SolveDifficulty)
                {
                    bosses++;
                }
            }

            return ContinentEconomy.Power(Continents[continent].PowerBudget, bosses, Rules);
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
