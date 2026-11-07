namespace Game.Balance
{
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

    /// <summary>
    /// The roadmap's finale plan as pure data (round 22f; it lived on the editor's BalanceSheet, but the PlayMode finale probe
    /// needs it too): the six continent bosses with their counterplay, the towers handed out before the Crystal Plains finale
    /// (STORY_ROADMAP S5 and "Later": Tesla and Railgun in Crystal Plains 7-20, Cryo back early in continent 2; the curator's
    /// working assumption, not built content), and the continent's research budget the finale player has spent by level 20.
    /// </summary>
    public static class FinalePlan
    {
        /// <summary>
        /// Crystal Plains' research budget: its six towers' research upgrades (6 x 96) plus base, gates and commander (172).
        /// The later continents get their budgets with their content.
        /// </summary>
        public const int CrystalPlainsResearchBudget = 748;

        public static readonly PlannedTower[] PlannedTowers =
        {
            new PlannedTower("Tesla", 0, 9),
            new PlannedTower("Railgun", 0, 13),
            new PlannedTower("Cryo", 1, 4),
        };

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
    }
}
