namespace Game.Platform
{
    /// <summary>
    /// Conventions that differ per pad family. Nintendo players confirm with A and go back with B, and on their pads A sits
    /// east and B south: the opposite positions of the Xbox and PlayStation convention (south confirms, east goes back).
    /// Bindings are positional ("buttonSouth"), so the Nintendo layout swaps the two positions for confirm and cancel, in
    /// the game's actions and in menu navigation alike.
    /// </summary>
    public static class PadLayoutRules
    {
        public const string SouthPath = "<Gamepad>/buttonSouth";
        public const string EastPath = "<Gamepad>/buttonEast";

        /// <summary>True when the layout confirms with the east button (Nintendo A) and cancels with the south one (B).</summary>
        public static bool ConfirmsEast(PadLayout layout) => layout == PadLayout.Nintendo;

        /// <summary>The pad button that confirms on this layout (default bindings, before any rebinding).</summary>
        public static string ConfirmPath(PadLayout layout) => ConfirmsEast(layout) ? EastPath : SouthPath;

        /// <summary>The pad button that cancels on this layout (default bindings, before any rebinding).</summary>
        public static string CancelPath(PadLayout layout) => ConfirmsEast(layout) ? SouthPath : EastPath;
    }
}
