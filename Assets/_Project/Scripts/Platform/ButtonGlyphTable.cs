using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>
    /// What a prompt prints for a control: "A" for the south face button on an Xbox-style pad, "B" on a Nintendo one,
    /// "Esc" for the escape key. Controls missing from the table fall back to the Input System's own display string.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Platform/Button Glyph Table", fileName = "Glyphs_")]
    public sealed class ButtonGlyphTable : ScriptableObject
    {
        [Tooltip("The pad layout this table names; the platform settings pick the table. Unused on the keyboard table.")]
        [SerializeField] private PadLayout _layout;

        [SerializeField] private ButtonGlyph[] _glyphs = new ButtonGlyph[0];

        public PadLayout Layout => _layout;

        public IReadOnlyList<ButtonGlyph> Glyphs => _glyphs;

        /// <summary>The label of a control path such as "&lt;Gamepad&gt;/buttonSouth" or "buttonSouth"; false when the table has none.</summary>
        public bool TryGet(string controlPath, out string label) => TryFind(_glyphs, controlPath, out label);

        /// <summary>The lookup itself, pure so tests can run it on plain arrays.</summary>
        public static bool TryFind(IReadOnlyList<ButtonGlyph> glyphs, string controlPath, out string label)
        {
            string control = ControlName(controlPath);
            if (glyphs != null && control.Length > 0)
            {
                for (int i = 0; i < glyphs.Count; i++)
                {
                    if (string.Equals(glyphs[i].Control, control, StringComparison.OrdinalIgnoreCase))
                    {
                        label = glyphs[i].Label;
                        return true;
                    }
                }
            }

            label = string.Empty;
            return false;
        }

        /// <summary>"&lt;Gamepad&gt;/leftStick/up" becomes "leftStick/up": the device part is what the table does not care about.</summary>
        public static string ControlName(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
            {
                return string.Empty;
            }

            if (controlPath[0] != '<')
            {
                return controlPath;
            }

            int slash = controlPath.IndexOf('/');
            return slash < 0 ? string.Empty : controlPath.Substring(slash + 1);
        }

        private void OnValidate()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ButtonGlyph glyph in _glyphs)
            {
                if (string.IsNullOrWhiteSpace(glyph.Control) || string.IsNullOrWhiteSpace(glyph.Label))
                {
                    Debug.LogWarning($"Glyph table '{name}' has an entry without a control or a label.", this);
                }
                else if (!seen.Add(glyph.Control))
                {
                    Debug.LogWarning($"Glyph table '{name}' names '{glyph.Control}' twice.", this);
                }
            }
        }
    }
}
