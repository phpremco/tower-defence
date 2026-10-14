using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Plays the generated sound effects for what happens in a level: shots, defeated enemies, explosions and base hits.
    /// Listens to events only; gameplay does not know it exists. Volume follows the master volume setting.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        private const int VoiceCount = 10;
        private const float MinGapSeconds = 0.035f;
        private const float PitchJitter = 0.08f;

        private AudioSource[] _voices;
        private int _next;
        private float _lastShot;
        private float _lastKill;
        private AudioClip _shot;
        private AudioClip _kill;
        private AudioClip _explosion;
        private AudioClip _baseHit;
        private LevelContext _context;
        private float _lastBaseHealth;

        public void Initialize(LevelContext context)
        {
            _context = context;
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
            }

            _shot = ProceduralSfx.Shot();
            _kill = ProceduralSfx.Kill();
            _explosion = ProceduralSfx.Explosion();
            _baseHit = ProceduralSfx.BaseHit();

            _lastBaseHealth = context.BaseHealth.Current;
            context.Projectiles.Shot += OnShot;
            context.Projectiles.Exploded += OnExploded;
            context.EnemyDefeated.Subscribe(OnKilled);
            context.BaseHealth.Changed += OnBaseChanged;
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Projectiles.Shot -= OnShot;
            _context.Projectiles.Exploded -= OnExploded;
            _context.EnemyDefeated.Unsubscribe(OnKilled);
            _context.BaseHealth.Changed -= OnBaseChanged;
        }

        private void OnShot(Vector3 position)
        {
            if (Time.unscaledTime - _lastShot < MinGapSeconds)
            {
                return;
            }

            _lastShot = Time.unscaledTime;
            Play(_shot, 0.5f);
        }

        private void OnKilled()
        {
            if (Time.unscaledTime - _lastKill < MinGapSeconds)
            {
                return;
            }

            _lastKill = Time.unscaledTime;
            Play(_kill, 0.7f);
        }

        private void OnExploded(Vector3 position, float radius) => Play(_explosion, Mathf.Clamp(0.4f + radius * 0.2f, 0.4f, 1f));

        private void OnBaseChanged(float current)
        {
            if (current < _lastBaseHealth)
            {
                Play(_baseHit, 1f);
            }

            _lastBaseHealth = current;
        }

        private void Play(AudioClip clip, float volume)
        {
            AudioSource voice = _voices[_next];
            _next = (_next + 1) % _voices.Length;
            voice.pitch = 1f + Random.Range(-PitchJitter, PitchJitter);
            voice.PlayOneShot(clip, volume);
        }
    }
}
