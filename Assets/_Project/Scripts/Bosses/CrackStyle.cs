using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// How a line a boss lays on the ground looks (<see cref="CrackLineView"/>): the burrow's warm orange crack, the thin
    /// heat line that runs to a hunted tower, or the frost that creeps ahead of an ice boss. Look values only.
    /// </summary>
    public readonly struct CrackStyle
    {
        public CrackStyle(float width, float glowWidth, Color seam, Color halo, Color headGlow, float headRadius, Color grit, float gritShare, float fadeSeconds)
        {
            Width = width;
            GlowWidth = glowWidth;
            Seam = seam;
            Halo = halo;
            HeadGlow = headGlow;
            HeadRadius = headRadius;
            Grit = grit;
            GritShare = gritShare;
            FadeSeconds = fadeSeconds;
        }

        public float Width { get; }
        public float GlowWidth { get; }
        public Color Seam { get; }
        public Color Halo { get; }
        public Color HeadGlow { get; }
        public float HeadRadius { get; }
        public Color Grit { get; }

        /// <summary>Share of the grit spray at the open end (none for a line of light).</summary>
        public float GritShare { get; }
        public float FadeSeconds { get; }

        /// <summary>The burrow's crack: wide, warm orange, a spray of grit where it opens.</summary>
        public static CrackStyle Burrow => new CrackStyle(0.75f, 2.2f, new Color(1f, 0.42f, 0.1f, 1f) * 2.6f, new Color(1f, 0.35f, 0.08f, 1f) * 0.7f, new Color(1f, 0.5f, 0.15f, 0.9f), 1.4f, new Color(0.55f, 0.42f, 0.3f, 0.8f), 1f, 4f);

        /// <summary>"She has smelled you": a thin, hot thread of light from the boss to the hunted tower.</summary>
        public static CrackStyle Heat => new CrackStyle(0.14f, 0.6f, new Color(1f, 0.5f, 0.15f, 1f) * 1.6f, new Color(1f, 0.3f, 0.05f, 1f) * 0.3f, new Color(1f, 0.55f, 0.2f, 0.8f), 0.7f, new Color(1f, 0.6f, 0.3f, 0.6f), 0.3f, 2.5f);

        /// <summary>Frost creeping over the ground ahead of an ice boss: pale blue, soft, no grit.</summary>
        public static CrackStyle Frost => new CrackStyle(0.9f, 2.4f, new Color(0.65f, 0.9f, 1f, 1f) * 1.6f, new Color(0.5f, 0.8f, 1f, 1f) * 0.45f, new Color(0.6f, 0.9f, 1f, 0.6f), 1.1f, new Color(0.85f, 0.95f, 1f, 0.7f), 0.4f, 1.6f);
    }
}
