using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The flames of the hero's jetpack: at each nozzle socket a painted flame card from the effect library (the rocket
    /// exhaust, tinted in the colony's cool colour) laid along the socket's exhaust axis and turned to the camera, plus a
    /// short stream of glowing motes. Without the library the motes carry it alone. Lit and put out by the view; the
    /// flames grow and shrink over a moment so the thrust never pops.
    /// </summary>
    public sealed class JetThrust
    {
        private const float FadeSeconds = 0.12f;
        private const float MoteRate = 45f;
        private const float BareMoteRate = 90f;
        private const float FlameWidthShare = 0.45f;
        private const float FlameFlicker = 0.12f;
        private const float FlickerSpeed = 23f;
        private const float MoteGlow = 2.5f;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");

        private readonly Transform[] _nozzles;
        private readonly MeshRenderer[] _flames;
        private readonly ParticleSystem[] _motes;
        private readonly VfxSheet _sheet;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Transform _camera;
        private readonly float _length;
        private readonly float _moteRate;
        private bool _lit;
        private float _strength;
        private float _clock;

        public JetThrust(Transform owner, Transform[] nozzles, Color colour, float length)
        {
            _nozzles = nozzles;
            _length = length;
            Camera camera = Camera.main;
            _camera = camera != null ? camera.transform : null;
            _sheet = Services.TryGet(out VfxLibrary library) && library.WeaponFor(ProjectileStyle.Rocket) != null ? library.WeaponFor(ProjectileStyle.Rocket).Exhaust : null;
            bool cards = _sheet != null && _camera != null;
            _moteRate = cards ? MoteRate : BareMoteRate;
            _flames = new MeshRenderer[cards ? nozzles.Length : 0];
            _motes = new ParticleSystem[nozzles.Length];
            for (int i = 0; i < nozzles.Length; i++)
            {
                _motes[i] = Motes(nozzles[i], colour);
                if (cards)
                {
                    _flames[i] = VfxQuad.Create(owner, "JetFlame", VfxQuad.FromStart);
                    _flames[i].sharedMaterial = _sheet.MaterialFor(ModelQuality.IsLow);
                    _block.Clear();
                    _block.SetColor(TintId, colour);
                    _block.SetFloat(FrameId, 0f);
                    _flames[i].SetPropertyBlock(_block);
                }
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
            for (int i = 0; i < _flames.Length; i++)
            {
                MeshRenderer flame = _flames[i];
                if (flame.gameObject.activeSelf != shown)
                {
                    flame.gameObject.SetActive(shown);
                }

                if (!shown)
                {
                    continue;
                }

                Transform nozzle = _nozzles[i];
                float size = _length * _strength * (1f + FlameFlicker * Mathf.Sin(_clock * FlickerSpeed + i * 1.7f));
                flame.transform.SetPositionAndRotation(nozzle.position, VfxQuad.Along(nozzle.up, _camera.forward));
                flame.transform.localScale = new Vector3(size, size * Mathf.Max(FlameWidthShare, _sheet.CellAspect * FlameWidthShare), 1f);
                if (_sheet.Frames > 1 && _sheet.Fps > 0f)
                {
                    flame.GetPropertyBlock(_block);
                    _block.SetFloat(FrameId, Mathf.Floor(_clock * _sheet.Fps) % _sheet.Frames);
                    flame.SetPropertyBlock(_block);
                }
            }
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
