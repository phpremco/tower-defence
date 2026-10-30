using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// The sound bed of a level: the generated score, which crossfades from the calm loop to the heavy one while a
    /// boss is on the field, the theme's ambience underneath, and the alarm and ground rumble that open a boss wave,
    /// during which the ambience ducks. Listens to events and the mix only; gameplay does not know it exists.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private const float CalmVolume = 0.42f;
        private const float IntenseVolume = 0.55f;
        private const float AmbienceVolume = 0.45f;
        private const float AlarmVolume = 0.8f;
        private const float RumbleVolume = 1f;
        private const float CrossfadeSeconds = 1.6f;
        private const float AnticipationSeconds = 18f;
        private const float DuckSeconds = 6f;
        private const float DuckDepth = 0.3f;
        private const float EndFadeSeconds = 2.5f;
        private const float PauseDepth = 0.5f;

        private LevelContext _context;
        private AudioMix _mix;
        private AudioSource _calm;
        private AudioSource _intense;
        private AudioSource _ambience;
        private AudioSource _stingers;
        private AudioClip _alarm;
        private float _intensity;
        private float _intenseUntil = float.NegativeInfinity;
        private float _duckUntil = float.NegativeInfinity;
        private float _ending = 1f;

        public void Initialize(LevelContext context, AudioMix mix)
        {
            _context = context;
            _mix = mix;
            _calm = Loop(ProceduralMusic.Calm());
            _intense = Loop(ProceduralMusic.Intense());
            // The recorded siren when the library has one, the generated one otherwise.
            Services.TryGet(out SfxLibrary library);
            _alarm = library != null && library.BossAlarms.Count > 0 ? library.BossAlarms[0] : ProceduralSfx.BossAlarm();
            _stingers = gameObject.AddComponent<AudioSource>();
            _stingers.playOnAwake = false;
            _stingers.spatialBlend = 0f;

            AudioClip bed = ProceduralSfx.Ambience(context.Level.Theme.Ambience);
            if (bed != null)
            {
                _ambience = Loop(bed);
            }

            _context.BossWaveStarted.Subscribe(OnBossWave);
            Apply();
        }

        private void OnDestroy()
        {
            _context?.BossWaveStarted.Unsubscribe(OnBossWave);
        }

        private AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.Play();
            return source;
        }

        // The siren and the rumble together, and the heavy loop takes over before the boss itself steps out.
        private void OnBossWave()
        {
            _stingers.volume = _mix.EffectsGain;
            _stingers.PlayOneShot(_alarm, AlarmVolume);
            _stingers.PlayOneShot(ProceduralSfx.QuakeRumble(), RumbleVolume);
            _intenseUntil = Time.unscaledTime + AnticipationSeconds;
            _duckUntil = Time.unscaledTime + DuckSeconds;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool heavy = _context.BossesAlive > 0 || Time.unscaledTime < _intenseUntil;
            _intensity = Mathf.MoveTowards(_intensity, heavy ? 1f : 0f, dt / CrossfadeSeconds);
            if (_context.Outcome != LevelOutcome.Running)
            {
                _ending = Mathf.MoveTowards(_ending, 0f, dt / EndFadeSeconds);
            }

            Apply();
        }

        private void Apply()
        {
            float paused = _context.Paused ? PauseDepth : 1f;
            float music = _mix.MusicGain * _ending * paused;
            _calm.volume = CalmVolume * (1f - _intensity) * music;
            _intense.volume = IntenseVolume * _intensity * music;
            if (_ambience != null)
            {
                float duck = Time.unscaledTime < _duckUntil ? DuckDepth : 1f;
                _ambience.volume = AmbienceVolume * _mix.AmbienceGain * duck * _ending;
            }
        }
    }
}
