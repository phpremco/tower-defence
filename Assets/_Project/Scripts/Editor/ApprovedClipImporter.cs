using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the loose recordings Remco drops in Assets/_Project/Audio into approved 16-bit WAV clips the library
    /// generator picks up. The laser charge is cut down to its steadiest loud stretch and blended into a seamless
    /// loop, so a beam weapon can hold it for as long as it fires; the hero's death cry is taken whole.
    /// </summary>
    public static class ApprovedClipImporter
    {
        private const string LaserSource = "Assets/_Project/Audio/gregorquendel-laser-charge-175727.mp3";
        private const string LaserTarget = "Assets/_Project/Audio/Approved/laser_beam_loop.wav";
        private const string HeroDeathSource = "Assets/_Project/Audio/hero-death.mp3";
        private const string HeroDeathTarget = "Assets/_Project/Audio/Approved/Hero/hero_death.wav";
        private static readonly string[] HeroDeathReplaced = { "Assets/_Project/Audio/Approved/Hero/hero_death_01.wav", "Assets/_Project/Audio/Approved/Hero/hero_death_02.wav" };
        private const float WindowSeconds = 0.05f;
        private const float LoopSpanSeconds = 1.6f;
        private const float LoopBlendSeconds = 0.35f;
        // Only the loud part of the recording may be looped: a window counts when it holds this share of the peak.
        private const float LoudShare = 0.45f;

        [MenuItem("Tools/Audio/Import Approved Clips")]
        public static void ImportAll()
        {
            MakeBeamLoop();
            ImportWhole(HeroDeathSource, HeroDeathTarget, HeroDeathReplaced);
            AssetDatabase.Refresh();
        }

        // The steadiest loud stretch of the charge: among every span of the loop length inside the loud part, the
        // one whose loudness varies least, blended tail over head so the loop point is inaudible.
        public static void MakeBeamLoop()
        {
            if (!ReadSamples(LaserSource, out float[] samples, out int channels, out int frequency))
            {
                return;
            }

            int window = Mathf.Max(1, Mathf.RoundToInt(frequency * WindowSeconds));
            float[] envelope = LoopMaker.Envelope(samples, channels, window);
            float peak = 0f;
            foreach (float e in envelope)
            {
                peak = Mathf.Max(peak, e);
            }

            int span = Mathf.RoundToInt(LoopSpanSeconds / WindowSeconds);
            int bestStart = -1;
            float bestSpread = float.MaxValue;
            for (int start = 0; start + span <= envelope.Length; start++)
            {
                float min = float.MaxValue;
                float max = 0f;
                for (int i = start; i < start + span; i++)
                {
                    min = Mathf.Min(min, envelope[i]);
                    max = Mathf.Max(max, envelope[i]);
                }

                if (min < peak * LoudShare)
                {
                    continue;
                }

                float spread = max - min;
                if (spread < bestSpread)
                {
                    bestSpread = spread;
                    bestStart = start;
                }
            }

            if (bestStart < 0)
            {
                Debug.LogWarning("ApprovedClipImporter: the laser charge has no loud stretch long enough to loop.");
                return;
            }

            int startFrame = bestStart * window;
            int length = span * window;
            int fade = Mathf.RoundToInt(frequency * LoopBlendSeconds);
            float[] looped = LoopMaker.Blend(samples, channels, startFrame, length, fade);
            LoopMaker.Normalise(looped);
            Directory.CreateDirectory(Path.GetDirectoryName(LaserTarget));
            LoopMaker.WriteWav(LaserTarget, looped, channels, frequency);
            AssetDatabase.ImportAsset(LaserTarget);
            Debug.Log($"ApprovedClipImporter: laser loop from {startFrame / (float)frequency:0.00}s, {(length - fade) / (float)frequency:0.00}s long.");
        }

        private static void ImportWhole(string source, string target, string[] replaced)
        {
            if (!ReadSamples(source, out float[] samples, out int channels, out int frequency))
            {
                return;
            }

            LoopMaker.Normalise(samples);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            LoopMaker.WriteWav(target, samples, channels, frequency);
            AssetDatabase.ImportAsset(target);
            foreach (string old in replaced)
            {
                if (File.Exists(old))
                {
                    AssetDatabase.DeleteAsset(old);
                }
            }

            Debug.Log($"ApprovedClipImporter: {Path.GetFileName(target)} written, {samples.Length / channels / (float)frequency:0.0}s.");
        }

        private static bool ReadSamples(string source, out float[] samples, out int channels, out int frequency)
        {
            samples = null;
            channels = 0;
            frequency = 0;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(source);
            if (clip == null)
            {
                Debug.LogWarning($"ApprovedClipImporter: no audio clip at {source}.");
                return false;
            }

            if (!LoopMaker.Load(clip))
            {
                Debug.LogWarning($"ApprovedClipImporter: {source} did not finish loading.");
                return false;
            }

            channels = clip.channels;
            frequency = clip.frequency;
            samples = new float[clip.samples * channels];
            clip.GetData(samples, 0);
            return true;
        }
    }
}
