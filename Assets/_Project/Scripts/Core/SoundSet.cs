using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One sound of the game as a handful of recorded variants plus the gain that brings them to the shared loudness.
    /// The gain is measured by the generator, so playback code only chooses how loud a kind of sound should be
    /// relative to the others and never has to know how hot a file was mastered.
    /// </summary>
    [Serializable]
    public sealed class SoundSet
    {
        public const float MinGain = 0.1f;
        public const float MaxGain = 8f;
        public const float MinPitch = 0.25f;
        public const float MaxPitch = 3f;

        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] [Range(MinGain, MaxGain)] private float _gain = 1f;

        /// <summary>Playback speed of every variant: one plays the file as recorded; a stand-in borrowed from another creature or gun is pitched to sound its own.</summary>
        [SerializeField] [Range(MinPitch, MaxPitch)] private float _pitch = 1f;

        public int Count => _clips.Length;

        public bool IsEmpty => _clips.Length == 0;

        public float Gain => _gain;

        public float Pitch => _pitch;

        public AudioClip this[int index] => _clips[index];

        /// <summary>The variant in turn, so a volley never repeats one file; null when the set is empty.</summary>
        public AudioClip Pick(ref int cursor)
        {
            if (_clips.Length == 0)
            {
                return null;
            }

            AudioClip clip = _clips[cursor % _clips.Length];
            cursor++;
            return clip;
        }
    }
}
