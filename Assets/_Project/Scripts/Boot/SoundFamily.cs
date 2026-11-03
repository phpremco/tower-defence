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
        private readonly AudioClip _fallback;
        private readonly SoundSet _set;
        private int _cursor;

        public SoundFamily(SoundSet set, AudioClip fallback = null)
        {
            _set = set;
            _fallback = fallback;
        }

        /// <summary>True when there is anything to play at all.</summary>
        public bool HasSound => (_set != null && !_set.IsEmpty) || _fallback != null;

        public float Gain => _set != null && !_set.IsEmpty ? _set.Gain : 1f;

        /// <summary>Playback speed of the set; the generated fallback always plays as made.</summary>
        public float Pitch => _set != null && !_set.IsEmpty ? _set.Pitch : 1f;

        public AudioClip Next()
        {
            AudioClip clip = _set?.Pick(ref _cursor);
            return clip != null ? clip : _fallback;
        }
    }
}
