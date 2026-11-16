using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// The level's wave timeline without the spawning: which wave comes next, whether one is still coming out, and the countdown
    /// between waves. A wave's countdown starts once the wave before it has fully spawned, whatever is still alive on the field,
    /// so waves overlap whenever creatures outlive the countdown, and more so when the player calls the next wave early.
    /// The level is cleared only when every wave has started and spawned and nothing is left alive.
    /// </summary>
    public sealed class WaveClock
    {
        private readonly float[] _startDelays;

        /// <param name="startDelays">Each wave's countdown, measured from the end of the previous wave's spawning (the first from the level start).</param>
        public WaveClock(IReadOnlyList<float> startDelays)
        {
            _startDelays = new float[startDelays.Count];
            for (int i = 0; i < _startDelays.Length; i++)
            {
                _startDelays[i] = startDelays[i];
            }

            Countdown = _startDelays.Length > 0 ? _startDelays[0] : 0f;
        }

        public int WaveCount => _startDelays.Length;

        /// <summary>How many waves have started; the index of the next wave.</summary>
        public int Started { get; private set; }

        /// <summary>True from a wave's start until its last creature (held nest spawns included) is out.</summary>
        public bool Spawning { get; private set; }

        /// <summary>Seconds until the next wave starts; zero while a wave spawns and after the last one.</summary>
        public float Countdown { get; private set; }

        public bool AllStarted => Started >= _startDelays.Length;

        /// <summary>True while the countdown to a further wave runs (nothing spawning, a wave left).</summary>
        public bool CountingDown => !Spawning && !AllStarted;

        /// <summary>Runs the countdown; true when the next wave is due now (the caller then starts it).</summary>
        public bool Tick(float deltaTime)
        {
            if (!CountingDown)
            {
                return false;
            }

            Countdown -= deltaTime;
            return Countdown <= 0f;
        }

        /// <summary>The next wave starts (its countdown ran out, or it was called early); returns its index.</summary>
        public int BeginWave()
        {
            int index = Started;
            Started++;
            Spawning = true;
            Countdown = 0f;
            return index;
        }

        /// <summary>The running wave has sent its last creature out: the countdown to the next wave begins.</summary>
        public void EndSpawning()
        {
            Spawning = false;
            Countdown = AllStarted ? 0f : _startDelays[Started];
        }

        /// <summary>Whether the level is won: every wave started and spawned, and nothing left alive.</summary>
        public bool IsCleared(int alive) => AllStarted && !Spawning && alive == 0;
    }
}
