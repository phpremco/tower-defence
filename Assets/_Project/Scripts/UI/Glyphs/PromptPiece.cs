namespace Game.UI
{
    /// <summary>One piece of a help line: a run of words, or a hole that a glyph (or a control's printed name) fills.</summary>
    public readonly struct PromptPiece
    {
        private const int NoHole = -1;

        private PromptPiece(string text, int hole, bool gapBefore)
        {
            Text = text;
            Hole = hole;
            GapBefore = gapBefore;
        }

        /// <summary>The words of a text piece, without spaces at either end; null for a hole.</summary>
        public string Text { get; }

        /// <summary>The index of the hole ("{2}" is 2); negative for a text piece.</summary>
        public int Hole { get; }

        /// <summary>True when the template had a space between this piece and the one before it in the group.</summary>
        public bool GapBefore { get; }

        public bool IsHole => Hole >= 0;

        public static PromptPiece Words(string text, bool gapBefore) => new PromptPiece(text, NoHole, gapBefore);

        public static PromptPiece ForHole(int hole, bool gapBefore) => new PromptPiece(null, hole, gapBefore);
    }
}
