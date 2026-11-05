namespace Game.Bosses
{
    /// <summary>
    /// How a ground-mark crack reads as a torn fissure from the game camera (Remco 2026-11-05: "a thin glowing laser
    /// line", then "die open start met recht einde en die rechte hoek zijn niet zo mooi"): a dark gap between two lips lit
    /// from the melt below, a faint molten core deep in the gap, ends that narrow to a hairline with small branching
    /// cracks, bends drawn as curves, a meandering line whose width breathes along its length, and rubble thrown up at
    /// the open end. Look values only, fed to <c>Game/BossDecal</c> and <see cref="CrackLineView"/>.
    /// </summary>
    public readonly struct FissureLook
    {
        public FissureLook(float gapHalf, float lipGlow, float lipBand, float coreGlow, float rubbleRate, float endTaper, float headTaper, float wiggle, float widthJitter, int branchesPerEnd)
        {
            GapHalf = gapHalf;
            LipGlow = lipGlow;
            LipBand = lipBand;
            CoreGlow = coreGlow;
            RubbleRate = rubbleRate;
            EndTaper = endTaper;
            HeadTaper = headTaper;
            Wiggle = wiggle;
            WidthJitter = widthJitter;
            BranchesPerEnd = branchesPerEnd;
        }

        /// <summary>Half the dark gap's width as a share of the strip's width (the strip runs from -0.5 to 0.5).</summary>
        public float GapHalf { get; }

        /// <summary>Glow of the lips, as a share of the mark's glow colour.</summary>
        public float LipGlow { get; }

        /// <summary>Half the width of each lit lip band, as a share of the strip's width.</summary>
        public float LipBand { get; }

        /// <summary>Glow of the thin core line deep in the gap, as a share of the mark's glow colour.</summary>
        public float CoreGlow { get; }

        /// <summary>Rock pieces per second thrown up at the open end while it tears on.</summary>
        public float RubbleRate { get; }

        /// <summary>Metres over which the strip narrows to a hairline at both of its ends.</summary>
        public float EndTaper { get; }

        /// <summary>Metres behind the tearing head over which the strip comes to a point.</summary>
        public float HeadTaper { get; }

        /// <summary>How far, in metres, the line meanders sideways from its planned route.</summary>
        public float Wiggle { get; }

        /// <summary>How much the width breathes along the line (0.2: between 80% and 120%).</summary>
        public float WidthJitter { get; }

        /// <summary>Hairline cracks branching off near each end.</summary>
        public int BranchesPerEnd { get; }

        /// <summary>False for the default (no fissure look: the mark keeps its material's own profile).</summary>
        public bool IsSet => GapHalf > 0f;
    }
}
