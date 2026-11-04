using System;
using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// The cycle of every spawn origin (the level's nests), driven by the wave data: an origin any wave sends creatures
    /// through is <see cref="OriginState.Marked"/> from the start, the rest stay <see cref="OriginState.Closed"/>. In a
    /// wave that uses it, it starts opening <see cref="OriginTiming.OpenLead"/> before its first creature is due, bursts,
    /// lets the creatures out once the maw is up, and closes back to marked a moment after its last one. Pure logic with
    /// its own clock; views listen to <see cref="Signalled"/>. Origins cannot be hit, targeted or destroyed.
    /// </summary>
    public sealed class OriginDirector
    {
        private readonly OriginTiming _timing;
        private readonly OriginState[] _states;
        private readonly float[] _stateStart;
        private readonly float[] _openAt;
        private readonly bool[] _burst;
        private readonly int[] _planned;
        private readonly int[] _emerged;
        private readonly float[] _closeAt;
        private readonly NestAssignment[] _waves;
        private float _clock;
        private float _waveClock;

        /// <param name="nests">The level's nests: origin i is nest i.</param>
        /// <param name="waves">The nest of every nest spawn of every wave, drawn at the level start.</param>
        public OriginDirector(int nests, IReadOnlyList<NestAssignment> waves, OriginTiming timing)
        {
            _timing = timing;
            _states = new OriginState[nests];
            _stateStart = new float[nests];
            _openAt = new float[nests];
            _burst = new bool[nests];
            _planned = new int[nests];
            _emerged = new int[nests];
            _closeAt = new float[nests];
            _waves = new NestAssignment[waves.Count];
            for (int w = 0; w < waves.Count; w++)
            {
                _waves[w] = waves[w];
            }

            for (int i = 0; i < nests; i++)
            {
                _states[i] = UsedByAnyWave(i) ? OriginState.Marked : OriginState.Closed;
                _openAt[i] = float.PositiveInfinity;
                _closeAt[i] = -1f;
            }
        }

        /// <summary>An origin changed state, burst open or let a creature out.</summary>
        public event Action<int, OriginSignal> Signalled;

        public int Count => _states.Length;

        public OriginState StateOf(int origin) => _states[origin];

        /// <summary>Seconds since the origin entered its state.</summary>
        public float TimeInState(int origin) => _clock - _stateStart[origin];

        /// <summary>The nest assignment of a wave (0-based), for the controller to route each nest spawn.</summary>
        public NestAssignment Wave(int index) => _waves[index];

        /// <summary>A new wave starts: each origin it uses opens its lead time before that origin's first creature.</summary>
        public void BeginWave(int waveIndex, WaveScheduler scheduler)
        {
            _waveClock = 0f;
            NestAssignment wave = _waves[waveIndex];
            for (int i = 0; i < _states.Length; i++)
            {
                _planned[i] = 0;
                _emerged[i] = 0;
                _closeAt[i] = -1f;
                _openAt[i] = float.PositiveInfinity;
            }

            for (int g = 0; g < wave.GroupCount && g < scheduler.GroupCount; g++)
            {
                for (int k = 0; k < wave.CountOf(g) && k < scheduler.Total(g); k++)
                {
                    int nest = wave.NestOf(g, k);
                    if (nest < 0 || nest >= _states.Length)
                    {
                        continue;
                    }

                    _planned[nest]++;
                    _openAt[nest] = Math.Min(_openAt[nest], Math.Max(0f, scheduler.PlannedTime(g, k) - _timing.OpenLead));
                }
            }
        }

        /// <summary>Advances the clocks and moves every origin on through its cycle.</summary>
        public void Advance(float deltaTime)
        {
            _clock += deltaTime;
            _waveClock += deltaTime;
            for (int i = 0; i < _states.Length; i++)
            {
                Step(i);
            }
        }

        /// <summary>Whether a creature can come out of the origin now (the maw is up).</summary>
        public bool IsReady(int origin)
        {
            OriginState state = _states[origin];
            return state == OriginState.Open || (state == OriginState.Opening && TimeInState(origin) >= _timing.Emerge);
        }

        /// <summary>A creature came out: once the wave's last one from this origin is out, it closes a moment later.</summary>
        public void NotifyEmerged(int origin)
        {
            _emerged[origin]++;
            Signalled?.Invoke(origin, OriginSignal.Emerged);
            if (_emerged[origin] >= _planned[origin])
            {
                _closeAt[origin] = _clock + _timing.CloseDelay;
            }
        }

        /// <summary>Opens an origin at once (a wave called early sends its creatures out without waiting).</summary>
        public void ForceOpen(int origin)
        {
            if (IsReady(origin))
            {
                return;
            }

            if (!_burst[origin])
            {
                _burst[origin] = true;
                Signalled?.Invoke(origin, OriginSignal.Burst);
            }

            SetState(origin, OriginState.Open);
        }

        private void Step(int i)
        {
            switch (_states[i])
            {
                case OriginState.Marked:
                case OriginState.Closing:
                    if (_emerged[i] < _planned[i] && _waveClock >= _openAt[i])
                    {
                        _burst[i] = false;
                        SetState(i, OriginState.Opening);
                    }
                    else if (_states[i] == OriginState.Closing && TimeInState(i) >= _timing.Close)
                    {
                        SetState(i, OriginState.Marked);
                    }

                    break;
                case OriginState.Opening:
                    if (!_burst[i] && TimeInState(i) >= _timing.Burst)
                    {
                        _burst[i] = true;
                        Signalled?.Invoke(i, OriginSignal.Burst);
                    }

                    if (TimeInState(i) >= _timing.Open)
                    {
                        SetState(i, OriginState.Open);
                    }

                    break;
                case OriginState.Open:
                    if (_closeAt[i] >= 0f && _clock >= _closeAt[i] && _emerged[i] >= _planned[i])
                    {
                        _closeAt[i] = -1f;
                        SetState(i, OriginState.Closing);
                    }

                    break;
            }
        }

        private void SetState(int origin, OriginState state)
        {
            _states[origin] = state;
            _stateStart[origin] = _clock;
            Signalled?.Invoke(origin, OriginSignal.StateChanged);
        }

        private bool UsedByAnyWave(int nest)
        {
            foreach (NestAssignment wave in _waves)
            {
                if (wave.Uses(nest))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
