using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The sound of a cutscene: music moods that cross-fade, one-shot cues, layers a shot holds (beds, engines, breathing)
    /// and loops a set drives itself, such as a drill or drone fans. A sound comes from the cutscene's own bank first, then
    /// from the game's sound library, then from a generated clip. Everything is flat (2D) and follows the player's volume
    /// settings; the end of the cutscene fades it all out.
    /// </summary>
    public sealed class CutsceneAudio : MonoBehaviour
    {
        private const int OneShotVoices = 8;
        private const int TimedVoices = 4;
        // Round 59: one-shots that swell in from silence (the salvo's quieter launches), each on a source of its own so the swell
        // shapes that sound alone.
        private const int SwellVoices = 8;
        private const float MusicFadeSeconds = 1.6f;
        private const float WonderVolume = 0.5f;
        private const float TensionVolume = 0.42f;
        private const float AttackVolume = 0.55f;
        // How fast a layer's level may change, as seconds for the whole range: smooths shot changes, where a layer the
        // new shot does not list fades out and one it lists at another level glides there.
        private const float LayerSlewSeconds = 0.6f;
        private const float OutroSeconds = 0.9f;
        // Variants are picked at random; a fixed seed keeps a played run repeatable.
        private const int VariantSeed = 4207;

        private readonly List<CutsceneLayerVoice> _held = new List<CutsceneLayerVoice>();
        private readonly System.Random _random = new System.Random(VariantSeed);

        private SfxLibrary _library;
        private AudioMix _mix;
        private CutsceneCast _cast;
        private CutsceneSoundBank _bank;
        private int[] _lastVariant;
        // Round 44: one-shots a set starts at a given point of their file (a burrow's close lands on the frame it shuts).
        private AudioSource[] _timed;
        private int _nextTimed;
        private AudioSource[] _swells;
        private float[] _swellStart;
        private float[] _swellSeconds;
        private float[] _swellEnd;
        private AudioSource[] _voices;
        private int _nextVoice;
        private int _cursor;
        private AudioSource _musicA;
        private AudioSource _musicB;
        private float _musicTarget;
        private float _fadeLevelA;
        private float _fadeLevelB;
        private CutsceneLayer[] _layers;
        private bool _snap;
        private bool _fadingOut;
        private float _outro = 1f;

        public void Initialize(SfxLibrary library, AudioMix mix, CutsceneCast cast, CutsceneSoundBank bank)
        {
            _library = library;
            _mix = mix;
            _cast = cast;
            _bank = bank;
            _lastVariant = new int[bank != null ? bank.Slots.Count : 0];
            for (int i = 0; i < _lastVariant.Length; i++)
            {
                _lastVariant[i] = -1;
            }

            _voices = new AudioSource[OneShotVoices];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = NewSource($"Voice{i}", false);
            }

            _timed = new AudioSource[TimedVoices];
            for (int i = 0; i < _timed.Length; i++)
            {
                _timed[i] = NewSource($"Timed{i}", false);
            }

            _swells = new AudioSource[SwellVoices];
            _swellStart = new float[SwellVoices];
            _swellSeconds = new float[SwellVoices];
            _swellEnd = new float[SwellVoices];
            for (int i = 0; i < _swells.Length; i++)
            {
                _swells[i] = NewSource($"Swell{i}", false);
            }

            _musicA = NewSource("MusicA", true);
            _musicB = NewSource("MusicB", true);
        }

        /// <summary>The held sounds now, for probes.</summary>
        public IReadOnlyList<CutsceneLayerVoice> Held => _held;

        /// <summary>Plays a cue's sound once; its own clip wins over the bank and the library.</summary>
        public void Play(CutsceneCue cue)
        {
            float gain = 1f;
            float pitch = 1f;
            CutsceneSoundChannel channel = CutsceneSoundChannel.Effects;
            AudioClip clip = cue.Clip != null ? cue.Clip : Resolve(cue.Sound, true, out gain, out pitch, out channel, out bool _);
            float strength = cue.Strength > 0f ? cue.Strength : 1f;
            if (cue.FadeIn > 0f)
            {
                Swell(clip, strength * gain, pitch, channel, cue.FadeIn);
                return;
            }

            Play(clip, strength * gain, pitch, channel);
        }

        /// <summary>
        /// One recorded variant of a bank slot, chosen by <paramref name="index"/> (wrapped), with its gain; null while the slot
        /// has no file (no fallback: a sound that must land on a frame is silent rather than borrowed).
        /// </summary>
        public AudioClip Variant(CutsceneSound sound, int index, out float gain)
        {
            gain = 1f;
            int slot = _bank != null ? _bank.IndexOf(sound) : -1;
            if (slot < 0 || _bank.Slots[slot].IsEmpty)
            {
                return null;
            }

            SoundSet set = _bank.Slots[slot].Clips;
            gain = set.Gain;
            return set[((index % set.Count) + set.Count) % set.Count];
        }

        /// <summary>
        /// Plays a clip once from <paramref name="offset"/> seconds into it (a seek that lands inside it picks it up there), so a
        /// moment inside the file meets a moment of the picture. Stopped by a seek like every one-shot.
        /// </summary>
        public void PlayFrom(AudioClip clip, float volume, float offset, CutsceneSoundChannel channel = CutsceneSoundChannel.Effects)
        {
            if (clip == null || _timed == null || offset < 0f || offset >= clip.length)
            {
                return;
            }

            AudioSource source = _timed[_nextTimed];
            _nextTimed = (_nextTimed + 1) % _timed.Length;
            source.Stop();
            source.clip = clip;
            source.time = offset;
            source.volume = Mathf.Clamp01(volume * ChannelGain(channel) * _outro);
            source.Play();
        }

        /// <summary>Plays a clip once at a volume relative to the effects mix (a library gain may lift it above one).</summary>
        public void Play(AudioClip clip, float volume, float pitch = 1f) => Play(clip, volume, pitch, CutsceneSoundChannel.Effects);

        /// <summary>
        /// A looping source a set drives itself (drill, drone fans); its volume is the set's share before the mix. It glides to a
        /// new share over <paramref name="slewSeconds"/> for the whole range (zero: the pace of the shots' layers).
        /// </summary>
        public AudioSource CreateLoop(string name, CutsceneSound sound, float slewSeconds = 0f)
        {
            AudioSource source = NewSource(name, true);
            source.clip = Resolve(sound, false, out float gain, out float pitch, out CutsceneSoundChannel channel, out bool fromBank);
            source.pitch = pitch;
            source.volume = 0f;
            if (source.clip != null)
            {
                source.Play();
            }

            // A borrowed library loop keeps the level it was tuned at before the bank existed (its gain was never applied).
            _held.Add(new CutsceneLayerVoice(sound, source, fromBank ? gain : 1f, channel, true, true) { SlewSeconds = slewSeconds });
            return source;
        }

        /// <summary>Sets the share a set's loop glides to (through the mix), so a set switching it on or off never clicks.</summary>
        public void SetLoopVolume(AudioSource loop, float share)
        {
            if (loop == null)
            {
                return;
            }

            foreach (CutsceneLayerVoice voice in _held)
            {
                if (voice.Source == loop)
                {
                    voice.Target = Mathf.Clamp01(share);
                    return;
                }
            }
        }

        /// <summary>A new shot: from now on its layers set the held sounds; any other held sound fades out.</summary>
        public void EnterShot(CutsceneLayer[] layers) => _layers = layers;

        /// <summary>Sets every held sound to the moment of the shot; after <see cref="Snap"/> it lands there at once.</summary>
        public void PoseLayers(float time, float delta)
        {
            foreach (CutsceneLayerVoice voice in _held)
            {
                if (!voice.DrivenBySet)
                {
                    voice.Target = 0f;
                    voice.Offset = -1f;
                }
            }

            if (_layers != null)
            {
                foreach (CutsceneLayer layer in _layers)
                {
                    TargetLayer(layer, time);
                }
            }

            float step = delta / LayerSlewSeconds;
            foreach (CutsceneLayerVoice voice in _held)
            {
                if (voice.DrivenBySet)
                {
                    float pace = voice.SlewSeconds > 0f ? delta / voice.SlewSeconds : step;
                    voice.Level = _snap ? voice.Target : Mathf.MoveTowards(voice.Level, voice.Target, pace);
                    ApplyVolume(voice);
                }
                else
                {
                    PoseVoice(voice, step);
                }
            }

            _snap = false;
        }

        /// <summary>
        /// A seek: the one-shots in flight stop, the held sounds land on the new moment at their next pose (a one-shot held
        /// as a layer jumps to its place in the clip), and a finishing fade is undone.
        /// </summary>
        public void Snap()
        {
            if (_voices != null)
            {
                foreach (AudioSource voice in _voices)
                {
                    voice.Stop();
                    voice.volume = 1f;
                }
            }

            if (_timed != null)
            {
                foreach (AudioSource timed in _timed)
                {
                    timed.Stop();
                }
            }

            if (_swells != null)
            {
                for (int i = 0; i < _swells.Length; i++)
                {
                    _swells[i].Stop();
                    _swellEnd[i] = 0f;
                }
            }

            _snap = true;
            _fadingOut = false;
            _outro = 1f;
        }

        /// <summary>The cutscene ends: everything still sounding fades out with the picture.</summary>
        public void FadeOut()
        {
            _fadingOut = true;
            SetMusic(CutsceneMusic.Silence);
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

        private float ChannelGain(CutsceneSoundChannel channel)
        {
            if (_mix == null)
            {
                return 1f;
            }

            return channel == CutsceneSoundChannel.Ambience ? _mix.AmbienceGain : _mix.EffectsGain;
        }

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

            if (_fadingOut)
            {
                FadeAll(Time.unscaledDeltaTime);
            }

            PoseSwells(Time.unscaledTime);
        }

        // Each swelling one-shot rises from silence over its own fade-in, eased, and follows the outro like every voice.
        private void PoseSwells(float now)
        {
            for (int i = 0; i < _swells.Length; i++)
            {
                if (now < _swellEnd[i])
                {
                    _swells[i].volume = CutsceneMath.Fade(now - _swellStart[i], _swellSeconds[i]) * _outro;
                }
            }
        }

        // A one-shot that swells in from silence over seconds, on a free swell source (or the one closest to its end).
        private void Swell(AudioClip clip, float volume, float pitch, CutsceneSoundChannel channel, float seconds)
        {
            if (clip == null || _swells == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            int pick = 0;
            for (int i = 1; i < _swells.Length; i++)
            {
                if (_swellEnd[i] < _swellEnd[pick])
                {
                    pick = i;
                }
            }

            AudioSource source = _swells[pick];
            source.Stop();
            source.pitch = pitch;
            source.volume = 0f;
            source.PlayOneShot(clip, Mathf.Clamp(volume, 0f, SoundSet.MaxGain) * ChannelGain(channel));
            _swellStart[pick] = now;
            _swellSeconds[pick] = seconds;
            _swellEnd[pick] = now + clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
        }

        private void FadeAll(float delta)
        {
            float before = _outro;
            _outro = Mathf.MoveTowards(_outro, 0f, delta / OutroSeconds);
            foreach (AudioSource voice in _voices)
            {
                voice.volume = _outro;
            }

            foreach (AudioSource timed in _timed)
            {
                timed.volume = before > 0f ? timed.volume * (_outro / before) : 0f;
            }

            foreach (CutsceneLayerVoice held in _held)
            {
                ApplyVolume(held);
            }
        }

        private void TargetLayer(CutsceneLayer layer, float time)
        {
            CutsceneLayerVoice voice = VoiceFor(layer.Sound);
            float level = layer.Volume * layer.Envelope(time);
            if (level > voice.Target)
            {
                voice.Target = level;
                voice.Offset = time - layer.From;
            }
        }

        private void PoseVoice(CutsceneLayerVoice voice, float step)
        {
            bool snap = _snap;
            voice.Level = snap ? voice.Target : Mathf.MoveTowards(voice.Level, voice.Target, step);
            if (voice.Source == null)
            {
                return;
            }

            if (voice.Level <= 0f)
            {
                if (voice.Source.isPlaying)
                {
                    voice.Source.Stop();
                }

                return;
            }

            if (!voice.Source.isPlaying)
            {
                voice.StartAtOffset();
            }
            else if (snap && !voice.Loop)
            {
                // A seek puts a held one-shot where the clock says, or ends it when the clock is past it.
                voice.Source.Stop();
                voice.StartAtOffset();
            }

            ApplyVolume(voice);
        }

        private void ApplyVolume(CutsceneLayerVoice voice)
        {
            if (voice.Source != null)
            {
                voice.Source.volume = Mathf.Clamp01(voice.Level * voice.Gain * ChannelGain(voice.Channel) * _outro);
            }
        }

        // One held source per sound, made the first time a layer asks for it (so the allocation happens once).
        private CutsceneLayerVoice VoiceFor(CutsceneSound sound)
        {
            foreach (CutsceneLayerVoice held in _held)
            {
                if (held.Sound == sound && !held.DrivenBySet)
                {
                    return held;
                }
            }

            AudioClip clip = Resolve(sound, false, out float gain, out float pitch, out CutsceneSoundChannel channel, out bool _);
            // The slot says whether its file loops (a fallback plays the same way); a sound the bank does not know is a loop.
            int slot = _bank != null ? _bank.IndexOf(sound) : -1;
            bool loop = slot < 0 || _bank.Slots[slot].Loop;
            AudioSource source = null;
            if (clip != null)
            {
                source = NewSource($"Layer_{sound}", loop);
                source.clip = clip;
                source.pitch = pitch;
                source.volume = 0f;
            }

            var voice = new CutsceneLayerVoice(sound, source, gain, channel, loop, false);
            _held.Add(voice);
            return voice;
        }

        private void Play(AudioClip clip, float volume, float pitch, CutsceneSoundChannel channel)
        {
            if (clip == null || _voices == null)
            {
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, Mathf.Clamp(volume, 0f, SoundSet.MaxGain) * ChannelGain(channel));
        }

        private AudioClip MusicFor(CutsceneMusic mood, out float volume)
        {
            // A cutscene with its own bank plays only its own music: an empty mood stays silent.
            if (_bank != null)
            {
                volume = 0f;
                if (_bank.TryGetMusic(mood, out CutsceneMusicSlot track))
                {
                    volume = track.Volume;
                    return Recorded(track.Track);
                }

                return null;
            }

            switch (mood)
            {
                case CutsceneMusic.Wonder:
                    volume = WonderVolume;
                    // The prologue opens on the recorded menu theme (approved), so the first launch and the menu share one voice.
                    return OrElse(Recorded(_library != null ? _library.MenuMusic : null), () => ProceduralMusic.Calm());
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

        // The bank's slot when it has files; an empty slot plays its fallback (or nothing); a sound the bank does not
        // know comes from the library, then from a generated clip.
        private AudioClip Resolve(CutsceneSound sound, bool oneShot, out float gain, out float pitch, out CutsceneSoundChannel channel, out bool fromBank)
        {
            channel = CutsceneSoundChannel.Effects;
            fromBank = false;
            int slot = _bank != null ? _bank.IndexOf(sound) : -1;
            if (slot >= 0)
            {
                CutsceneSoundSlot entry = _bank.Slots[slot];
                channel = entry.Channel;
                if (!entry.IsEmpty)
                {
                    fromBank = true;
                    gain = entry.Clips.Gain;
                    pitch = entry.Clips.Pitch;
                    return oneShot ? RandomVariant(slot, entry.Clips) : entry.Clips[_random.Next(entry.Clips.Count)];
                }

                sound = entry.Fallback;
                if (sound == CutsceneSound.None)
                {
                    gain = 1f;
                    pitch = 1f;
                    return null;
                }
            }

            return ClipFor(sound, out gain, out pitch);
        }

        // A random variant that is never the one played last, so footsteps and bursts do not repeat back to back.
        private AudioClip RandomVariant(int slot, SoundSet set)
        {
            int count = set.Count;
            int index = count > 1 ? _random.Next(count - 1) : 0;
            if (count > 1 && index >= _lastVariant[slot])
            {
                index++;
            }

            _lastVariant[slot] = index;
            return set[index];
        }

        private AudioClip ClipFor(CutsceneSound sound, out float gain, out float pitch)
        {
            AudioClip clip = LibraryClip(sound, out gain, out pitch);
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

        // A borrowed set plays at its own pitch, as in the level (SfxLibrary pitches).
        private AudioClip LibraryClip(CutsceneSound sound, out float gain, out float pitch)
        {
            gain = 1f;
            pitch = 1f;
            SoundSet set = LibrarySet(sound);
            if (set == null || set.IsEmpty)
            {
                return null;
            }

            gain = set.Gain;
            pitch = set.Pitch;
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
                case CutsceneSound.RocketLaunch: return _library.RocketLaunches;
                case CutsceneSound.Thrusters: return _library.SteamBursts;
                case CutsceneSound.HeavySteps: return _library.HeavySteps;
                // The capsule touching down and any far explosion: Remco's recorded distant blast.
                case CutsceneSound.Landing:
                case CutsceneSound.DistantBlast: return _library.DistantBlasts;
                default: return null;
            }
        }

        private static AudioClip Generated(CutsceneSound sound)
        {
            switch (sound)
            {
                case CutsceneSound.Earthquake:
                case CutsceneSound.Rumble: return ProceduralSfx.QuakeRumble();
                case CutsceneSound.Landing:
                case CutsceneSound.DistantBlast: return ProceduralSfx.BigExplosion();
                case CutsceneSound.RocketLaunch: return ProceduralSfx.RocketLaunch();
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
