using Game.Platform;

namespace Game.UI
{
    /// <summary>Maps a device kind and a pad layout to the glyph family that draws its prompts.</summary>
    public static class GlyphFamilies
    {
        public static GlyphFamily For(InputDeviceKind device, PadLayout layout)
        {
            if (device == InputDeviceKind.KeyboardMouse)
            {
                return GlyphFamily.Keyboard;
            }

            switch (layout)
            {
                case PadLayout.PlayStation: return GlyphFamily.PlayStation;
                case PadLayout.Nintendo: return GlyphFamily.Nintendo;
                default: return GlyphFamily.Xbox;
            }
        }
    }
}
