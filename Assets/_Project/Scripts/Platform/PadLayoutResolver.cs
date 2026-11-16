using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Platform
{
    /// <summary>
    /// Picks the pad layout from the Input System layout of the pad in use: a DualShock or DualSense shows PlayStation
    /// shapes, a Switch Pro controller Nintendo letters, an Xbox pad Xbox letters, anything unknown the platform default.
    /// The decision reads the layout and its base layouts by name, so it is pure and testable without a device.
    /// </summary>
    public static class PadLayoutResolver
    {
        // Base layouts of the Input System's own pad support; a name match also catches HID variants of them.
        private static readonly string[] PlayStationNames = { "DualShock", "DualSense" };
        private static readonly string[] NintendoNames = { "SwitchPro", "Switch" };
        private static readonly string[] XboxNames = { "XInput", "Xbox" };
        private const int MaxDepth = 16;

        /// <summary>The layout of a device by its Input System layout name, walking its base layouts.</summary>
        public static PadLayout ForDeviceLayout(string layoutName, PadLayout fallback)
        {
            return ForLayoutChain(Chain(layoutName), fallback);
        }

        /// <summary>The layout for a chain of layout names, most specific first ("DualSenseGamepadHID", "DualShockGamepad", "Gamepad").</summary>
        public static PadLayout ForLayoutChain(IEnumerable<string> chain, PadLayout fallback)
        {
            if (chain == null)
            {
                return fallback;
            }

            foreach (string name in chain)
            {
                if (Matches(name, PlayStationNames))
                {
                    return PadLayout.PlayStation;
                }

                if (Matches(name, NintendoNames))
                {
                    return PadLayout.Nintendo;
                }

                if (Matches(name, XboxNames))
                {
                    return PadLayout.Xbox;
                }
            }

            return fallback;
        }

        /// <summary>The layout name and every base layout it derives from, as the Input System registered them.</summary>
        public static List<string> Chain(string layoutName)
        {
            var chain = new List<string>();
            string current = layoutName;
            while (!string.IsNullOrEmpty(current) && chain.Count < MaxDepth)
            {
                chain.Add(current);
                current = InputSystem.GetNameOfBaseLayout(current);
            }

            return chain;
        }

        private static bool Matches(string name, string[] parts)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
