using Game.Core;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The credits screen of the main menu, built from a <see cref="CreditsDefinition"/>: headings and lines from the
    /// string tables, then the licence texts word for word. One scrolling page with a single Back button: the left stick,
    /// the d-pad, the camera keys and the mouse wheel scroll it; Cancel or Back closes it.
    /// </summary>
    public sealed class CreditsView : MonoBehaviour
    {
        private const float PanelWidth = 1400f;
        private const float PanelHeight = 920f;
        private const float Padding = 32f;
        private const float HeaderHeight = 64f;
        private const float BackWidth = 260f;
        private const float SectionGap = 34f;
        private const float LineGap = 10f;
        private const float ScrollPixelsPerSecond = 900f;
        private const float StickDeadZone = 0.2f;
        private const float WheelSensitivity = 40f;
        private const int TitleSize = 48;
        private const int HeadingSize = 32;
        private const int LineSize = 24;
        private const int LicenceSize = 20;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color PanelTint = new Color(0.08f, 0.12f, 0.22f, 0.98f);
        private static readonly Color PageTint = new Color(0.04f, 0.07f, 0.14f, 0.9f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.4f);
        private static readonly Color Heading = new Color(0.55f, 0.85f, 1f);
        private static readonly Color Body = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Muted = new Color(0.68f, 0.76f, 0.88f);

        private CreditsDefinition _definition;
        private IInputProvider _input;
        private RectTransform _safeRoot;
        private System.Action _closed;
        private GameObject _root;
        private ScrollRect _scroll;
        private RectTransform _content;
        private Button _back;
        private GameObject _lastFocus;
        private bool _open;

        public bool IsOpen => _open;

        /// <summary>The scroll position, 1 at the top and 0 at the bottom (the autotest and stills read it).</summary>
        public float ScrollPosition => _scroll != null ? _scroll.verticalNormalizedPosition : 1f;

        public void Initialize(RectTransform safeRoot, CreditsDefinition definition, IInputProvider input, System.Action closed)
        {
            _safeRoot = safeRoot;
            _definition = definition;
            _input = input;
            _closed = closed;
            if (_input != null)
            {
                _input.Cancelled += OnCancelled;
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancelled;
            }
        }

        public void Open()
        {
            if (_open)
            {
                return;
            }

            if (_root == null)
            {
                Build();
            }

            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            _open = true;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
            UiKit.Select(_back);
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            _root.SetActive(false);
            _closed?.Invoke();
        }

        /// <summary>Moves the page to a scroll position (1 = top); for the stills and the autotest.</summary>
        public void ScrollTo(float normalized)
        {
            if (_scroll != null)
            {
                _scroll.verticalNormalizedPosition = Mathf.Clamp01(normalized);
            }
        }

        private void Update()
        {
            if (!_open)
            {
                return;
            }

            UiKit.KeepFocus(ref _lastFocus);
            Scroll(ReadScrollInput());
        }

        private float ReadScrollInput()
        {
            if (_input == null)
            {
                return 0f;
            }

            float value = _input.CursorMove.y + _input.CameraPan.y;
            return Mathf.Abs(value) < StickDeadZone ? 0f : Mathf.Clamp(value, -1f, 1f);
        }

        // The stick moves the page at a fixed speed in screen pixels, so a long licence text scrolls as fast as a short list.
        private void Scroll(float direction)
        {
            if (direction == 0f)
            {
                return;
            }

            float scrollable = _content.rect.height - _scroll.viewport.rect.height;
            if (scrollable <= 0f)
            {
                return;
            }

            float step = direction * ScrollPixelsPerSecond * Time.unscaledDeltaTime / scrollable;
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + step);
        }

        private void OnCancelled()
        {
            if (_open)
            {
                Close();
            }
        }

        private void Build()
        {
            Image dim = UiKit.CreatePanel(_safeRoot, "CreditsDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            dim.raycastTarget = true;
            _root = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "CreditsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Text title = UiKit.CreateFixedText(rect, Loc.Get(_definition.TitleKey), TitleSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(Padding, -Padding), new Vector2(PanelWidth - BackWidth - Padding * 3f, HeaderHeight));
            title.color = Gold;

            _back = UiKit.CreateButton(rect, Loc.Get("common.back"), Close);
            Destroy(_back.GetComponent<LayoutElement>());
            RectTransform backRect = _back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 1f);
            backRect.anchorMax = new Vector2(1f, 1f);
            backRect.pivot = new Vector2(1f, 1f);
            backRect.anchoredPosition = new Vector2(-Padding, -Padding);
            backRect.sizeDelta = new Vector2(BackWidth, HeaderHeight);

            BuildPage(rect, Padding + HeaderHeight + Padding * 0.5f);
            FillPage();
        }

        private void BuildPage(RectTransform panel, float top)
        {
            Image page = UiKit.CreatePanel(panel, "Page", Vector2.zero, Vector2.one, new Vector2(Padding, Padding), new Vector2(-Padding, -top), PageTint);
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(page.transform, false);
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Padding, Padding * 0.5f);
            viewport.offsetMax = new Vector2(-Padding, -Padding * 0.5f);
            viewportGo.AddComponent<RectMask2D>();
            viewportGo.AddComponent<Image>().color = Color.clear;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = LineGap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = page.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = WheelSensitivity;
        }

        private void FillPage()
        {
            CreditsSection[] sections = _definition.Sections;
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] == null)
                {
                    continue;
                }

                AddHeading(Loc.Get(sections[i].TitleKey), i > 0);
                string[] lines = sections[i].LineKeys;
                for (int j = 0; j < lines.Length; j++)
                {
                    AddLine(Loc.Get(lines[j]), LineSize, Body);
                }
            }

            TextAsset[] licences = _definition.LicenceTexts;
            if (licences.Length == 0)
            {
                return;
            }

            AddHeading(Loc.Get(_definition.LicencesTitleKey), true);
            for (int i = 0; i < licences.Length; i++)
            {
                if (licences[i] != null)
                {
                    AddLicence(licences[i].text);
                }
            }
        }

        private void AddHeading(string text, bool gapAbove)
        {
            if (gapAbove)
            {
                AddSpacer(SectionGap);
            }

            AddLine(text, HeadingSize, Heading);
        }

        // A licence is cut into its paragraphs: one legacy Text holds a limited number of characters, and the OFL alone is
        // several thousand.
        private void AddLicence(string text)
        {
            string[] paragraphs = text.Replace("\r\n", "\n").Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < paragraphs.Length; i++)
            {
                string paragraph = paragraphs[i].Trim();
                if (paragraph.Length > 0)
                {
                    AddLine(paragraph, LicenceSize, Muted);
                }
            }

            AddSpacer(SectionGap);
        }

        private void AddLine(string text, int size, Color color)
        {
            Text label = UiKit.CreateText(_content, text, size, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = color;
            label.raycastTarget = false;
        }

        private void AddSpacer(float height)
        {
            var go = new GameObject("Gap", typeof(RectTransform));
            go.transform.SetParent(_content, false);
            go.AddComponent<LayoutElement>().minHeight = height;
        }
    }
}
