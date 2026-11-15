using System;
using Game.Core;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// One sound of the game at play time: the recorded set handed out in turn, or the generated fallback when the
    /// set is empty. Carries the set's loudness gain so the player can mix by kind of sound alone.
    /// </summary>
    public sealed class SoundFamily
    {
        private readonly SoundSet _set;
        private readonly Func<AudioClip> _makeFallback;
        private AudioClip _fallback;
        private int _cursor;

        public SoundFamily(SoundSet set, AudioClip fallback = null)
        {
            _set = set;
            _fallback = fallback;
        }

        /// <summary>
        /// A family whose fallback is generated only when it is first needed (round 64). Handing over the generated clip itself
        /// made every level start synthesise some forty sounds it then never played, because the recordings exist: a quarter of
        /// a second of the frame that built the level.
        /// </summary>
        public SoundFamily(SoundSet set, Func<AudioClip> makeFallback)
        {
            _set = set;
            _makeFallback = makeFallback;
        }

        /// <summary>True when there is anything to play at all.</summary>
        public bool HasSound => (_set != null && !_set.IsEmpty) || _fallback != null || _makeFallback != null;

        public float Gain => _set != null && !_set.IsEmpty ? _set.Gain : 1f;

        /// <summary>Playback speed of the set; the generated fallback always plays as made.</summary>
        public float Pitch => _set != null && !_set.IsEmpty ? _set.Pitch : 1f;

        public AudioClip Next()
        {
            AudioClip clip = _set?.Pick(ref _cursor);
            if (clip != null)
            {
                return clip;
            }

            if (_fallback == null && _makeFallback != null)
            {
                _fallback = _makeFallback();
            }

            return _fallback;
        }
    }
}
