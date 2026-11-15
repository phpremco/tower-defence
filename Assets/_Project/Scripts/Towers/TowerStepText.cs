using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// What the next upgrade step of a tower is called and what it changes, in the words the tower menu shows
    /// ("Faster cycling: shots/s 2.9 > 6.0"). Read from the tower's data: the step's name and the one thing it improves.
    /// </summary>
    public static class TowerStepText
    {
        /// <summary>The line for the step that leads from a level to the next; empty at the top level.</summary>
        public static string Next(TowerDefinition tower, int levelIndex)
        {
            if (tower == null || levelIndex >= tower.MaxLevelIndex)
            {
                return string.Empty;
            }

            TowerLevel now = tower.GetLevel(levelIndex);
            TowerLevel next = tower.GetLevel(levelIndex + 1);
            return $"{next.StepName}: {Change(tower, now, next)}";
        }

        private static string Change(TowerDefinition tower, TowerLevel now, TowerLevel next)
        {
            switch (next.Step)
            {
                case TowerStep.Damage:
                    return $"damage {now.Damage:0.#} > {next.Damage:0.#}";
                case TowerStep.FireRate:
                    return $"shots/s {1f / now.FireInterval:0.0} > {1f / next.FireInterval:0.0}";
                case TowerStep.Range:
                    return $"range {now.Range:0.0} > {next.Range:0.0}";
                case TowerStep.Slow:
                    return $"slows to {Percent(Slow(tower, now))} > {Percent(Slow(tower, next))} speed";
                case TowerStep.Support:
                    return $"boost x{Scale(now):0.#} > x{Scale(next):0.#}";
                default:
                    return Refit(now, next);
            }
        }

        // A structure's refit changes several things: each that moves is listed.
        private static string Refit(TowerLevel now, TowerLevel next)
        {
            string text = string.Empty;
            if (!Mathf.Approximately(now.Damage, next.Damage))
            {
                text += $"damage {now.Damage:0.#} > {next.Damage:0.#}   ";
            }

            if (!Mathf.Approximately(now.Range, next.Range))
            {
                text += $"range {now.Range:0.0} > {next.Range:0.0}   ";
            }

            if (!Mathf.Approximately(now.FireInterval, next.FireInterval))
            {
                text += $"shots/s {1f / now.FireInterval:0.0} > {1f / next.FireInterval:0.0}   ";
            }

            if (next.HealthBonus > 0f)
            {
                text += $"+{next.HealthBonus:0} HP";
            }

            return text.TrimEnd();
        }

        private static float Slow(TowerDefinition tower, TowerLevel level) => level.SlowFactor > 0f ? level.SlowFactor : tower.SlowFactor;

        private static float Scale(TowerLevel level) => level.SupportScale > 0f ? level.SupportScale : 1f;

        private static string Percent(float share) => $"{Mathf.RoundToInt(share * 100f)}%";
    }
}
