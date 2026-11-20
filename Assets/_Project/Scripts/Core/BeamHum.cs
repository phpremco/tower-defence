using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The hum of the beam towers as a small choir (round 88a, Remco: "the laser sound will get on my nerves in a long
    /// game"; a Laser fires nine to fifteen pulses a second). A firing tower holds one voice of a few; nothing sounds per
    /// pulse. Three rules keep it pleasant at scale: the voices share one loudness (two beams are not twice one), a voice
    /// that has hummed for a while settles to a quieter bed and only a tower that opens fire after a silence is heard at
    /// full level again, and every voice is tuned a little apart and drifts slowly, so equal recordings never stack into
    /// one hard tone. With every voice taken a further tower stays silent. Pure: the audio player reads the levels and
    /// pitches and sets them on its loops.
    /// </summary>
    public sealed class BeamHum
    {
        /// <summary>The share of the opening level a hum settles to while its tower keeps firing.</summary>
        public const float SustainShare = 0.55f;

        /// <summary>Seconds a tower fires at the opening level before its hum starts to settle.</summary>
        public const float OpeningSeconds = 0.6f;

        /// <summary>Seconds the settling takes.</summary>
        public const float SettleSeconds = 2.5f;

        /// <summary>How far apart the voices are tuned (each a step further from the recording, alternately down and up).</summary>
        public const float DetuneStep = 0.035f;

        /// <summary>The slow drift of each voice round its tuning.</summary>
        public const float DriftDepth = 0.012f;

        /// <summary>The period of that drift in seconds.</summary>
        public const float DriftSeconds = 3.7f;

        // Each voice drifts this share of a period behind the one before it.
        private const float DriftPhaseStep = 0.37f;

        private readonly Vector3[] _origins;
        private readonly float[] _lastTick;
        private readonly float[] _since;
        private readonly float _holdSeconds;
        private readonly float _sameTowerSqr;
        private float _now;

        /// <param name="voices">How many towers can be heard at once.</param>
        /// <param name="holdSeconds">A voice is released this long after its last pulse.</param>
        /// <param name="sameTowerDistance">Pulses that leave within this distance of a voice belong to the same tower.</param>
        public BeamHum(int voices, float holdSeconds, float sameTowerDistance)
        {
            _origins = new Vector3[voices];
            _lastTick = new float[voices];
            _since = new float[voices];
            for (int i = 0; i < voices; i++)
            {
                _lastTick[i] = float.NegativeInfinity;
                _since[i] = float.NegativeInfinity;
            }

            _holdSeconds = holdSeconds;
            _sameTowerSqr = sameTowerDistance * sameTowerDistance;
        }

        public int Voices => _origins.Length;

        /// <summary>How many voices hum right now.</summary>
        public int Active
        {
            get
            {
                int active = 0;
                for (int i = 0; i < _origins.Length; i++)
                {
                    active += IsLive(i) ? 1 : 0;
                }

                return active;
            }
        }

        /// <summary>The clock the levels are read at; the player sets it once a frame.</summary>
        public void Step(float now) => _now = now;

        /// <summary>
        /// A pulse left a tower at <paramref name="origin"/>: it keeps that voice alive, or claims a free voice for a
        /// tower that just opened fire. With every voice busy the pulse goes unheard.
        /// </summary>
        public void Touch(Vector3 origin, float now)
        {
            _now = now;
            int free = -1;
            for (int i = 0; i < _origins.Length; i++)
            {
                bool live = IsLive(i);
                if (live && (origin - _origins[i]).sqrMagnitude < _sameTowerSqr)
                {
                    _lastTick[i] = now;
                    return;
                }

                if (!live && free < 0)
                {
                    free = i;
                }
            }

            if (free >= 0)
            {
                _origins[free] = origin;
                _lastTick[free] = now;
                _since[free] = now;
            }
        }

        /// <summary>
        /// The level of a voice as a share of the opening level of one beam: zero when silent, otherwise its settling (one
        /// down to <see cref="SustainShare"/>) over the root of the number of voices humming, so the power of the choir
        /// stays that of one beam.
        /// </summary>
        public float Level(int voice)
        {
            if (!IsLive(voice))
            {
                return 0f;
            }

            float held = _now - _since[voice];
            float settle = Mathf.Clamp01((held - OpeningSeconds) / SettleSeconds);
            return Mathf.Lerp(1f, SustainShare, settle) / Mathf.Sqrt(Mathf.Max(1, Active));
        }

        /// <summary>The playback speed of a voice as a factor on that of the recording: its tuning plus its slow drift.</summary>
        public float Pitch(int voice)
        {
            // Voice 0 keeps the pitch of the recording; 1 lies a step below, 2 a step above, 3 two steps below, and so on.
            int step = (voice + 1) / 2;
            float tuning = 1f + (voice % 2 == 1 ? -step : step) * DetuneStep;
            return tuning + Mathf.Sin((_now / DriftSeconds + voice * DriftPhaseStep) * Mathf.PI * 2f) * DriftDepth;
        }

        /// <summary>Where in its loop a voice starts, as a share of the loop, so equal recordings never run in step.</summary>
        public float Phase(int voice) => voice / (float)Mathf.Max(1, _origins.Length);

        private bool IsLive(int voice) => _now - _lastTick[voice] < _holdSeconds;
    }
}
