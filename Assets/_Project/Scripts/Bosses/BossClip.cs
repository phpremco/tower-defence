using Game.Core;
using Game.Enemies;

namespace Game.Bosses
{
    /// <summary>
    /// One of a boss's mechanic clips as the mechanic plays it: how long it runs at its playback rate, when its moments
    /// fall, and how far (world units) the body has travelled by its last frame (fixed-pivot travel clips). Read from
    /// the imported art; without the clip every number is zero and the mechanic simply skips the pose.
    /// </summary>
    public readonly struct BossClip
    {
        private BossClip(ModelClipRole role, float rate, float seconds, float moment, float cue, float forward, float up, float anchor)
        {
            Anchor = anchor;
            Role = role;
            Rate = rate;
            Seconds = seconds;
            Moment = moment;
            Cue = cue;
            Forward = forward;
            Up = up;
        }

        public ModelClipRole Role { get; }
        public float Rate { get; }

        /// <summary>Real seconds the clip takes at its rate.</summary>
        public float Seconds { get; }

        /// <summary>Real seconds to its main moment (the impact, the throw, the breach).</summary>
        public float Moment { get; }

        /// <summary>Real seconds to its earlier cue (the burst before the throw), or zero.</summary>
        public float Cue { get; }

        /// <summary>World units forward the body has moved by the last frame.</summary>
        public float Forward { get; }

        /// <summary>World units up the body has moved by the last frame.</summary>
        public float Up { get; }

        /// <summary>World units ahead of the pivot where the clip meets the world: the hole, the bank, the ledge face.</summary>
        public float Anchor { get; }

        public bool Exists => Seconds > 0f;

        public static BossClip Of(EnemyView view, ModelClipRole role, float rate = 1f)
        {
            ModelClip clip = view != null ? view.ClipOf(role) : null;
            if (clip == null || rate <= 0f)
            {
                return new BossClip(role, rate, 0f, 0f, 0f, 0f, 0f, 0f);
            }

            float scale = view.ModelScale;
            return new BossClip(role, rate, clip.Length / rate, clip.EventSeconds / rate, clip.CueSeconds / rate, clip.EndOffset.x * scale, clip.EndOffset.y * scale, clip.AnchorForward * scale);
        }
    }
}
