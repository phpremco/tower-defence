using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The sound of a dialogue box, shared by every presenter (the dialogue box and the boss answer box): the opener when a
    /// speaker starts talking and the typing blips of the speaker, flat (2D) through the effects volume. The timing rules
    /// live in <see cref="TypingCadence"/>; this only picks the clips and plays them.
    /// </summary>
    public sealed class DialogueVoice
    {
        // Variants are picked at random; a fixed seed keeps a played run repeatable.
        private const int VariantSeed = 7321;

        private readonly DialogueSounds _sounds;
        private readonly AudioMix _mix;
        private readonly AudioSource _source;
        private readonly TypingCadence _cadence;
        private SoundSet _blips;

        public DialogueVoice(GameObject host)
        {
            if (Services.TryGet(out SfxLibrary library))
            {
                _sounds = library.Dialogue;
            }

            Services.TryGet(out _mix);
            _cadence = new TypingCadence(_sounds != null ? _sounds.BlipEveryLetters : 1, VariantSeed);
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        /// <summary>Blips played so far, for probes.</summary>
        public int BlipCount { get; private set; }

        /// <summary>Openers played so far, for probes.</summary>
        public int OpenerCount { get; private set; }

        public void ShowLine(DialogueLine line)
        {
            SoundSet own = line.Speaker != null ? line.Speaker.TypingBlips : null;
            _blips = own != null && !own.IsEmpty ? own : _sounds != null ? _sounds.DefaultBlips : null;
            if (_cadence.BeginLine(line) && _sounds != null)
            {
                OpenerCount++;
                Play(_sounds.Opener, _sounds.OpenerVolume);
            }
        }

        public void Reveal(int visible, bool complete)
        {
            if (_cadence.Reveal(visible, complete) && _sounds != null)
            {
                BlipCount++;
                Play(_blips, _sounds.BlipVolume);
            }
        }

        /// <summary>The box closed (or was hidden at once): the next line opens again. Nothing sounds on closing.</summary>
        public void Closed() => _cadence.BoxClosed();

        private void Play(SoundSet set, float volume)
        {
            if (set == null || set.IsEmpty)
            {
                return;
            }

            // A single clip (the opener) never touches the blips' no-repeat memory.
            AudioClip clip = set.Count == 1 ? set[0] : set[_cadence.NextVariant(set.Count)];
            float gain = _mix != null ? _mix.EffectsGain : 1f;
            _source.pitch = set.Pitch;
            _source.PlayOneShot(clip, Mathf.Clamp(volume * set.Gain, 0f, SoundSet.MaxGain) * gain);
        }
    }
}
