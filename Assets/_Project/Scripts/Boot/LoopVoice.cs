using Game.Core;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// A looping sound that fades towards whatever level it is asked for each frame: rotor hum, slime, wings, a cart
    /// on the road. Null when there is no clip, so callers can keep one reference per loop without checks.
    /// </summary>
    public sealed class LoopVoice
    {
        private const float FadeSeconds = 0.25f;

        private readonly AudioSource _source;
        private readonly float _gain;
        private float _target;

        private LoopVoice(AudioSource source, float gain)
        {
            _source = source;
            _gain = gain;
        }

        /// <summary>The loop for the first clip of the set, or null when the set is empty.</summary>
        public static LoopVoice Create(GameObject host, SoundSet set) => set == null || set.IsEmpty ? null : Create(host, set[0], set.Gain);

        public static LoopVoice Create(GameObject host, AudioClip clip, float gain = 1f)
        {
            if (clip == null)
            {
                return null;
            }

            var source = host.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.Play();
            return new LoopVoice(source, gain);
        }

        /// <summary>The level to fade towards; set it every frame, zero to fall silent.</summary>
        public float Target
        {
            get => _target;
            set => _target = Mathf.Max(0f, value);
        }

        /// <summary>Stereo position, -1 left to 1 right, for a loop that belongs to one place on the field.</summary>
        public float Pan
        {
            set => _source.panStereo = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>Playback speed; below one slows the loop down and lowers it, for a recording that churns too fast.</summary>
        public float Pitch
        {
            set => _source.pitch = Mathf.Max(0.1f, value);
        }

        public void Tick(float unscaledDeltaTime, float mixGain)
        {
            _source.volume = Mathf.MoveTowards(_source.volume, _target * _gain * mixGain, unscaledDeltaTime / FadeSeconds);
        }
    }
}
