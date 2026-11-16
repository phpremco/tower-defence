using System;
using System.Text;
using Game.Core;
using Game.Platform;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Button tokens inside localised text. A table string never names a key or button in words; it writes
    /// <c>{action:Confirm}</c> (a game action, so rebinding and the pad family move it) or <c>{control:&lt;Keyboard&gt;/escape}</c>
    /// (a fixed control). At runtime each token becomes the glyph of the device in use: inline glyph sprites in
    /// TextMeshPro text, the printed name in the key colour in legacy text.
    /// </summary>
    public static class PromptText
    {
        public const string KeyColour = "#FFD45C";
        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";

        /// <summary>True when the text holds at least one token.</summary>
        public static bool HasToken(string text) => PromptTokens.HasToken(text);

        /// <summary>Legacy text: each token becomes the printed name of its control in the key colour.</summary>
        public static string ForText(string text)
        {
            if (!HasToken(text))
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            PromptTokens.Walk(text, run => builder.Append(run), token => builder.Append("<color=").Append(KeyColour).Append('>').Append(NameOf(token)).Append("</color>"));
            return builder.ToString();
        }

        /// <summary>The text with every token as its printed name and no markup (for logs, the typewriter's voice, tests).</summary>
        public static string Plain(string text)
        {
            if (!HasToken(text))
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            PromptTokens.Walk(text, run => builder.Append(run), token => builder.Append(NameOf(token)));
            return builder.ToString();
        }

        /// <summary>
        /// TextMeshPro text: each token becomes an inline glyph (one character for the typewriter), or its printed name
        /// where no glyph can be drawn; the words around it are guarded against markup. <paramref name="visible"/> is the
        /// number of characters the result shows. Set <see cref="GlyphSpriteAtlas.Asset"/> on the text when
        /// <paramref name="usedGlyphs"/> is true.
        /// </summary>
        public static string ForTmp(string text, out int visible, out bool usedGlyphs)
        {
            var builder = new StringBuilder(text?.Length ?? 0);
            int count = 0;
            bool glyphs = false;
            PromptTokens.Walk(text ?? string.Empty, run =>
            {
                AppendGuarded(builder, run);
                count += run.Length;
            }, token =>
            {
                if (TrySpec(token, out GlyphSpec spec))
                {
                    builder.Append(GlyphSpriteAtlas.Tag(spec));
                    count++;
                    glyphs = true;
                    return;
                }

                string name = NameOf(token);
                builder.Append("<color=").Append(KeyColour).Append('>');
                AppendGuarded(builder, name);
                builder.Append("</color>");
                count += name.Length;
            });
            visible = count;
            usedGlyphs = glyphs;
            return builder.ToString();
        }

        /// <summary>Reads a token's body ("action:Confirm"): an action, or a fixed control path. False when it is neither.</summary>
        public static bool TryParse(string token, out GameAction action, out string controlPath)
        {
            action = default;
            controlPath = null;
            string name = PromptTokens.Body(token, PromptTokens.ActionPrefix);
            if (name != null)
            {
                return Enum.TryParse(name, false, out action) && Enum.IsDefined(typeof(GameAction), action);
            }

            controlPath = PromptTokens.Body(token, PromptTokens.ControlPrefix);
            if (controlPath != null)
            {
                return !string.IsNullOrEmpty(InputControlPath.TryGetDeviceLayout(controlPath));
            }

            return false;
        }

        private static string NameOf(string token)
        {
            if (!TryParse(token, out GameAction action, out string path))
            {
                return token;
            }

            if (path == null)
            {
                string printed = Services.TryGet(out IInputProvider input) ? input.PromptFor(action) : string.Empty;
                return string.IsNullOrEmpty(printed) ? action.ToString() : printed;
            }

            if (Services.TryGet(out UiSkin skin) && skin.KeyGlyphs != null && skin.KeyGlyphs.TryGet(path, out string label))
            {
                return label;
            }

            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        private static bool TrySpec(string token, out GlyphSpec spec)
        {
            spec = default;
            if (!TryParse(token, out GameAction action, out string path))
            {
                return false;
            }

            if (path == null)
            {
                return PromptGlyphs.TrySpec(action, out spec);
            }

            // A fixed key shows as a key cap, a fixed pad control in the pad family in use.
            PadLayout layout = Services.TryGet(out IInputBindings bindings) ? bindings.ActivePadLayout : PadLayout.Xbox;
            InputDeviceKind device = InputControlPath.TryGetDeviceLayout(path) == "Gamepad" ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;
            spec = GlyphCatalog.Describe(GlyphFamilies.For(device, layout), path, NameOf(token));
            return true;
        }

        private static void AppendGuarded(StringBuilder builder, string text)
        {
            if (text.IndexOf('<') < 0)
            {
                builder.Append(text);
                return;
            }

            builder.Append(NoParseOpen).Append(text).Append(NoParseClose);
        }
    }
}
