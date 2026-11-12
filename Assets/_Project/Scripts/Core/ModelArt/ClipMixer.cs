using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The weights and clocks behind <see cref="ModelClipPlayer"/>, without any Unity animation objects so the blending can be
    /// tested on its own. Every clip has a slot; a clip that does not loop (a hit, a shot, a spawn) has a second "echo" slot, so
    /// when it restarts while it still shows, the old run fades out on one slot while the new one fades in on the other
    /// instead of the pose jumping back to its first frame. Cycles cross-fade through per-slot weights, so a cycle that changes
    /// again in the middle of a fade (idle, walk, idle) never drops the one that was half shown. The clip on top fades in, and a
    /// clip that replaces it cross-fades with it rather than cutting it. Fades are eased (smoothstep) so the pose starts and ends
    /// a blend without a kink. Nothing allocates after construction.
    /// </summary>
    public sealed class ClipMixer
    {
        /// <summary>Default cross-fade between two cycles (idle and walk): long enough to hide the change of gait, short enough not to drag.</summary>
        public const float DefaultCycleBlendSeconds = 0.2f;

        /// <summary>Default fade-in of a clip on top: quick, so a strike or a flinch still reads at once.</summary>
        public const float DefaultOnceBlendSeconds = 0.08f;

        /// <summary>Fade back to the cycle over a clip's last moments (or after a stop).</summary>
        public const float OnceFadeOutSeconds = 0.15f;

        // A clip's fade-out window never takes more than this share of the clip, so a very short clip still shows.
        private const float MaxFadeOutShare = 0.5f;
        private const float MinLength = 0.0001f;

        private readonly int _clipCount;
        private readonly float[] _lengths;
        private readonly float[] _overlay;
        private readonly float[] _blendSeconds;
        private readonly int[] _slotClip;
        private readonly int[] _echoSlot;

        private readonly float[] _times;
        private readonly float[] _speeds;
        private readonly float[] _cycleWeights;
        // The cycle weights when the cycle last changed: the blend runs from them (several cycles mid-fade included) to the new one.
        private readonly float[] _cycleFrom;
        private readonly float[] _onceWeights;
        private readonly float[] _onceDecay;
        private readonly float[] _weights;

        private int _cycleSlot = -1;
        private float _cycleFadeRate = 1f / DefaultCycleBlendSeconds;
        private float _cycleProgress = 1f;
        private int _onceSlot = -1;
        private bool _onceHolds;
        private float _onceAge;
        private float _onceFrom;
        private float _onceBlend = DefaultOnceBlendSeconds;
        private float _stopAge = -1f;

        /// <param name="lengths">Each clip's length in seconds.</param>
        /// <param name="overlay">How much of the pose each clip takes over when it plays on top (the art's clip weight).</param>
        /// <param name="blendSeconds">Each clip's own blend-in time from its data, or zero for the default.</param>
        /// <param name="loops">Whether each clip loops; a clip that does not gets an echo slot.</param>
        public ClipMixer(float[] lengths, float[] overlay, float[] blendSeconds, bool[] loops)
        {
            _clipCount = lengths.Length;
            _lengths = new float[_clipCount];
            _overlay = new float[_clipCount];
            _blendSeconds = new float[_clipCount];
            _echoSlot = new int[_clipCount];
            int slots = _clipCount;
            for (int i = 0; i < _clipCount; i++)
            {
                _lengths[i] = Mathf.Max(MinLength, lengths[i]);
                _overlay[i] = overlay[i];
                _blendSeconds[i] = blendSeconds[i];
                _echoSlot[i] = loops[i] ? -1 : slots++;
            }

            _slotClip = new int[slots];
            for (int i = 0; i < _clipCount; i++)
            {
                _slotClip[i] = i;
                if (_echoSlot[i] >= 0)
                {
                    _slotClip[_echoSlot[i]] = i;
                }
            }

            _times = new float[slots];
            _speeds = new float[slots];
            _cycleWeights = new float[slots];
            _cycleFrom = new float[slots];
            _onceWeights = new float[slots];
            _onceDecay = new float[slots];
            _weights = new float[slots];
        }

        /// <summary>Clips plus echo slots: the inputs a mixer needs.</summary>
        public int SlotCount => _slotClip.Length;

        /// <summary>The clip a slot plays.</summary>
        public int ClipOf(int slot) => _slotClip[slot];

        /// <summary>The weight of a slot in the final pose after the last <see cref="Advance"/>.</summary>
        public float WeightOf(int slot) => _weights[slot];

        /// <summary>Seconds into its clip a slot stands.</summary>
        public float TimeOf(int slot) => _times[slot];

        /// <summary>The looping cycle, or -1.</summary>
        public int Cycle => _cycleSlot >= 0 ? _slotClip[_cycleSlot] : -1;

        /// <summary>The playback rate of the cycle.</summary>
        public float CycleRate => _cycleSlot >= 0 ? _speeds[_cycleSlot] : 0f;

        /// <summary>The clip on top of the cycle, or -1.</summary>
        public int Once => _onceSlot >= 0 ? _slotClip[_onceSlot] : -1;

        /// <summary>Seconds into the clip on top.</summary>
        public float OnceTime => _onceSlot >= 0 ? _times[_onceSlot] : 0f;

        /// <summary>Nothing playing, every weight and clock at zero: a pooled owner sent out again.</summary>
        public void Restart()
        {
            for (int s = 0; s < _weights.Length; s++)
            {
                _times[s] = 0f;
                _speeds[s] = 0f;
                _cycleWeights[s] = 0f;
                _cycleFrom[s] = 0f;
                _onceWeights[s] = 0f;
                _onceDecay[s] = 0f;
                _weights[s] = 0f;
            }

            _cycleSlot = -1;
            _onceSlot = -1;
            _stopAge = -1f;
            _cycleProgress = 1f;
        }

        /// <summary>Takes over another mixer's whole state (same clips): a corpse that falls out of the pose the body died in.</summary>
        public void CopyFrom(ClipMixer other)
        {
            if (other == null || other._weights.Length != _weights.Length)
            {
                return;
            }

            System.Array.Copy(other._times, _times, _times.Length);
            System.Array.Copy(other._speeds, _speeds, _speeds.Length);
            System.Array.Copy(other._cycleWeights, _cycleWeights, _cycleWeights.Length);
            System.Array.Copy(other._cycleFrom, _cycleFrom, _cycleFrom.Length);
            _cycleProgress = other._cycleProgress;
            System.Array.Copy(other._onceWeights, _onceWeights, _onceWeights.Length);
            System.Array.Copy(other._onceDecay, _onceDecay, _onceDecay.Length);
            System.Array.Copy(other._weights, _weights, _weights.Length);
            _cycleSlot = other._cycleSlot;
            _cycleFadeRate = other._cycleFadeRate;
            _onceSlot = other._onceSlot;
            _onceHolds = other._onceHolds;
            _onceAge = other._onceAge;
            _onceFrom = other._onceFrom;
            _onceBlend = other._onceBlend;
            _stopAge = other._stopAge;
        }

        /// <summary>
        /// Loops a cycle at a rate. A new cycle cross-fades over whatever cycles still show (a cycle that was fading out picks
        /// up where it is); asking for the cycle already playing only changes its rate. The very first cycle shows at once.
        /// </summary>
        public void Loop(int clip, float rate, float startSeconds)
        {
            if (clip < 0)
            {
                return;
            }

            if (_cycleSlot >= 0 && _slotClip[_cycleSlot] == clip)
            {
                _speeds[_cycleSlot] = rate;
                return;
            }

            // A clip on top that uses the clip's own slot leaves the cycle the echo.
            int slot = clip;
            if ((slot == _onceSlot || _onceWeights[slot] > 0f) && _echoSlot[clip] >= 0)
            {
                slot = _echoSlot[clip];
            }

            // A cycle still showing (taken back mid-fade) carries on from where it is.
            if (_cycleWeights[slot] <= 0f)
            {
                _times[slot] = Wrap(startSeconds, _lengths[clip]);
            }

            _speeds[slot] = rate;
            bool first = !AnyCycle();
            System.Array.Copy(_cycleWeights, _cycleFrom, _cycleFrom.Length);
            _cycleSlot = slot;
            _cycleFadeRate = 1f / BlendOf(clip, DefaultCycleBlendSeconds);
            _cycleProgress = first ? 1f : 0f;
            UpdateCycleWeights();
        }

        /// <summary>
        /// Plays a clip once on top of the cycle from a start time (backwards from its end at a negative rate). A held clip
        /// stays on its last frame until another clip or <see cref="StopOnce"/> replaces it. A clip that replaces the one on
        /// top (or the same clip restarting) cross-fades with it.
        /// </summary>
        public void PlayOnce(int clip, float startSeconds, bool hold, float rate)
        {
            if (clip < 0)
            {
                return;
            }

            // The same clip asked again while it is still fading in (a burst of spawns, two hits in a frame or two): it has barely
            // moved, so it restarts where it is instead of taking a second slot and pushing a third run onto a busy one.
            if (Once == clip && _onceAge < _onceBlend && _stopAge < 0f)
            {
                RestartInPlace(startSeconds, hold, rate);
                return;
            }

            int slot = FreeSlotFor(clip);
            float blend = BlendOf(clip, DefaultOnceBlendSeconds);
            if (_onceSlot >= 0 && _onceSlot != slot)
            {
                Release(_onceSlot, blend);
            }

            _onceSlot = slot;
            _onceDecay[slot] = 0f;
            _speeds[slot] = rate;
            _onceHolds = hold;
            _onceAge = 0f;
            _stopAge = -1f;
            _onceBlend = blend;
            // A slot still fading out (both slots busy) carries on from the weight it shows instead of dropping to zero.
            _onceFrom = _overlay[clip] > 0f ? Mathf.Clamp01(_onceWeights[slot] / _overlay[clip]) : 0f;
            float length = _lengths[clip];
            float start = rate < 0f ? length - startSeconds : startSeconds;
            _times[slot] = Mathf.Clamp(start, 0f, length);
        }

        private void RestartInPlace(float startSeconds, bool hold, float rate)
        {
            int slot = _onceSlot;
            float length = _lengths[_slotClip[slot]];
            _speeds[slot] = rate;
            _onceHolds = hold;
            _times[slot] = Mathf.Clamp(rate < 0f ? length - startSeconds : startSeconds, 0f, length);
        }

        /// <summary>Changes the playback rate of the clip on top where it is: zero holds it on its current frame (a tail kept on aim).</summary>
        public void SetOnceRate(float rate)
        {
            if (_onceSlot >= 0)
            {
                _speeds[_onceSlot] = rate;
            }
        }

        /// <summary>Moves the clip on top to a time in it (a lasting power looping its sustain), clamped to the clip.</summary>
        public void SetOnceTime(float seconds)
        {
            if (_onceSlot >= 0)
            {
                _times[_onceSlot] = Mathf.Clamp(seconds, 0f, _lengths[_slotClip[_onceSlot]]);
            }
        }

        /// <summary>Lets the clip on top fade out now, wherever it is.</summary>
        public void StopOnce()
        {
            if (_onceSlot >= 0)
            {
                _onceHolds = false;
                _stopAge = 0f;
            }
        }

        /// <summary>
        /// Advances every clock by <paramref name="deltaTime"/> (game seconds) and the fades by
        /// <paramref name="deltaTime"/> / <paramref name="stretch"/>: at a raised game speed the clips run faster, but a
        /// blend still takes the same real time, so it never collapses into a one-frame jump.
        /// </summary>
        public void Advance(float deltaTime, float stretch)
        {
            stretch = Mathf.Max(1f, stretch);
            float blendDelta = deltaTime / stretch;
            AdvanceCycles(deltaTime, blendDelta);
            AdvanceOnce(deltaTime, blendDelta, stretch);
            AdvanceReleased(deltaTime, blendDelta);
            Compose();
        }

        private bool AnyCycle()
        {
            for (int s = 0; s < _cycleWeights.Length; s++)
            {
                if (_cycleWeights[s] > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        // The clip's own slot unless it is busy (the same clip still showing, or the cycle on it); then its echo, if free.
        private int FreeSlotFor(int clip)
        {
            int echo = _echoSlot[clip];
            if (echo < 0 || !Busy(clip))
            {
                return clip;
            }

            if (!Busy(echo))
            {
                return echo;
            }

            // Both show: reuse the one on top of the cycle that weighs least, never the cycle's own.
            bool clipIsCycle = clip == _cycleSlot || _cycleWeights[clip] > 0f;
            return clipIsCycle || _onceWeights[echo] < _onceWeights[clip] ? echo : clip;
        }

        private bool Busy(int slot) => slot == _onceSlot || slot == _cycleSlot || _onceWeights[slot] > 0f || _cycleWeights[slot] > 0f;

        // The replaced clip on top keeps running and fades out from the weight it shows over the newcomer's blend.
        private void Release(int slot, float blendSeconds)
        {
            _onceDecay[slot] = _onceWeights[slot] / Mathf.Max(MinLength, blendSeconds);
        }

        private void AdvanceCycles(float deltaTime, float blendDelta)
        {
            for (int s = 0; s < _cycleWeights.Length; s++)
            {
                bool current = s == _cycleSlot;
                if (!current && _cycleWeights[s] <= 0f)
                {
                    continue;
                }

                _times[s] = Wrap(_times[s] + deltaTime * _speeds[s], _lengths[_slotClip[s]]);
            }

            _cycleProgress = Mathf.MoveTowards(_cycleProgress, 1f, blendDelta * _cycleFadeRate);
            UpdateCycleWeights();
        }

        // Every cycle that showed when the cycle changed shrinks in proportion while the new one grows (eased): the weights
        // always add up to one, and a change in the middle of a fade starts from exactly what is shown.
        private void UpdateCycleWeights()
        {
            float eased = Ease(_cycleProgress);
            for (int s = 0; s < _cycleWeights.Length; s++)
            {
                _cycleWeights[s] = _cycleFrom[s] * (1f - eased) + (s == _cycleSlot ? eased : 0f);
            }
        }

        // The clip on top fades in, plays, and fades out over its last moments unless it holds.
        private void AdvanceOnce(float deltaTime, float blendDelta, float stretch)
        {
            if (_onceSlot < 0)
            {
                return;
            }

            int slot = _onceSlot;
            int clip = _slotClip[slot];
            float length = _lengths[clip];
            float speed = _speeds[slot];
            _onceAge += blendDelta;
            float time = Mathf.Clamp(_times[slot] + deltaTime * speed, 0f, length);
            _times[slot] = time;
            float left = speed < 0f ? time : length - time;
            // The window is in clip seconds; at a raised game speed it widens so the fade lasts as long on screen.
            float window = Mathf.Min(OnceFadeOutSeconds * stretch, length * MaxFadeOutShare);
            float fadeOut = _onceHolds ? 1f : Mathf.Clamp01(left / Mathf.Max(MinLength, window));
            if (_stopAge >= 0f)
            {
                _stopAge += blendDelta;
                fadeOut = Mathf.Min(fadeOut, 1f - _stopAge / OnceFadeOutSeconds);
            }

            if (fadeOut <= 0f)
            {
                _onceWeights[slot] = 0f;
                _onceSlot = -1;
                return;
            }

            float fadeIn = Mathf.Clamp01(_onceFrom + _onceAge / _onceBlend);
            _onceWeights[slot] = Ease(Mathf.Min(fadeIn, fadeOut)) * _overlay[clip];
        }

        private void AdvanceReleased(float deltaTime, float blendDelta)
        {
            for (int s = 0; s < _onceWeights.Length; s++)
            {
                if (s == _onceSlot || _onceWeights[s] <= 0f)
                {
                    continue;
                }

                float length = _lengths[_slotClip[s]];
                _times[s] = Mathf.Clamp(_times[s] + deltaTime * _speeds[s], 0f, length);
                _onceWeights[s] = Mathf.Max(0f, _onceWeights[s] - _onceDecay[s] * blendDelta);
            }
        }

        // The clips on top take their share (never more than the whole pose); the cycles share the rest.
        private void Compose()
        {
            float once = 0f;
            float cycles = 0f;
            for (int s = 0; s < _weights.Length; s++)
            {
                once += _onceWeights[s];
                cycles += _cycleWeights[s];
            }

            float onceScale = once > 1f ? 1f / once : 1f;
            float cycleShare = cycles > 0f ? (1f - Mathf.Min(1f, once)) / cycles : 0f;
            for (int s = 0; s < _weights.Length; s++)
            {
                _weights[s] = _onceWeights[s] * onceScale + _cycleWeights[s] * cycleShare;
            }
        }

        private float BlendOf(int clip, float fallback) => _blendSeconds[clip] > 0f ? _blendSeconds[clip] : fallback;

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static float Wrap(float time, float length)
        {
            time %= length;
            return time < 0f ? time + length : time;
        }
    }
}
