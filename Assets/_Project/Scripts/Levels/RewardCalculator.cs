using Game.Economy;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Works out and applies the reward for a finished level. Higher difficulties pay more.</summary>
    public static class RewardCalculator
    {
        public static LevelResult Compute(LevelDefinition level, DifficultyProfile difficulty, ProgressService progress, bool won, float baseHealthFraction)
        {
            if (!won)
            {
                return new LevelResult { Won = false };
            }

            LevelReward reward = level.Reward;
            bool firstClear = !progress.IsLevelCompleted(level.Id);
            bool giveWeapon = firstClear && reward.NewTower != null && !progress.IsTowerUnlocked(reward.NewTower.Id);

            // Points come once per level per difficulty, so replaying cannot farm them; stars can still improve.
            bool earnedBefore = progress.IsCompleted(level.Id, difficulty.name);

            return new LevelResult
            {
                Won = true,
                Stars = StarsFor(level, baseHealthFraction),
                RewardsAlreadyEarned = earnedBefore,
                ResearchPoints = earnedBefore ? 0 : Mathf.RoundToInt(reward.ResearchPoints * difficulty.RewardMultiplier),
                PowerCurrency = earnedBefore ? 0 : Mathf.RoundToInt(reward.PowerCurrency * difficulty.PowerCurrencyMultiplier),
                NewTower = giveWeapon ? reward.NewTower : null,
                Source = giveWeapon ? reward.Source : UnlockSource.None,
                Story = giveWeapon ? reward.Story : null,
            };
        }

        /// <summary>One star for winning, one more for each threshold the remaining base health reaches.</summary>
        public static int StarsFor(LevelDefinition level, float baseHealthFraction)
        {
            int stars = 1;
            foreach (float threshold in level.StarBaseHealthFractions)
            {
                if (baseHealthFraction >= threshold)
                {
                    stars++;
                }
            }

            return Mathf.Min(stars, 3);
        }

        public static void Apply(LevelResult result, LevelDefinition level, DifficultyProfile difficulty, ProgressService progress)
        {
            if (!result.Won)
            {
                return;
            }

            progress.AddRewards(result.ResearchPoints, result.PowerCurrency);
            progress.MarkCompleted(level.Id, difficulty.name);
            progress.RecordStars(level.Id, difficulty.name, result.Stars);
            if (result.NewTower != null)
            {
                progress.UnlockTower(result.NewTower.Id);
            }

            progress.Save();
        }
    }
}
