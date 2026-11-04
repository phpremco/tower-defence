using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The look of one spawn origin (README round 23): the living burrow or liquid vent and its clips, glow, light and
    /// spores, following the <see cref="OriginDirector"/>. Closed shows bare ground; Marked breathes (a burrow's cracks
    /// pulse); the opening rumbles up to the burst, where the effect library's earth burst plays and the camera kicks
    /// once; open it idles and swells as each creature comes out; closing plays the opening backwards. View only.
    /// </summary>
    public sealed class SpawnOriginView : MonoBehaviour
    {
        private const string GlowSocket = "Glow";
        private const string DustSocket = "Dust";
        private const float CloseRate = -2f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private ArtModel _model;
        private SpawnOriginArt _art;
        private SpawnOriginDefinition _definition;
        private Color _tint;
        private Color _fullGlow;
        private Light _light;
        private ParticleSystem _spores;
        private GameObject _pool;
        private Transform _dust;
        private LevelContext _context;
        private OriginDirector _director;
        private int _index = -1;
        private OriginState _state = OriginState.Closed;
        private float _stateTime;
        private float _glowFrom;
        private float _glow;
        private int _closedClip;
        private int _markedClip;
        private int _openClip;
        private int _idleClip;
        private int _spawnClip;

        /// <summary>Builds the model in its closed pose; <see cref="Bind"/> hands it to the director.</summary>
        /// <param name="index">The nest this origin stands on, which is its index in the director.</param>
        public void Initialize(int index, SpawnOriginArt art, SpawnOriginDefinition definition, Color tint, ParticleSystem spores, GameObject pool)
        {
            _index = index;
            _art = art;
            _definition = definition;
            _tint = tint;
            _spores = spores;
            _pool = pool;
            _model = ArtModel.Create(transform, art.Model, true);
            KeepBoundsLive(_model);
            Material material = _model.Material;
            Color baked = material.GetColor(EmissionColorId);
            _fullGlow = art.TintWithElement ? tint * baked.maxColorComponent : baked;
            ModelArtDefinition model = art.Model;
            _closedClip = model.IndexOf(ModelClipRole.Closed);
            _markedClip = model.IndexOf(ModelClipRole.Marked);
            _openClip = model.IndexOf(ModelClipRole.Open);
            _idleClip = model.IndexOf(ModelClipRole.Idle);
            _spawnClip = model.IndexOf(ModelClipRole.Spawn);
            Transform glow = _model.Find(GlowSocket);
            _dust = _model.Find(DustSocket) ?? transform;
            _light = CreateLight(glow != null ? glow : transform, tint, definition.LightRange);
            if (_spores != null)
            {
                _spores.transform.SetParent(glow != null ? glow : transform, false);
            }

            Enter(OriginState.Closed);
        }

        /// <summary>Follows its origin in the level's director from now on.</summary>
        public void Bind(LevelContext context)
        {
            _context = context;
            _director = context.Origins;
            if (_director == null || _index >= _director.Count)
            {
                return;
            }

            _director.Signalled += OnSignalled;
            Enter(_director.StateOf(_index));
        }

        /// <summary>The state and the clip on top, for probes.</summary>
        public string Describe() => $"{_state} {_stateTime:0.0}s glow {_glow:0.00} once {(_model?.Player != null ? _model.Player.Once : -1)}";

        public OriginState State => _state;

        private void OnDestroy()
        {
            if (_director != null)
            {
                _director.Signalled -= OnSignalled;
            }

            _model?.Dispose();
        }

        private void OnSignalled(int origin, OriginSignal signal)
        {
            if (origin != _index)
            {
                return;
            }

            switch (signal)
            {
                case OriginSignal.StateChanged:
                    Enter(_director.StateOf(origin));
                    break;
                case OriginSignal.Burst:
                    Burst();
                    break;
                case OriginSignal.Emerged:
                    _model.Player?.PlayOnce(_spawnClip);
                    break;
            }
        }

        private void Enter(OriginState state)
        {
            OriginState previous = _state;
            _state = state;
            _stateTime = 0f;
            _glowFrom = _glow;
            ModelClipPlayer player = _model.Player;
            if (player == null)
            {
                return;
            }

            switch (state)
            {
                case OriginState.Closed:
                    player.Loop(_closedClip >= 0 ? _closedClip : _markedClip, 1f);
                    player.StopOnce();
                    break;
                case OriginState.Marked:
                    player.Loop(_markedClip, 1f);
                    if (previous != OriginState.Closing)
                    {
                        player.StopOnce();
                    }

                    break;
                case OriginState.Opening:
                    player.Loop(_markedClip, 1f);
                    player.PlayOnce(_openClip, 0f, true);
                    break;
                case OriginState.Open:
                    // The idle starts on the opening's last frame: swap the cycle under the held clip, then let it go.
                    player.Loop(_idleClip, 1f);
                    player.StopOnce();
                    break;
                case OriginState.Closing:
                    player.Loop(_markedClip, 1f);
                    player.PlayOnce(_openClip, 0f, false, CloseRate);
                    break;
            }

            bool open = state == OriginState.Opening || state == OriginState.Open;
            if (_spores != null)
            {
                if (open)
                {
                    _spores.Play();
                }
                else
                {
                    _spores.Stop();
                }
            }

            if (_pool != null)
            {
                _pool.SetActive(state != OriginState.Closed);
            }
        }

        private void Burst()
        {
            if (_context == null || _definition.BurstVfx == null)
            {
                return;
            }

            Vector3 at = _dust.position;
            _context.Projectiles.PlayBurst(at, _definition.BurstRadius, _definition.BurstVfx, _tint);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _stateTime += dt;
            _model?.Player?.Tick(dt);
            _glow = GlowNow();
            _model.Material.SetColor(EmissionColorId, _fullGlow * _glow);
            Vector2 light = _definition.LightIntensity;
            _light.enabled = _state != OriginState.Closed;
            _light.intensity = Mathf.Lerp(light.x, light.y, OpenShare());
        }

        // A burrow's cracks show only the glow: dark when closed, a slow pulse when marked. A vent's glow stays on.
        private float GlowNow()
        {
            float marked = _art.GlowMarks ? Pulse() : 1f;
            float closed = _art.GlowMarks ? 0f : 1f;
            float ramp = Mathf.Clamp01(_stateTime / _definition.OpenGlowSeconds);
            switch (_state)
            {
                case OriginState.Closed: return closed;
                case OriginState.Marked: return Mathf.Lerp(_glowFrom, marked, ramp);
                case OriginState.Opening: return Mathf.Lerp(_glowFrom, 1f, ramp);
                case OriginState.Open: return 1f;
                default: return Mathf.Lerp(1f, marked, ramp);
            }
        }

        private float Pulse()
        {
            Vector2 range = _definition.MarkedGlow;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / _definition.MarkedPulseSeconds);
            return Mathf.Lerp(range.x, range.y, wave);
        }

        private float OpenShare()
        {
            float ramp = Mathf.Clamp01(_stateTime / _definition.OpenGlowSeconds);
            switch (_state)
            {
                case OriginState.Opening: return ramp;
                case OriginState.Open: return 1f;
                case OriginState.Closing: return 1f - ramp;
                default: return 0f;
            }
        }

        /// <summary>
        /// The bind pose lies a long way below the pivot (the maw stored underground) and the clips move the mesh far from
        /// it, so a skinned origin or gate measures its bounds every frame; there are only a handful on a level.
        /// </summary>
        public static void KeepBoundsLive(ArtModel model)
        {
            foreach (Renderer renderer in model.Renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = true;
                }
            }
        }

        private static Light CreateLight(Transform parent, Color colour, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
