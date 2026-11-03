using System.Collections.Generic;
using Game.Economy;
using Game.Hero;
using Game.PathObjects;
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
            HeroPerkDefinition perk = firstClear && reward.HeroPerk != null && !progress.IsHeroPerkUnlocked(reward.HeroPerk.Id) ? reward.HeroPerk : null;

            // Points come once per level per difficulty, so replaying cannot farm them; stars can still improve.
            bool earnedBefore = progress.IsCompleted(level.Id, difficulty.name);

            PathObjectDefinition[] gadgets = firstClear ? NewGadgets(reward.NewPathObjects, progress) : new PathObjectDefinition[0];
            bool unlocksAnything = giveWeapon || gadgets.Length > 0 || perk != null;
            return new LevelResult
            {
                Won = true,
                Credit = unlocksAnything && !string.IsNullOrEmpty(reward.Credit) ? reward.Credit : null,
                Stars = StarsFor(level, baseHealthFraction),
                RewardsAlreadyEarned = earnedBefore,
                ResearchPoints = earnedBefore ? 0 : Mathf.RoundToInt(reward.ResearchPoints * difficulty.RewardMultiplier),
                PowerCurrency = earnedBefore ? 0 : Mathf.RoundToInt(reward.PowerCurrency * difficulty.PowerCurrencyMultiplier),
                NewTower = giveWeapon ? reward.NewTower : null,
                Source = giveWeapon ? reward.Source : UnlockSource.None,
                Story = giveWeapon ? reward.Story : null,
                NewPathObjects = gadgets,
                HeroPerk = perk,
            };
        }

        private static PathObjectDefinition[] NewGadgets(PathObjectDefinition[] granted, ProgressService progress)
        {
            var result = new List<PathObjectDefinition>();
            if (granted == null)
            {
                return result.ToArray();
            }

            foreach (PathObjectDefinition gadget in granted)
            {
                if (gadget != null && !progress.IsPathObjectUnlocked(gadget.name))
                {
                    result.Add(gadget);
                }
            }

            return result.ToArray();
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

            foreach (PathObjectDefinition gadget in result.NewPathObjects)
            {
                progress.UnlockPathObject(gadget.name);
            }

            if (result.HeroPerk != null)
            {
                progress.UnlockHeroPerk(result.HeroPerk.Id);
            }

            progress.Save();
        }
    }
}
