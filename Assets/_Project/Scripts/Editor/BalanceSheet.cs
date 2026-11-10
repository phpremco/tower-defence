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

            /// <summary>Share of a continent boss's escort call released (round 30: Psycho carries Hard's boss, lighter escorts).</summary>
            public readonly float BossEscort;

            public Difficulty(string id, string displayName, float health, float count, float interval, float research, float power, float structureDamage, float income, float startMoney, float bossHealth, float bossEscort = 1f)
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
                BossEscort = bossEscort;
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
        // moved it again (Hard about 1.76, Psycho 1.49); round 21a3 (the ledger counts in-play efficiency) back to Hard about
        // 1.43, Psycho 1.78.
        // Round 29: 0.8 since the Easy finale player (138 research, 1266 money) lost to Rotbloom as the data stood (holds x0.92, also at a share of 0.9)
        // while beating every other boss; Easy forgives, and the stage reports Easy only.
        private const float EasyBossHealth = 0.8f;
        private const float MediumBossHealth = 1f;
        // Round 29 (2026-11-09): the finale probe with the boss powers derives Hard 1.80 and Psycho 1.05 (Psycho's finale
        // player meets heavier crowds and escorts, so its boss carries less). Round 30 (Remco): Psycho's share is never below
        // Hard's (BalanceRules.PsychoBossShareAtLeastHard); the Psycho finale player is given the means instead (start money).
        private const float HardBossHealth = 1.25f;
        private const float PsychoBossHealth = 1.25f;

        /// <summary>
        /// Remco 2026-11-10 (round 31, question 3): Hard and Psycho keep the designer's boss share of 1.25, lighter than the
        /// finale probe's Psycho ceiling (1.53), for now. The finale stage keeps it and reports the probe's figure as advice;
        /// ContinentFinaleTests asserts it. 0 hands the share back to the probe (round 30's rule).
        /// </summary>
        public static readonly float DesignerHardBossShare = HardBossHealth;

        // Round 30 (Remco): with Psycho's boss share at Hard's, the Psycho finale player (money about 1950 at level 20, like Hard's)
        // meets a boss body 1.7x heavier than before beside its heavier crowds; it gets more starting money rather than a lighter
        // boss. The finale probe sets the number (Docs/BALANCE.md "Round 30"); the impact stage shows what it does to the
        // campaign's Psycho cells.
        private const float PsychoStartMoney = 3f;

        // Round 30 (Remco): money may go to x3 at most; the Psycho finale player still lost to Gravemaw, the Matriarch and the
        // Sealed One at Hard's boss share (holds x0.85-1.0), so Psycho's escort calls release this share of their count (the
        // finale probe sets it; the boss itself stays Hard's).
        private const float PsychoBossEscort = 0.5f;

        // Research and power multipliers come from the rules (Easy pays a fifth of a continent's research budget, Psycho all
        // of it; power only from Medium on). Health, numbers and tempo shape each difficulty before the calibration trims its
        // waves to the human curve (CampaignModel.BuildWaves). The asset ids stay; only Psycho shows another name.
        public static Difficulty[] Difficulties => new[]
        {
            new Difficulty("Easy", "Easy", 0.68f, 0.9f, 1.2f, Rules.ResearchMultipliers[0], Rules.PowerMultipliers[0], 0.6f, EasyIncome, EasyStartMoney, EasyBossHealth),
            new Difficulty("Medium", "Medium", 1f, 1f, 1f, Rules.ResearchMultipliers[1], Rules.PowerMultipliers[1], 0.75f, MediumIncome, 1f, MediumBossHealth),
            new Difficulty("Hard", "Hard", 1.15f, 1.3f, 0.85f, Rules.ResearchMultipliers[2], Rules.PowerMultipliers[2], 1f, HardIncome, 1f, HardBossHealth),
            new Difficulty("Psycho", "Crazy", 1.65f, 1.65f, 0.7f, Rules.ResearchMultipliers[3], Rules.PowerMultipliers[3], 1.4f, PsychoIncome, PsychoStartMoney, PsychoBossHealth, PsychoBossEscort),
        };

        /// <summary>
        /// The continents' budgets, by region index. Crystal Plains: its six towers' research upgrades (6 x 96) plus base,
        /// gates and commander (172) make 748 research points, and every drone upgrade makes 186 power. The later
        /// continents get their budgets with their content (the upgrades tagged to them).
        /// </summary>
        public static readonly Continent[] Continents =
        {
            new Continent("C1", FinalePlan.CrystalPlainsResearchBudget, 186),
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
        public static PlannedTower[] PlannedTowers => FinalePlan.PlannedTowers;

        /// <summary>The six continent bosses at their finales and the tools their counterplay relies on (the data lives in FinalePlan since round 22f).</summary>
        public static ContinentFinale[] Finales => FinalePlan.Finales;

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

        /// <summary>Build prices of the guns the sheet prices; a tower not listed keeps the price the generator gives it.</summary>
        private static int BuildPrice(string towerId)
        {
            switch (towerId)
            {
                case "PulseRifle":
                    return 50;
                case "Cannon":
                    return 100;
                case "Rocket":
                    return 120;
                case "Laser":
                    return 130;
                case "Tesla":
                    return 140;
                case "Railgun":
                    return 220;
                default:
                    return 0;
            }
        }

        /// <summary>Upgrade steps are rounded down to this many coins.</summary>
        private const int PriceStep = 5;

        // ---- Drone prices (round 22d) --------------------------------------------------------------------------------------

        /// <summary>
        /// Remco 2026-11-07: the repair drone is an investment like the harvester, not a cheap convenience. Its price is what it
        /// saves in repair coins over <see cref="BalanceRules.RepairDroneBreakEvenWaves"/> waves: repair rate x the measured busy
        /// share (the calibration) x the repair cost per hit point (the cheapest gun's) x the mean wave spacing of the levels that
        /// sell it (Medium). Bought early it pays back many times over, mid-level still clearly, in the last two waves never.
        /// </summary>
        public static int RepairDronePrice(float repairPerSecond, float busyShare, float repairCostPerHealth, float waveSeconds, BalanceRules rules) =>
            System.Math.Max(PriceStep, (int)System.Math.Round(repairPerSecond * busyShare * repairCostPerHealth * waveSeconds * rules.RepairDroneBreakEvenWaves / PriceStep) * PriceStep);

        /// <summary>The price the asset carries (round 31): the designer's <see cref="BalanceRules.RepairDronePrice"/> when set, else the rule's.</summary>
        public static int ChosenRepairDronePrice(int rulePrice, BalanceRules rules) => rules.RepairDronePrice > 0 ? rules.RepairDronePrice : rulePrice;

        /// <summary>
        /// The repair drone's price from the data on disk: the calibration's busy share, the cheapest repair cost per hit point of the guns the
        /// levels that sell the drone offer, and their mean wave spacing on Medium; <paramref name="fallback"/> before any level exists.
        /// </summary>
        public static int RepairDronePriceFromData(float repairPerSecond, int fallback)
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
            var calibration = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BalanceCalibration>(Game.Levels.BalanceCalibration.AssetPath);
            return catalog != null && RepairDroneInputs(catalog, calibration, out float busy, out float costPerHealth, out float waveSeconds)
                ? ChosenRepairDronePrice(RepairDronePrice(repairPerSecond, busy, costPerHealth, waveSeconds, Rules), Rules)
                : fallback;
        }

        /// <summary>The rule's inputs from the data: false when no level sells a repair drone yet.</summary>
        public static bool RepairDroneInputs(Game.Levels.CampaignCatalog catalog, Game.Levels.BalanceCalibration calibration, out float busyShare, out float repairCostPerHealth, out float waveSeconds)
        {
            busyShare = calibration != null ? calibration.RepairDroneBusyShare : Rules.RepairDroneBusyShare;
            repairCostPerHealth = 0f;
            waveSeconds = 0f;
            int levels = 0;
            Game.Levels.DifficultyProfile medium = catalog.Difficulties.Count > AcceptanceRules.Medium ? catalog.Difficulties[AcceptanceRules.Medium] : null;
            foreach (Game.Levels.MapNode node in catalog.Nodes)
            {
                if (node.Level == null || medium == null || !System.Array.Exists(node.Level.Drones, d => d != null && d.Kind == Game.Drones.DroneKind.Repair))
                {
                    continue;
                }

                foreach (Game.Towers.TowerDefinition tower in node.Level.AvailableTowers)
                {
                    if (!tower.BlocksGroundEnemies && tower.RepairCostPerHealth > 0f && (repairCostPerHealth == 0f || tower.RepairCostPerHealth < repairCostPerHealth))
                    {
                        repairCostPerHealth = tower.RepairCostPerHealth;
                    }
                }

                waveSeconds += WaveTiming.MeanWaveSeconds(node.Level, medium);
                levels++;
            }

            if (levels == 0 || repairCostPerHealth <= 0f)
            {
                return false;
            }

            waveSeconds /= levels;
            return true;
        }

        /// <summary>Coins of repairs the drone saves per wave on a level: what the price is weighed against, wave by wave.</summary>
        public static float RepairDroneSavingPerWave(float repairPerSecond, float busyShare, float repairCostPerHealth, float waveSeconds) =>
            repairPerSecond * busyShare * repairCostPerHealth * waveSeconds;

        /// <summary>The last wave (1-based) a purchase still earns its price back before the level ends; 0 when it never does.</summary>
        public static int RepairDroneBreakEvenWave(int price, float savingPerWave, int waveCount) =>
            savingPerWave <= 0f ? 0 : System.Math.Max(0, waveCount - (int)System.Math.Ceiling(price / savingPerWave) + 1);


        /// <summary>
        /// Tower level prices (build, then each upgrade step) from the tower's DPS per level. Remco's rule (2026-11-06): an upgrade
        /// must beat more towers of the same type at equal coins on single-target DPS, by a clear margin (<see cref="BalanceRules.UpgradeMargin"/>,
        /// asserted in TowerRolesTests): the coins spent to reach a level buy at least margin times the DPS per coin of the level
        /// below, so every step's price follows from the DPS it buys and never has to be typed. More towers still win on targets
        /// and spread the risk. Null for a tower the sheet does not price.
        /// </summary>
        public static int[] Prices(string towerId, float[] dps)
        {
            int build = BuildPrice(towerId);
            if (build <= 0 || dps == null || dps.Length == 0)
            {
                return null;
            }

            var prices = new int[dps.Length];
            prices[0] = build;
            int total = build;
            for (int i = 1; i < dps.Length; i++)
            {
                float ratio = dps[i - 1] > 0f ? dps[i] / dps[i - 1] : 1f;
                int reach = (int)System.Math.Floor(total * ratio / Rules.UpgradeMargin / PriceStep) * PriceStep;
                prices[i] = System.Math.Max(PriceStep, reach - total);
                total += prices[i];
            }

            return prices;
        }
    }
}
