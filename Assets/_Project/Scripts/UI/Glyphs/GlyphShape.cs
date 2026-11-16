namespace Game.UI
{
    /// <summary>The outline a button glyph is drawn in; the painter fills it and writes the label or symbol inside.</summary>
    public enum GlyphShape
    {
        /// <summary>A face button: a dark disc with a coloured letter or symbol.</summary>
        Round = 0,

        /// <summary>A bumper: a wide pill with its name.</summary>
        Shoulder = 1,

        /// <summary>A trigger: a tall rounded block with its name.</summary>
        Trigger = 2,

        /// <summary>A keyboard key: a light cap, as wide as its name needs.</summary>
        KeyCap = 3,

        /// <summary>A stick: a ring around a disc with its letter.</summary>
        Stick = 4,

        /// <summary>The d-pad: a plus, one arm lit when the glyph means one direction.</summary>
        Dpad = 5,

        /// <summary>A mouse with one part lit (left, right, middle button or the wheel).</summary>
        Mouse = 6,

        /// <summary>A menu button: a dark disc with three lines (Xbox Menu, PlayStation Options).</summary>
        Menu = 7,

        /// <summary>The Xbox View button: a dark disc with two overlapping windows.</summary>
        View = 8,
    }
}
