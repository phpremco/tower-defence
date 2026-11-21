using System;

namespace Game.Levels
{
    /// <summary>
    /// The coins a level starts with on a difficulty (round 88d): the level's starting money times the difficulty's
    /// multiplier, plus, on a level whose waves on that difficulty hold an air raid, the price of the sky gun the level
    /// sells times the difficulty's factor (<see cref="DifficultyProfile.RaidStartGuns"/>). The coins for the air
    /// defence are in the purse from the first second instead of arriving in the middle of the level; a player who
    /// spends them on ground guns has chosen so. Easy and Medium carry a factor of zero: there the first-flyer rule
    /// already holds the flyers back until the bounties have paid for the gun.
    /// The one place this sum is made: the level bootstrapper, the balance model's level facts, the affordability
    /// check, the labs and the tests all read it here, so the game and the model cannot drift apart.
    /// </summary>
    public static class StartMoneyRule
    {
        /// <summary>Everything the wallet holds when the level starts.</summary>
        public static int For(LevelDefinition level, DifficultyProfile difficulty) => Base(level, difficulty) + RaidAllowance(level, difficulty);

        /// <summary>The level's starting money through the difficulty's multiplier, rounded to even as Mathf.RoundToInt does.</summary>
        public static int Base(LevelDefinition level, DifficultyProfile difficulty) =>
            (int)Math.Round(level.StartingMoney * difficulty.StartingMoneyMultiplier, MidpointRounding.ToEven);

        /// <summary>The coins for the air defence: zero on a level without an air raid on this difficulty, or where the difficulty pays none.</summary>
        public static int RaidAllowance(LevelDefinition level, DifficultyProfile difficulty)
        {
            WaveDefinition[] waves = level.GetWaves(difficulty);
            if (waves == null || difficulty.RaidStartGuns <= 0f || FirstRaidRule.FirstRaid(waves) == FirstRaidRule.None)
            {
                return 0;
            }

            return (int)Math.Round(FirstRaidRule.SkyGunPrice(level.AvailableTowers) * difficulty.RaidStartGuns, MidpointRounding.AwayFromZero);
        }
    }
}
