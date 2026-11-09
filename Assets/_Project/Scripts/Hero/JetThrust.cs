using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The flames of the hero's jetpack, all shaped by <see cref="HeroArtDefinition"/>: at each nozzle socket a wide outer
    /// flame card from the effect library (the rocket exhaust, tinted in the colony's cool colour), a shorter, narrower and
    /// brighter core card over it, a soft radial heat glow facing the camera, and a short stream of glowing motes; on the
    /// High profile one point light at the pack lights the ground under him. Without the library the motes carry it alone.
    /// Lit and put out by the view; everything grows and shrinks over a moment so the thrust never pops.
    /// </summary>
    public sealed class JetThrust
    {
        private const float FadeSeconds = 0.12f;
        private const float MoteRate = 45f;
        private const float BareMoteRate = 90f;
        private const float LowMoteShare = 0.5f;
        private const float FlameFlicker = 0.12f;
        private const float CoreFlicker = 0.06f;
        private const float GlowFlicker = 0.08f;
        private const float FlickerSpeed = 23f;
        private const float MoteGlow = 2.5f;
        private const int CoreSortingOrder = 1;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");

        private readonly Transform[] _nozzles;
        private readonly MeshRenderer[] _flames;
        private readonly MeshRenderer[] _cores;
        private readonly MeshRenderer[] _glows;
        private readonly ParticleSystem[] _motes;
        private readonly Light _light;
        private readonly VfxSheet _sheet;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Transform _camera;
        private readonly HeroArtDefinition _art;
        private readonly float _moteRate;
        private bool _lit;
        private float _strength;
        private float _clock;

        public JetThrust(Transform owner, Transform[] nozzles, HeroArtDefinition art)
        {
            _nozzles = nozzles;
            _art = art;
            Camera camera = Camera.main;
            _camera = camera != null ? camera.transform : null;
            bool low = ModelQuality.IsLow;
            WeaponVfxDefinition rocket = Services.TryGet(out VfxLibrary library) ? library.WeaponFor(ProjectileStyle.Rocket) : null;
            _sheet = rocket != null ? rocket.Exhaust : null;
            bool cards = _sheet != null && _camera != null;
            _moteRate = (cards ? MoteRate : BareMoteRate) * (low ? LowMoteShare : 1f);
            int cardCount = cards ? nozzles.Length : 0;
            _flames = new MeshRenderer[cardCount];
            _cores = new MeshRenderer[cardCount];
            _glows = new MeshRenderer[_camera != null && art.HeatGlowSize > 0f ? nozzles.Length : 0];
            _motes = new ParticleSystem[nozzles.Length];
            for (int i = 0; i < nozzles.Length; i++)
            {
                _motes[i] = Motes(nozzles[i], art.ThrustColor);
                if (i < cardCount)
                {
                    _flames[i] = Card(owner, "JetFlame", art.ThrustColor * art.ThrustGlow, low, 0);
                    _cores[i] = Card(owner, "JetCore", art.ThrustCoreColor * art.ThrustCoreGlow, low, CoreSortingOrder);
                }

                if (i < _glows.Length)
                {
                    _glows[i] = VfxQuad.Create(owner, "JetHeat", GlowDecal.Quad());
                    _glows[i].sharedMaterial = GlowDecal.RadialMaterial(art.HeatGlowColor * art.HeatGlowStrength);
                }
            }

            // One light for both nozzles: the pack is a single hot spot from the game camera, and a light costs per pixel.
            if (!low && art.HeatLightIntensity > 0f && art.HeatLightRange > 0f && nozzles.Length > 0)
            {
                _light = new GameObject("JetLight").AddComponent<Light>();
                _light.transform.SetParent(owner, false);
                _light.type = LightType.Point;
                _light.color = art.HeatGlowColor;
                _light.range = art.HeatLightRange;
                _light.shadows = LightShadows.None;
                _light.renderMode = LightRenderMode.ForcePixel;
                _light.enabled = false;
            }
        }

        public bool IsLit => _lit;

        public void Set(bool lit)
        {
            if (lit == _lit)
            {
                return;
            }

            _lit = lit;
            foreach (ParticleSystem motes in _motes)
            {
                ParticleKit.SetRate(motes, lit ? _moteRate : 0f);
            }
        }

        /// <summary>Grows or shrinks the flames and lays them along their nozzles; call once a frame after the pose.</summary>
        public void Tick(float deltaTime)
        {
            _strength = Mathf.MoveTowards(_strength, _lit ? 1f : 0f, deltaTime / FadeSeconds);
            _clock += deltaTime;
            bool shown = _strength > 0f;
            float frame = _sheet != null && _sheet.Frames > 1 && _sheet.Fps > 0f ? Mathf.Floor(_clock * _sheet.Fps) % _sheet.Frames : -1f;
            for (int i = 0; i < _flames.Length; i++)
            {
                float wave = Mathf.Sin(_clock * FlickerSpeed + i * 1.7f);
                float length = _art.ThrustLength * _strength * (1f + FlameFlicker * wave);
                float width = length * Mathf.Max(_art.ThrustWidth, _sheet.CellAspect * _art.ThrustWidth);
                PlaceFlame(_flames[i], _nozzles[i], shown, length, width, frame);
                float coreLength = _art.ThrustLength * _art.ThrustCoreLength * _strength * (1f + CoreFlicker * wave);
                PlaceFlame(_cores[i], _nozzles[i], shown, coreLength, width * _art.ThrustCoreWidth, frame);
            }

            for (int i = 0; i < _glows.Length; i++)
            {
                PlaceGlow(_glows[i], _nozzles[i], shown, i);
            }

            if (_light != null)
            {
                _light.enabled = shown;
                if (shown)
                {
                    _light.transform.position = PackCentre();
                    _light.intensity = _art.HeatLightIntensity * _strength * (1f + GlowFlicker * Mathf.Sin(_clock * FlickerSpeed));
                }
            }
        }

        private MeshRenderer Card(Transform owner, string name, Color tint, bool low, int sortingOrder)
        {
            MeshRenderer card = VfxQuad.Create(owner, name, VfxQuad.FromStart);
            card.sharedMaterial = _sheet.MaterialFor(low);
            card.sortingOrder = sortingOrder;
            _block.Clear();
            _block.SetColor(TintId, tint);
            _block.SetFloat(FrameId, 0f);
            card.SetPropertyBlock(_block);
            return card;
        }

        private void PlaceFlame(MeshRenderer card, Transform nozzle, bool shown, float length, float width, float frame)
        {
            if (card.gameObject.activeSelf != shown)
            {
                card.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            card.transform.SetPositionAndRotation(nozzle.position, VfxQuad.Along(nozzle.up, _camera.forward));
            card.transform.localScale = new Vector3(length, width, 1f);
            if (frame >= 0f)
            {
                card.GetPropertyBlock(_block);
                _block.SetFloat(FrameId, frame);
                card.SetPropertyBlock(_block);
            }
        }

        // The glow sits a little down the exhaust, where the flame is widest, and always faces the camera.
        private void PlaceGlow(MeshRenderer glow, Transform nozzle, bool shown, int index)
        {
            if (glow.gameObject.activeSelf != shown)
            {
                glow.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            Vector3 centre = nozzle.position + nozzle.up * (_art.ThrustLength * _art.ThrustCoreLength * 0.5f * _strength);
            glow.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(_camera.forward));
            float size = _art.HeatGlowSize * _strength * (1f + GlowFlicker * Mathf.Sin(_clock * FlickerSpeed * 0.7f + index * 2.3f));
            glow.transform.localScale = new Vector3(size, size, 1f);
        }

        private Vector3 PackCentre()
        {
            Vector3 sum = Vector3.zero;
            foreach (Transform nozzle in _nozzles)
            {
                sum += nozzle.position;
            }

            return sum / _nozzles.Length;
        }

        // Motes stream out of the nozzle along its exhaust axis (the socket's Y), a narrow cone.
        private static ParticleSystem Motes(Transform nozzle, Color colour)
        {
            ParticleSystem system = ParticleKit.Create(nozzle, "JetMotes", Vector3.zero, colour * MoteGlow, 0.12f, 0.28f, 0.035f, 0.08f, 2.2f, 3.4f, 0f, 60, 0.02f);
            system.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.025f;
            return system;
        }
    }
}
