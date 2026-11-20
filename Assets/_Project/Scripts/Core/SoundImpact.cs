using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Where the blow of a recorded sound lies and when to start the recording so that blow lands on a moment on screen
    /// (round 88a, Remco: "the shockwave sound plays just after the shockwave"). A power sound swells before it hits; started
    /// at the moment of the wave, its hit came a quarter of a second late. The generator measures the blow per sound set
    /// (<see cref="SoundSet.Impact"/>); the players start the sound that much ahead of the moment, or, when the moment is
    /// already too near, start it part-way in. Pure.
    /// </summary>
    public static class SoundImpact
    {
        /// <summary>The blow is the first moment the sound reaches this share of its loudest window.</summary>
        public const float ImpactShare = 0.7f;

        /// <summary>The length of the windows the loudness is read in, in seconds.</summary>
        public const float WindowSeconds = 0.01f;

        /// <summary>
        /// Seconds into the recording at which it first reaches <see cref="ImpactShare"/> of its loudest window (interleaved
        /// samples); zero for a sound that hits at once and for silence.
        /// </summary>
        public static float Seconds(float[] samples, int channels, int frequency)
        {
            int window = Mathf.Max(1, Mathf.RoundToInt(frequency * WindowSeconds)) * Mathf.Max(1, channels);
            int count = samples.Length / window;
            if (count == 0)
            {
                return 0f;
            }

            var loudness = new double[count];
            double peak = 0d;
            for (int w = 0; w < count; w++)
            {
                double sum = 0d;
                for (int i = w * window; i < (w + 1) * window; i++)
                {
                    sum += samples[i] * samples[i];
                }

                loudness[w] = sum;
                peak = System.Math.Max(peak, sum);
            }

            if (peak <= 0d)
            {
                return 0f;
            }

            // Loudness is compared as amplitude (the root of the mean square), so the share reads as on a level meter.
            double threshold = peak * ImpactShare * ImpactShare;
            for (int w = 0; w < count; w++)
            {
                if (loudness[w] >= threshold)
                {
                    return w * WindowSeconds;
                }
            }

            return 0f;
        }

        /// <summary>
        /// How to start a sound whose blow lies <paramref name="impact"/> seconds into the recording (at playback speed one)
        /// so the blow is heard <paramref name="lead"/> seconds from now, on a device that plays <paramref name="latency"/>
        /// seconds behind: wait <paramref name="delay"/> first, or start <paramref name="skip"/> seconds into the recording
        /// when the moment is nearer than the swell is long. Never skips past the blow itself.
        /// </summary>
        public static void Plan(float impact, float pitch, float lead, float latency, out float delay, out float skip)
        {
            float speed = Mathf.Max(0.01f, pitch);
            float swell = Mathf.Max(0f, impact) / speed;
            float wait = Mathf.Max(0f, lead) - swell - Mathf.Max(0f, latency);
            delay = Mathf.Max(0f, wait);
            skip = Mathf.Min(Mathf.Max(0f, impact), Mathf.Max(0f, -wait) * speed);
        }
    }
}
