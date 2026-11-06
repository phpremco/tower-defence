using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The HUD's no-go areas for world labels (rebuild badges, boss and base markers): every HUD panel that is showing,
    /// as a rect in the safe area's space, and the priority labels (the hero's speech bubble) the others must not cover.
    /// The HUD registers its panels once; the labels ask where they may sit, so the HUD stays on top and readable and a
    /// label is never drawn over a card, a price or a menu. Lives in the UI layer;
    /// gameplay knows nothing of it. The rects are measured at most once per frame, however many labels ask.
    /// </summary>
    public sealed class HudKeepOut
    {
        // A label keeps a small gap from a panel edge, so the two never seem to touch.
        private const float Padding = 6f;
        private const float MinTextWidth = 1f;

        private readonly RectTransform _area;
        private readonly List<RectTransform> _panels = new List<RectTransform>();
        private readonly List<Text> _texts = new List<Text>();
        private readonly List<Rect> _zones = new List<Rect>();
        private readonly List<RectTransform> _priority = new List<RectTransform>();
        private readonly List<Rect> _hudZones = new List<Rect>();
        private readonly Vector3[] _corners = new Vector3[4];
        private int _measuredFrame = -1;

        public HudKeepOut(RectTransform area)
        {
            _area = area;
        }

        /// <summary>The space the labels and the zones share: the safe area, centred on its middle.</summary>
        public Rect Area => _area.rect;

        /// <summary>A panel that labels must stay off while it is active.</summary>
        public void Add(RectTransform panel)
        {
            if (panel != null && !_panels.Contains(panel))
            {
                _panels.Add(panel);
            }
        }

        /// <summary>
        /// A world label that wins over the others (the hero's speech bubble, Docs/BOSS_INTROS.md): while it shows, the other
        /// labels keep off it as they keep off the HUD, moving aside or hiding. It keeps off the HUD itself through
        /// <see cref="PlaceOverHud"/>.
        /// </summary>
        public void AddPriorityLabel(RectTransform label)
        {
            if (label != null && !_priority.Contains(label))
            {
                _priority.Add(label);
            }
        }

        /// <summary>A line of HUD text: only the part the words fill counts, so a wide text box keeps no empty field free.</summary>
        public void Add(Text text)
        {
            if (text != null && !_texts.Contains(text))
            {
                _texts.Add(text);
            }
        }

        /// <summary>The showing panels this frame, in the area's space.</summary>
        public IReadOnlyList<Rect> Zones
        {
            get
            {
                Measure();
                return _zones;
            }
        }

        /// <summary>True when a label with this rect (area space) can show; <paramref name="offset"/> moves it off the HUD.</summary>
        public bool Place(Rect label, out Vector2 offset)
        {
            return KeepOutMath.Resolve(label, Zones, _area.rect, out offset);
        }

        /// <summary>As <see cref="Place"/> for a priority label: it keeps off the HUD only, never off itself or the other labels.</summary>
        public bool PlaceOverHud(Rect label, out Vector2 offset)
        {
            Measure();
            return KeepOutMath.Resolve(label, _hudZones, _area.rect, out offset);
        }

        /// <summary>
        /// Moves a marker made of two parts (an arrow and its word, both children of a layer that covers the area) off
        /// the HUD as one piece, so the word stays with its arrow. Inactive parts do not count. False when it cannot show.
        /// </summary>
        public bool PlaceTogether(RectTransform first, RectTransform second)
        {
            bool any = false;
            Rect union = default;
            Include(first, ref union, ref any);
            Include(second, ref union, ref any);
            if (!any)
            {
                return true;
            }

            if (!Place(union, out Vector2 offset))
            {
                return false;
            }

            first.anchoredPosition += offset;
            second.anchoredPosition += offset;
            return true;
        }

        private static void Include(RectTransform part, ref Rect union, ref bool any)
        {
            if (!part.gameObject.activeSelf)
            {
                return;
            }

            Vector2 size = part.sizeDelta;
            var rect = new Rect(part.anchoredPosition - Vector2.Scale(part.pivot, size), size);
            union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, rect.xMin), Mathf.Min(union.yMin, rect.yMin), Mathf.Max(union.xMax, rect.xMax), Mathf.Max(union.yMax, rect.yMax)) : rect;
            any = true;
        }

        private void Measure()
        {
            if (_measuredFrame == Time.frameCount)
            {
                return;
            }

            _measuredFrame = Time.frameCount;
            _zones.Clear();
            foreach (RectTransform panel in _panels)
            {
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    _zones.Add(Expand(ToArea(panel)));
                }
            }

            foreach (Text text in _texts)
            {
                if (text != null && text.gameObject.activeInHierarchy && text.enabled && !string.IsNullOrEmpty(text.text))
                {
                    Rect words = Words(text, ToArea(text.rectTransform));
                    if (words.width > MinTextWidth)
                    {
                        _zones.Add(Expand(words));
                    }
                }
            }

            _hudZones.Clear();
            _hudZones.AddRange(_zones);
            foreach (RectTransform label in _priority)
            {
                if (label != null && label.gameObject.activeInHierarchy)
                {
                    _zones.Add(Expand(ToArea(label)));
                }
            }
        }

        private Rect ToArea(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            Vector3 min = _area.InverseTransformPoint(_corners[0]);
            Vector3 max = _area.InverseTransformPoint(_corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        // The text's own width at its alignment inside its box; the box's local scale carries over.
        private Rect Words(Text text, Rect box)
        {
            float scale = text.rectTransform.rect.width > 0f ? box.width / text.rectTransform.rect.width : 1f;
            float width = Mathf.Min(box.width, text.preferredWidth * scale);
            float x;
            switch (text.alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    x = box.xMin;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    x = box.xMax - width;
                    break;
                default:
                    x = box.center.x - width * 0.5f;
                    break;
            }

            return new Rect(x, box.y, width, box.height);
        }

        private static Rect Expand(Rect rect)
        {
            return Rect.MinMaxRect(rect.xMin - Padding, rect.yMin - Padding, rect.xMax + Padding, rect.yMax + Padding);
        }
    }
}
