using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Simple generated sound effects, so the game has audio feedback before real sound files exist.
    /// Replace the clips with imported audio later by changing where SfxPlayer gets them.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 22050;

        public static AudioClip Shot() => Tone("Shot", 0.09f, 900f, 420f, 0.25f, 0f);

        public static AudioClip Kill() => Tone("Kill", 0.14f, 260f, 110f, 0.35f, 0.15f);

        public static AudioClip Explosion() => Tone("Explosion", 0.3f, 140f, 50f, 0.5f, 0.8f);

        public static AudioClip BaseHit() => Tone("BaseHit", 0.35f, 120f, 70f, 0.6f, 0.4f);

        public static AudioClip Build() => Tone("Build", 0.12f, 500f, 800f, 0.3f, 0f);

        /// <summary>A short decaying tone that slides from one pitch to another, with an optional share of noise.</summary>
        private static AudioClip Tone(string name, float seconds, float startHz, float endHz, float volume, float noise)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var samples = new float[count];
            var random = new System.Random(name.GetHashCode());
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(startHz, endHz, t);
                phase += frequency / SampleRate;
                float tone = Mathf.Sin(phase * Mathf.PI * 2f);
                float hiss = (float)(random.NextDouble() * 2.0 - 1.0);
                float envelope = (1f - t) * (1f - t);
                samples[i] = Mathf.Lerp(tone, hiss, noise) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
