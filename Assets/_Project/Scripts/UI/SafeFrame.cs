using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Where the HUD may draw: inside the platform's safe area (a notch, rounded corners) and inside a title-safe margin
    /// on every edge (a TV that crops its picture). Pure math in normalised screen units, so it is tested without a screen.
    /// </summary>
    public static class SafeFrame
    {
        /// <summary>A margin beyond this would squeeze the HUD into an unreadable middle; the option stops here.</summary>
        public const float MaxMarginPercent = 10f;

        private const float PercentScale = 100f;

        /// <summary>The margin in use: the player's choice when there is one (0 or more), else the platform's default.</summary>
        public static float Effective(int chosenPercent, float platformPercent)
        {
            float percent = chosenPercent >= 0 ? chosenPercent : platformPercent;
            return Mathf.Clamp(percent, 0f, MaxMarginPercent);
        }

        /// <summary>
        /// The HUD's anchors in the canvas (0 to 1 on both axes): the safe area, moved in to the margin where the margin
        /// reaches further. <paramref name="safeArea"/> is in pixels, as <c>Screen.safeArea</c> gives it.
        /// </summary>
        public static void Anchors(Rect safeArea, float screenWidth, float screenHeight, float marginPercent, out Vector2 min, out Vector2 max)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            float margin = Mathf.Clamp(marginPercent, 0f, MaxMarginPercent) / PercentScale;
            min = new Vector2(Mathf.Max(safeArea.xMin / screenWidth, margin), Mathf.Max(safeArea.yMin / screenHeight, margin));
            max = new Vector2(Mathf.Min(safeArea.xMax / screenWidth, 1f - margin), Mathf.Min(safeArea.yMax / screenHeight, 1f - margin));
        }

        /// <summary>
        /// Anchors for a child of the HUD frame that must still cover the whole screen (a dim, a backdrop), given the
        /// frame's anchors in the canvas: the screen's edges expressed in the frame's own 0-to-1 units.
        /// </summary>
        public static void CoverAnchors(Vector2 frameMin, Vector2 frameMax, out Vector2 min, out Vector2 max)
        {
            Vector2 size = frameMax - frameMin;
            if (size.x <= 0f || size.y <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            min = new Vector2(-frameMin.x / size.x, -frameMin.y / size.y);
            max = new Vector2((1f - frameMin.x) / size.x, (1f - frameMin.y) / size.y);
        }
    }
}
