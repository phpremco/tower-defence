using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The volume groups and the generated music.</summary>
    public sealed class AudioTests
    {
        [Test]
        public void AudioMix_ClampsAndCombinesGroupsWithTheMaster()
        {
            var mix = new AudioMix();
            int changes = 0;
            mix.Changed += () => changes++;

            mix.Set(0.5f, 2f, 0.4f, -1f);

            Assert.AreEqual(0.5f, mix.Master, 0.001f);
            Assert.AreEqual(1f, mix.Music, 0.001f, "clamped to one");
            Assert.AreEqual(0f, mix.Ambience, 0.001f, "clamped to zero");
            Assert.AreEqual(0.5f, mix.MusicGain, 0.001f);
            Assert.AreEqual(0.2f, mix.EffectsGain, 0.001f);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ProceduralMusic_LoopsAreEightBarsLongAndNotSilent()
        {
            AudioClip calm = ProceduralMusic.Calm();
            AudioClip intense = ProceduralMusic.Intense();
            AudioClip menu = ProceduralMusic.Menu();

            Assert.AreEqual(8f * 4f * 60f / 100f, calm.length, 0.05f, "eight bars at 100 bpm");
            Assert.AreEqual(8f * 4f * 60f / 132f, intense.length, 0.05f, "eight bars at 132 bpm");
            Assert.AreEqual(8f * 4f * 60f / 104f, menu.length, 0.05f, "eight bars at 104 bpm");
            Assert.Greater(Peak(calm), 0.3f);
            Assert.Greater(Peak(intense), 0.3f);
            Assert.Greater(Peak(menu), 0.3f);
        }

        [Test]
        public void BossSounds_Exist()
        {
            Assert.Greater(ProceduralSfx.BossAlarm().length, 2f);
            Assert.Greater(ProceduralSfx.QuakeRumble().length, 4f);
            Assert.Greater(ProceduralSfx.EmergeGround().length, 1f);
        }

        private static float Peak(AudioClip clip)
        {
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);
            float peak = 0f;
            foreach (float sample in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            return peak;
        }
    }
}
