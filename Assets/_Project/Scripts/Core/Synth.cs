using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A sample buffer and the operations that layer sound into it: tones with pitch sweeps, filtered noise,
    /// crackle, wind, drones, echo and soft clipping. Sound effects and music are both built on it.
    /// </summary>
    public sealed class Synth
    {
        public const int SampleRate = 22050;

        private const float Tau = Mathf.PI * 2f;

        private readonly System.Random _random;
        private float[] _buffer;

        public Synth(float seconds, int seed)
        {
            _buffer = new float[Mathf.CeilToInt(seconds * SampleRate)];
            _random = new System.Random(seed);
        }

        /// <summary>Length of the buffer in seconds.</summary>
        public float Seconds => _buffer.Length / (float)SampleRate;

        public float Chance() => (float)_random.NextDouble();

        /// <summary>A tone sliding from one pitch to another with a quick attack and a decay to silence.</summary>
        public void Tone(float at, float seconds, float startHz, float endHz, float volume, float attack, SynthWave wave, float sustain = 0f)
        {
            int from = Index(at);
            int count = Count(seconds);
            double phase = 0.0;
            for (int i = 0; i < count && from + i < _buffer.Length; i++)
            {
                float t = i / (float)count;
                float hz = startHz * Mathf.Pow(endHz / startHz, t);
                phase += hz / SampleRate;
                _buffer[from + i] += Oscillate(phase, wave) * volume * Envelope(t, attack, seconds, sustain);
            }
        }

        /// <summary>White noise through a low-pass filter whose cutoff slides, shaped by the same envelope.</summary>
        public void Noise(float at, float seconds, float volume, float attack, float startHz, float endHz, float curve = 2f)
        {
            int from = Index(at);
            int count = Count(seconds);
            float state = 0f;
            for (int i = 0; i < count && from + i < _buffer.Length; i++)
            {
                float t = i / (float)count;
                float cutoff = startHz * Mathf.Pow(endHz / startHz, t);
                float alpha = 1f - Mathf.Exp(-Tau * cutoff / SampleRate);
                state += alpha * (White() - state);
                _buffer[from + i] += state * volume * Envelope(t, attack, seconds, 0f, curve);
            }
        }

        /// <summary>Bright unfiltered noise with a fast decay: hi-hats, hiss and sand.</summary>
        public void Hiss(float at, float seconds, float volume, float curve = 3f)
        {
            int from = Index(at);
            int count = Count(seconds);
            float previous = 0f;
            for (int i = 0; i < count && from + i < _buffer.Length; i++)
            {
                float t = i / (float)count;
                // The difference of two samples is a crude high-pass, which keeps the hiss out of the bass.
                float white = White();
                _buffer[from + i] += (white - previous) * 0.5f * volume * Mathf.Pow(1f - t, curve);
                previous = white;
            }
        }

        /// <summary>Sparse random clicks that die out over the window, like embers and debris.</summary>
        public void Crackle(float at, float seconds, float volume, float density)
        {
            int from = Index(at);
            int count = Count(seconds);
            const int ClickLength = 40;
            for (int i = 0; i < count && from + i < _buffer.Length; i++)
            {
                if (Chance() > density)
                {
                    continue;
                }

                float strength = (0.3f + 0.7f * Chance()) * (1f - i / (float)count);
                for (int k = 0; k < ClickLength && from + i + k < _buffer.Length; k++)
                {
                    _buffer[from + i + k] += White() * volume * strength * (1f - k / (float)ClickLength);
                }
            }
        }

        /// <summary>Filtered noise whose loudness and colour wander slowly, the whole clip long.</summary>
        public void Wind(float volume, float cutoffHz, float depth, float lfoHz)
        {
            float state = 0f;
            float phase = Chance() * Tau;
            float phase2 = Chance() * Tau;
            for (int i = 0; i < _buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float gust = 0.5f + 0.5f * Mathf.Sin(Tau * lfoHz * t + phase) * Mathf.Sin(Tau * lfoHz * 0.37f * t + phase2);
                float cutoff = cutoffHz * (1f + depth * (gust - 0.5f));
                float alpha = 1f - Mathf.Exp(-Tau * cutoff / SampleRate);
                state += alpha * (White() - state);
                _buffer[i] += state * volume * (0.55f + 0.45f * gust);
            }
        }

        /// <summary>A steady hum that swells and fades slowly, for caverns and ruins.</summary>
        public void Drone(float hz, float volume, float lfoHz)
        {
            float phase = Chance() * Tau;
            for (int i = 0; i < _buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float swell = 0.6f + 0.4f * Mathf.Sin(Tau * lfoHz * t + phase);
                _buffer[i] += (Mathf.Sin(Tau * hz * t) + 0.3f * Mathf.Sin(Tau * hz * 2f * t)) * volume * swell;
            }
        }

        /// <summary>A low tone pulsing like a heartbeat.</summary>
        public void Throb(float hz, float beatHz, float volume)
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Tau * beatHz * t)), 6f);
                _buffer[i] += Mathf.Sin(Tau * hz * t) * volume * beat;
            }
        }

        /// <summary>A slow swell and fall of low rumble over the window, like the ground shifting.</summary>
        public void Rumble(float at, float seconds, float volume, float cutoffHz)
        {
            int from = Index(at);
            int count = Count(seconds);
            float state = 0f;
            float alpha = 1f - Mathf.Exp(-Tau * cutoffHz / SampleRate);
            for (int i = 0; i < count && from + i < _buffer.Length; i++)
            {
                float t = i / (float)count;
                state += alpha * (White() - state);
                float swell = Mathf.Sin(t * Mathf.PI);
                float tremor = 0.7f + 0.3f * Mathf.Sin(Tau * 9f * t * seconds);
                _buffer[from + i] += state * volume * swell * tremor;
            }
        }

        /// <summary>Repeats the sound a few times, quieter each time, to give it room and weight.</summary>
        public void Echo(float delaySeconds, float feedback, int taps)
        {
            int delay = Count(delaySeconds);
            float[] source = (float[])_buffer.Clone();
            for (int tap = 1; tap <= taps; tap++)
            {
                float gain = Mathf.Pow(feedback, tap);
                int offset = delay * tap;
                for (int i = offset; i < _buffer.Length; i++)
                {
                    _buffer[i] += source[i - offset] * gain;
                }
            }
        }

        /// <summary>Soft clipping: louder parts flatten, which adds grit without harsh digital edges.</summary>
        public void Distort(float drive)
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                float x = _buffer[i] * drive;
                _buffer[i] = x / (1f + Mathf.Abs(x));
            }
        }

        /// <summary>Rolls off the top end, so a distorted layer sits behind the others instead of fizzing.</summary>
        public void LowPass(float cutoffHz)
        {
            float alpha = 1f - Mathf.Exp(-Tau * cutoffHz / SampleRate);
            float state = 0f;
            for (int i = 0; i < _buffer.Length; i++)
            {
                state += alpha * (_buffer[i] - state);
                _buffer[i] = state;
            }
        }

        /// <summary>Blends the head of the clip into its tail so it can loop without a click, and trims the overlap.</summary>
        public void Loopable(float fadeSeconds)
        {
            int fade = Count(fadeSeconds);
            int length = _buffer.Length - fade;
            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                float sample = _buffer[i];
                if (i < fade)
                {
                    float w = i / (float)fade;
                    sample = _buffer[length + i] * (1f - w) + sample * w;
                }

                result[i] = sample;
            }

            _buffer = result;
        }

        public void Normalize(float peak)
        {
            float max = 0f;
            for (int i = 0; i < _buffer.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(_buffer[i]));
            }

            if (max <= 0f)
            {
                return;
            }

            float gain = peak / max;
            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] *= gain;
            }
        }

        public AudioClip ToClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, _buffer.Length, 1, SampleRate, false);
            clip.SetData(_buffer, 0);
            return clip;
        }

        private float White() => (float)(_random.NextDouble() * 2.0 - 1.0);

        private static int Index(float seconds) => Mathf.RoundToInt(seconds * SampleRate);

        private static int Count(float seconds) => Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate));

        // A short ramp in, then either a hold (sustain) with a quick release or a curved fall to silence.
        private static float Envelope(float t, float attackSeconds, float seconds, float sustain, float curve = 2f)
        {
            float attackShare = seconds > 0f ? Mathf.Clamp01(attackSeconds / seconds) : 0f;
            float rise = attackShare > 0f ? Mathf.Clamp01(t / attackShare) : 1f;
            if (sustain > 0f)
            {
                float release = Mathf.Clamp01((1f - t) / Mathf.Max(0.0001f, 1f - sustain));
                return rise * Mathf.Min(1f, release);
            }

            return rise * Mathf.Pow(1f - t, curve);
        }

        private static float Oscillate(double phase, SynthWave wave)
        {
            float cycle = (float)(phase - System.Math.Floor(phase));
            switch (wave)
            {
                case SynthWave.Saw:
                    // A soft saw: the pure ramp plus a sine to round off the teeth.
                    return (cycle * 2f - 1f) * 0.7f + 0.3f * Mathf.Sin(cycle * Tau);
                case SynthWave.Square:
                    return (cycle < 0.5f ? 0.7f : -0.7f) + 0.3f * Mathf.Sin(cycle * Tau);
                default:
                    return Mathf.Sin(cycle * Tau);
            }
        }
    }
}
