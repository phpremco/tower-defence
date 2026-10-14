using Game.Economy;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Works out and applies the reward for a finished level. Higher difficulties pay more.</summary>
    public static class RewardCalculator
    {
        public static LevelResult Compute(LevelDefinition level, DifficultyProfile difficulty, ProgressService progress, bool won)
        {
            if (!won)
            {
                return new LevelResult { Won = false };
            }

            LevelReward reward = level.Reward;
            bool firstClear = !progress.IsLevelCompleted(level.Id);
            bool giveWeapon = firstClear && reward.NewTower != null && !progress.IsTowerUnlocked(reward.NewTower.Id);

            return new LevelResult
            {
                Won = true,
                ResearchPoints = Mathf.RoundToInt(reward.ResearchPoints * difficulty.RewardMultiplier),
                PowerCurrency = Mathf.RoundToInt(reward.PowerCurrency * difficulty.PowerCurrencyMultiplier),
                NewTower = giveWeapon ? reward.NewTower : null,
                Source = giveWeapon ? reward.Source : UnlockSource.None,
                Story = giveWeapon ? reward.Story : null,
            };
        }

        public static void Apply(LevelResult result, LevelDefinition level, DifficultyProfile difficulty, ProgressService progress)
        {
            if (!result.Won)
            {
                return;
            }

            progress.AddRewards(result.ResearchPoints, result.PowerCurrency);
            progress.MarkCompleted(level.Id, difficulty.name);
            if (result.NewTower != null)
            {
                progress.UnlockTower(result.NewTower.Id);
            }

            progress.Save();
        }
    }
}
