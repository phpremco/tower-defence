using System.Collections.Generic;
using Game.Combat;
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
        private const float BeamCoreWhiteness = 0.5f;
        // How far the beam runs along the aimed socket before it curves to the target: at most this many world units, and never
        // more than this share of the whole beam. The curve is a smooth Bézier (LineKit.Curve), so the bend has no kink.
        private const float BeamLeadLength = 0.9f;
        private const float BeamLeadShare = 0.3f;
        // A quadratic Bézier only gets about half way to its control point, so the control point lies this many leads out.
        private const float BendControlReach = 2f;
        // Below this share of the spade's axis across the screen the beam leaves straight for the target instead.
        private const float MinScreenLead = 0.3f;
        // How well the spade must point at the target (cosine of the angle on screen) before the beam follows it at all: a wide
        // strip bent through a sharper turn folds over its inner edge.
        private const float MinLeadAlignment = 0.5f;
        // The soft glows where the beam leaves the socket and where it strikes, as multiples of the halo width, and how bright.
        private const float SourceFlareScale = 1.2f;
        private const float ImpactFlareScale = 1.6f;
        private const float SourceFlareStrength = 0.9f;
        private const float ImpactFlareStrength = 0.7f;
        private const float BeamSparkRate = 70f;
        private const string EmitterName = "Orb";
        private const float EmitterFiringScale = 1.5f;
        private const float RingSeconds = 0.7f;
        private const float RingHeight = 0.08f;
        private const float RingThickness = 0.06f;
        private const float RingAlpha = 0.85f;
        private const float RingEmission = 2.5f;
        private const int RingSegments = 48;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const int PulseSparks = 60;
        private const int SporeBurst = 30;
        private const int VolleyBolts = 6;
        private const float VolleyBoltSize = 1.4f;
        private const int RegrowSparks = 40;
        private const float WardRingRadius = 2.6f;

        private BossAbilities _abilities;
        private Enemy _enemy;
        private readonly EnemyAttachments _attachments = new EnemyAttachments();
        private LineRenderer _beam;
        private LineRenderer _beamGlow;
        private BeamFlare _sourceFlare;
        private BeamFlare _impactFlare;
        private Transform _ring;
        private Material _ringMaterial;
        private Color _ringColour = Color.white;
        private bool _paintedRing;
        private float _ringAge = float.PositiveInfinity;
        private float _ringRadius;
        private ParticleSystem _sparks;
        private ParticleSystem _beamSparks;
        private EnemyView _view;
        private Camera _eye;
        private Transform _socket;
        private bool _socketSearched;
        private Transform _emitter;
        private bool _emitterSearched;
        private ParticleSystem _spores;
        private EnemyBoltView[] _bolts;
        private int _nextBolt;

        private void Awake()
        {
            _abilities = GetComponent<BossAbilities>();
            _enemy = GetComponent<Enemy>();
            _view = GetComponent<EnemyView>();
            if (_enemy != null)
            {
                _enemy.Deactivated += OnDeactivated;
            }

            _abilities.Pulsed += OnPulsed;
            _abilities.MinionsReleased += OnMinionsReleased;
            _abilities.Volleyed += OnVolleyed;
            _abilities.Rallied += OnRallied;
            _abilities.Regrown += OnRegrown;
            _abilities.Warded += OnWarded;
            _beam = _attachments.Track(Line("BossBeam", BeamWidth));
            _beamGlow = _attachments.Track(Line("BossBeamGlow", BeamGlowWidth));
            _sourceFlare = Flare("BeamSource");
            _impactFlare = Flare("BeamImpact");
            _ring = BuildRing();
            _sparks = _attachments.Track(ParticleKit.Create(transform.parent, "PulseSparks", Vector3.zero, Color.white, 0.4f, 0.8f, 0.05f, 0.12f, 2f, 5f, 0.6f, PulseSparks * 2, 0.3f));
            _beamSparks = _attachments.Track(ParticleKit.Create(transform.parent, "BeamSparks", Vector3.zero, Color.white, 0.25f, 0.5f, 0.04f, 0.09f, 0.8f, 2.5f, 1f, 80, 0.2f));
            _spores = _attachments.Track(ParticleKit.UseCell(ParticleKit.Create(transform.parent, "Spores", Vector3.zero, Color.white, 0.6f, 1.2f, 0.06f, 0.14f, 0.5f, 1.8f, -0.1f, SporeBurst * 2, 0.4f), MarkerArt.ParticleCell.Spore));
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
                _abilities.Warded -= OnWarded;
            }

            if (_enemy != null)
            {
                _enemy.Deactivated -= OnDeactivated;
            }
        }

        // Everything that lives beside the boss goes out with it the moment it leaves the field: a pulse ring
        // mid-fade, the beam and its tip, sparks, spores and bolts. OnDisable covers a boss that is destroyed.
        private void OnDeactivated(Enemy enemy) => Clear();

        private void OnDisable() => Clear();

        private void Clear()
        {
            _ringAge = float.PositiveInfinity;
            _attachments.Clear();
            _socketSearched = false;
            if (_emitter != null)
            {
                _emitter.localScale = Vector3.one;
            }
        }

        private void LateUpdate()
        {
            bool laser = _abilities.LaserActive;
            if (laser)
            {
                Color color = _abilities.LaserColor;
                // A white-hot core inside the wide soft halo; both lines wear the beam profile, so the beam has no hard edge.
                Color core = Color.Lerp(color, Color.white, BeamCoreWhiteness);
                _beam.startColor = core;
                _beam.endColor = core;
                Color halo = color;
                halo.a = BeamGlowAlpha;
                _beamGlow.startColor = halo;
                _beamGlow.endColor = halo;
                Vector3 origin = BeamOrigin(_abilities.LaserOrigin);
                Vector3 end = _abilities.LaserEnd;
                Vector3 bend = BeamBend(origin, end);
                LineKit.Curve(_beam, origin, bend, end, LineKit.FlaredEndWidth);
                LineKit.Curve(_beamGlow, origin, bend, end, LineKit.FlaredEndWidth);
                // The beam starts and ends at half width under glows that cover its tapers, so it grows out of the glow on the socket
                // and into the one where it strikes, with no cut edge and no dark gap between glow and beam.
                _sourceFlare.Show(origin, BeamGlowWidth * SourceFlareScale, color, SourceFlareStrength);
                _impactFlare.Show(end, BeamGlowWidth * ImpactFlareScale, color, ImpactFlareStrength);
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
                if (!laser)
                {
                    _sourceFlare.Hide();
                    _impactFlare.Hide();
                }

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
                SetRingColour(_ringColour, RingAlpha * (1f - t));
                _ring.gameObject.SetActive(t < 1f);
            }
        }

        private void OnPulsed(Vector3 centre, float radius, Color color)
        {
            _ringAge = 0f;
            _ringRadius = radius;
            _ring.position = new Vector3(centre.x, RingHeight, centre.z);
            _ringColour = color;
            SetRingColour(color, RingAlpha);
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
                    _bolts[i] = _attachments.Track(EnemyBoltView.Create(transform.parent, color));
                }
            }

            Vector3 from = BeamOrigin(origin);
            for (int i = 0; i < targets.Count; i++)
            {
                EnemyBoltView bolt = _bolts[_nextBolt];
                _nextBolt = (_nextBolt + 1) % _bolts.Length;
                bolt.Fire(from, targets[i] + Vector3.up * 0.4f, VolleyBoltSize);
            }
        }

        // A rally is a pulse that hurts nothing: the ring runs out wide and spores rise, in the call's colour.
        private void OnRallied(Vector3 centre, Color color)
        {
            OnPulsed(centre, 6f, color);
            OnMinionsReleased(centre, color);
        }

        // A ward reads as a ring that snaps round the body and a shimmer; when it drops, a second burst of sparks says "open".
        private void OnWarded(bool up, Color color)
        {
            if (up)
            {
                OnPulsed(_enemy.Position, WardRingRadius, color);
            }

            OnRegrown(color);
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

        // While the model holds its aim (the ray boss's tail over its back) the beam leaves the socket along its up axis, the way
        // the spade points, and curves to the target: the returned point is the control point of the beam's Bézier, so the beam
        // starts along that axis and bends without a kink. Otherwise the control point sits halfway and the beam is straight.
        private Vector3 BeamBend(Vector3 origin, Vector3 end)
        {
            Vector3 straight = Vector3.Lerp(origin, end, 0.5f);
            if (_socket == null || _view == null || !_view.IsAimingBeam)
            {
                return straight;
            }

            // Only the part of the spade's axis that crosses the screen: a lead towards or away from the camera folds the start
            // of the beam into a few pixels, where the camera-facing strip twists and the beam reads as a zigzag (round 51).
            Vector3 axis = _socket.up;
            Vector3 toEnd = end - origin;
            Camera eye = Eye();
            if (eye != null)
            {
                Vector3 view = eye.transform.forward;
                axis -= Vector3.Dot(axis, view) * view;
                toEnd -= Vector3.Dot(toEnd, view) * view;
            }

            if (axis.sqrMagnitude < MinScreenLead * MinScreenLead || toEnd.sqrMagnitude < 0.0001f)
            {
                return straight;
            }

            // A spade pointing away from the target would send the beam out and back in a hairpin, which folds the strip over
            // itself: the lead shrinks as the spade turns from the target and is gone once it points 60 degrees or more off.
            float towards = Mathf.InverseLerp(MinLeadAlignment, 1f, Vector3.Dot(axis.normalized, toEnd.normalized));
            float lead = Mathf.Min(BeamLeadLength, Vector3.Distance(origin, end) * BeamLeadShare) * towards;
            return Vector3.Lerp(straight, origin + axis.normalized * (lead * BendControlReach), towards);
        }

        private Camera Eye()
        {
            if (_eye == null)
            {
                _eye = Camera.main;
            }

            return _eye;
        }

        // Where a beam or a volley leaves the body this frame: the bone the art names for it (the ray boss's tail, the bat
        // boss's lens), so the shot follows the animated body; else a procedural model's orb; else the logic's point.
        private Vector3 BeamOrigin(Vector3 fallback)
        {
            if (!_socketSearched)
            {
                // The model is built after the first Initialize and again for a reused body, so it is looked up late.
                _socketSearched = true;
                _socket = _view != null ? _view.FindBeamSocket() : null;
            }

            if (_socket != null)
            {
                return _socket.position;
            }

            return Emitter() != null ? _emitter.position : fallback;
        }

        // A procedural model names the part the beam leaves from; it is built after the first Initialize, so look it up late.
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
            line.shadowCastingMode = ShadowCastingMode.Off;
            LineKit.UseFlaredBeam(line);
            line.enabled = false;
            return line;
        }

        private BeamFlare Flare(string name)
        {
            BeamFlare flare = BeamFlare.Create(transform.parent, name);
            _attachments.Track(flare.GameObject);
            return flare;
        }

        // The painted ring fades through its tint's alpha; the plain one through its colour and keeps its emission.
        private void SetRingColour(Color colour, float alpha)
        {
            if (_paintedRing)
            {
                _ringMaterial.SetColor(MarkerArt.Tint, new Color(colour.r, colour.g, colour.b, alpha));
                return;
            }

            _ringMaterial.color = new Color(colour.r, colour.g, colour.b, alpha);
            _ringMaterial.SetColor(EmissionColorId, colour * RingEmission);
        }

        // A thin ring of unit radius, scaled out as the shockwave travels, with its own copy of the see-through material.
        private Transform BuildRing()
        {
            MarkerArt art = MarkerArt.Active;
            MeshRenderer painted = art != null ? art.Place(MarkerKind.PulseRing, transform.parent, "PulseRing") : null;
            if (painted != null)
            {
                // README round 25b: the serrated organic ring, scaled in XZ only.
                _paintedRing = true;
                _ringMaterial = new Material(painted.sharedMaterial);
                painted.sharedMaterial = _ringMaterial;
                _attachments.Track(painted.gameObject);
                painted.gameObject.SetActive(false);
                return painted.transform;
            }

            var builder = new PropMeshBuilder(1);
            builder.AddTorus(Vector3.zero, Vector3.up, 1f, RingThickness, RingSegments, 6, 0);
            var ring = new GameObject("PulseRing");
            ring.transform.SetParent(transform.parent, false);
            ring.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("PulseRing");
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            _ringMaterial = new Material(PlaceholderVisuals.TransparentMaterialFor(Color.white, RingAlpha, RingEmission));
            renderer.sharedMaterial = _ringMaterial;
            _attachments.Track(ring);
            ring.SetActive(false);
            return ring.transform;
        }
    }
}
