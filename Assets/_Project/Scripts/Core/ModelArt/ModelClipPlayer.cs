using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Core
{
    /// <summary>
    /// Plays the clips of an imported model without an Animator Controller: one PlayableGraph per animated rig (the near
    /// and the far LOD each have their own), a mixer with one input per clip, a looping cycle that cross-fades when it
    /// changes, and one clip at a time on top of it (a hit, a strike, a power, a fall) that fades in and out or holds
    /// its last frame. The player keeps the clock itself and sets every clip's time each frame, so a rig the camera
    /// culls (the LOD that is not shown) picks up at exactly the same moment when it appears. Nothing allocates after
    /// construction; <see cref="Restart"/> resets it for a pooled owner.
    /// </summary>
    public sealed class ModelClipPlayer
    {
        private const float CycleFadeSeconds = 0.2f;
        private const float OnceFadeInSeconds = 0.08f;
        private const float OnceFadeOutSeconds = 0.15f;

        private readonly ModelArtDefinition _art;
        private readonly PlayableGraph[] _graphs;
        private readonly AnimationMixerPlayable[] _mixers;
        private readonly AnimationClipPlayable[] _playables;
        private readonly float[] _times;
        private readonly float[] _lengths;
        private readonly int _clipCount;

        private int _cycle = -1;
        private int _fadingCycle = -1;
        private float _cycleSpeed = 1f;
        private float _fadingSpeed = 1f;
        private float _cycleBlend = 1f;
        private int _once = -1;
        private float _onceSpeed = 1f;
        private float _onceAge;
        private bool _onceHolds;
        private float _stopAge = -1f;
        private bool _disposed;

        /// <param name="animators">The rigs to drive: the near LOD first, then the far one, which plays the far clips. Their
        /// Animators need no controller.</param>
        public ModelClipPlayer(ModelArtDefinition art, Animator[] animators)
        {
            _art = art;
            ModelClip[] clips = art.Clips;
            _clipCount = clips.Length;
            _times = new float[_clipCount];
            _lengths = new float[_clipCount];
            for (int i = 0; i < _clipCount; i++)
            {
                _lengths[i] = Mathf.Max(0.0001f, clips[i].Length);
            }

            _graphs = new PlayableGraph[animators.Length];
            _mixers = new AnimationMixerPlayable[animators.Length];
            _playables = new AnimationClipPlayable[animators.Length * _clipCount];
            for (int g = 0; g < animators.Length; g++)
            {
                Animator animator = animators[g];
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                // The far LOD's rig only evaluates while its mesh is drawn.
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                PlayableGraph graph = PlayableGraph.Create(art.name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(graph, "Model", animator);
                var mixer = AnimationMixerPlayable.Create(graph, _clipCount);
                for (int i = 0; i < _clipCount; i++)
                {
                    var playable = AnimationClipPlayable.Create(graph, g == 0 ? clips[i].Clip : clips[i].FarClip);
                    playable.SetApplyFootIK(false);
                    // The player sets the time itself every frame, so the graph never advances a clip on its own.
                    playable.SetSpeed(0d);
                    graph.Connect(playable, 0, mixer, i);
                    _playables[g * _clipCount + i] = playable;
                }

                output.SetSourcePlayable(mixer);
                _graphs[g] = graph;
                _mixers[g] = mixer;
                graph.Play();
            }
        }

        /// <summary>The clip on top of the cycle, or -1.</summary>
        public int Once => _once;

        /// <summary>Seconds into the clip on top of the cycle.</summary>
        public float OnceTime => _once >= 0 ? _times[_once] : 0f;

        /// <summary>True while the clip of a role plays on top of the cycle.</summary>
        public bool IsPlaying(ModelClipRole role) => _once >= 0 && _art.Clips[_once].Role == role;

        /// <summary>Back to the first cycle at its start, nothing on top: for a pooled owner sent out again.</summary>
        public void Restart()
        {
            for (int i = 0; i < _clipCount; i++)
            {
                _times[i] = 0f;
            }

            _cycle = -1;
            _fadingCycle = -1;
            _cycleBlend = 1f;
            _once = -1;
        }

        /// <summary>
        /// Loops a cycle at a playback rate (1 = as authored). A new cycle fades in over the old one; asking for the one
        /// already playing only changes its rate.
        /// </summary>
        public void Loop(int index, float rate)
        {
            if (index < 0)
            {
                return;
            }

            if (index == _cycle)
            {
                _cycleSpeed = rate;
                return;
            }

            _fadingCycle = _cycle;
            _fadingSpeed = _cycleSpeed;
            _cycle = index;
            _cycleSpeed = rate;
            _times[index] = 0f;
            _cycleBlend = _fadingCycle < 0 ? 1f : 0f;
        }

        /// <summary>
        /// Plays a clip once on top of the cycle from a start time. A held clip stays on its last frame (a fall, a raised
        /// shield) until another clip or <see cref="StopOnce"/> replaces it. A negative rate plays it backwards from its end.
        /// </summary>
        public void PlayOnce(int index, float startSeconds = 0f, bool hold = false, float rate = 1f)
        {
            if (index < 0)
            {
                return;
            }

            _once = index;
            _onceSpeed = rate;
            _onceHolds = hold;
            _onceAge = 0f;
            _stopAge = -1f;
            float start = rate < 0f ? _lengths[index] - startSeconds : startSeconds;
            _times[index] = Mathf.Clamp(start, 0f, _lengths[index]);
        }

        /// <summary>Lets the clip on top fade out now, wherever it is.</summary>
        public void StopOnce()
        {
            if (_once >= 0)
            {
                _onceHolds = false;
                _stopAge = 0f;
            }
        }

        /// <summary>Advances the clocks and writes weights and times into every graph.</summary>
        public void Tick(float deltaTime)
        {
            if (_disposed || _clipCount == 0)
            {
                return;
            }

            AdvanceCycles(deltaTime);
            float onceWeight = AdvanceOnce(deltaTime);
            float cycleWeight = 1f - onceWeight;
            for (int g = 0; g < _graphs.Length; g++)
            {
                AnimationMixerPlayable mixer = _mixers[g];
                for (int i = 0; i < _clipCount; i++)
                {
                    float weight = 0f;
                    if (i == _once)
                    {
                        weight += onceWeight;
                    }

                    if (i == _cycle)
                    {
                        weight += cycleWeight * _cycleBlend;
                    }
                    else if (i == _fadingCycle)
                    {
                        weight += cycleWeight * (1f - _cycleBlend);
                    }

                    mixer.SetInputWeight(i, weight);
                    if (weight > 0f)
                    {
                        _playables[g * _clipCount + i].SetTime(_times[i]);
                    }
                }
            }
        }

        /// <summary>Applies the current pose at once, without waiting for the next animation update (a still for a menu).</summary>
        public void Evaluate()
        {
            Tick(0f);
            for (int g = 0; g < _graphs.Length; g++)
            {
                _graphs[g].Evaluate(0f);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (int g = 0; g < _graphs.Length; g++)
            {
                if (_graphs[g].IsValid())
                {
                    _graphs[g].Destroy();
                }
            }
        }

        private void AdvanceCycles(float deltaTime)
        {
            if (_cycle >= 0)
            {
                _times[_cycle] = Wrap(_times[_cycle] + deltaTime * _cycleSpeed, _lengths[_cycle]);
            }

            if (_fadingCycle >= 0 && _fadingCycle != _cycle)
            {
                _times[_fadingCycle] = Wrap(_times[_fadingCycle] + deltaTime * _fadingSpeed, _lengths[_fadingCycle]);
            }

            _cycleBlend = Mathf.MoveTowards(_cycleBlend, 1f, deltaTime / CycleFadeSeconds);
            if (_cycleBlend >= 1f)
            {
                _fadingCycle = -1;
            }
        }

        // The clip on top fades in quickly, plays, and fades out over its last moments unless it holds.
        private float AdvanceOnce(float deltaTime)
        {
            if (_once < 0)
            {
                return 0f;
            }

            _onceAge += deltaTime;
            float length = _lengths[_once];
            float time = Mathf.Clamp(_times[_once] + deltaTime * _onceSpeed, 0f, length);
            _times[_once] = time;
            float left = _onceSpeed < 0f ? time : length - time;
            float fadeOut = _onceHolds ? 1f : Mathf.Clamp01(left / OnceFadeOutSeconds);
            if (_stopAge >= 0f)
            {
                _stopAge += deltaTime;
                fadeOut = Mathf.Min(fadeOut, 1f - _stopAge / OnceFadeOutSeconds);
            }

            if (fadeOut <= 0f)
            {
                _once = -1;
                return 0f;
            }

            float fadeIn = Mathf.Clamp01(_onceAge / OnceFadeInSeconds);
            return Mathf.Min(fadeIn, fadeOut) * _art.Clips[_once].Weight;
        }

        private static float Wrap(float time, float length)
        {
            time %= length;
            return time < 0f ? time + length : time;
        }
    }
}
