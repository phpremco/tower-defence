using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>
    /// The PC display options: window mode, resolution, v-sync and the frame-rate cap. The list and mapping helpers are
    /// pure; <see cref="ApplyWindow"/> and <see cref="ApplyFrameRate"/> touch the screen. The editor never changes its
    /// Game view or its quality settings: both apply in a player build only.
    /// </summary>
    public static class DisplayOptions
    {
        /// <summary>The frame-rate caps the player steps through; 0 is no cap.</summary>
        public static readonly int[] FrameRateCaps = { 30, 60, 120, 0 };

        private const int VSyncEveryFrame = 1;
        private const int UnlimitedFrameRate = -1;

        public static FullScreenMode ToFullScreenMode(DisplayMode mode)
        {
            switch (mode)
            {
                case DisplayMode.Fullscreen: return FullScreenMode.ExclusiveFullScreen;
                case DisplayMode.Windowed: return FullScreenMode.Windowed;
                default: return FullScreenMode.FullScreenWindow;
            }
        }

        public static DisplayMode FromFullScreenMode(FullScreenMode mode)
        {
            switch (mode)
            {
                case FullScreenMode.ExclusiveFullScreen: return DisplayMode.Fullscreen;
                case FullScreenMode.Windowed: return DisplayMode.Windowed;
                default: return DisplayMode.Borderless;
            }
        }

        /// <summary>The screen sizes the monitor offers, each once (the list repeats a size per refresh rate), smallest first.</summary>
        public static List<Vector2Int> Resolutions(IReadOnlyList<Resolution> offered)
        {
            var sizes = new List<Vector2Int>();
            if (offered == null)
            {
                return sizes;
            }

            for (int i = 0; i < offered.Count; i++)
            {
                var size = new Vector2Int(offered[i].width, offered[i].height);
                if (size.x > 0 && size.y > 0 && !sizes.Contains(size))
                {
                    sizes.Add(size);
                }
            }

            sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return sizes;
        }

        /// <summary>The index of a size in the list, or of the closest one by pixel count when it is missing; -1 for an empty list.</summary>
        public static int IndexOf(IReadOnlyList<Vector2Int> sizes, Vector2Int size)
        {
            int best = -1;
            long bestGap = long.MaxValue;
            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i] == size)
                {
                    return i;
                }

                long gap = Math.Abs((long)sizes[i].x * sizes[i].y - (long)size.x * size.y);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>The index of a cap in <see cref="FrameRateCaps"/>; an unknown value counts as no cap.</summary>
        public static int CapIndex(int cap)
        {
            int index = Array.IndexOf(FrameRateCaps, cap);
            return index >= 0 ? index : FrameRateCaps.Length - 1;
        }

        /// <summary>Sets the window mode and size; a size of zero keeps the current one. Ignored in the editor.</summary>
        public static void ApplyWindow(DisplayMode mode, int width, int height)
        {
            if (Application.isEditor)
            {
                return;
            }

            if (width <= 0 || height <= 0)
            {
                width = Screen.width;
                height = Screen.height;
            }

            Screen.SetResolution(width, height, ToFullScreenMode(mode));
        }

        /// <summary>V-sync on syncs to the monitor (Unity then ignores the cap); off, the cap (0: none) holds the rate.</summary>
        public static void ApplyFrameRate(bool vSync, int cap)
        {
            // In the editor a v-sync change is written into the project's quality settings: players only.
            if (Application.isEditor)
            {
                return;
            }

            QualitySettings.vSyncCount = vSync ? VSyncEveryFrame : 0;
            Application.targetFrameRate = cap > 0 ? cap : UnlimitedFrameRate;
        }
    }
}
