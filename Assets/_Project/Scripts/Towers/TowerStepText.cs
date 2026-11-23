using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// What the next upgrade step of a tower is called and what it changes, in the words the tower menu shows
    /// ("Faster cycling: shots/s 2.9 > 6.0"). Read from the tower's data: the step's name and the one thing it improves.
    /// </summary>
    public static class TowerStepText
    {
        private const string RefitGap = "   ";
        private const string BlastGap = ", ";

        /// <summary>The line for the step that leads from a level to the next; empty at the top level.</summary>
        public static string Next(TowerDefinition tower, int levelIndex)
        {
            if (tower == null || levelIndex >= tower.MaxLevelIndex)
            {
                return string.Empty;
            }

            TowerLevel now = tower.GetLevel(levelIndex);
            TowerLevel next = tower.GetLevel(levelIndex + 1);
            // A blast that hurts more creatures at the next level says so beside the step's own thing (round 88b).
            string blast = BlastRead.Step(tower, levelIndex);
            // So does the health a star adds to a gun (round 88g); a refit lists its health itself.
            string health = next.Step != TowerStep.Refit && next.HealthBonus > 0f ? BlastGap + Loc.Format("tower.step.health", next.HealthBonus) : string.Empty;
            return $"{Loc.Get(next.StepName)}: {Change(tower, now, next)}{(blast.Length > 0 ? BlastGap + blast : string.Empty)}{health}";
        }

        private static string Change(TowerDefinition tower, TowerLevel now, TowerLevel next)
        {
            switch (next.Step)
            {
                case TowerStep.Damage:
                    return Loc.Format("tower.step.damage", now.Damage, next.Damage);
                case TowerStep.FireRate:
                    return Loc.Format("tower.step.fire_rate", 1f / now.FireInterval, 1f / next.FireInterval);
                case TowerStep.Range:
                    return Loc.Format("tower.step.range", now.Range, next.Range);
                case TowerStep.Slow:
                    return Loc.Format("tower.step.slow", Percent(Slow(tower, now)), Percent(Slow(tower, next)));
                case TowerStep.Support:
                    return Loc.Format("tower.step.boost", Scale(now), Scale(next));
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
                text += Loc.Format("tower.step.damage", now.Damage, next.Damage) + RefitGap;
            }

            if (!Mathf.Approximately(now.Range, next.Range))
            {
                text += Loc.Format("tower.step.range", now.Range, next.Range) + RefitGap;
            }

            if (!Mathf.Approximately(now.FireInterval, next.FireInterval))
            {
                text += Loc.Format("tower.step.fire_rate", 1f / now.FireInterval, 1f / next.FireInterval) + RefitGap;
            }

            if (next.HealthBonus > 0f)
            {
                text += Loc.Format("tower.step.health", next.HealthBonus);
            }

            return text.TrimEnd();
        }

        private static float Slow(TowerDefinition tower, TowerLevel level) => level.SlowFactor > 0f ? level.SlowFactor : tower.SlowFactor;

        private static float Scale(TowerLevel level) => level.SupportScale > 0f ? level.SupportScale : 1f;

        private static string Percent(float share) => $"{Mathf.RoundToInt(share * 100f)}%";
    }
}
