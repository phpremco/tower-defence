using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A help line with button glyphs inside the sentence: the localised template keeps its "{0}" holes, and each hole
    /// becomes one or more glyph images (or the printed name when a glyph is missing). Text runs and glyphs sit in a
    /// centred row, so a translation can move the holes freely.
    /// </summary>
    public sealed class PromptLine : MonoBehaviour
    {
        private const float GlyphScale = 1.25f;
        private const float Spacing = 4f;

        private RectTransform _row;
        private int _size;
        private Color _colour;

        /// <summary>The row of the line, stretched over <paramref name="area"/>; text size and colour for its runs.</summary>
        public static PromptLine Create(RectTransform area, int size, Color colour)
        {
            var row = new GameObject("PromptLine", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(area, false);
            row.anchorMin = Vector2.zero;
            row.anchorMax = Vector2.one;
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var line = row.gameObject.AddComponent<PromptLine>();
            line._row = row;
            line._size = size;
            line._colour = colour;
            return line;
        }

        /// <summary>
        /// Lays out the template; hole <c>{i}</c> shows <paramref name="glyphs"/>[i] (several are joined by a slash), or
        /// <paramref name="names"/>[i] when that hole has no glyph.
        /// </summary>
        public void Set(string template, IReadOnlyList<Sprite[]> glyphs, IReadOnlyList<string> names)
        {
            Clear();
            var run = new StringBuilder();
            for (int i = 0; i < template.Length; i++)
            {
                int hole = HoleAt(template, i);
                if (hole < 0)
                {
                    run.Append(template[i]);
                    continue;
                }

                Flush(run);
                AddHole(hole, glyphs, names);
                i += 2;
            }

            Flush(run);
        }

        private void AddHole(int hole, IReadOnlyList<Sprite[]> glyphs, IReadOnlyList<string> names)
        {
            Sprite[] sprites = glyphs != null && hole < glyphs.Count ? glyphs[hole] : null;
            if (sprites == null || sprites.Length == 0 || System.Array.IndexOf(sprites, null) >= 0)
            {
                AddText(names != null && hole < names.Count ? names[hole] : string.Empty);
                return;
            }

            for (int s = 0; s < sprites.Length; s++)
            {
                if (s > 0)
                {
                    AddText("/");
                }

                Image image = PromptGlyphs.CreateImage(_row, _size * GlyphScale);
                image.gameObject.AddComponent<LayoutElement>();
                PromptGlyphs.Show(image, sprites[s]);
            }
        }

        // A long line (a translation, a wide key name) shrinks evenly to its box instead of running off both sides.
        private void LateUpdate()
        {
            float needed = LayoutUtility.GetPreferredWidth(_row);
            float room = ((RectTransform)_row.parent).rect.width;
            float scale = needed > room && needed > 0f ? room / needed : 1f;
            if (!Mathf.Approximately(_row.localScale.x, scale))
            {
                _row.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // "{3}" with a single digit is a hole; anything else is text.
        private static int HoleAt(string template, int i)
        {
            if (i + 2 >= template.Length || template[i] != '{' || template[i + 2] != '}' || !char.IsDigit(template[i + 1]))
            {
                return -1;
            }

            return template[i + 1] - '0';
        }

        private void Flush(StringBuilder run)
        {
            if (run.Length == 0)
            {
                return;
            }

            AddText(run.ToString());
            run.Clear();
        }

        private void AddText(string text)
        {
            Text label = UiKit.CreateText(_row, text, _size, TextAnchor.MiddleCenter);
            label.color = _colour;
        }

        // Detached first, so this frame's layout no longer counts the old runs.
        private void Clear()
        {
            for (int i = _row.childCount - 1; i >= 0; i--)
            {
                Transform child = _row.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
