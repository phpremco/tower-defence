using Game.Core;
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
            HasMark = false;
            Mark = default;
            MarkWidth = 0f;
            Chunks = 0;
            MarkGlow = default;
            Slabs = false;
        }

        private CrackStyle(CrackStyle style, bool slabs)
            : this(style, style.Mark, style.MarkWidth, style.Chunks, style.MarkGlow)
        {
            HasMark = style.HasMark;
            Slabs = slabs;
        }

        private CrackStyle(CrackStyle style, BossFxArt.Mark mark, float markWidth, int chunks, Color markGlow)
            : this(style.Width, style.GlowWidth, style.Seam, style.Halo, style.HeadGlow, style.HeadRadius, style.Grit, style.GritShare, style.FadeSeconds)
        {
            HasMark = true;
            Mark = mark;
            MarkWidth = markWidth;
            Chunks = chunks;
            MarkGlow = markGlow;
        }

        /// <summary>True when the line is drawn as a painted ground mark (round 29) where the art exists.</summary>
        public bool HasMark { get; }
        public BossFxArt.Mark Mark { get; }

        /// <summary>World width of the mark across the line.</summary>
        public float MarkWidth { get; }

        /// <summary>Rock chunks heaved up beside the line per metre (0: none).</summary>
        public int Chunks { get; }

        /// <summary>True when basalt slabs (README round 30, Gravemaw's crack slabs) heave up along both lips.</summary>
        public bool Slabs { get; }

        /// <summary>The mark's glow in another colour than its own (alpha 0: its own).</summary>
        public Color MarkGlow { get; }

        /// <summary>The same line drawn as a ground mark: the glow seam stays only as a soft halo over it.</summary>
        public CrackStyle WithMark(BossFxArt.Mark mark, float markWidth, int chunksPerFourMetres, Color glow = default) => new CrackStyle(this, mark, markWidth, chunksPerFourMetres, glow);

        /// <summary>The same line with heaved basalt slabs along its lips where the art exists.</summary>
        public CrackStyle WithSlabs() => new CrackStyle(this, true);

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
        public static CrackStyle Burrow => new CrackStyle(0.75f, 2.2f, new Color(1f, 0.42f, 0.1f, 1f) * 2.6f, new Color(1f, 0.35f, 0.08f, 1f) * 0.7f, new Color(1f, 0.5f, 0.15f, 0.9f), 1.4f, new Color(0.55f, 0.42f, 0.3f, 0.8f), 1f, 4f).WithMark(BossFxArt.Mark.GroundCrack, 3f, 5).WithSlabs();

        /// <summary>"She has smelled you": a thin, hot thread of light from the boss to the hunted tower.</summary>
        public static CrackStyle Heat => new CrackStyle(0.14f, 0.6f, new Color(1f, 0.5f, 0.15f, 1f) * 1.6f, new Color(1f, 0.3f, 0.05f, 1f) * 0.3f, new Color(1f, 0.55f, 0.2f, 0.8f), 0.7f, new Color(1f, 0.6f, 0.3f, 0.6f), 0.3f, 2.5f);

        /// <summary>Frost creeping over the ground ahead of an ice boss: pale blue, soft, no grit.</summary>
        public static CrackStyle Frost => new CrackStyle(0.9f, 2.4f, new Color(0.65f, 0.9f, 1f, 1f) * 1.6f, new Color(0.5f, 0.8f, 1f, 1f) * 0.45f, new Color(0.6f, 0.9f, 1f, 0.6f), 1.1f, new Color(0.85f, 0.95f, 1f, 0.7f), 0.4f, 1.6f);

        /// <summary>A crystal giant's cracks of light and its shards' routes lighting up: thin, cold white-blue, no grit.</summary>
        public static CrackStyle Light => new CrackStyle(0.22f, 1.1f, new Color(0.75f, 0.9f, 1f, 1f) * 2.4f, new Color(0.55f, 0.75f, 1f, 1f) * 0.45f, new Color(0.8f, 0.92f, 1f, 0.8f), 0.9f, new Color(0.85f, 0.95f, 1f, 0.6f), 0.2f, 1.2f);

        /// <summary>The new road that rises to the Sealed One's base: a wide violet seam with stone grit where it opens.</summary>
        public static CrackStyle Seal => new CrackStyle(1f, 2.6f, new Color(0.7f, 0.35f, 1f, 1f) * 2.2f, new Color(0.45f, 0.2f, 0.9f, 1f) * 0.55f, new Color(0.75f, 0.45f, 1f, 0.9f), 1.6f, new Color(0.5f, 0.45f, 0.55f, 0.8f), 1f, 3f).WithMark(BossFxArt.Mark.GroundCrack, 2.6f, 4, new Color(0.62f, 0.3f, 1f, 1f) * 3.2f);

        /// <summary>The furrow a hauling body ploughs: dark broken ground with a faint violet glow, grit along it.</summary>
        public static CrackStyle Furrow => new CrackStyle(1.3f, 2f, new Color(0.35f, 0.16f, 0.55f, 1f) * 0.9f, new Color(0.18f, 0.1f, 0.3f, 1f) * 0.35f, new Color(0.45f, 0.25f, 0.7f, 0.5f), 1f, new Color(0.42f, 0.36f, 0.32f, 0.9f), 1f, 6f).WithMark(BossFxArt.Mark.Furrow, 2.6f, 3);

        /// <summary>The thread along which a boss drains a gate: thin and violet.</summary>
        public static CrackStyle Drain => new CrackStyle(0.12f, 0.55f, new Color(0.75f, 0.4f, 1f, 1f) * 1.8f, new Color(0.5f, 0.25f, 1f, 1f) * 0.35f, new Color(0.75f, 0.45f, 1f, 0.8f), 0.7f, new Color(0.7f, 0.5f, 1f, 0.5f), 0.2f, 2f);
    }
}
