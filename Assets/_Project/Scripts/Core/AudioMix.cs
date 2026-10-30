using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The four volume groups of the game: master, music, effects and ambience, each from 0 to 1. Players set them in
    /// the options; the sound players multiply their group gain in. Kept in code instead of a mixer asset so the whole
    /// project stays buildable from scripts.
    /// </summary>
    public sealed class AudioMix
    {
        private float _master = 1f;
        private float _music = 1f;
        private float _effects = 1f;
        private float _ambience = 1f;

        public float Master => _master;
        public float Music => _music;
        public float Effects => _effects;
        public float Ambience => _ambience;

        /// <summary>The gain a music source plays at: master and music combined.</summary>
        public float MusicGain => _master * _music;

        public float EffectsGain => _master * _effects;

        public float AmbienceGain => _master * _ambience;

        /// <summary>Raised after any group changes, so playing sources can follow.</summary>
        public event Action Changed;

        public void Set(float master, float music, float effects, float ambience)
        {
            _master = Mathf.Clamp01(master);
            _music = Mathf.Clamp01(music);
            _effects = Mathf.Clamp01(effects);
            _ambience = Mathf.Clamp01(ambience);
            Changed?.Invoke();
        }
    }
}
