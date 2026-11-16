using System;
using Game.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels
{
    /// <summary>
    /// A run of text in one style. Lines are split into segments instead of using inline markup, so a struck word or
    /// a redacted span has exact edges, is checked when the data is saved, and nothing is parsed at runtime. The asset
    /// stores a localisation key; <see cref="Text"/> is the text in the player's language.
    /// </summary>
    [Serializable]
    public struct DialogueSegment
    {
        [FormerlySerializedAs("Text")] [TextArea(1, 4)] [SerializeField] private string _text;
        public SegmentStyle Style;

        public DialogueSegment(string text, SegmentStyle style)
        {
            _text = text;
            Style = style;
        }

        /// <summary>The stored value: a localisation key once the tables are written (Docs/LOCALISATION.md).</summary>
        public string Key => _text;

        public string Text => Loc.Get(_text);

        public int Length => PromptTokens.VisibleLength(Text);
    }
}
