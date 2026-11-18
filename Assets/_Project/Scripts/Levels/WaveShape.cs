using System.Collections.Generic;
using Game.Balance;

namespace Game.Levels
{
    /// <summary>
    /// The wave grammar's bounds on a stored wave (round 79, Docs/WAVE_GRAMMAR.md), as the scheduler will play it on its
    /// difficulty: a breather never becomes dead air and a wave never drags on. Which spawn points a wave uses is free (round 81:
    /// any main paths with any surprise points; a surprise the level wrote may be one creature). The level validator runs it on every wave. Pure timing, no scene objects.
    /// </summary>
    public static class WaveShape
    {
        /// <summary>Seconds from the wave's start to its last spawn, without jitter.</summary>
        public static float Span(WaveDefinition wave, DifficultyProfile difficulty) => Times(wave, difficulty, out float span, out _) ? span : 0f;

        /// <summary>The longest silence between two spawns of the wave, in seconds, without jitter.</summary>
        public static float LongestPause(WaveDefinition wave, DifficultyProfile difficulty) => Times(wave, difficulty, out _, out float pause) ? pause : 0f;

        /// <summary>
        /// The creatures the wave sends out of the level's nests on this difficulty, all nests together: the wave's surprise
        /// (a rift opens on a surprise wave only). The groups may come out of several nests at once.
        /// </summary>
        public static int RiftHeads(WaveDefinition wave, DifficultyProfile difficulty)
        {
            int heads = 0;
            foreach (SpawnGroup group in wave.Groups)
            {
                heads += group.Source == SpawnSource.Nest && group.Enemy != null ? WaveScheduler.CountOf(group, difficulty) : 0;
            }

            return heads;
        }

        /// <summary>Adds an error for every bound the wave breaks.</summary>
        public static void Check(string name, WaveDefinition wave, DifficultyProfile difficulty, List<string> errors)
        {
            if (!Times(wave, difficulty, out float span, out float pause))
            {
                return;
            }

            if (span > WaveGrammarRules.MaxWaveSeconds)
            {
                errors.Add($"Wave '{name}' spawns for {span:0} s: longer than the grammar's {WaveGrammarRules.MaxWaveSeconds:0} s.");
            }

            if (pause > WaveGrammarRules.MaxPauseSeconds)
            {
                errors.Add($"Wave '{name}' has {pause:0.#} s of silence between two spawns: a breather is at most {WaveGrammarRules.MaxPauseSeconds:0} s.");
            }
        }

        private static bool Times(WaveDefinition wave, DifficultyProfile difficulty, out float span, out float pause)
        {
            span = 0f;
            pause = 0f;
            if (wave == null || difficulty == null || wave.Groups.Count == 0)
            {
                return false;
            }

            var scheduler = new WaveScheduler(wave, difficulty);
            var times = new List<float>();
            for (int g = 0; g < scheduler.GroupCount; g++)
            {
                for (int k = 0; k < scheduler.Total(g); k++)
                {
                    times.Add(scheduler.PlannedTime(g, k));
                }
            }

            times.Sort();
            for (int i = 1; i < times.Count; i++)
            {
                pause = System.Math.Max(pause, times[i] - times[i - 1]);
            }

            span = times.Count > 0 ? times[times.Count - 1] : 0f;
            return times.Count > 0;
        }
    }
}
