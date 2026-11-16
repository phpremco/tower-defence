using UnityEngine;

namespace Game.UI
{
    /// <summary>Everything the painter needs to draw one glyph: shape, colours, and the label or symbol inside.</summary>
    public readonly struct GlyphSpec
    {
        public GlyphSpec(GlyphShape shape, Color32 fill, Color32 ink, string text, GlyphSymbol symbol = GlyphSymbol.None)
        {
            Shape = shape;
            Fill = fill;
            Ink = ink;
            Text = text ?? string.Empty;
            Symbol = symbol;
        }

        public GlyphShape Shape { get; }

        public Color32 Fill { get; }

        public Color32 Ink { get; }

        public string Text { get; }

        public GlyphSymbol Symbol { get; }

        /// <summary>A stable cache key for the drawn sprite.</summary>
        public string Key => $"{(int)Shape}|{Fill.r},{Fill.g},{Fill.b}|{Ink.r},{Ink.g},{Ink.b}|{(int)Symbol}|{Text}";
    }
}
