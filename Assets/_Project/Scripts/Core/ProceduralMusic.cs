using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Generated music, so the levels have a score before real tracks exist: a light rock loop (drums, bass, power
    /// chords, a sparse lead) for ordinary play, a faster, heavier loop for boss waves and a darker rock loop for
    /// the menus. All loop on a bar line.
    /// Replace them with imported tracks later by changing where MusicPlayer gets its clips.
    /// </summary>
    public static class ProceduralMusic
    {
        private const int Bars = 8;
        private const int BeatsPerBar = 4;
        private const float LoopFade = 0.4f;
        private const float A2 = 110f;

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        // Semitones from A2: the two chord progressions, one chord per bar, as (root, fifth) pairs.
        private static readonly int[] CalmRoots = { -5, -9, -2, -7, -5, -9, -7, -5 };
        private static readonly int[] IntenseRoots = { -5, -2, -7, -9, -5, -2, -9, -7 };

        // E minor pentatonic, two octaves above the bass, for the lead.
        private static readonly int[] Pentatonic = { 19, 22, 24, 26, 29, 31, 34 };

        public static AudioClip Calm() => Cached("MusicCalm", 100f, Calm);

        public static AudioClip Intense() => Cached("MusicIntense", 132f, Intense);

        private static void Calm(Track t)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                float root = Hz(CalmRoots[bar]);
                for (int beat = 0; beat < BeatsPerBar; beat++)
                {
                    float at = t.At(bar, beat);
                    if (beat == 0 || beat == 2)
                    {
                        t.Kick(at, 0.9f);
                        t.PowerChord(at, root, t.Beat * 1.9f, 0.16f);
                    }
                    else
                    {
                        t.Snare(at, 0.55f);
                    }

                    t.Hat(at, 0.3f);
                    t.Hat(at + t.Beat * 0.5f, 0.18f);
                    t.Bass(at, root, t.Beat * 0.45f, 0.4f);
                    t.Bass(at + t.Beat * 0.5f, beat == 3 ? root * 1.5f : root, t.Beat * 0.4f, 0.32f);
                }

                // A pick-up kick leads into the next chord every other bar.
                if (bar % 2 == 1)
                {
                    t.Kick(t.At(bar, 3) + t.Beat * 0.5f, 0.6f);
                }
            }

            // The lead only enters in the second half, so the loop breathes.
            int[] melody = { 2, 4, 3, 1, 4, 5, 3, 2, 0, 2, 4, 3, 5, 6, 4, 2 };
            for (int i = 0; i < melody.Length; i++)
            {
                int bar = 4 + i / 4;
                float at = t.At(bar, i % 4) + (i % 3 == 2 ? t.Beat * 0.5f : 0f);
                t.Lead(at, Hz(Pentatonic[melody[i]]), t.Beat * 0.7f, 0.17f);
            }

            t.Finish(1.5f, 6000f, 0.55f);
        }

        private static void Intense(Track t)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                float root = Hz(IntenseRoots[bar]);
                for (int beat = 0; beat < BeatsPerBar; beat++)
                {
                    float at = t.At(bar, beat);
                    t.Kick(at, 1f);
                    if (beat == 1 || beat == 3)
                    {
                        t.Snare(at, 0.8f);
                        t.Kick(at + t.Beat * 0.5f, 0.75f);
                    }

                    for (int sixteenth = 0; sixteenth < 4; sixteenth++)
                    {
                        t.Hat(at + t.Beat * 0.25f * sixteenth, sixteenth == 0 ? 0.3f : sixteenth == 2 ? 0.22f : 0.14f);
                    }

                    // Driving eighths on the root, palm muted short, with a chord stab on each beat.
                    t.Bass(at, root, t.Beat * 0.3f, 0.5f);
                    t.Bass(at + t.Beat * 0.5f, root, t.Beat * 0.3f, 0.42f);
                    t.PowerChord(at, root, beat < 2 ? t.Beat * 0.45f : t.Beat * 0.95f, 0.2f);
                    if (beat < 2)
                    {
                        t.PowerChord(at + t.Beat * 0.5f, root, t.Beat * 0.4f, 0.16f);
                    }
                }
            }

            // An octave riff rides the second half; the last bar ends on a tom fill.
            int[] riff = { 0, 0, 2, 0, 3, 2, 0, 4, 0, 0, 2, 0, 5, 4, 3, 2 };
            for (int i = 0; i < riff.Length; i++)
            {
                int bar = 4 + i / 4;
                float at = t.At(bar, i % 4);
                t.Lead(at, Hz(Pentatonic[riff[i]]), t.Beat * 0.45f, 0.2f);
                t.Lead(at + t.Beat * 0.5f, Hz(Pentatonic[riff[i]] + 12), t.Beat * 0.35f, 0.14f);
            }

            for (int i = 0; i < 6; i++)
            {
                t.Tom(t.At(7, 2) + t.Beat * 0.33f * i, 220f - i * 22f, 0.6f);
            }

            t.Finish(2.4f, 5200f, 0.72f);
        }

        // The menu's chord loop stays in E minor: i, VI, VII, i, then round again with a turnaround up to the V so
        // the loop falls back onto E. No major chords, no lift.
        private static readonly int[] MenuRoots = { -5, -9, -7, -5, -5, -9, -7, 2 };

        // The menu riff as (bar, beat, pentatonic step, length in beats): short low phrases that answer the chords
        // in the second and fourth pair of bars and leave the chugs alone in between.
        private static readonly float[,] MenuRiff =
        {
            { 2, 0f, 4, 0.5f }, { 2, 0.5f, 3, 0.5f }, { 2, 1f, 2, 1f }, { 2, 2.5f, 1, 0.5f }, { 2, 3f, 2, 1f },
            { 3, 0f, 0, 1.5f }, { 3, 2f, 1, 0.5f }, { 3, 2.5f, 2, 0.5f }, { 3, 3f, 1, 1f },
            { 6, 0f, 4, 0.5f }, { 6, 0.5f, 3, 0.5f }, { 6, 1f, 2, 1f }, { 6, 2.5f, 4, 0.5f }, { 6, 3f, 3, 1f },
            { 7, 0f, 4, 1f }, { 7, 1f, 3, 0.5f }, { 7, 1.5f, 2, 0.5f }, { 7, 2f, 3, 2f },
        };

        // The riff sits an octave under the lead's range, where a guitar riff lives.
        private const int RiffOctave = -12;

        /// <summary>The menu loop: mid-tempo rock in a minor key, palm-muted eighths under open chords and a low riff.</summary>
        public static AudioClip Menu() => Cached("MusicMenu", 104f, Menu);

        private static void Menu(Track t)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                float root = Hz(MenuRoots[bar]);
                for (int beat = 0; beat < BeatsPerBar; beat++)
                {
                    float at = t.At(bar, beat);
                    if (beat == 0 || beat == 2)
                    {
                        t.Kick(at, 0.95f);
                    }
                    else
                    {
                        t.Snare(at, 0.7f);
                    }

                    t.Hat(at, 0.26f);
                    t.Hat(at + t.Beat * 0.5f, 0.16f);

                    // Palm-muted eighths on the root carry the drive; the bass doubles them.
                    t.Chug(at, root, 0.2f);
                    t.Chug(at + t.Beat * 0.5f, root, 0.15f);
                    t.Bass(at, root, t.Beat * 0.4f, 0.45f);
                    t.Bass(at + t.Beat * 0.5f, root, t.Beat * 0.35f, 0.36f);
                }

                // An open chord on the one and a pushed one before the two, with a kick under the push so the band
                // hits it together. A crash opens each half of the loop.
                t.PowerChord(t.At(bar, 0), root, t.Beat * 1.2f, 0.18f);
                t.PowerChord(t.At(bar, 1) + t.Beat * 0.5f, root, t.Beat * 0.9f, 0.16f);
                t.Kick(t.At(bar, 1) + t.Beat * 0.5f, 0.7f);
                if (bar % 4 == 0)
                {
                    t.Crash(t.At(bar, 0), 0.3f);
                }
            }

            for (int i = 0; i < MenuRiff.GetLength(0); i++)
            {
                float at = t.At((int)MenuRiff[i, 0], 0) + MenuRiff[i, 1] * t.Beat;
                t.Lead(at, Hz(Pentatonic[(int)MenuRiff[i, 2]] + RiffOctave), MenuRiff[i, 3] * t.Beat * 0.85f, 0.2f);
            }

            // A tom fill under the last bar rolls the loop round.
            for (int i = 0; i < 4; i++)
            {
                t.Tom(t.At(7, 2) + t.Beat * 0.5f * i, 200f - i * 30f, 0.65f);
            }

            t.Finish(2.1f, 5200f, 0.62f);
        }

        private static float Hz(int semitonesFromA2) => A2 * Mathf.Pow(2f, semitonesFromA2 / 12f);

        private static AudioClip Cached(string name, float bpm, System.Action<Track> build)
        {
            if (Cache.TryGetValue(name, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            var track = new Track(bpm, name.GetHashCode());
            build(track);
            clip = track.ToClip(name);
            Cache[name] = clip;
            return clip;
        }

        /// <summary>
        /// A synth with a tempo grid and the instruments of the band. Notes that start inside the loop fade are
        /// written again after the last bar, so the crossfade that makes the clip loop hears the same thing twice.
        /// </summary>
        private sealed class Track
        {
            private readonly Synth _synth;
            private readonly float _length;

            public Track(float bpm, int seed)
            {
                Beat = 60f / bpm;
                _length = Beat * BeatsPerBar * Bars;
                _synth = new Synth(_length + LoopFade, seed);
            }

            public float Beat { get; }

            public float At(int bar, int beat) => (bar * BeatsPerBar + beat) * Beat;

            public void Kick(float at, float volume) => Each(at, a =>
            {
                _synth.Tone(a, 0.22f, 150f, 42f, volume, 0.002f, SynthWave.Sine);
                _synth.Noise(a, 0.03f, volume * 0.3f, 0.001f, 3000f, 500f);
            });

            public void Snare(float at, float volume) => Each(at, a =>
            {
                _synth.Noise(a, 0.17f, volume * 0.8f, 0.002f, 5000f, 1800f);
                _synth.Tone(a, 0.1f, 200f, 150f, volume * 0.5f, 0.002f, SynthWave.Sine);
            });

            public void Hat(float at, float volume) => Each(at, a => _synth.Hiss(a, 0.05f, volume));

            public void Tom(float at, float hz, float volume) => Each(at, a => _synth.Tone(a, 0.2f, hz, hz * 0.6f, volume, 0.002f, SynthWave.Sine));

            public void Bass(float at, float hz, float seconds, float volume) => Each(at, a =>
            {
                _synth.Tone(a, seconds, hz, hz, volume, 0.004f, SynthWave.Square, 0.7f);
                _synth.Tone(a, seconds, hz * 0.5f, hz * 0.5f, volume * 0.5f, 0.004f, SynthWave.Sine, 0.7f);
            });

            // Root, fifth and octave of a guitar chord, slightly detuned across two voices for width.
            public void PowerChord(float at, float rootHz, float seconds, float volume) => Each(at, a =>
            {
                float[] notes = { rootHz * 2f, rootHz * 3f, rootHz * 4f };
                for (int i = 0; i < notes.Length; i++)
                {
                    _synth.Tone(a, seconds, notes[i], notes[i], volume, 0.006f, SynthWave.Saw, 0.8f);
                    _synth.Tone(a, seconds, notes[i] * 1.004f, notes[i] * 1.004f, volume * 0.7f, 0.006f, SynthWave.Saw, 0.8f);
                }
            });

            public void Lead(float at, float hz, float seconds, float volume) => Each(at, a =>
            {
                _synth.Tone(a, seconds, hz, hz, volume, 0.01f, SynthWave.Saw, 0.6f);
                _synth.Tone(a, seconds, hz * 2.002f, hz * 2f, volume * 0.3f, 0.01f, SynthWave.Sine, 0.6f);
            });

            // A palm-muted guitar hit: root and fifth, over before the next eighth, with hardly any sustain.
            public void Chug(float at, float rootHz, float volume) => Each(at, a =>
            {
                _synth.Tone(a, 0.11f, rootHz * 2f, rootHz * 2f, volume, 0.003f, SynthWave.Saw, 0.15f);
                _synth.Tone(a, 0.11f, rootHz * 3f, rootHz * 3f, volume * 0.6f, 0.003f, SynthWave.Saw, 0.15f);
            });

            // A cymbal crash: a long bright hiss that fades slowly.
            public void Crash(float at, float volume) => Each(at, a => _synth.Hiss(a, 1.1f, volume, 1.3f));

            // Crunch, tame the fizz, level out and close the loop.
            public void Finish(float drive, float lowPassHz, float peak)
            {
                _synth.Distort(drive);
                _synth.LowPass(lowPassHz);
                _synth.Loopable(LoopFade);
                _synth.Normalize(peak);
            }

            public AudioClip ToClip(string name) => _synth.ToClip(name);

            private void Each(float at, System.Action<float> place)
            {
                place(at);
                if (at < LoopFade)
                {
                    place(at + _length);
                }
            }
        }
    }
}
