using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A run of text in one style. Lines are split into segments instead of using inline markup, so a struck word or
    /// a redacted span has exact edges, is checked when the data is saved, and nothing is parsed at runtime.
    /// </summary>
    [Serializable]
    public struct DialogueSegment
    {
        [TextArea(1, 4)] public string Text;
        public SegmentStyle Style;

        public DialogueSegment(string text, SegmentStyle style)
        {
            Text = text;
            Style = style;
        }

        public int Length => Text == null ? 0 : Text.Length;
    }
}
