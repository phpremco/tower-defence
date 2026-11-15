using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The aura of the blade once its damage upgrade has reached a stage (orange halfway, red at the last tier; the
    /// stage gives the colour and how strong the aura is): a long soft glow laid along the blade and turned to the camera, a rounder
    /// glow about its middle that reads from the play camera, and embers shed along its length. The cards are laid from
    /// the blade's own base and tip every frame after the body is posed, so they follow it through every cut, and all of
    /// it scales with how far the blade has ignited, so it comes and goes with the blade instead of popping. Shaped by
    /// <see cref="HeroGearArtDefinition"/>.
    /// </summary>
    public sealed class BladeAura
    {
        private const float EmberLowShare = 0.34f;
        private const float EmberMinLife = 0.18f;
        private const float EmberMaxLife = 0.4f;
        private const float EmberMinSize = 0.02f;
        private const float EmberMaxSize = 0.045f;
        private const float EmberMaxSpeed = 0.15f;
        private const float EmberRise = -0.2f;
        private const int EmberMax = 40;
        private const float EmberGlow = 2f;

        // The long glow reaches a little past the hilt and the tip, where it has faded to nothing.
        private const float SheathOverreach = 1.3f;

        private readonly HeroGearArtDefinition _art;
        private readonly BladeAuraStage _stage;
        private readonly Transform _blade;
        private readonly MeshRenderer _sheath;
        private readonly MaterialPropertyBlock _sheathBlock = new MaterialPropertyBlock();
        private readonly BeamFlare _glow;
        private readonly ParticleSystem _embers;
        private readonly float _bottom;
        private readonly float _length;
        private Transform _camera;

        private BladeAura(HeroGearArtDefinition art, BladeAuraStage stage, ArtModel blade, Transform owner)
        {
            _art = art;
            _stage = stage;
            _blade = blade.Root;
            MeasureAlong(blade, out _bottom, out _length);
            _sheath = VfxQuad.Create(owner, "BladeAuraSheath", VfxQuad.Centred);
            _sheath.sharedMaterial = LineKit.Flare();
            _glow = BeamFlare.Create(owner, "BladeAuraGlow");
            _glow.Hide();
            if (art.EmberRate > 0f)
            {
                _embers = ParticleKit.Create(_blade, "BladeEmbers", new Vector3(0f, _bottom + _length * 0.5f, 0f), stage.EmberColor * EmberGlow, EmberMinLife, EmberMaxLife, EmberMinSize, EmberMaxSize, 0f, EmberMaxSpeed, EmberRise, EmberMax);
                // Shed along the blade's whole length (its own Y), not from a ball at its middle.
                ParticleSystem.ShapeModule shape = _embers.shape;
                shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
                shape.radius = _length * 0.5f;
                shape.rotation = new Vector3(0f, 0f, 90f);
            }
        }

        /// <summary>The aura for a blade model (it grows along its root's own Y as it ignites); <paramref name="owner"/> is where the glow cards live.</summary>
        public static BladeAura Create(ArtModel blade, HeroGearArtDefinition art, BladeAuraStage stage, Transform owner) => new BladeAura(art, stage, blade, owner);

        /// <summary>The stage this aura shows.</summary>
        public BladeAuraStage Stage => _stage;

        /// <summary>Lays the aura on the blade as it is this frame. Call after the body has been posed (LateUpdate).</summary>
        public void Tick(bool lit, float ignite)
        {
            float shown = lit ? Mathf.Clamp01(ignite) : 0f;
            bool on = shown > 0.001f && FindCamera();
            if (_sheath.gameObject.activeSelf != on)
            {
                _sheath.gameObject.SetActive(on);
            }

            if (_embers != null)
            {
                ParticleKit.SetRate(_embers, on ? _art.EmberRate * _stage.Strength * (ModelQuality.IsLow ? EmberLowShare : 1f) * shown : 0f);
            }

            if (!on)
            {
                _glow.Hide();
                return;
            }

            // The blade's root grows along its Y with the ignition, so base and tip are where the lit blade really is.
            Vector3 hilt = _blade.TransformPoint(new Vector3(0f, _bottom, 0f));
            Vector3 tip = _blade.TransformPoint(new Vector3(0f, _bottom + _length, 0f));
            Vector3 axis = tip - hilt;
            float reach = axis.magnitude;
            if (reach < 0.0001f)
            {
                return;
            }

            float breath = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / _art.AuraPulseSeconds);
            float strength = Mathf.Lerp(_art.AuraLowShare, 1f, breath) * shown * _stage.Strength;
            Vector3 middle = (hilt + tip) * 0.5f;
            _sheath.transform.SetPositionAndRotation(middle, VfxQuad.Along(axis, _camera.forward));
            _sheath.transform.localScale = new Vector3(reach * SheathOverreach, _art.AuraWidth, 1f);
            LineKit.SetFlareGlow(_sheathBlock, _stage.BladeColor, _art.AuraGlow * strength);
            _sheath.SetPropertyBlock(_sheathBlock);
            _glow.Show(middle, Mathf.Max(0.05f, reach * _art.HaloSize), _stage.BladeColor, _art.HaloStrength * strength);
        }

        public void Dispose()
        {
            if (_sheath != null)
            {
                Object.Destroy(_sheath.gameObject);
            }

            if (_glow != null)
            {
                Object.Destroy(_glow.GameObject);
            }
        }

        private bool FindCamera()
        {
            if (_camera == null)
            {
                Camera main = Camera.main;
                _camera = main != null ? main.transform : null;
            }

            return _camera != null;
        }

        // Where the blade's meshes start and how long they are along the blade root's own Y, with the blade at full length.
        private static void MeasureAlong(ArtModel model, out float bottom, out float length)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            Transform root = model.Root;
            foreach (Renderer renderer in model.Renderers)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                Bounds local = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3(
                        (corner & 1) == 0 ? local.min.x : local.max.x,
                        (corner & 2) == 0 ? local.min.y : local.max.y,
                        (corner & 4) == 0 ? local.min.z : local.max.z);
                    float y = root.InverseTransformPoint(renderer.transform.TransformPoint(point)).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }

            bool any = max > min;
            bottom = any ? min : 0f;
            length = any ? max - min : 1f;
        }
    }
}
