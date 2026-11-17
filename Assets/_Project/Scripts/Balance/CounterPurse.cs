using System;

namespace Game.Balance
{
    /// <summary>
    /// The purse rule behind "can the first counter tower be paid in time" (round 76's affordability check, round 77's
    /// first-flyer rule): the wave the counter must stand in is the trait's first wave plus the grace waves of the difficulty
    /// (<see cref="GrowthWindow.GraceWaves"/>), and the counter is payable when the bounties of every wave before that one,
    /// through the difficulty's income share, reach its price. One definition for the check on the level data
    /// (Game.Levels.CounterAffordability) and for the wave solver that moves the first flyers until it holds
    /// (<see cref="CampaignModel.BuildWaves"/>), so the generator and the validator cannot disagree. Pure.
    /// </summary>
    public static class CounterPurse
    {
        // Guards the floor against a sum like 119.99999 that the wallet's running remainder would have paid as 120.
        private const double IncomeEpsilon = 1e-4;

        /// <summary>The grace waves of a difficulty (Easy first); a difficulty past the table takes its last entry.</summary>
        public static int GraceWaves(int difficulty) => GrowthWindow.GraceWaves[Math.Min(difficulty, GrowthWindow.GraceWaves.Length - 1)];

        /// <summary>The wave (1-based) at whose start the counter must stand: the trait's first wave plus the grace waves, inside the level.</summary>
        public static int DeadlineWave(int firstWave, int difficulty, int waveCount) => Math.Min(firstWave + GraceWaves(difficulty), waveCount);

        /// <summary>
        /// What a sum of bounties pays through the income share. The wallet carries the remainder of every bounty over, so the
        /// total is the floor of the scaled sum.
        /// </summary>
        public static int KillCoins(long bounty, double income) => (int)Math.Floor(bounty * income + IncomeEpsilon);

        /// <summary>True when the bounties alone pay for the counter, so a player who spent the opening elsewhere still gets it in time.</summary>
        public static bool PayableFromBounties(int killCoins, int price) => killCoins >= price;
    }
}
