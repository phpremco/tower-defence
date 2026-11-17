using System;
using System.Collections.Generic;
using Game.Balance;
using Game.Core;
using Game.Enemies;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// The affordability of the first counter tower, from the level data alone (Docs/BALANCE_CRITIC_2026-11-17b.md, item 3):
    /// for a level, a difficulty and a counter trait (flyers, plated creatures), the coins a player can hold by the wave the
    /// counter must stand in (the trait's first wave plus the grace window of <see cref="GrowthWindow"/>) against the price of
    /// the cheapest owned tower made for the trait. Coins are what the game pays without any choice of the player: the
    /// starting money and the bounty of every creature of the waves before that one, through the difficulty's income share
    /// (<see cref="Game.Economy.Wallet.Earn"/>). The game pays no bonus for a cleared wave; the early-call bonus and a
    /// harvest drone's ore are the player's own doing and are left out. Every creature is taken as killed, so the coins are
    /// an upper bound. No bot, no balance model.
    /// </summary>
    public static class CounterAffordability
    {
        /// <summary>The traits a player answers with a tower of its own.</summary>
        public static readonly EnemyTraits[] CounterTraits = { EnemyTraits.Airborne, EnemyTraits.Armoured };

        /// <summary>How a cell and trait read.</summary>
        public enum Verdict
        {
            /// <summary>The bounties before the deadline pay for the counter on their own.</summary>
            Payable = 0,

            /// <summary>Only with starting money kept back: a player who spent the opening cannot earn the counter in time.</summary>
            NeedsStartingMoney = 1,

            /// <summary>Not even the starting money and every bounty together reach the price in time.</summary>
            OutOfReach = 2,

            /// <summary>The player of this cell owns no tower made for the trait.</summary>
            NoCounterOwned = 3,
        }

        /// <summary>One cell and trait of the table.</summary>
        public readonly struct Row
        {
            public Row(string level, int difficulty, EnemyTraits trait, int firstWave, int deadlineWave, int startingMoney, int killCoins, string counterId, int price)
            {
                Level = level;
                Difficulty = difficulty;
                Trait = trait;
                FirstWave = firstWave;
                DeadlineWave = deadlineWave;
                StartingMoney = startingMoney;
                KillCoins = killCoins;
                CounterId = counterId;
                Price = price;
            }

            public string Level { get; }

            public int Difficulty { get; }

            public EnemyTraits Trait { get; }

            /// <summary>The first wave (1-based) that sends a creature with the trait (bosses aside).</summary>
            public int FirstWave { get; }

            /// <summary>The wave (1-based) at whose start the counter must stand: the first wave plus the grace waves.</summary>
            public int DeadlineWave { get; }

            public int StartingMoney { get; }

            /// <summary>The bounties of every wave before <see cref="DeadlineWave"/>, through the income share.</summary>
            public int KillCoins { get; }

            /// <summary>The cheapest owned tower made for the trait; empty when none is owned.</summary>
            public string CounterId { get; }

            public int Price { get; }

            public Verdict Verdict =>
                string.IsNullOrEmpty(CounterId) ? Verdict.NoCounterOwned
                : KillCoins >= Price ? Verdict.Payable
                : StartingMoney + KillCoins >= Price ? Verdict.NeedsStartingMoney
                : Verdict.OutOfReach;

            /// <summary>The share of the starting money the counter takes when the bounties fall short (0 when they pay for it).</summary>
            public float StartingMoneyShare => StartingMoney > 0 ? Math.Max(0, Price - KillCoins) / (float)StartingMoney : 0f;
        }

        /// <summary>The rows of a level on a difficulty: one per counter trait its waves send.</summary>
        public static List<Row> Rows(LevelDefinition level, DifficultyProfile difficulty, int difficultyIndex, IReadOnlyList<string> ownedTowerIds)
        {
            var rows = new List<Row>();
            WaveDefinition[] waves = level != null && difficulty != null ? level.GetWaves(difficulty) : null;
            if (waves == null)
            {
                return rows;
            }

            int start = StartingMoney(level, difficulty);
            foreach (EnemyTraits trait in CounterTraits)
            {
                int first = FirstWave(waves, trait);
                if (first == 0)
                {
                    continue;
                }

                int grace = GrowthWindow.GraceWaves[Math.Min(difficultyIndex, GrowthWindow.GraceWaves.Length - 1)];
                int deadline = Math.Min(first + grace, waves.Length);
                TowerDefinition counter = CheapestCounter(level, ownedTowerIds, trait);
                rows.Add(new Row(level.name, difficultyIndex, trait, first, deadline, start, KillCoins(waves, difficulty, deadline - 1), counter != null ? counter.Id : string.Empty, counter != null ? counter.BuildCost : 0));
            }

            return rows;
        }

        /// <summary>The rows of every level of the campaign's first <paramref name="nodes"/> nodes on every difficulty.</summary>
        public static List<Row> Rows(CampaignCatalog catalog, IReadOnlyList<string> startingTowerIds, int nodes)
        {
            var rows = new List<Row>();
            for (int node = 0; node < nodes && node < catalog.Nodes.Count; node++)
            {
                LevelDefinition level = catalog.Nodes[node].Level;
                for (int d = 0; level != null && d < catalog.Difficulties.Count; d++)
                {
                    rows.AddRange(Rows(level, catalog.Difficulties[d], d, OwnedTowers(catalog, startingTowerIds, node, d)));
                }
            }

            return rows;
        }

        /// <summary>The warning for a row, or null when the bounties pay for the counter in time.</summary>
        public static string Warning(Row row)
        {
            string cell = $"{row.Level} difficulty {row.Difficulty}, {row.Trait} from wave {row.FirstWave}";
            switch (row.Verdict)
            {
                case Verdict.NoCounterOwned:
                    return $"{cell}: the player of this cell owns no tower made for it.";
                case Verdict.OutOfReach:
                    return $"{cell}: the {row.CounterId} ({row.Price}) cannot be paid by wave {row.DeadlineWave}: starting money {row.StartingMoney} and bounties {row.KillCoins} together fall short.";
                case Verdict.NeedsStartingMoney:
                    return $"{cell}: the {row.CounterId} ({row.Price}) is payable by wave {row.DeadlineWave} only from the starting money ({row.StartingMoney}; the bounties until then pay {row.KillCoins}): a player who spent the opening on other guns cannot earn it in time.";
                default:
                    return null;
            }
        }

        /// <summary>The coins a level starts with on a difficulty, as the level bootstrapper rounds them.</summary>
        public static int StartingMoney(LevelDefinition level, DifficultyProfile difficulty) =>
            (int)Math.Round(level.StartingMoney * difficulty.StartingMoneyMultiplier, MidpointRounding.ToEven);

        /// <summary>The bounties of the first <paramref name="waveCount"/> waves through the income share, every creature killed.</summary>
        public static int KillCoins(WaveDefinition[] waves, DifficultyProfile difficulty, int waveCount)
        {
            long bounty = 0;
            for (int w = 0; w < waveCount && w < waves.Length; w++)
            {
                foreach (SpawnGroup group in waves[w].Groups)
                {
                    bounty += group.Enemy != null ? (long)WaveScheduler.CountOf(group, difficulty) * group.Enemy.Reward : 0;
                }
            }

            // The wallet carries the remainder of every bounty over, so the total is the floor of the scaled sum.
            return (int)Math.Floor(bounty * (double)difficulty.IncomeMultiplier + IncomeEpsilon);
        }

        // Guards the floor against a sum like 119.99999 that the wallet's running remainder would have paid as 120.
        private const double IncomeEpsilon = 1e-4;

        /// <summary>The first wave (1-based) that sends a creature with the trait, bosses aside (a boss carries its own fight); 0 for none.</summary>
        public static int FirstWave(WaveDefinition[] waves, EnemyTraits trait)
        {
            for (int w = 0; w < waves.Length; w++)
            {
                foreach (SpawnGroup group in waves[w].Groups)
                {
                    if (group.Enemy != null && !group.Enemy.IsBoss && Has(group.Enemy, trait))
                    {
                        return w + 1;
                    }
                }
            }

            return 0;
        }

        /// <summary>Whether a creature carries a counter trait as the field shows it: it flies, or it walks in armour.</summary>
        public static bool Has(EnemyDefinition enemy, EnemyTraits trait) =>
            trait == EnemyTraits.Airborne ? enemy.Flies : trait == EnemyTraits.Armoured && enemy.HasArmor && !enemy.Flies;

        /// <summary>
        /// The towers the player of a cell owns: the starting set, the reward of every level before, and the level's own reward
        /// when a player of the difficulty has cleared it on a lower one first (<see cref="Progression.OwnLevelPathOf"/>).
        /// </summary>
        public static List<string> OwnedTowers(CampaignCatalog catalog, IReadOnlyList<string> startingTowerIds, int node, int difficulty)
        {
            var owned = new List<string>(startingTowerIds);
            int last = Progression.OwnLevelPathOf(difficulty).Length > 0 ? node : node - 1;
            for (int i = 0; i <= last && i < catalog.Nodes.Count; i++)
            {
                TowerDefinition reward = catalog.Nodes[i].Level != null ? catalog.Nodes[i].Level.Reward.NewTower : null;
                if (reward != null && !owned.Contains(reward.Id))
                {
                    owned.Add(reward.Id);
                }
            }

            return owned;
        }

        private static TowerDefinition CheapestCounter(LevelDefinition level, IReadOnlyList<string> ownedTowerIds, EnemyTraits trait)
        {
            TowerDefinition counter = null;
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower == null || tower.IsSupport || !Contains(ownedTowerIds, tower.Id) || !Answers(tower, trait))
                {
                    continue;
                }

                if (counter == null || tower.BuildCost < counter.BuildCost)
                {
                    counter = tower;
                }
            }

            return counter;
        }

        private static bool Answers(TowerDefinition tower, EnemyTraits trait)
        {
            foreach (TowerRole role in tower.Roles)
            {
                if ((CombatRules.Answers(role) & trait) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
