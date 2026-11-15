using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// The base's shuttle on its calm round: it rests on the landing pad, spools its engines up (glow, soft plumes at its lift
    /// jets, dust off the pad, the roar swelling in), lifts off slowly and climbs away in a banking turn, nose up, burning on
    /// its main engine and drawing a vapour trail, and shrinks into the sky; it stays away a while, then comes home along the
    /// same way and settles. Round 63 (Remco: "it literally disappears as it starts to fly"): it never leaves the picture by a
    /// cut: it reaches nothing exactly as the climb ends, the plumes and the trail fading with it. Pure view code; every number
    /// is on the base's <see cref="BaseArtDefinition"/>.
    /// </summary>
    public sealed class BaseShuttleView : MonoBehaviour
    {
        private const float ThrustRate = 40f;
        private const float GlowRate = 18f;
        private const float LightIntensity = 2.2f;
        private const float LightRange = 2.5f;

        // The engine wash on the pad: a pool of the engine colour, widest as the shuttle hangs just above it.
        private const float WashRadius = 0.9f;
        private const float WashLift = 0.02f;
        private const float WashBrightness = 0.6f;
        private const float WashHide = 0.02f;

        // The lift jets carry it off the pad and hand over to the main engine over this first share of the climb.
        private const float LiftHandover = 0.45f;
        // The trail grows in once the shuttle is clear of its own dust, over this share of the climb.
        private const float TrailFrom = 0.08f;
        private const float TrailIn = 0.15f;
        private const int TrailPoints = 16;
        // The path is integrated in this many steps (the heading turns as it goes).
        private const int PathSteps = 12;
        private const float PlumeHide = 0.002f;
        // The main plume swings from straight down (lifting) to straight back (cruising) as the nose comes up.
        private const float PlumeBackShare = 0.6f;
        // On the pad the main engine only glows at this share of the lift jets' burn; it opens up as the shuttle leaves the pad.
        private const float MainIdleShare = 0.5f;
        private static readonly Color PlumeCore = new Color(0.9f, 0.96f, 1f) * 5f;
        private static readonly Color DustColour = new Color(0.55f, 0.55f, 0.6f, 0.55f);

        private Transform[] _parts;
        private Vector3[] _restPositions;
        private Quaternion[] _restRotations;
        private Vector3[] _restScales;
        private Vector3 _up;
        private Vector3 _nose;
        private BaseArtDefinition _art;
        private ParticleSystem[] _thrust = new ParticleSystem[0];
        private ParticleSystem _glow;
        private ParticleSystem _dust;
        private Light _light;
        private Transform _wash;
        private Vector3 _washScale;
        private Transform _exhaust;
        private Transform[] _lifts = new Transform[0];
        private Plume _main;
        private Plume[] _liftPlumes = new Plume[0];
        private LineRenderer _trail;
        private readonly Vector3[] _trailPoints = new Vector3[TrailPoints];
        private AudioSource _roar;
        private AudioMix _mix;
        private float _clock;
        private float _cycle;
        private bool _shown = true;

        /// <summary>A soft plume in the cutscene's ExhaustPlume style: an outer cone of gas and a white-hot core.</summary>
        private sealed class Plume
        {
            public Transform Outer;
            public Transform Core;
            public Material OuterMaterial;
            public Material CoreMaterial;
        }

        /// <summary>
        /// Takes over the shuttle's meshes (<paramref name="parts"/>, near first) with the sockets under the near one: the
        /// engine glow at <paramref name="exhaust"/> and the downward lift jets at <paramref name="lifts"/>.
        /// </summary>
        public void Initialize(BaseArtDefinition art, Transform[] parts, Transform exhaust, Transform[] lifts)
        {
            _art = art;
            _parts = parts;
            _exhaust = exhaust;
            _lifts = lifts;
            _restPositions = new Vector3[parts.Length];
            _restRotations = new Quaternion[parts.Length];
            _restScales = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                _restPositions[i] = parts[i].localPosition;
                _restRotations[i] = parts[i].localRotation;
                _restScales[i] = parts[i].localScale;
            }

            Transform parent = parts[0].parent;
            _up = parent.InverseTransformDirection(Vector3.up).normalized;
            // The nose points away from the main engine; the climb leans that way.
            Vector3 nose = exhaust != null ? parts[0].localPosition - parent.InverseTransformPoint(exhaust.position) : Vector3.forward;
            nose -= Vector3.Dot(nose, _up) * _up;
            _nose = nose.sqrMagnitude > 0.0001f ? nose.normalized : Vector3.forward;

            Color engine = art.EngineColor;
            _thrust = new ParticleSystem[lifts.Length];
            _liftPlumes = new Plume[lifts.Length];
            for (int i = 0; i < lifts.Length; i++)
            {
                _thrust[i] = ParticleKit.Create(lifts[i], "LiftJet", Vector3.zero, engine * 2f, 0.15f, 0.35f, 0.05f, 0.11f, 0.2f, 0.6f, 2.5f, 40, 0.04f);
                _liftPlumes[i] = BuildPlume($"LiftPlume{i}", engine);
            }

            if (exhaust != null)
            {
                _glow = ParticleKit.Create(exhaust, "EngineGlow", Vector3.zero, engine * 3f, 0.1f, 0.25f, 0.08f, 0.16f, 0.1f, 0.3f, 0f, 20, 0.03f);
                _light = new GameObject("EngineLight").AddComponent<Light>();
                _light.transform.SetParent(exhaust, false);
                _light.type = LightType.Point;
                _light.color = engine;
                _light.range = LightRange;
                _light.intensity = 0f;
                _light.shadows = LightShadows.None;
                _main = BuildPlume("MainPlume", engine);
            }

            // The wash lies on the pad under the resting shuttle, not on the shuttle, so it stays put as it climbs.
            GameObject wash = GlowDecal.PlacePool(parent, engine * WashBrightness, WashRadius);
            _wash = wash.transform;
            _wash.localPosition = new Vector3(_restPositions[0].x, _restPositions[0].y + WashLift, _restPositions[0].z);
            _washScale = _wash.localScale;
            wash.SetActive(false);

            _dust = ParticleKit.UseSmoke(ParticleKit.Create(parent, "PadDust", _wash.localPosition, DustColour, 0.9f, 1.8f, 0.3f, 0.75f, 0.9f, 1.9f, -0.04f, 120, 0.45f));
            ParticleKit.SetRate(_dust, 0f);
            _trail = BuildTrail(engine);
            BuildRoar();

            _cycle = art.ShuttleRestSeconds + art.ShuttleAwaySeconds + 2f * art.ShuttleFlightSeconds;
            // Each base starts somewhere in its rest, so the first lift-off is a moment into the level.
            _clock = Random.Range(0f, art.ShuttleRestSeconds * 0.75f);
        }

        /// <summary>Puts the shuttle back on the pad and stops its round (the base has fallen).</summary>
        public void Stop()
        {
            Pose(0f, 0f, 0f);
            if (_roar != null)
            {
                _roar.Stop();
            }

            enabled = false;
        }

        private void Update()
        {
            _clock = (_clock + Time.deltaTime) % _cycle;
            float rest = _art.ShuttleRestSeconds;
            float flight = _art.ShuttleFlightSeconds;
            float spool = Mathf.Min(_art.ShuttleSpoolSeconds, rest * 0.5f);
            float awayEnd = rest + flight + _art.ShuttleAwaySeconds;
            float idle = _art.ShuttleIdleThrust;
            if (_clock < rest)
            {
                // Spooling down just after touch-down and up again in the last moment of the rest.
                float down = 1f - Smooth(_clock / spool);
                float up = Smooth((_clock - (rest - spool)) / spool);
                float thrust = Mathf.Max(down, up);
                Pose(0f, thrust * idle, Mathf.Max(down * _art.ShuttleReturnShare, up) * thrust);
            }
            else if (_clock < rest + flight)
            {
                float climb = (_clock - rest) / flight;
                Pose(climb, Mathf.Lerp(idle, 1f, Smooth(climb / LiftHandover)), Mathf.Pow(1f - climb, 1.5f));
            }
            else if (_clock < awayEnd)
            {
                Show(false);
                Sound(0f);
            }
            else
            {
                float descent = (_clock - awayEnd) / flight;
                // The engines burn on through the touch-down and spool down after it; the roar swells as it comes home.
                Pose(1f - descent, Mathf.Lerp(idle, 1f, Smooth((1f - descent) / LiftHandover)), descent * descent * _art.ShuttleReturnShare);
            }
        }

        // Climb share 0 on the pad to 1 gone, the engines at the given thrust (0..1), the roar at the given share.
        private void Pose(float climb, float thrust, float roar)
        {
            Show(climb < 1f);
            Transform parent = _parts[0].parent;
            Vector3 offset = PathAt(climb);
            Quaternion turn = TurnAt(climb);
            float shrink = Smooth((climb - (1f - _art.ShuttleShrinkShare)) / _art.ShuttleShrinkShare);
            float scale = 1f - shrink;
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].localPosition = _restPositions[i] + offset;
                _parts[i].localRotation = turn * _restRotations[i];
                _parts[i].localScale = _restScales[i] * scale;
            }

            float lifting = 1f - Smooth(climb / LiftHandover);
            float burn = thrust * scale;
            for (int i = 0; i < _thrust.Length; i++)
            {
                ParticleKit.SetRate(_thrust[i], burn * lifting * ThrustRate);
            }

            Vector3 down = -(parent.rotation * (turn * _up));
            for (int i = 0; i < _liftPlumes.Length; i++)
            {
                PosePlume(_liftPlumes[i], _lifts[i].position, down, _art.ShuttleLiftPlumeRadius * scale, _art.ShuttleLiftPlumeLength * scale, burn * lifting);
            }

            if (_glow != null)
            {
                ParticleKit.SetRate(_glow, burn * GlowRate);
                _light.intensity = burn * LightIntensity;
                // From straight down on the pad to back along the climb as the nose comes up and the speed builds.
                Vector3 back = -(parent.rotation * (turn * _nose));
                Vector3 direction = Vector3.Slerp(down, back, PlumeBackShare * Smooth(climb / LiftHandover));
                PosePlume(_main, _exhaust.position, direction, _art.ShuttlePlumeRadius * scale, _art.ShuttlePlumeLength * scale, burn * Mathf.Lerp(MainIdleShare, 1f, Smooth(climb / TrailFrom)));
            }

            // Strongest with the engines on just above the pad, gone once the shuttle is high or the engines idle.
            float height = Vector3.Dot(offset, _up);
            float low = 1f - Smooth(height / _art.ShuttleDustHeight);
            float wash = thrust * low;
            _wash.gameObject.SetActive(wash > WashHide);
            _wash.localScale = _washScale * wash;
            ParticleKit.SetRate(_dust, thrust * low * _art.ShuttleDustRate);
            PoseTrail(climb, scale);
            Sound(roar);
        }

        // Where the shuttle is (its pivot's offset from its rest, in the base's frame) at a share of the climb: it lifts slowly
        // and then ever faster, and drifts forward along a heading that turns as it goes.
        private Vector3 PathAt(float climb)
        {
            float lift = _art.ShuttleClimb * climb * climb;
            Vector3 along = Vector3.zero;
            float previous = 0f;
            for (int step = 1; step <= PathSteps; step++)
            {
                float u = climb * step / PathSteps;
                float reach = _art.ShuttleDrift * u * u * u;
                Vector3 heading = Quaternion.AngleAxis(_art.ShuttleTurnDegrees * Smooth(u), _up) * _nose;
                along += heading * (reach - previous);
                previous = reach;
            }

            return _up * lift + along;
        }

        // Its turn at a share of the climb: the heading follows the path, the nose comes up with the speed and it banks into
        // the turn while it turns (none on the pad, none once straight).
        private Quaternion TurnAt(float climb)
        {
            float yaw = _art.ShuttleTurnDegrees * Smooth(climb);
            Vector3 nose = Quaternion.AngleAxis(yaw, _up) * _nose;
            Vector3 right = Vector3.Cross(_up, nose);
            float bank = Mathf.Sin(Mathf.Clamp01(climb) * Mathf.PI) * _art.ShuttleBankDegrees * Mathf.Sign(_art.ShuttleTurnDegrees);
            float pitch = _art.ShuttleClimbPitch * Smooth(climb / LiftHandover);
            return Quaternion.AngleAxis(-bank, nose) * Quaternion.AngleAxis(-pitch, right) * Quaternion.AngleAxis(yaw, _up);
        }

        // The vapour trail: the exhaust's way over the last moments of flight, from the engine (thin, bright) to nothing.
        private void PoseTrail(float climb, float scale)
        {
            float share = Smooth((climb - TrailFrom) / TrailIn) * scale;
            _trail.enabled = share > PlumeHide && _exhaust != null;
            if (!_trail.enabled)
            {
                return;
            }

            Transform parent = _parts[0].parent;
            Vector3 engine = _exhaust.position - parent.TransformPoint(_restPositions[0] + PathAt(climb));
            float span = _art.ShuttleTrailSeconds / _art.ShuttleFlightSeconds;
            for (int i = 0; i < TrailPoints; i++)
            {
                float u = Mathf.Max(0f, climb - span * i / (TrailPoints - 1));
                _trailPoints[i] = parent.TransformPoint(_restPositions[0] + PathAt(u)) + engine;
            }

            _trail.SetPositions(_trailPoints);
            _trail.widthMultiplier = _art.ShuttleTrailWidth * Mathf.Max(scale, 0.25f);
            Color head = _art.EngineColor;
            head.a = share;
            Color tail = head;
            tail.a = 0f;
            _trail.startColor = head;
            _trail.endColor = tail;
        }

        private void PosePlume(Plume plume, Vector3 mouth, Vector3 direction, float radius, float length, float throttle)
        {
            if (plume == null)
            {
                return;
            }

            bool on = throttle > PlumeHide && radius > PlumeHide;
            plume.Outer.gameObject.SetActive(on);
            plume.Core.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            float clock = Time.time;
            float flicker = 1f + 0.06f * Mathf.Sin(clock * 31f) + 0.04f * Mathf.Sin(clock * 53f + 1.3f);
            float reach = length * flicker * Mathf.Lerp(0.35f, 1f, throttle);
            Quaternion facing = Quaternion.LookRotation(direction);
            plume.Outer.SetPositionAndRotation(mouth, facing);
            plume.Outer.localScale = new Vector3(radius, radius, reach);
            plume.Core.SetPositionAndRotation(mouth, facing);
            plume.Core.localScale = new Vector3(radius * 0.45f, radius * 0.45f, reach * 0.4f);
            float intensity = _art.ShuttlePlumeIntensity * throttle;
            PlumeKit.Burn(plume.OuterMaterial, intensity * 0.45f, clock);
            PlumeKit.Burn(plume.CoreMaterial, intensity * 0.75f, clock + 0.37f);
        }

        private Plume BuildPlume(string name, Color engine)
        {
            Color edge = engine * 2.2f;
            var plume = new Plume { OuterMaterial = PlumeKit.Outer(PlumeCore, edge), CoreMaterial = PlumeKit.Core(PlumeCore, edge) };
            plume.Outer = PlumeKit.Cone(transform, name, plume.OuterMaterial);
            plume.Core = PlumeKit.Cone(transform, name + "Core", plume.CoreMaterial);
            plume.Outer.gameObject.SetActive(false);
            plume.Core.gameObject.SetActive(false);
            return plume;
        }

        private LineRenderer BuildTrail(Color engine)
        {
            var go = new GameObject("ShuttleTrail");
            go.transform.SetParent(transform, false);
            LineRenderer line = LineKit.UseBeam(go.AddComponent<LineRenderer>());
            line.useWorldSpace = true;
            line.positionCount = TrailPoints;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = engine;
            line.endColor = engine;
            line.enabled = false;
            return line;
        }

        private void BuildRoar()
        {
            Services.TryGet(out _mix);
            if (_art.ShuttleRoar == null)
            {
                return;
            }

            _roar = gameObject.AddComponent<AudioSource>();
            _roar.clip = _art.ShuttleRoar;
            _roar.loop = true;
            _roar.playOnAwake = false;
            _roar.spatialBlend = 0f;
            _roar.volume = 0f;
        }

        // The roar at a share of its level: it starts silent and stops silent, never with a click; a paused game holds it.
        private void Sound(float share)
        {
            if (_roar == null)
            {
                return;
            }

            float volume = Mathf.Clamp01(share) * _art.ShuttleRoarVolume * (_mix != null ? _mix.EffectsGain : 1f);
            _roar.volume = volume;
            bool paused = Time.timeScale <= 0f;
            if (volume <= 0f || paused)
            {
                if (_roar.isPlaying)
                {
                    if (paused)
                    {
                        _roar.Pause();
                    }
                    else
                    {
                        _roar.Stop();
                    }
                }

                return;
            }

            if (!_roar.isPlaying)
            {
                _roar.UnPause();
                if (!_roar.isPlaying)
                {
                    _roar.Play();
                }
            }
        }

        private void Show(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].gameObject.SetActive(shown);
            }
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
