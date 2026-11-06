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

            /// <summary>Extra health of a continent boss on top of <see cref="Health"/> (Docs/BALANCE.md, "Boss health per difficulty").</summary>
            public readonly float BossHealth;

            public Difficulty(string id, string displayName, float health, float count, float interval, float research, float power, float structureDamage, float income, float startMoney, float bossHealth)
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
                BossHealth = bossHealth;
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

        // In-level money per difficulty. First pass (2026-11-04, Remco on C1_01 Easy: "je krijgt gewoon te veel geld"): Easy
        // 85% income and 80% start, which took a competent player from 2.3-2.6x to 1.75-2.0x the firepower the plain waves need.
        //
        // 2026-11-04, "alles wat zwaarder maken": the waves are now calibrated to a human target curve (BalanceRules.HumanSurplus),
        // so these shares no longer set how hard a difficulty is; they set what it feels like at that hardness. Less money
        // means fewer guns against weaker creatures, more money more guns against tougher ones, and more creatures per wave
        // pay for themselves (kill money follows the crowd). Easy: 80% income, 80% start, 90% of the crowd (was 85/80/80):
        // fewer guns than before against bigger, weak crowds. Medium: 95% income. Hard and Psycho: bigger crowds (1.3 and
        // 1.65, were 1.2 and 1.5) and 10% more income, so their creatures stay tougher than Medium's at the same pressure
        // (at full income Hard's creatures on the first levels came out weaker than Medium's).
        private const float EasyIncome = 0.8f;
        private const float EasyStartMoney = 0.8f;
        private const float MediumIncome = 0.95f;
        private const float HardIncome = 1.1f;
        private const float PsychoIncome = 1.1f;

        // 2026-11-05, Remco: boss health per difficulty. One health for all difficulties left the continent boss alone asking
        // 0.6-0.65 of the Hard/Psycho hero cap against 0.96 on Medium (FinaleModel): there the plain waves were the climax, not
        // the boss. These shares put a continent boss on Hard and Psycho at about Medium's share of its cap (ContinentFinaleTests
        // derives and checks them). Easy keeps one: the model would ask about 1.2, and Easy is meant to forgive.
        // Round 21a: with the end-game Psycho player's cheaper upgrades and the roles by enemy (plating, the Gun Turret's half
        // damage in the air) the derived Psycho share fell from 1.7 to about 1.1-1.15; round 21a2 (the Cannon reloads slower)
        // moved it again: Hard about 1.76, Psycho about 1.49.
        private const float EasyBossHealth = 1f;
        private const float MediumBossHealth = 1f;
        private const float HardBossHealth = 1.75f;
        private const float PsychoBossHealth = 1.5f;

        // Research and power multipliers come from the rules (Easy pays a fifth of a continent's research budget, Psycho all
        // of it; power only from Medium on). Health, numbers and tempo shape each difficulty before the calibration trims its
        // waves to the human curve (CampaignModel.BuildWaves). The asset ids stay; only Psycho shows another name.
        public static Difficulty[] Difficulties => new[]
        {
            new Difficulty("Easy", "Easy", 0.68f, 0.9f, 1.2f, Rules.ResearchMultipliers[0], Rules.PowerMultipliers[0], 0.6f, EasyIncome, EasyStartMoney, EasyBossHealth),
            new Difficulty("Medium", "Medium", 1f, 1f, 1f, Rules.ResearchMultipliers[1], Rules.PowerMultipliers[1], 0.75f, MediumIncome, 1f, MediumBossHealth),
            new Difficulty("Hard", "Hard", 1.15f, 1.3f, 0.85f, Rules.ResearchMultipliers[2], Rules.PowerMultipliers[2], 1f, HardIncome, 1f, HardBossHealth),
            new Difficulty("Psycho", "Crazy", 1.65f, 1.65f, 0.7f, Rules.ResearchMultipliers[3], Rules.PowerMultipliers[3], 1.4f, PsychoIncome, 1f, PsychoBossHealth),
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

        /// <summary>
        /// The roadmap's tower unlocks after Crystal Plains 6 (STORY_ROADMAP S5 and "Later"), for the finale check before those
        /// levels exist: Tesla and Railgun in Crystal Plains 7-20, Cryo back early in continent 2. The levels are the curator's
        /// working assumption (one new gun a few levels apart), not built content; the Acid Sprayer, the continent-3 combos and
        /// the continent-5 superweapons have no data yet and are not counted.
        /// </summary>
        public static readonly PlannedTower[] PlannedTowers =
        {
            new PlannedTower("Tesla", 0, 9),
            new PlannedTower("Railgun", 0, 13),
            new PlannedTower("Cryo", 1, 4),
        };

        /// <summary>One continent finale: its boss and what its counterplay needs, each with the continent it arrives in.</summary>
        public readonly struct ContinentFinale
        {
            public ContinentFinale(string boss, int continent, params (string What, int Continent, bool Built)[] counters)
            {
                Boss = boss;
                Continent = continent;
                Counters = counters;
            }

            /// <summary>The boss's enemy asset name.</summary>
            public string Boss { get; }

            /// <summary>Index of the continent whose level 20 it ends.</summary>
            public int Continent { get; }

            /// <summary>The tools its Codex counterplay names, the continent each is first owned in, and whether it exists as data.</summary>
            public (string What, int Continent, bool Built)[] Counters { get; }
        }

        /// <summary>The six continent bosses at their finales (Docs/BOSSES.md) and the tools their counterplay relies on.</summary>
        public static readonly ContinentFinale[] Finales =
        {
            new ContinentFinale("Gravemaw", 0, ("Mine", 0, true), ("spread towers", 0, true)),
            new ContinentFinale("CinderMatriarch", 1, ("Cryo (bait)", 1, true), ("long road without cold towers", 0, true)),
            new ContinentFinale("FrostWarden", 2, ("LavaBall (fire cracks the armour)", 0, true), ("hero Repair thaws a gate", 0, true)),
            new ContinentFinale("Rotbloom", 3, ("Cannon (splash burns pads)", 0, true), ("Rocket/Flak (shoot pods)", 0, true), ("hero Repair burns vines", 0, true)),
            new ContinentFinale("Shattered", 4, ("Rocket/Flak (air shard)", 0, true), ("towers on the second road", 0, true)),
            new ContinentFinale("SealedOne", 5, ("three or four tower types", 0, true)),
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
        /// Tower level prices (build, then each upgrade). Remco's rule (2026-11-06): an upgrade must beat more towers of the same
        /// type at equal coins on single-target DPS, so every level's DPS per coin spent to reach it is at least about 5% above
        /// the level below (asserted in TowerRolesTests); more towers still win on targets and spread the risk. Round 21a cut
        /// the upgrade steps that missed it (Pulse, Cannon, Laser, Tesla, Railgun; the Flak also hits harder). A tower not listed
        /// keeps the price the generator gives it.
        /// </summary>
        public static int[] Prices(string towerId)
        {
            switch (towerId)
            {
                case "PulseRifle":
                    return new[] { 50, 50, 90 };
                case "Cannon":
                    return new[] { 100, 110, 180 };
                case "Rocket":
                    return new[] { 120, 100, 150 };
                case "Laser":
                    return new[] { 130, 75, 145 };
                case "Tesla":
                    return new[] { 140, 100, 155 };
                case "Railgun":
                    return new[] { 220, 125, 200 };
                default:
                    return null;
            }
        }
    }
}
