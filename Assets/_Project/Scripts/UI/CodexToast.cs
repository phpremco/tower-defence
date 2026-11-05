using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// "New Codex entry": a small note under the boss bar when a boss is met for the first time and its Codex page opens
    /// (Docs/BOSS_INTROS.md). Slides down, stays a few real seconds, fades. Registered with the HUD keep-out.
    /// </summary>
    public sealed class CodexToast : MonoBehaviour
    {
        private const float Width = 520f;
        private const float Height = 56f;
        // Under the boss bar and its name line (BossHealthView hangs about this far down from the top of the safe area).
        private const float Top = 196f;
        private const float SlideDistance = 24f;
        private const float FadeSeconds = 0.3f;
        private const int TextSize = 24;
        private const string Prefix = "NEW CODEX ENTRY  ·  ";
        private static readonly Color Backdrop = new Color(0.05f, 0.1f, 0.16f, 0.9f);
        private static readonly Color Ink = new Color(0.55f, 0.9f, 1f);

        private RectTransform _panel;
        private CanvasGroup _group;
        private Text _text;
        private float _left;
        private float _shown;

        /// <summary>The panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;

        /// <summary>True while the note shows.</summary>
        public bool IsShowing => _left > 0f;

        public void Initialize(RectTransform safe)
        {
            Image panel = UiKit.CreatePanel(safe, "CodexToast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Backdrop);
            _panel = panel.rectTransform;
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.sizeDelta = new Vector2(Width, Height);
            _panel.anchoredPosition = new Vector2(0f, -Top);
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _text = UiKit.CreateFixedText(_panel, string.Empty, TextSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width - 24f, Height - 8f));
            _text.color = Ink;
            _panel.gameObject.SetActive(false);
        }

        public void Show(string entry, float seconds)
        {
            _text.text = Prefix + entry.ToUpperInvariant();
            _left = seconds;
            _shown = 0f;
            _panel.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (_left <= 0f)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _left -= dt;
            _shown += dt;
            if (_left <= 0f)
            {
                _panel.gameObject.SetActive(false);
                return;
            }

            float into = Mathf.Clamp01(_shown / FadeSeconds);
            _group.alpha = Mathf.Min(into, Mathf.Clamp01(_left / FadeSeconds));
            _panel.anchoredPosition = new Vector2(0f, -Top + SlideDistance * (1f - into));
        }
    }
}
