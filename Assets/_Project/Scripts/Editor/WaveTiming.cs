using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The clock of a level's waves as the scheduler plays them: when each wave starts, how long the level runs, the mean wave
    /// spacing. The drone lens and the drone price rule (round 22d) read it, so a drone's payback is measured in the level's own time.
    /// </summary>
    public static class WaveTiming
    {
        /// <summary>Seconds after the last creature spawns until a level is over, for the lens's end-of-level reckoning.</summary>
        public const float ClearSeconds = 30f;

        /// <summary>Start time of every wave from the level's clock, and the level's end.</summary>
        public static List<float> Starts(LevelDefinition level, DifficultyProfile difficulty, out float end)
        {
            var starts = new List<float>();
            float clock = 0f;
            WaveDefinition[] waves = level.GetWaves(difficulty) ?? new WaveDefinition[0];
            foreach (WaveDefinition wave in waves)
            {
                clock += wave.StartDelay;
                starts.Add(clock);
                clock += Span(wave, difficulty);
            }

            end = clock + ClearSeconds;
            return starts;
        }

        /// <summary>Seconds from a wave's first spawn to its last.</summary>
        public static float Span(WaveDefinition wave, DifficultyProfile difficulty)
        {
            var scheduler = new WaveScheduler(wave, difficulty);
            float span = 0f;
            for (int g = 0; g < scheduler.GroupCount; g++)
            {
                int total = scheduler.Total(g);
                if (total > 0)
                {
                    span = Mathf.Max(span, scheduler.PlannedTime(g, total - 1));
                }
            }

            return span;
        }

        /// <summary>Mean time from one wave's start to the next; a one-wave level counts its whole run.</summary>
        public static float MeanWaveSeconds(LevelDefinition level, DifficultyProfile difficulty)
        {
            List<float> starts = Starts(level, difficulty, out float end);
            return starts.Count > 1 ? (starts[starts.Count - 1] - starts[0]) / (starts.Count - 1) : end;
        }
    }
}
