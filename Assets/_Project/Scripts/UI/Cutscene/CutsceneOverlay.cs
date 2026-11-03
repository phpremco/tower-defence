using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The frame around the cutscene picture: cinema bars that slide in and out, fades to and from black, the title
    /// card, the radar and the skip control (a small button for the mouse, and a hint after the first press). The
    /// dialogue box lives between the fade and the skip control, so a line can be read over black.
    /// </summary>
    public sealed class CutsceneOverlay : MonoBehaviour
    {
        private const float BarHeightShare = 0.1f;
        private const float BarSpeed = 1.6f;
        private const int TitleSize = 86;
        private const float TitleSpacing = 28f;
        private const float TitleRise = 30f;
        private const float TitleFadeSeconds = 1.1f;
        private const float TitleLineWidth = 760f;
        private const float TitleLineHeight = 3f;
        private const float TitleGlowWidth = 1100f;
        private const float TitleGlowHeight = 260f;
        private const int SkipSize = 22;
        private const float SkipWidth = 150f;
        private const float SkipHeight = 44f;
        private const float SkipMargin = 26f;
        private const int HintSize = 24;
        private const float HintFadeSpeed = 6f;
        private const float SkipIdleAlpha = 0.55f;

        private static readonly Color TitleColour = new Color(0.86f, 0.95f, 1f);
        private static readonly Color TitleGlow = new Color(0.3f, 0.75f, 1f, 0.35f);
        private static readonly Color SkipFill = new Color(0.02f, 0.04f, 0.08f, 0.7f);
        private static readonly Color SkipEdge = new Color(0.45f, 0.8f, 1f, 0.8f);
        private static readonly Color HintColour = new Color(0.75f, 0.9f, 1f);

        private RectTransform _canvasRoot;
        private RectTransform _top;
        private RectTransform _bottom;
        private Image _fade;
        private CanvasGroup _title;
        private RectTransform _titleRect;
        private TextMeshProUGUI _hint;
        private CanvasGroup _skipGroup;
        private float _bars;
        private float _barsTarget;
        private float _hintAlpha;
        private bool _hintShown;

        /// <summary>The safe-area root the dialogue box is built into.</summary>
        public RectTransform DialogueRoot { get; private set; }

        public RadarOverlay Radar { get; private set; }

        /// <summary>Height of one letterbox bar when fully in, in canvas units.</summary>
        public float BarHeight => _canvasRoot != null ? _canvasRoot.rect.height * BarHeightShare : 0f;

        /// <summary>Current darkness of the fade, 0 clear to 1 black, for tests and probes.</summary>
        public float FadeAlpha => _fade != null ? _fade.color.a : 0f;

        /// <summary>Builds the frame; <paramref name="onSkipClicked"/> runs when the skip button is clicked.</summary>
        public void Build(Action onSkipClicked)
        {
            RectTransform safe = UiKit.CreateRoot(transform, false);
            _canvasRoot = (RectTransform)safe.parent;
            safe.parent.GetComponent<Canvas>().sortingOrder = 10;

            _top = Bar("BarTop", 1f);
            _bottom = Bar("BarBottom", 0f);
            var radarRoot = NewRect("Radar", safe);
            Stretch(radarRoot);
            Radar = radarRoot.gameObject.AddComponent<RadarOverlay>();
            Radar.Build(radarRoot);
            BuildTitle(safe);

            _fade = NewChild<Image>(_canvasRoot, "Fade");
            Stretch(_fade.rectTransform);
            _fade.color = Color.black;
            _fade.raycastTarget = false;

            DialogueRoot = NewRect("Dialogue", _canvasRoot);
            Stretch(DialogueRoot);
            DialogueRoot.anchorMin = safe.anchorMin;
            DialogueRoot.anchorMax = safe.anchorMax;

            RectTransform top = NewRect("Top", _canvasRoot);
            Stretch(top);
            top.anchorMin = safe.anchorMin;
            top.anchorMax = safe.anchorMax;
            BuildSkip(top, onSkipClicked);
        }

        /// <summary>Darkness of the picture, 0 clear to 1 black.</summary>
        public void SetFade(float alpha) => _fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));

        public void SetLetterbox(bool on) => _barsTarget = on ? 1f : 0f;

        /// <summary>Shows the title card at this opacity, 0 hidden; it settles from slightly below as it appears.</summary>
        public void SetTitle(string text, float alpha)
        {
            bool shown = !string.IsNullOrEmpty(text) && alpha > 0f;
            _title.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            TextMeshProUGUI label = _title.GetComponentInChildren<TextMeshProUGUI>();
            if (label.text != text)
            {
                label.text = text;
            }

            float eased = Mathf.SmoothStep(0f, 1f, alpha);
            _title.alpha = eased;
            _titleRect.anchoredPosition = new Vector2(0f, -TitleRise * (1f - eased));
        }

        /// <summary>How long a title takes to appear, for the player to time it against the end of a shot.</summary>
        public static float TitleSeconds => TitleFadeSeconds;

        /// <summary>Shows or hides the "press again to skip" hint.</summary>
        public void SetSkipHint(bool shown) => _hintShown = shown;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _bars = Mathf.MoveTowards(_bars, _barsTarget, dt * BarSpeed);
            float eased = Mathf.SmoothStep(0f, 1f, _bars);
            float height = _canvasRoot.rect.height * BarHeightShare * eased;
            _top.sizeDelta = new Vector2(0f, height);
            _bottom.sizeDelta = new Vector2(0f, height);

            _hintAlpha = Mathf.MoveTowards(_hintAlpha, _hintShown ? 1f : 0f, dt * HintFadeSpeed);
            _hint.alpha = _hintAlpha;
            _skipGroup.alpha = Mathf.Lerp(SkipIdleAlpha, 1f, _hintAlpha);
        }

        private RectTransform Bar(string name, float edge)
        {
            Image image = NewChild<Image>(_canvasRoot, name);
            image.color = Color.black;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, edge);
            rect.anchorMax = new Vector2(1f, edge);
            rect.pivot = new Vector2(0.5f, edge);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        // A wide, spaced title over a soft glow, with a thin lit line under it.
        private void BuildTitle(RectTransform safe)
        {
            _titleRect = NewRect("Title", safe);
            _titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            _titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            _titleRect.sizeDelta = new Vector2(TitleGlowWidth, TitleGlowHeight);
            _title = _titleRect.gameObject.AddComponent<CanvasGroup>();
            _title.blocksRaycasts = false;

            RawImage glow = NewChild<RawImage>(_titleRect, "Glow");
            glow.texture = DialogueArt.Radial;
            glow.color = TitleGlow;
            glow.raycastTarget = false;
            Stretch(glow.rectTransform);

            TextMeshProUGUI label = NewText(_titleRect, "Label", TitleSize, TextAlignmentOptions.Center);
            label.characterSpacing = TitleSpacing;
            label.color = TitleColour;
            Stretch(label.rectTransform);

            Image line = NewChild<Image>(_titleRect, "Line");
            line.color = new Color(TitleGlow.r, TitleGlow.g, TitleGlow.b, 0.9f);
            line.raycastTarget = false;
            line.rectTransform.sizeDelta = new Vector2(TitleLineWidth, TitleLineHeight);
            line.rectTransform.anchoredPosition = new Vector2(0f, -TitleSize * 0.75f);
            _titleRect.gameObject.SetActive(false);
        }

        // The skip button never takes keyboard or gamepad selection, so a confirm press only ever reads dialogue.
        private void BuildSkip(RectTransform safe, Action onSkipClicked)
        {
            RectTransform rect = NewRect("Skip", safe);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(SkipWidth, SkipHeight);
            rect.anchoredPosition = new Vector2(-SkipMargin, -SkipMargin);
            _skipGroup = rect.gameObject.AddComponent<CanvasGroup>();

            Image fill = rect.gameObject.AddComponent<Image>();
            fill.sprite = DialogueArt.Panel;
            fill.type = Image.Type.Sliced;
            fill.color = SkipFill;
            Image edge = NewChild<Image>(rect, "Edge");
            edge.sprite = DialogueArt.Edge;
            edge.type = Image.Type.Sliced;
            edge.color = SkipEdge;
            edge.raycastTarget = false;
            Stretch(edge.rectTransform);

            TextMeshProUGUI label = NewText(rect, "Label", SkipSize, TextAlignmentOptions.Center);
            label.text = "SKIP  »";
            label.characterSpacing = 6f;
            label.color = HintColour;
            Stretch(label.rectTransform);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onSkipClicked?.Invoke());

            _hint = NewText(safe, "SkipHint", HintSize, TextAlignmentOptions.Right);
            _hint.text = "Press again to skip";
            _hint.color = HintColour;
            _hint.alpha = 0f;
            RectTransform hint = _hint.rectTransform;
            hint.anchorMin = new Vector2(1f, 1f);
            hint.anchorMax = new Vector2(1f, 1f);
            hint.pivot = new Vector2(1f, 1f);
            hint.sizeDelta = new Vector2(500f, 40f);
            hint.anchoredPosition = new Vector2(-SkipMargin, -SkipMargin - SkipHeight - 10f);
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T NewChild<T>(RectTransform parent, string name) where T : Component => NewRect(name, parent).gameObject.AddComponent<T>();

        private static TextMeshProUGUI NewText(RectTransform parent, string name, int size, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = NewChild<TextMeshProUGUI>(parent, name);
            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
