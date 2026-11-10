using System;

namespace Game.Platform
{
    /// <summary>One printed name for one control: the control path without its device ("buttonSouth", "escape") and its label.</summary>
    [Serializable]
    public struct ButtonGlyph
    {
        public string Control;
        public string Label;

        public ButtonGlyph(string control, string label)
        {
            Control = control;
            Label = label;
        }
    }
}
