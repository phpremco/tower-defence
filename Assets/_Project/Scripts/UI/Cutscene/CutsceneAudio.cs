using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The sound of a cutscene: music moods that cross-fade, one-shot cues from the game's sound library (with a
    /// generated clip when a set is empty) and loops a set can hold, such as a drill or drone fans. Everything is
    /// flat (2D) and follows the player's volume settings.
    /// </summary>
    public sealed class CutsceneAudio : MonoBehaviour
    {
        private const int OneShotVoices = 8;
        private const float MusicFadeSeconds = 1.6f;
        private const float WonderVolume = 0.5f;
        private const float TensionVolume = 0.42f;
        private const float AttackVolume = 0.55f;

        private SfxLibrary _library;
        private AudioMix _mix;
        private CutsceneCast _cast;
        private AudioSource[] _voices;
        private int _nextVoice;
        private int _cursor;
        private AudioSource _musicA;
        private AudioSource _musicB;
        private float _musicTarget;
        private float _fadeLevelA;
        private float _fadeLevelB;

        public void Initialize(SfxLibrary library, AudioMix mix, CutsceneCast cast)
        {
            _library = library;
            _mix = mix;
            _cast = cast;
            _voices = new AudioSource[OneShotVoices];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = NewSource($"Voice{i}", false);
            }

            _musicA = NewSource("MusicA", true);
            _musicB = NewSource("MusicB", true);
        }

        /// <summary>Plays a cue's sound once; its own clip wins over the library.</summary>
        public void Play(CutsceneCue cue)
        {
            float gain = 1f;
            AudioClip clip = cue.Clip != null ? cue.Clip : ClipFor(cue.Sound, out gain);
            float strength = cue.Strength > 0f ? cue.Strength : 1f;
            Play(clip, strength * gain);
        }

        /// <summary>Plays a clip once at a volume relative to the effects mix (a library gain may lift it above one).</summary>
        public void Play(AudioClip clip, float volume)
        {
            if (clip == null || _voices == null)
            {
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = 1f;
            voice.PlayOneShot(clip, Mathf.Clamp(volume, 0f, SoundSet.MaxGain) * EffectsGain);
        }

        /// <summary>A looping source a set drives itself (drill, drone fans); its volume is the set's share before the mix.</summary>
        public AudioSource CreateLoop(string name, CutsceneSound sound)
        {
            AudioSource source = NewSource(name, true);
            source.clip = ClipFor(sound, out float _);
            source.volume = 0f;
            if (source.clip != null)
            {
                source.Play();
            }

            return source;
        }

        /// <summary>Sets a loop's volume from the set's share, through the effects mix.</summary>
        public void SetLoopVolume(AudioSource loop, float share)
        {
            if (loop != null)
            {
                loop.volume = Mathf.Clamp01(share) * EffectsGain;
            }
        }

        /// <summary>Changes the music mood; Keep does nothing, Silence fades out.</summary>
        public void SetMusic(CutsceneMusic mood)
        {
            if (mood == CutsceneMusic.Keep || _musicA == null)
            {
                return;
            }

            AudioClip clip = MusicFor(mood, out float volume);
            if (clip != null && _musicA.clip == clip && _musicA.isPlaying)
            {
                _musicTarget = volume;
                return;
            }

            // The playing track moves to the second source and fades out while the new one fades in.
            (_musicA, _musicB) = (_musicB, _musicA);
            (_fadeLevelA, _fadeLevelB) = (0f, _fadeLevelA);
            _musicTarget = volume;
            _musicA.clip = clip;
            _musicA.volume = 0f;
            if (clip != null)
            {
                _musicA.Play();
            }
        }

        private float EffectsGain => _mix != null ? _mix.EffectsGain : 1f;

        private void Update()
        {
            if (_musicA == null)
            {
                return;
            }

            float step = Time.unscaledDeltaTime / MusicFadeSeconds;
            _fadeLevelA = Mathf.MoveTowards(_fadeLevelA, _musicA.clip != null ? _musicTarget : 0f, step);
            _fadeLevelB = Mathf.MoveTowards(_fadeLevelB, 0f, step);
            float gain = _mix != null ? _mix.MusicGain : 1f;
            _musicA.volume = _fadeLevelA * gain;
            _musicB.volume = _fadeLevelB * gain;
            if (_fadeLevelB <= 0f && _musicB.isPlaying)
            {
                _musicB.Stop();
            }
        }

        private AudioClip MusicFor(CutsceneMusic mood, out float volume)
        {
            switch (mood)
            {
                case CutsceneMusic.Wonder:
                    volume = WonderVolume;
                    return ProceduralMusic.Calm();
                case CutsceneMusic.Tension:
                    volume = TensionVolume;
                    return OrElse(Recorded(_library != null ? _library.GameMusic : null), () => ProceduralMusic.Intense());
                case CutsceneMusic.Attack:
                    volume = AttackVolume;
                    return OrElse(Recorded(_library != null ? _library.BossMusic : null), () => ProceduralMusic.Intense());
                default:
                    volume = 0f;
                    return null;
            }
        }

        private AudioClip ClipFor(CutsceneSound sound, out float gain)
        {
            AudioClip clip = LibraryClip(sound, out gain);
            if (clip == null)
            {
                clip = Generated(sound);
            }

            if (clip != null && gain <= 0f)
            {
                gain = 1f;
            }

            return clip;
        }

        private AudioClip LibraryClip(CutsceneSound sound, out float gain)
        {
            gain = 1f;
            SoundSet set = LibrarySet(sound);
            if (set == null || set.IsEmpty)
            {
                return null;
            }

            gain = set.Gain;
            return set.Pick(ref _cursor);
        }

        private SoundSet LibrarySet(CutsceneSound sound)
        {
            CreatureVoice voice = _cast != null && _cast.Screamer != null ? _cast.Screamer.Voice : null;
            switch (sound)
            {
                case CutsceneSound.Scream:
                    return voice != null ? voice.Screeches : null;
                case CutsceneSound.Growl:
                    return voice != null ? voice.Growls : null;
            }

            if (_library == null)
            {
                return null;
            }

            switch (sound)
            {
                case CutsceneSound.Earthquake: return _library.Earthquakes;
                case CutsceneSound.BuildClank: return _library.StructureHits;
                case CutsceneSound.CoreIgnite: return _library.Upgrades;
                case CutsceneSound.DroneFans: return _library.DroneFanLoops;
                case CutsceneSound.DroneHarvest: return _library.DroneHarvestLoops;
                case CutsceneSound.BaseAlert: return _library.BaseAlerts;
                case CutsceneSound.BossAlarm: return _library.BossAlarms;
                case CutsceneSound.HeroStep: return _library.HeroSteps;
                case CutsceneSound.Blaster: return _library.HeroBlasters;
                case CutsceneSound.Drums: return _library.WaveDrums;
                default: return null;
            }
        }

        private static AudioClip Generated(CutsceneSound sound)
        {
            switch (sound)
            {
                case CutsceneSound.Earthquake:
                case CutsceneSound.Rumble: return ProceduralSfx.QuakeRumble();
                case CutsceneSound.Landing: return ProceduralSfx.BigExplosion();
                case CutsceneSound.BuildClank: return ProceduralSfx.Build();
                case CutsceneSound.CoreIgnite: return ProceduralSfx.Upgrade();
                case CutsceneSound.BossAlarm: return ProceduralSfx.BossAlarm();
                case CutsceneSound.Blaster: return ProceduralSfx.HeroBlaster();
                case CutsceneSound.Scream: return ProceduralSfx.BossRoar();
                case CutsceneSound.Growl: return ProceduralSfx.BossGrowl();
                case CutsceneSound.Emerge: return ProceduralSfx.EmergeGround();
                case CutsceneSound.RadarPing: return ProceduralSfx.Tick();
                default: return null;
            }
        }

        private static AudioClip Recorded(SoundSet set) => set != null && !set.IsEmpty ? set[0] : null;

        // Unity objects compare with their own null (a destroyed clip is not a C# null), so no ?? on clips.
        private static AudioClip OrElse(AudioClip clip, System.Func<AudioClip> fallback) => clip != null ? clip : fallback();

        private AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
