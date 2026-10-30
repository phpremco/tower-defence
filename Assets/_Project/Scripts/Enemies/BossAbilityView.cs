using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Enemies
{
    /// <summary>
    /// Shows what a boss's powers do: a thick glowing beam with a burning tip while the laser sweeps, an expanding
    /// ring and sparks for a pulse, a burst of spores when minions are released. Reads <see cref="BossAbilities"/> only.
    /// </summary>
    [RequireComponent(typeof(BossAbilities))]
    public sealed class BossAbilityView : MonoBehaviour
    {
        private const float BeamWidth = 0.3f;
        private const float BeamGlowWidth = 0.95f;
        private const float BeamGlowAlpha = 0.45f;
        private const float BeamEmission = 4f;
        private const float BeamTipSize = 0.5f;
        private const float BeamTipEmission = 3f;
        private const float BeamSparkRate = 70f;
        private const string EmitterName = "Orb";
        private const float EmitterFiringScale = 1.5f;
        private const float RingSeconds = 0.7f;
        private const float RingHeight = 0.08f;
        private const float RingThickness = 0.06f;
        private const float RingAlpha = 0.85f;
        private const float RingEmission = 2.5f;
        private const int RingSegments = 48;
        private const int PulseSparks = 60;
        private const int SporeBurst = 30;
        private const int VolleyBolts = 6;
        private const float VolleyBoltSize = 1.4f;
        private const int RegrowSparks = 40;

        private BossAbilities _abilities;
        private LineRenderer _beam;
        private LineRenderer _beamGlow;
        private Transform _beamTip;
        private Renderer _beamTipRenderer;
        private Transform _ring;
        private Material _ringMaterial;
        private float _ringAge = float.PositiveInfinity;
        private float _ringRadius;
        private ParticleSystem _sparks;
        private ParticleSystem _beamSparks;
        private Transform _emitter;
        private bool _emitterSearched;
        private ParticleSystem _spores;
        private EnemyBoltView[] _bolts;
        private int _nextBolt;

        private void Awake()
        {
            _abilities = GetComponent<BossAbilities>();
            _abilities.Pulsed += OnPulsed;
            _abilities.MinionsReleased += OnMinionsReleased;
            _abilities.Volleyed += OnVolleyed;
            _abilities.Rallied += OnRallied;
            _abilities.Regrown += OnRegrown;
            _beam = Line("BossBeam", BeamWidth);
            _beamGlow = Line("BossBeamGlow", BeamGlowWidth);
            _beamTip = BuildTip();
            _ring = BuildRing();
            _sparks = ParticleKit.Create(transform.parent, "PulseSparks", Vector3.zero, Color.white, 0.4f, 0.8f, 0.05f, 0.12f, 2f, 5f, 0.6f, PulseSparks * 2, 0.3f);
            _beamSparks = ParticleKit.Create(transform.parent, "BeamSparks", Vector3.zero, Color.white, 0.25f, 0.5f, 0.04f, 0.09f, 0.8f, 2.5f, 1f, 80, 0.2f);
            _spores = ParticleKit.Create(transform.parent, "Spores", Vector3.zero, Color.white, 0.6f, 1.2f, 0.06f, 0.14f, 0.5f, 1.8f, -0.1f, SporeBurst * 2, 0.4f);
        }

        private void OnDestroy()
        {
            if (_abilities != null)
            {
                _abilities.Pulsed -= OnPulsed;
                _abilities.MinionsReleased -= OnMinionsReleased;
                _abilities.Volleyed -= OnVolleyed;
                _abilities.Rallied -= OnRallied;
                _abilities.Regrown -= OnRegrown;
            }
        }

        // The beam lives beside the boss, so when the pooled boss is switched off LateUpdate cannot hide it: do it here.
        private void OnDisable()
        {
            if (_beam != null && _beam.enabled)
            {
                _beam.enabled = false;
                _beamGlow.enabled = false;
                _beamTip.gameObject.SetActive(false);
                ParticleKit.SetRate(_beamSparks, 0f);
            }
        }

        private void LateUpdate()
        {
            bool laser = _abilities.LaserActive;
            if (laser)
            {
                Color color = _abilities.LaserColor;
                // The core is an opaque, glowing material, so it never sinks behind the see-through halo drawn around it.
                _beam.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(color, Color.white, 0.5f), BeamEmission);
                Color halo = color;
                halo.a = BeamGlowAlpha;
                _beamGlow.startColor = halo;
                _beamGlow.endColor = halo;
                Vector3 origin = Emitter() != null ? _emitter.position : _abilities.LaserOrigin;
                Vector3 end = _abilities.LaserEnd;
                _beam.SetPosition(0, origin);
                _beam.SetPosition(1, end);
                _beamGlow.SetPosition(0, origin);
                _beamGlow.SetPosition(1, end);
                _beamTip.position = end;
                _beamTipRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(color, Color.white, 0.4f), BeamTipEmission);
                _beamSparks.transform.position = end;
                ParticleSystem.MainModule main = _beamSparks.main;
                Color spark = color * 2f;
                spark.a = 1f;
                main.startColor = spark;
            }

            if (_beam.enabled != laser)
            {
                _beam.enabled = laser;
                _beamGlow.enabled = laser;
                _beamTip.gameObject.SetActive(laser);
                ParticleKit.SetRate(_beamSparks, laser ? BeamSparkRate : 0f);
                if (Emitter() != null)
                {
                    // The emitter swells while it fires, so the weapon reads as the source of the beam.
                    _emitter.localScale = Vector3.one * (laser ? EmitterFiringScale : 1f);
                }

            }

            if (_ringAge < RingSeconds)
            {
                _ringAge += Time.deltaTime;
                float t = Mathf.Clamp01(_ringAge / RingSeconds);
                float radius = Mathf.Lerp(0.3f, _ringRadius, Mathf.Sqrt(t));
                _ring.localScale = new Vector3(radius, 1f, radius);
                Color color = _ringMaterial.color;
                color.a = RingAlpha * (1f - t);
                _ringMaterial.color = color;
                _ring.gameObject.SetActive(t < 1f);
            }
        }

        private void OnPulsed(Vector3 centre, float radius, Color color)
        {
            _ringAge = 0f;
            _ringRadius = radius;
            _ring.position = new Vector3(centre.x, RingHeight, centre.z);
            Color tint = color;
            tint.a = RingAlpha;
            _ringMaterial.color = tint;
            _ringMaterial.SetColor("_EmissionColor", color * RingEmission);
            _ring.gameObject.SetActive(true);

            _sparks.transform.position = centre + Vector3.up * 0.3f;
            ParticleSystem.MainModule main = _sparks.main;
            Color glow = color * 2f;
            glow.a = 1f;
            main.startColor = glow;
            _sparks.Emit(PulseSparks);
        }

        private void OnMinionsReleased(Vector3 centre, Color color)
        {
            _spores.transform.position = centre + Vector3.up * 0.5f;
            ParticleSystem.MainModule main = _spores.main;
            Color glow = color * 1.6f;
            glow.a = 1f;
            main.startColor = glow;
            _spores.Emit(SporeBurst);
        }

        // Every bolt of a volley flies from the boss to its own target; the bolts are made on the first volley.
        private void OnVolleyed(Vector3 origin, IReadOnlyList<Vector3> targets, Color color)
        {
            if (_bolts == null)
            {
                _bolts = new EnemyBoltView[VolleyBolts];
                for (int i = 0; i < VolleyBolts; i++)
                {
                    _bolts[i] = EnemyBoltView.Create(transform.parent, color);
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyBoltView bolt = _bolts[_nextBolt];
                _nextBolt = (_nextBolt + 1) % _bolts.Length;
                bolt.Fire(origin, targets[i] + Vector3.up * 0.4f, VolleyBoltSize);
            }
        }

        // A rally is a pulse that hurts nothing: the ring runs out wide and spores rise, in the call's colour.
        private void OnRallied(Vector3 centre, Color color)
        {
            OnPulsed(centre, 6f, color);
            OnMinionsReleased(centre, color);
        }

        // Regrowing armour shimmers: a burst of sparks that hang around the body.
        private void OnRegrown(Color color)
        {
            _sparks.transform.position = transform.position + Vector3.up * 0.6f;
            ParticleSystem.MainModule main = _sparks.main;
            Color glow = color * 2f;
            glow.a = 1f;
            main.startColor = glow;
            _sparks.Emit(RegrowSparks);
        }

        // The model names the part the beam leaves from; it is built after the first Initialize, so look it up late.
        private Transform Emitter()
        {
            if (!_emitterSearched)
            {
                _emitterSearched = true;
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child.name == EmitterName)
                    {
                        _emitter = child;
                        break;
                    }
                }
            }

            return _emitter;
        }

        // Beams live beside the boss, not under it, so they are never scaled or tilted with the body.
        private LineRenderer Line(string name, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.positionCount = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            line.enabled = false;
            return line;
        }

        private Transform BuildTip()
        {
            GameObject tip = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.white, transform.parent, Vector3.zero, Vector3.one * BeamTipSize, BeamTipEmission);
            tip.name = "BeamTip";
            _beamTipRenderer = tip.GetComponent<Renderer>();
            _beamTipRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tip.SetActive(false);
            return tip.transform;
        }

        // A thin ring of unit radius, scaled out as the shockwave travels, with its own copy of the see-through material.
        private Transform BuildRing()
        {
            var builder = new PropMeshBuilder(1);
            builder.AddTorus(Vector3.zero, Vector3.up, 1f, RingThickness, RingSegments, 6, 0);
            var ring = new GameObject("PulseRing");
            ring.transform.SetParent(transform.parent, false);
            ring.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("PulseRing");
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            _ringMaterial = new Material(PlaceholderVisuals.TransparentMaterialFor(Color.white, RingAlpha, RingEmission));
            renderer.sharedMaterial = _ringMaterial;
            ring.SetActive(false);
            return ring.transform;
        }
    }
}
