using Game.Platform;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Which glyph a control gets in each family: the Xbox letters in their colours, the PlayStation shapes, the Nintendo
    /// letters (in their own positions), bumpers, triggers, sticks, the d-pad, a mouse, or a key cap carrying the key's
    /// name. Unknown pad controls become a pill with the name the input layer printed, so nothing is ever blank.
    /// </summary>
    public static class GlyphCatalog
    {
        private const string KeyboardLayout = "Keyboard";
        private const string MouseLayout = "Mouse";
        private const string PointerLayout = "Pointer";

        private static readonly Color32 PadBody = new Color32(62, 70, 86, 255);
        private static readonly Color32 PadInk = new Color32(236, 240, 246, 255);
        private static readonly Color32 KeyFace = new Color32(226, 232, 240, 255);
        private static readonly Color32 KeyInk = new Color32(24, 30, 40, 255);
        private static readonly Color32 MouseBody = new Color32(214, 222, 232, 255);
        private static readonly Color32 Lit = new Color32(255, 196, 64, 255);

        private static readonly Color32 XboxGreen = new Color32(110, 200, 90, 255);
        private static readonly Color32 XboxRed = new Color32(232, 80, 70, 255);
        private static readonly Color32 XboxBlue = new Color32(70, 150, 236, 255);
        private static readonly Color32 XboxYellow = new Color32(244, 196, 54, 255);
        private static readonly Color32 PsBlue = new Color32(132, 170, 236, 255);
        private static readonly Color32 PsRed = new Color32(255, 104, 112, 255);
        private static readonly Color32 PsPink = new Color32(232, 146, 210, 255);
        private static readonly Color32 PsGreen = new Color32(64, 210, 180, 255);

        /// <summary>The glyph for a bound control path in a family; <paramref name="label"/> is the printed name as a fallback.</summary>
        public static GlyphSpec Describe(GlyphFamily family, string controlPath, string label)
        {
            string device = InputControlPath.TryGetDeviceLayout(controlPath) ?? string.Empty;
            string control = ButtonGlyphTable.ControlName(controlPath);
            if (device == MouseLayout || device == PointerLayout)
            {
                return Mouse(control);
            }

            if (family == GlyphFamily.Keyboard || device == KeyboardLayout)
            {
                return KeyCap(label);
            }

            return Pad(family, control, label);
        }

        /// <summary>A key cap with a name in capitals, as long as it needs to be.</summary>
        public static GlyphSpec KeyCap(string label)
        {
            string text = string.IsNullOrEmpty(label) ? "?" : label.ToUpperInvariant();
            return new GlyphSpec(GlyphShape.KeyCap, KeyFace, KeyInk, text);
        }

        private static GlyphSpec Mouse(string control)
        {
            GlyphSymbol part;
            switch (control.ToLowerInvariant())
            {
                case "leftbutton": part = GlyphSymbol.MouseLeft; break;
                case "rightbutton": part = GlyphSymbol.MouseRight; break;
                case "middlebutton": part = GlyphSymbol.MouseMiddle; break;
                default: part = control.StartsWith("scroll") ? GlyphSymbol.MouseWheel : GlyphSymbol.None; break;
            }

            return new GlyphSpec(GlyphShape.Mouse, MouseBody, Lit, string.Empty, part);
        }

        private static GlyphSpec Pad(GlyphFamily family, string control, string label)
        {
            switch (control.ToLowerInvariant())
            {
                case "buttonsouth": return Face(family, 0);
                case "buttoneast": return Face(family, 1);
                case "buttonwest": return Face(family, 2);
                case "buttonnorth": return Face(family, 3);
                case "leftshoulder": return Named(GlyphShape.Shoulder, family, "LB", "L1", "L");
                case "rightshoulder": return Named(GlyphShape.Shoulder, family, "RB", "R1", "R");
                case "lefttrigger": return Named(GlyphShape.Trigger, family, "LT", "L2", "ZL");
                case "righttrigger": return Named(GlyphShape.Trigger, family, "RT", "R2", "ZR");
                case "start": return family == GlyphFamily.Nintendo ? Round("+", PadInk) : new GlyphSpec(GlyphShape.Menu, PadBody, PadInk, string.Empty);
                case "select":
                    if (family == GlyphFamily.Nintendo)
                    {
                        return Round("-", PadInk);
                    }

                    return family == GlyphFamily.PlayStation ? new GlyphSpec(GlyphShape.Shoulder, PadBody, PadInk, "CREATE") : new GlyphSpec(GlyphShape.View, PadBody, PadInk, string.Empty);
                case "leftstickpress": return Stick(family == GlyphFamily.Xbox ? "LS" : "L3");
                case "rightstickpress": return Stick(family == GlyphFamily.Xbox ? "RS" : "R3");
                case "leftstick": return Stick("L");
                case "rightstick": return Stick("R");
                case "dpad": return Dpad(GlyphSymbol.None);
                case "dpad/up": return Dpad(GlyphSymbol.Up);
                case "dpad/down": return Dpad(GlyphSymbol.Down);
                case "dpad/left": return Dpad(GlyphSymbol.Left);
                case "dpad/right": return Dpad(GlyphSymbol.Right);
            }

            string text = string.IsNullOrEmpty(label) ? control : label;
            return new GlyphSpec(GlyphShape.Shoulder, PadBody, PadInk, text.ToUpperInvariant());
        }

        // Index 0 south, 1 east, 2 west, 3 north. The Nintendo letters sit swapped against the Xbox ones.
        private static GlyphSpec Face(GlyphFamily family, int index)
        {
            switch (family)
            {
                case GlyphFamily.PlayStation:
                {
                    GlyphSymbol[] shapes = { GlyphSymbol.Cross, GlyphSymbol.Circle, GlyphSymbol.Square, GlyphSymbol.Triangle };
                    Color32[] colours = { PsBlue, PsRed, PsPink, PsGreen };
                    return new GlyphSpec(GlyphShape.Round, PadBody, colours[index], string.Empty, shapes[index]);
                }

                case GlyphFamily.Nintendo:
                {
                    string[] letters = { "B", "A", "Y", "X" };
                    return Round(letters[index], PadInk);
                }

                default:
                {
                    string[] letters = { "A", "B", "X", "Y" };
                    Color32[] colours = { XboxGreen, XboxRed, XboxBlue, XboxYellow };
                    return Round(letters[index], colours[index]);
                }
            }
        }

        private static GlyphSpec Named(GlyphShape shape, GlyphFamily family, string xbox, string playStation, string nintendo)
        {
            string text = family == GlyphFamily.PlayStation ? playStation : family == GlyphFamily.Nintendo ? nintendo : xbox;
            return new GlyphSpec(shape, PadBody, PadInk, text);
        }

        private static GlyphSpec Round(string text, Color32 ink) => new GlyphSpec(GlyphShape.Round, PadBody, ink, text);

        private static GlyphSpec Stick(string text) => new GlyphSpec(GlyphShape.Stick, PadBody, PadInk, text);

        private static GlyphSpec Dpad(GlyphSymbol arm) => new GlyphSpec(GlyphShape.Dpad, PadBody, Lit, string.Empty, arm);
    }
}
