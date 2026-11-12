using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// What the hero's abilities and reloads look like, apart from the ring the hero view plays: a dust ring kicked up by
    /// a shockwave, a welding beam from the hero to the gate with green sparks for a repair, and a short glint at the gun
    /// when it reloads (yellow when a perk makes it fast). Listens to the commander's events only.
    /// </summary>
    public sealed class HeroAbilityView : MonoBehaviour
    {
        private const float BeamSeconds = 2f;
        private const float BeamWidth = 0.07f;
        private const float BeamHeight = 0.9f;
        private const float BeamFlicker = 22f;
        private const int DustCount = 36;
        private const int WeldCount = 4;
        private const float WeldInterval = 0.12f;
        private const int GlintCount = 6;
        private const float GlintHeight = 1.1f;
        private static readonly Color DustColor = new Color(0.62f, 0.56f, 0.48f, 0.7f);
        private static readonly Color WeldColor = new Color(0.45f, 1f, 0.55f);
        private static readonly Color ReloadColor = new Color(0.35f, 0.95f, 1f);
        private static readonly Color FastReloadColor = new Color(1f, 0.9f, 0.35f);

        private Commander _hero;
        private ParticleSystem _dust;
        private ParticleSystem _weld;
        private ParticleSystem _glint;
        private LineRenderer _beam;
        private float _beamLeft;
        private float _nextWeld;
        private Color _beamColor;

        public void Initialize(Commander hero, float particleScale)
        {
            _hero = hero;
            _dust = ParticleKit.UseSmoke(ParticleKit.Create(transform, "AbilityDust", new Vector3(0f, 0.1f, 0f), DustColor, 0.6f, 1.1f, 0.12f, 0.3f, 2.5f, 4.5f, -0.05f, Scaled(DustCount * 2, particleScale), 0.3f));
            _weld = ParticleKit.Create(transform.parent, "WeldSparks", Vector3.zero, WeldColor * 2f, 0.2f, 0.45f, 0.02f, 0.05f, 1f, 2.5f, 1.5f, Scaled(60, particleScale), 0.1f);
            _glint = ParticleKit.Create(transform, "ReloadGlint", new Vector3(0f, GlintHeight, 0f), ReloadColor * 2f, 0.15f, 0.3f, 0.03f, 0.07f, 0.4f, 1.2f, 0f, Scaled(20, particleScale), 0.08f);

            var beamGo = new GameObject("RepairBeam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.useWorldSpace = true;
            _beam.widthMultiplier = BeamWidth;
            LineKit.UseBeam(_beam);
            _beam.shadowCastingMode = ShadowCastingMode.Off;
            _beam.enabled = false;

            _hero.AbilityUsed += OnAbilityUsed;
            _hero.Reloading += OnReloading;
        }

        private static int Scaled(int count, float scale) => Mathf.Max(1, Mathf.RoundToInt(count * scale));

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.AbilityUsed -= OnAbilityUsed;
                _hero.Reloading -= OnReloading;
            }

            if (_weld != null)
            {
                Destroy(_weld.gameObject);
            }
        }

        private void OnAbilityUsed(int slot, HeroAbilityDefinition ability, Vector3 position)
        {
            switch (ability.Kind)
            {
                case HeroAbilityKind.Shockwave:
                    EmitRing(_dust, DustCount, ability.Radius * 0.5f);
                    break;
                case HeroAbilityKind.GateRepair:
                    _beamLeft = BeamSeconds;
                    _beamColor = Color.Lerp(ability.Tint, Color.white, 0.3f);
                    _beam.enabled = true;
                    break;
            }
        }

        // The dust leaves the hero outwards along the ground, so the ring reads as a blast and not a puff.
        private static void EmitRing(ParticleSystem system, int count, float speed)
        {
            var emit = new ParticleSystem.EmitParams();
            Vector3 centre = system.transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var direction = new Vector3(Mathf.Cos(angle), 0.08f, Mathf.Sin(angle));
                emit.position = centre + direction * 0.3f;
                emit.velocity = direction * (speed + Random.Range(0f, 1.2f));
                emit.applyShapeToPosition = false;
                system.Emit(emit, 1);
            }
        }

        private void OnReloading(float seconds)
        {
            bool fast = _hero.Perks.ReloadMultiplier < 1f;
            ParticleSystem.MainModule main = _glint.main;
            main.startColor = (fast ? FastReloadColor : ReloadColor) * 2f;
            _glint.Emit(GlintCount);
        }

        private void LateUpdate()
        {
            if (_beamLeft <= 0f)
            {
                return;
            }

            _beamLeft -= Time.deltaTime;
            if (_beamLeft <= 0f || _hero.IsDown)
            {
                _beamLeft = 0f;
                _beam.enabled = false;
                return;
            }

            Vector3 from = _hero.transform.position + Vector3.up * BeamHeight;
            Vector3 to = _hero.GuardedPosition + Vector3.up * BeamHeight;
            _beam.SetPosition(0, from);
            _beam.SetPosition(1, to);
            float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * BeamFlicker);
            float fade = Mathf.Clamp01(_beamLeft / 0.3f);
            Color colour = _beamColor * flicker;
            colour.a = fade;
            _beam.startColor = colour;
            _beam.endColor = colour;

            _nextWeld -= Time.deltaTime;
            if (_nextWeld <= 0f)
            {
                _nextWeld = WeldInterval;
                _weld.transform.position = to;
                _weld.Emit(WeldCount);
            }
        }
    }
}
