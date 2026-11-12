using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Core
{
    /// <summary>
    /// Plays the clips of an imported model without an Animator Controller: one PlayableGraph per animated rig (the near
    /// and the far LOD each have their own), a mixer with one input per <see cref="ClipMixer"/> slot (every clip, plus an
    /// echo for each clip that does not loop), cycles that cross-fade when they change, and clips on top that fade in, cross-
    /// fade when one replaces another (or the same one restarts) and fade out or hold their last frame. The player keeps the
    /// clock itself and sets every clip's time each frame, so a rig the camera culls (the LOD that is not shown) picks up at
    /// exactly the same moment when it appears. Nothing allocates after construction; <see cref="Restart"/> resets it for a
    /// pooled owner.
    /// </summary>
    public sealed class ModelClipPlayer
    {
        /// <summary>
        /// Evaluate every rig every frame, drawn or not. A headless balance worker (round 25) runs with its cameras off, and a culled
        /// rig would leave its sockets (muzzles, hands) at the bind pose, so the game it measures would differ from the one on screen.
        /// </summary>
        public static bool AlwaysAnimate;

        // Frames a woken rig evaluates even while culled: the tick after the wake, and one more for its renderers' visibility to
        // register (a culled rig of a pooled owner just switched on would show its last life's pose for a frame).
        private const int WakeFrames = 2;

        private readonly ModelArtDefinition _art;
        private readonly Animator[] _animators;
        private int _wakeFrames;
        private readonly ClipMixer _mixer;
        private readonly PlayableGraph[] _graphs;
        private readonly AnimationMixerPlayable[] _mixers;
        private readonly AnimationClipPlayable[] _playables;
        private readonly float[] _written;
        private readonly int _clipCount;
        private readonly int _slotCount;

        // The upper-body layer (a hero shooting while he runs): a second mixer of the clips marked upper body, masked to
        // the art's upper-body bone and laid over the base mixer. Null when the art has no such clips.
        private readonly AnimationLayerMixerPlayable[] _layers;
        private readonly AnimationMixerPlayable[] _upperMixers;
        private readonly AnimationClipPlayable[] _upperPlayables;
        private readonly float[] _lengths;
        private int _upper = -1;
        private float _upperTime;
        private float _upperSpeed = 1f;
        private float _upperAge;
        private bool _upperHolds;
        private float _upperStopAge = -1f;
        private bool _disposed;

        /// <param name="animators">The rigs to drive: the near LOD first, then the far one, which plays the far clips. Their
        /// Animators need no controller.</param>
        public ModelClipPlayer(ModelArtDefinition art, Animator[] animators)
        {
            _art = art;
            ModelClip[] clips = art.Clips;
            _animators = animators;
            _clipCount = clips.Length;
            _mixer = CreateMixer(clips);
            _slotCount = _mixer.SlotCount;
            _written = new float[_slotCount];
            _lengths = new float[_clipCount];
            for (int i = 0; i < _clipCount; i++)
            {
                _lengths[i] = Mathf.Max(0.0001f, clips[i].Length);
            }

            _graphs = new PlayableGraph[animators.Length];
            _mixers = new AnimationMixerPlayable[animators.Length];
            _playables = new AnimationClipPlayable[animators.Length * _slotCount];
            bool layered = HasUpperBody(art);
            if (layered)
            {
                _layers = new AnimationLayerMixerPlayable[animators.Length];
                _upperMixers = new AnimationMixerPlayable[animators.Length];
                _upperPlayables = new AnimationClipPlayable[animators.Length * _clipCount];
            }

            for (int g = 0; g < animators.Length; g++)
            {
                Animator animator = animators[g];
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                // The far LOD's rig only evaluates while its mesh is drawn.
                animator.cullingMode = RestingCulling;
                PlayableGraph graph = PlayableGraph.Create(art.name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(graph, "Model", animator);
                var mixer = AnimationMixerPlayable.Create(graph, _slotCount);
                for (int s = 0; s < _slotCount; s++)
                {
                    ModelClip clip = clips[_mixer.ClipOf(s)];
                    var playable = AnimationClipPlayable.Create(graph, g == 0 ? clip.Clip : clip.FarClip);
                    playable.SetApplyFootIK(false);
                    // The player sets the time itself every frame, so the graph never advances a clip on its own.
                    playable.SetSpeed(0d);
                    graph.Connect(playable, 0, mixer, s);
                    _playables[g * _slotCount + s] = playable;
                }

                _graphs[g] = graph;
                _mixers[g] = mixer;
                output.SetSourcePlayable(layered ? LayerUpperBody(g, graph, mixer, animator, clips) : (Playable)mixer);
                graph.Play();
            }
        }

        private static AnimatorCullingMode RestingCulling => AlwaysAnimate ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;

        // Lengths, overlay weights, blend times and looping from the art's clips: the mixer itself knows no Unity clips.
        private static ClipMixer CreateMixer(ModelClip[] clips)
        {
            int count = clips.Length;
            var lengths = new float[count];
            var overlay = new float[count];
            var blend = new float[count];
            var loops = new bool[count];
            for (int i = 0; i < count; i++)
            {
                lengths[i] = clips[i].Length;
                overlay[i] = clips[i].Weight;
                blend[i] = clips[i].BlendSeconds;
                loops[i] = clips[i].Clip == null || clips[i].Clip.isLooping;
            }

            return new ClipMixer(lengths, overlay, blend, loops);
        }

        private static bool HasUpperBody(ModelArtDefinition art)
        {
            if (string.IsNullOrEmpty(art.UpperBodyBone))
            {
                return false;
            }

            foreach (ModelClip clip in art.Clips)
            {
                if (clip != null && clip.UpperBody && clip.Clip != null)
                {
                    return true;
                }
            }

            return false;
        }

        // The base mixer on layer 0, the upper-body clips on layer 1 behind a mask of the upper-body bone's branch.
        private Playable LayerUpperBody(int g, PlayableGraph graph, AnimationMixerPlayable baseMixer, Animator animator, ModelClip[] clips)
        {
            var layers = AnimationLayerMixerPlayable.Create(graph, 2);
            var upper = AnimationMixerPlayable.Create(graph, _clipCount);
            for (int i = 0; i < _clipCount; i++)
            {
                if (!clips[i].UpperBody || clips[i].Clip == null)
                {
                    continue;
                }

                var playable = AnimationClipPlayable.Create(graph, g == 0 ? clips[i].Clip : clips[i].FarClip);
                playable.SetApplyFootIK(false);
                playable.SetSpeed(0d);
                graph.Connect(playable, 0, upper, i);
                _upperPlayables[g * _clipCount + i] = playable;
            }

            graph.Connect(baseMixer, 0, layers, 0);
            graph.Connect(upper, 0, layers, 1);
            layers.SetInputWeight(0, 1f);
            layers.SetInputWeight(1, 0f);
            layers.SetLayerMaskFromAvatarMask(1, UpperBodyMask(animator.transform, _art.UpperBodyBone));
            _layers[g] = layers;
            _upperMixers[g] = upper;
            return layers;
        }

        // Every node of the rig by its path from the rig's root; only the upper-body bone and what hangs under it pass.
        private static AvatarMask UpperBodyMask(Transform root, string upperBone)
        {
            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            var mask = new AvatarMask { transformCount = nodes.Length };
            for (int i = 0; i < nodes.Length; i++)
            {
                mask.SetTransformPath(i, PathFrom(root, nodes[i]));
                mask.SetTransformActive(i, IsUnder(nodes[i], root, upperBone));
            }

            return mask;
        }

        private static string PathFrom(Transform root, Transform node)
        {
            string path = string.Empty;
            for (Transform t = node; t != null && t != root; t = t.parent)
            {
                path = path.Length == 0 ? t.name : t.name + "/" + path;
            }

            return path;
        }

        private static bool IsUnder(Transform node, Transform root, string bone)
        {
            for (Transform t = node; t != null && t != root; t = t.parent)
            {
                if (t.name == bone)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The clip on top of the cycle, or -1.</summary>
        public int Once => _mixer.Once;

        /// <summary>Seconds into the clip on top of the cycle.</summary>
        public float OnceTime => _mixer.OnceTime;

        /// <summary>The looping cycle, or -1.</summary>
        public int Cycle => _mixer.Cycle;

        /// <summary>The cycle's playback rate (1 = as authored).</summary>
        public float CycleRate => _mixer.CycleRate;

        /// <summary>True while the clip of a role plays on top of the cycle.</summary>
        public bool IsPlaying(ModelClipRole role) => _mixer.Once >= 0 && _art.Clips[_mixer.Once].Role == role;

        /// <summary>Back to nothing playing, every clock at its start: for a pooled owner sent out again.</summary>
        public void Restart()
        {
            _mixer.Restart();
            _upper = -1;
        }

        /// <summary>
        /// Takes over the whole state of another player of the same art: a corpse that goes on from the pose the body died in
        /// (mid-stride, mid-flinch) instead of starting from its idle.
        /// </summary>
        public void CopyFrom(ModelClipPlayer other)
        {
            if (other != null && other._art == _art)
            {
                _mixer.CopyFrom(other._mixer);
            }
        }

        /// <summary>
        /// For a pooled owner just switched on (a corpse, a respawned creature): its rigs were not drawn last frame, so a culled
        /// rig would skip this frame's animation update and show the pose of its last life. They evaluate for a couple of frames
        /// whether drawn or not, then cull again.
        /// </summary>
        public void Wake()
        {
            _wakeFrames = WakeFrames;
            SetCulling(AnimatorCullingMode.AlwaysAnimate);
        }

        private void SetCulling(AnimatorCullingMode mode)
        {
            foreach (Animator animator in _animators)
            {
                if (animator != null)
                {
                    animator.cullingMode = mode;
                }
            }
        }

        /// <summary>The upper-body clip playing, or -1.</summary>
        public int Upper => _upper;

        /// <summary>True while the clip of a role plays on the upper body.</summary>
        public bool IsPlayingUpper(ModelClipRole role) => _upper >= 0 && _art.Clips[_upper].Role == role;

        /// <summary>
        /// Plays an upper-body clip once over the legs' cycle; a held one stays on its last frame (a gun kept at aim) until
        /// another replaces it or <see cref="StopUpper"/> lets it go. Clips not marked upper body are ignored.
        /// </summary>
        public void PlayUpper(int index, float startSeconds = 0f, bool hold = false, float rate = 1f)
        {
            if (index < 0 || _upperMixers == null || !_art.Clips[index].UpperBody)
            {
                return;
            }

            // Restarting the clip that already shows keeps its weight: a second shot kicks without a fade.
            if (index != _upper || _upperStopAge >= 0f)
            {
                _upperAge = 0f;
            }

            _upper = index;
            _upperSpeed = rate;
            _upperHolds = hold;
            _upperStopAge = -1f;
            _upperTime = Mathf.Clamp(startSeconds, 0f, _lengths[index]);
        }

        /// <summary>Lets the upper-body clip fade out now.</summary>
        public void StopUpper()
        {
            if (_upper >= 0 && _upperStopAge < 0f)
            {
                _upperHolds = false;
                _upperStopAge = 0f;
            }
        }

        /// <summary>
        /// Loops a cycle at a playback rate (1 = as authored). A new cycle cross-fades over the old one (over the clip's own
        /// blend time, or the default); asking for the one already playing only changes its rate. A new cycle starts
        /// <paramref name="startSeconds"/> in (a flag that should not wave in step with its neighbours).
        /// </summary>
        public void Loop(int index, float rate, float startSeconds = 0f) => _mixer.Loop(index, rate, startSeconds);

        /// <summary>
        /// Plays a clip once on top of the cycle from a start time. A held clip stays on its last frame (a fall, a raised
        /// shield) until another clip or <see cref="StopOnce"/> replaces it. A negative rate plays it backwards from its end.
        /// A clip that replaces the one on top, or the same clip restarting, cross-fades with it.
        /// </summary>
        public void PlayOnce(int index, float startSeconds = 0f, bool hold = false, float rate = 1f) => _mixer.PlayOnce(index, startSeconds, hold, rate);

        /// <summary>Changes the playback rate of the clip on top where it is: zero holds it on its current frame.</summary>
        public void SetOnceRate(float rate) => _mixer.SetOnceRate(rate);

        /// <summary>Moves the clip on top to a time in it (a lasting power looping its sustain).</summary>
        public void SetOnceTime(float seconds) => _mixer.SetOnceTime(seconds);

        /// <summary>Lets the clip on top fade out now, wherever it is.</summary>
        public void StopOnce() => _mixer.StopOnce();

        /// <summary>Advances the clocks and writes weights and times into every graph.</summary>
        public void Tick(float deltaTime)
        {
            if (_disposed || _clipCount == 0)
            {
                return;
            }

            if (_wakeFrames > 0 && deltaTime > 0f && --_wakeFrames == 0)
            {
                SetCulling(RestingCulling);
            }

            // At a raised game speed the clips run faster but the blends keep their real duration (ClipMixer.Advance).
            _mixer.Advance(deltaTime, Time.timeScale);
            for (int s = 0; s < _slotCount; s++)
            {
                float weight = _mixer.WeightOf(s);
                bool changed = weight != _written[s];
                _written[s] = weight;
                for (int g = 0; g < _graphs.Length; g++)
                {
                    if (changed)
                    {
                        _mixers[g].SetInputWeight(s, weight);
                    }

                    if (weight > 0f)
                    {
                        _playables[g * _slotCount + s].SetTime(_mixer.TimeOf(s));
                    }
                }
            }

            if (_layers != null)
            {
                WriteUpper(AdvanceUpper(deltaTime));
            }
        }

        private float AdvanceUpper(float deltaTime)
        {
            if (_upper < 0)
            {
                return 0f;
            }

            _upperAge += deltaTime;
            float length = _lengths[_upper];
            _upperTime = Mathf.Clamp(_upperTime + deltaTime * _upperSpeed, 0f, length);
            float fadeOut = _upperHolds ? 1f : Mathf.Clamp01((length - _upperTime) / ClipMixer.OnceFadeOutSeconds);
            if (_upperStopAge >= 0f)
            {
                _upperStopAge += deltaTime;
                fadeOut = Mathf.Min(fadeOut, 1f - _upperStopAge / ClipMixer.OnceFadeOutSeconds);
            }

            if (fadeOut <= 0f)
            {
                _upper = -1;
                return 0f;
            }

            return Mathf.Min(Mathf.Clamp01(_upperAge / ClipMixer.DefaultOnceBlendSeconds), fadeOut) * _art.Clips[_upper].Weight;
        }

        private void WriteUpper(float weight)
        {
            for (int g = 0; g < _layers.Length; g++)
            {
                _layers[g].SetInputWeight(1, weight);
                AnimationMixerPlayable mixer = _upperMixers[g];
                for (int i = 0; i < _clipCount; i++)
                {
                    AnimationClipPlayable playable = _upperPlayables[g * _clipCount + i];
                    if (!playable.IsValid())
                    {
                        continue;
                    }

                    bool shown = i == _upper && weight > 0f;
                    mixer.SetInputWeight(i, shown ? 1f : 0f);
                    if (shown)
                    {
                        playable.SetTime(_upperTime);
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
    }
}
