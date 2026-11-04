using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The pieces the dialogue box and the cutscene radar are drawn with, painted in Blender (ArtSource README round 25,
    /// <c>UI_Dialogue</c>): the rounded panel and its glowing edge (nine-sliced on the same outline, the edge drawn
    /// <see cref="EdgeOverhang"/> outside the panel), the name tag, the portrait frame, the "next" arrow and the
    /// tiling scanlines, redaction noise, soft radial glow and bottom-up shade. All white, tinted by the view. Null
    /// without the art set (tests that build a screen on their own).
    /// </summary>
    public static class DialogueArt
    {
        // README: Dialogue_Edge keeps the old contract, 18 px (at 1x) outside the panel.
        private const float EdgeOutside = 18f;

        /// <summary>How far outside the panel the glow sprite reaches; the edge image is offset by this much.</summary>
        public static float EdgeOverhang => EdgeOutside;

        /// <summary>A filled rounded rectangle.</summary>
        public static Sprite Panel => Sprite("Dialogue_Panel");

        /// <summary>A thin bright line on the panel's border with a soft glow outside and a faint one inside.</summary>
        public static Sprite Edge => Sprite("Dialogue_Edge");

        /// <summary>The speaker's name tag.</summary>
        public static Sprite NameTag => Sprite("Dialogue_NameTag");

        /// <summary>The frame round the speaker's portrait.</summary>
        public static Sprite PortraitFrame => Sprite("Dialogue_PortraitFrame");

        /// <summary>A small arrow pointing down: more to read.</summary>
        public static Sprite Arrow => Sprite("Dialogue_Arrow");

        /// <summary>A radar's sweeping arm (pivot at the centre, leading edge up) and a contact blip.</summary>
        public static Sprite RadarSweep => Sprite("Radar_Sweep");

        public static Sprite RadarContact => Sprite("Radar_Contact");

        /// <summary>Two dark lines in eight rows (the old four-pixel period at 2x); tile it vertically with a RawImage uvRect.</summary>
        public static Texture2D Scanlines => UiArtSet.Active != null ? UiArtSet.Active.DialogueScanlines : null;

        /// <summary>A strip of noise for redacted words; tile it horizontally.</summary>
        public static Texture2D Noise => UiArtSet.Active != null ? UiArtSet.Active.DialogueNoise : null;

        /// <summary>A soft round glow, bright in the middle.</summary>
        public static Texture2D Radial => UiArtSet.Active != null ? UiArtSet.Active.DialogueRadial : null;

        /// <summary>A vertical shade, dark at the bottom.</summary>
        public static Texture2D Shade => UiArtSet.Active != null ? UiArtSet.Active.DialogueShade : null;

        private static Sprite Sprite(string name) => UiArtSet.Active != null ? UiArtSet.Active.Sprite(name) : null;
    }
}
