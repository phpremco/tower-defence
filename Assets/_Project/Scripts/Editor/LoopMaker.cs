using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns a generated music track into a seamless loop. Generators always fade their tracks in and out however
    /// they are asked, so the fades are cut off where the music reaches its running level, and the last bars are
    /// blended over the first with an equal-power crossfade, so the loop point lies inside the blend and never on a cut.
    /// The result is written as a 16-bit WAV next to the other approved clips, where the generator picks it up.
    /// </summary>
    public static class LoopMaker
    {
        private const string MusicFolder = "Assets/_Project/Audio/Approved/Music";
        private const float WindowSeconds = 0.05f;
        private const float LevelShare = 0.8f;
        private const float CrossfadeSeconds = 4f;
        // The level must hold this long before a cut counts: a loud first hit under a fade-in is not the music yet.
        private const float HoldSeconds = 2f;
        // Music breathes: after the full level is reached, it only has to stay above this share of the median.
        private const float HoldShare = 0.35f;
        private const float PeakCeiling = 0.95f;
        private const int LoadTimeoutMs = 20000;

        private static readonly (string Source, string Name)[] Tracks =
        {
            ("Assets/_Project/Audio/menu-music.mp3", "menu_loop"),
            ("Assets/_Project/Audio/game-music.mp3", "game_calm_loop"),
            ("Assets/_Project/Audio/boss-music.mp3", "game_boss_loop"),
        };

        [MenuItem("Tools/Audio/Make Music Loops")]
        public static void MakeAll()
        {
            Directory.CreateDirectory(MusicFolder);
            foreach ((string source, string name) in Tracks)
            {
                if (File.Exists(source))
                {
                    Make(source, name);
                }
                else
                {
                    Debug.Log($"LoopMaker: no {source}, skipped.");
                }
            }

            AssetDatabase.Refresh();
        }

        public static void Make(string sourcePath, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"LoopMaker: {sourcePath} is not an audio clip.");
                return;
            }

            if (!Load(clip))
            {
                Debug.LogWarning($"LoopMaker: {sourcePath} did not finish loading.");
                return;
            }

            int channels = clip.channels;
            int frequency = clip.frequency;
            var samples = new float[clip.samples * channels];
            clip.GetData(samples, 0);

            int window = Mathf.Max(1, Mathf.RoundToInt(frequency * WindowSeconds));
            float[] envelope = Envelope(samples, channels, window);
            float level = Percentile(envelope, 0.5f) * LevelShare;
            int hold = Mathf.Max(1, Mathf.RoundToInt(HoldSeconds / WindowSeconds));
            float floor = Percentile(envelope, 0.5f) * HoldShare;
            int firstFull = FirstSustained(envelope, level, floor, hold);
            int lastFull = LastSustained(envelope, level, floor, hold);
            if (firstFull < 0 || lastFull <= firstFull)
            {
                Debug.LogWarning($"LoopMaker: {sourcePath} never reaches a steady level.");
                return;
            }

            int startFrame = firstFull * window;
            int endFrame = Mathf.Min(clip.samples, (lastFull + 1) * window);
            int fade = Mathf.RoundToInt(frequency * CrossfadeSeconds);
            int length = endFrame - startFrame;
            if (length <= fade * 2)
            {
                Debug.LogWarning($"LoopMaker: {sourcePath} is too short to loop.");
                return;
            }

            float[] looped = Blend(samples, channels, startFrame, length, fade);
            Normalise(looped);
            string target = $"{MusicFolder}/{name}.wav";
            WriteWav(target, looped, channels, frequency);
            AssetDatabase.ImportAsset(target);
            float cutStart = startFrame / (float)frequency;
            float cutEnd = (clip.samples - endFrame) / (float)frequency;
            Debug.Log($"LoopMaker: {name} cut {cutStart:0.0}s in, {cutEnd:0.0}s out, loop {(length - fade) / (float)frequency:0.0}s with a {CrossfadeSeconds:0}s blend.");
        }

        internal static bool Load(AudioClip clip)
        {
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
            }

            int waited = 0;
            while (clip.loadState == AudioDataLoadState.Loading && waited < LoadTimeoutMs)
            {
                Thread.Sleep(50);
                waited += 50;
            }

            return clip.loadState == AudioDataLoadState.Loaded;
        }

        // RMS of every window across all channels, the shape of the track's loudness over time.
        internal static float[] Envelope(float[] samples, int channels, int window)
        {
            int frames = samples.Length / channels;
            int count = frames / window;
            var envelope = new float[count];
            for (int w = 0; w < count; w++)
            {
                double sum = 0;
                int from = w * window * channels;
                int to = from + window * channels;
                for (int i = from; i < to; i++)
                {
                    sum += samples[i] * samples[i];
                }

                envelope[w] = Mathf.Sqrt((float)(sum / (window * channels)));
            }

            return envelope;
        }

        // The first window from which the level holds for the whole hold span, and the last window up to which it did.
        private static int FirstSustained(float[] envelope, float level, float floor, int hold)
        {
            for (int i = 0; i + hold <= envelope.Length; i++)
            {
                if (envelope[i] >= level && Holds(envelope, floor, i, i + hold))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int LastSustained(float[] envelope, float level, float floor, int hold)
        {
            for (int i = envelope.Length - 1; i - hold >= 0; i--)
            {
                if (envelope[i] >= level && Holds(envelope, floor, i - hold + 1, i + 1))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool Holds(float[] envelope, float level, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                if (envelope[i] < level)
                {
                    return false;
                }
            }

            return true;
        }

        private static float Percentile(float[] values, float share)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            return sorted[Mathf.Clamp(Mathf.RoundToInt((sorted.Count - 1) * share), 0, sorted.Count - 1)];
        }

        // The tail is laid over the head with an equal-power crossfade and that blend becomes the END of the file,
        // after the untouched body: the file starts clean in full music, and the only blended bars sit right
        // before the wrap, where the head's full level continues straight into the first frame again.
        internal static float[] Blend(float[] samples, int channels, int startFrame, int length, int fade)
        {
            int outFrames = length - fade;
            int bodyFrames = outFrames - fade;
            var result = new float[outFrames * channels];
            for (int frame = 0; frame < outFrames; frame++)
            {
                for (int c = 0; c < channels; c++)
                {
                    float value;
                    if (frame < bodyFrames)
                    {
                        value = samples[(startFrame + fade + frame) * channels + c];
                    }
                    else
                    {
                        int into = frame - bodyFrames;
                        float t = into / (float)fade;
                        float head = samples[(startFrame + into) * channels + c];
                        float tail = samples[(startFrame + outFrames + into) * channels + c];
                        value = head * Mathf.Sin(t * Mathf.PI * 0.5f) + tail * Mathf.Cos(t * Mathf.PI * 0.5f);
                    }

                    result[frame * channels + c] = value;
                }
            }

            return result;
        }

        internal static void Normalise(float[] samples)
        {
            float peak = 0f;
            foreach (float s in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }

            if (peak > PeakCeiling)
            {
                float scale = PeakCeiling / peak;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= scale;
                }
            }
        }

        internal static void WriteWav(string path, float[] samples, int channels, int frequency)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataBytes = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);
            foreach (float s in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
