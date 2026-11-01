using System.Collections.Generic;
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
        private const float CalmVolume = 0.32f;
        private const float IntenseVolume = 0.42f;
        private const float AmbienceVolume = 0.3f;
        private const float AlarmVolume = 0.8f;
        private const float RumbleVolume = 1f;
        private const float CrossfadeSeconds = 1.6f;
        private const float AnticipationSeconds = 18f;
        private const float DuckSeconds = 6f;
        private const float DuckDepth = 0.3f;
        private const float EndFadeSeconds = 2.5f;
        private const float PauseDepth = 0.5f;
        private const float LayerVolume = 0.06f;
        private const float CellsForFullPool = 24f;
        private const float PanWidth = 0.7f;
        // The lava recording churns too fast for a slow pool; played at this speed it heaves.
        private const float LavaPitch = 0.6f;
        // As the bed of a whole level the bubbling sits level with the generated wind, not under it like one pool.
        private const float LavaBedScale = 4.2f;

        private LevelContext _context;
        private AudioMix _mix;
        private AudioSource _calm;
        private AudioSource _intense;
        private AudioSource _ambience;
        private AudioSource _stingers;
        private AudioClip _alarm;
        private AudioClip _rumble;
        private readonly List<AmbienceLayer> _layers = new List<AmbienceLayer>();
        private bool _panned;
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
            _alarm = library != null && !library.BossAlarms.IsEmpty ? library.BossAlarms[0] : ProceduralSfx.BossAlarm();
            _rumble = library != null && !library.Earthquakes.IsEmpty ? library.Earthquakes[0] : ProceduralSfx.QuakeRumble();
            CreateLayers(library, context.Level);
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

        // Recorded loops on top of the generated bed, only for what the player can see: wind through the leaves of a
        // forest, a blizzard over the ice, and each pool of water, bog or lava bubbling at its own place. A pool's
        // loop grows with its size and leans to the side of the screen the pool lies on, so the ear can find it.
        private void CreateLayers(SfxLibrary library, LevelDefinition level)
        {
            if (library == null)
            {
                return;
            }

            switch (level.Theme.Ambience)
            {
                case AmbienceKind.Forest:
                    AddLayer(library.LeavesLoops, 1f, null);
                    break;
                case AmbienceKind.Blizzard:
                    AddLayer(library.IcyWindLoops, 1f, null);
                    break;
                case AmbienceKind.Lava:
                    // In the caves the lava is everywhere underfoot, so its bubbling is the bed itself rather than one pool.
                    AddLayer(library.LavaLoops, LavaBedScale, null, LavaPitch);
                    break;
            }

            AddPool(library.WaterLoops, level, TerrainKind.Water, TerrainKind.Bog);
            if (level.Theme.Ambience != AmbienceKind.Lava)
            {
                AddPool(library.LavaLoops, level, TerrainKind.Lava, TerrainKind.Lava, LavaPitch);
            }
        }

        private void AddPool(SoundSet set, LevelDefinition level, TerrainKind first, TerrainKind second, float pitch = 1f)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (KeyValuePair<Vector2Int, TerrainKind> cell in level.Terrain.Cells)
            {
                if (cell.Value == first || cell.Value == second)
                {
                    sum += level.Grid.CellToWorld(cell.Key);
                    count++;
                }
            }

            if (count > 0)
            {
                AddLayer(set, Mathf.Min(1f, count / CellsForFullPool), sum / count, pitch);
            }
        }

        private void AddLayer(SoundSet set, float scale, Vector3? centre, float pitch = 1f)
        {
            LoopVoice layer = LoopVoice.Create(gameObject, set);
            if (layer != null)
            {
                layer.Pitch = pitch;
                _layers.Add(new AmbienceLayer(layer, scale, centre));
            }
        }

        // The camera is fixed apart from zoom, so a pool's place in the stereo field is settled once it exists.
        private void PanLayers()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            foreach (AmbienceLayer layer in _layers)
            {
                if (layer.Centre.HasValue)
                {
                    float x = camera.WorldToViewportPoint(layer.Centre.Value).x;
                    layer.Voice.Pan = Mathf.Clamp((x - 0.5f) * 2f, -1f, 1f) * PanWidth;
                }
            }

            _panned = true;
        }

        private readonly struct AmbienceLayer
        {
            public readonly LoopVoice Voice;
            public readonly float Scale;
            public readonly Vector3? Centre;

            public AmbienceLayer(LoopVoice voice, float scale, Vector3? centre)
            {
                Voice = voice;
                Scale = scale;
                Centre = centre;
            }
        }

        // The siren and the rumble together, and the heavy loop takes over before the boss itself steps out.
        private void OnBossWave()
        {
            _stingers.volume = _mix.EffectsGain;
            _stingers.PlayOneShot(_alarm, AlarmVolume);
            _stingers.PlayOneShot(_rumble, RumbleVolume);
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
            float ambienceDuck = Time.unscaledTime < _duckUntil ? DuckDepth : 1f;
            if (_ambience != null)
            {
                _ambience.volume = AmbienceVolume * _mix.AmbienceGain * ambienceDuck * _ending;
            }

            if (!_panned)
            {
                PanLayers();
            }

            for (int i = 0; i < _layers.Count; i++)
            {
                AmbienceLayer layer = _layers[i];
                layer.Voice.Target = LayerVolume * layer.Scale * ambienceDuck * _ending;
                layer.Voice.Tick(Time.unscaledDeltaTime, _mix.AmbienceGain);
            }
        }
    }
}
