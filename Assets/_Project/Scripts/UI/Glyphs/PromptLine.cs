using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A help line with button glyphs inside the sentence: the localised template keeps its "{0}" holes, and each hole
    /// becomes one or more glyph images (or the printed name when a glyph is missing). The template is read by
    /// <see cref="PromptLineLayout"/> into centred rows of hint groups; the gaps between groups, between a glyph and its
    /// words and between rows are fixed sizes, so a translation can move the holes freely and nothing ever overlaps.
    /// </summary>
    public sealed class PromptLine : MonoBehaviour
    {
        private const float GlyphScale = 1.25f;
        private const float WordGapScale = 0.34f;
        private const float GroupGapScale = 1.9f;
        private const float RowGap = 6f;
        private const string GlyphSeparator = "/";

        private RectTransform _block;
        private int _size;
        private Color _colour;
        private Color _nameColour;

        /// <summary>
        /// The line's block, hung centred from the top of <paramref name="area"/> and as large as its rows need; text size
        /// and colour for its words. A hole without a glyph prints its control's name in the key colour.
        /// </summary>
        public static PromptLine Create(RectTransform area, int size, Color colour)
        {
            var block = new GameObject("PromptLine", typeof(RectTransform)).GetComponent<RectTransform>();
            block.SetParent(area, false);
            block.anchorMin = block.anchorMax = block.pivot = new Vector2(0.5f, 1f);
            block.anchoredPosition = Vector2.zero;
            var layout = block.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowGap;
            Configure(layout);
            var fitter = block.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var line = block.gameObject.AddComponent<PromptLine>();
            line._block = block;
            line._size = size;
            line._colour = colour;
            line._nameColour = ColorUtility.TryParseHtmlString(PromptText.KeyColour, out Color key) ? key : colour;
            return line;
        }

        /// <summary>
        /// Lays out the template; hole <c>{i}</c> shows <paramref name="glyphs"/>[i] (several are joined by a slash), or
        /// <paramref name="names"/>[i] when that hole has no glyph.
        /// </summary>
        public void Set(string template, IReadOnlyList<Sprite[]> glyphs, IReadOnlyList<string> names)
        {
            Clear();
            foreach (List<List<PromptPiece>> row in PromptLineLayout.Parse(template))
            {
                RectTransform rowRect = AddRow(_block, "Row", _size * GroupGapScale);
                foreach (List<PromptPiece> group in row)
                {
                    RectTransform groupRect = AddRow(rowRect, "Group", 0f);
                    for (int i = 0; i < group.Count; i++)
                    {
                        if (i > 0 && group[i].GapBefore)
                        {
                            AddGap(groupRect);
                        }

                        AddPiece(groupRect, group[i], glyphs, names);
                    }
                }
            }
        }

        private void AddPiece(RectTransform group, PromptPiece piece, IReadOnlyList<Sprite[]> glyphs, IReadOnlyList<string> names)
        {
            if (!piece.IsHole)
            {
                AddText(group, piece.Text, _colour);
                return;
            }

            Sprite[] sprites = glyphs != null && piece.Hole < glyphs.Count ? glyphs[piece.Hole] : null;
            if (sprites == null || sprites.Length == 0 || System.Array.IndexOf(sprites, null) >= 0)
            {
                AddText(group, names != null && piece.Hole < names.Count ? names[piece.Hole] : string.Empty, _nameColour);
                return;
            }

            for (int s = 0; s < sprites.Length; s++)
            {
                if (s > 0)
                {
                    AddGap(group);
                    AddText(group, GlyphSeparator, _colour);
                    AddGap(group);
                }

                Image image = PromptGlyphs.CreateImage(group, _size * GlyphScale);
                image.gameObject.AddComponent<LayoutElement>();
                PromptGlyphs.Show(image, sprites[s]);
            }
        }

        // A long line (a translation, a wide key name) shrinks evenly to its box instead of running off both sides.
        // The block has the size its rows ask for, so shrinking never squeezes a piece into its neighbour.
        private void LateUpdate()
        {
            float needed = _block.rect.width;
            float room = ((RectTransform)_block.parent).rect.width;
            float scale = needed > room && needed > 0f ? room / needed : 1f;
            if (!Mathf.Approximately(_block.localScale.x, scale))
            {
                _block.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private static RectTransform AddRow(RectTransform parent, string name, float spacing)
        {
            var row = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            Configure(layout);
            return row;
        }

        // Every child keeps the size it asks for: nothing is stretched.
        private static void Configure(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void AddGap(RectTransform group)
        {
            var gap = new GameObject("Gap", typeof(RectTransform)).AddComponent<LayoutElement>();
            gap.transform.SetParent(group, false);
            gap.preferredWidth = _size * WordGapScale;
        }

        private void AddText(RectTransform group, string text, Color colour)
        {
            Text label = UiKit.CreateText(group, text, _size, TextAnchor.MiddleCenter);
            label.color = colour;
        }

        // Detached first, so this frame's layout no longer counts the old rows.
        private void Clear()
        {
            for (int i = _block.childCount - 1; i >= 0; i--)
            {
                Transform child = _block.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
