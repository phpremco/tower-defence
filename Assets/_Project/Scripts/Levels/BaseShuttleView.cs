using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The base's shuttle on its calm round: it rests on the landing pad, lifts off on its engines (eased in and out),
    /// climbs away on a slant and shrinks out of sight, stays away a while, then comes back down the same way and settles.
    /// Moves the shuttle's near and far mesh together and lights its engines while it flies. Pure view code.
    /// </summary>
    public sealed class BaseShuttleView : MonoBehaviour
    {
        // The last share of the climb in which the shuttle shrinks away (an orthographic camera shows no distance).
        private const float VanishShare = 0.35f;

        // Engines spool up before lift-off and down after touch-down, in seconds.
        private const float SpoolSeconds = 0.6f;

        private const float ThrustRate = 40f;
        private const float GlowRate = 18f;
        private const float LightIntensity = 2.2f;
        private const float LightRange = 2.5f;

        // The engine wash on the pad: a pool of the engine colour, widest as the shuttle hangs just above it.
        private const float WashRadius = 0.9f;
        private const float WashLift = 0.02f;
        private const float WashBrightness = 0.6f;
        private const float WashHide = 0.02f;

        private Transform[] _parts;
        private Vector3[] _restPositions;
        private Vector3[] _restScales;
        private Vector3 _up;
        private Vector3 _nose;
        private BaseArtDefinition _art;
        private ParticleSystem[] _thrust = new ParticleSystem[0];
        private ParticleSystem _glow;
        private Light _light;
        private Transform _wash;
        private Vector3 _washScale;
        private float _clock;
        private float _cycle;
        private bool _shown = true;

        /// <summary>
        /// Takes over the shuttle's meshes (<paramref name="parts"/>, near first) with the sockets under the near one: the
        /// engine glow at <paramref name="exhaust"/> and the downward lift jets at <paramref name="lifts"/>.
        /// </summary>
        public void Initialize(BaseArtDefinition art, Transform[] parts, Transform exhaust, Transform[] lifts)
        {
            _art = art;
            _parts = parts;
            _restPositions = new Vector3[parts.Length];
            _restScales = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                _restPositions[i] = parts[i].localPosition;
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
            for (int i = 0; i < lifts.Length; i++)
            {
                _thrust[i] = ParticleKit.Create(lifts[i], "LiftJet", Vector3.zero, engine * 2f, 0.15f, 0.35f, 0.05f, 0.11f, 0.2f, 0.6f, 2.5f, 40, 0.04f);
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
            }

            // The wash lies on the pad under the resting shuttle, not on the shuttle, so it stays put as it climbs.
            GameObject wash = GlowDecal.PlacePool(parent, engine * WashBrightness, WashRadius);
            _wash = wash.transform;
            _wash.localPosition = new Vector3(_restPositions[0].x, _restPositions[0].y + WashLift, _restPositions[0].z);
            _washScale = _wash.localScale;
            wash.SetActive(false);

            _cycle = art.ShuttleRestSeconds + art.ShuttleAwaySeconds + 2f * art.ShuttleFlightSeconds;
            // Each base starts somewhere in its rest, so the first lift-off is a moment into the level.
            _clock = Random.Range(0f, art.ShuttleRestSeconds * 0.75f);
        }

        /// <summary>Puts the shuttle back on the pad and stops its round (the base has fallen).</summary>
        public void Stop()
        {
            Pose(0f, 0f);
            enabled = false;
        }

        private void Update()
        {
            _clock = (_clock + Time.deltaTime) % _cycle;
            float rest = _art.ShuttleRestSeconds;
            float flight = _art.ShuttleFlightSeconds;
            float awayEnd = rest + flight + _art.ShuttleAwaySeconds;
            if (_clock < rest)
            {
                // Spooling down just after touch-down and up again in the last moment of the rest.
                float down = 1f - _clock / SpoolSeconds;
                float up = (_clock - (rest - SpoolSeconds)) / SpoolSeconds;
                Pose(0f, Mathf.Clamp01(Mathf.Max(down, up)));
            }
            else if (_clock < rest + flight)
            {
                Pose((_clock - rest) / flight, 1f);
            }
            else if (_clock < awayEnd)
            {
                Show(false);
            }
            else
            {
                float descent = (_clock - awayEnd) / flight;
                // The engines burn on through the touch-down and spool down after it.
                Pose(1f - descent, 1f);
            }
        }

        // Height share 0 on the pad to 1 gone; the engines at the given thrust.
        private void Pose(float climb, float thrust)
        {
            Show(true);
            float eased = climb * climb * (3f - 2f * climb);
            Vector3 lift = _up * (_art.ShuttleClimb * eased) + _nose * (_art.ShuttleDrift * eased * eased);
            float vanish = Mathf.Clamp01((climb - (1f - VanishShare)) / VanishShare);
            float scale = 1f - vanish * vanish * (3f - 2f * vanish);
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].localPosition = _restPositions[i] + lift;
                _parts[i].localScale = _restScales[i] * scale;
            }

            for (int i = 0; i < _thrust.Length; i++)
            {
                ParticleKit.SetRate(_thrust[i], thrust * ThrustRate * (1f - vanish));
            }

            if (_glow != null)
            {
                ParticleKit.SetRate(_glow, thrust * GlowRate * (1f - vanish));
                _light.intensity = thrust * LightIntensity * (1f - vanish);
            }

            // Strongest with the engines on just above the pad, gone once the shuttle is high or the engines idle.
            float wash = thrust * (1f - eased);
            _wash.gameObject.SetActive(wash > WashHide);
            _wash.localScale = _washScale * wash;
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
    }
}
