using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A short line over the hero's head (a boss intro's reaction, a bark): a dark panel with his line that follows him on
    /// screen, kept off the HUD panels (<see cref="HudKeepOut"/>), shown for a few real seconds and faded out. Lives in the
    /// world-label layer, so every HUD panel draws over it.
    /// </summary>
    public sealed class HeroSpeechBubble : MonoBehaviour
    {
        private const float Width = 440f;
        private const float Height = 74f;
        private const int TextSize = 26;
        private const float Lift = 2.2f;
        private const float FadeSeconds = 0.25f;
        private static readonly Color Backdrop = new Color(0.04f, 0.06f, 0.1f, 0.86f);
        private static readonly Color Speaker = new Color(1f, 0.82f, 0.3f);

        private Camera _camera;
        private RectTransform _area;
        private HudKeepOut _keepOut;
        private RectTransform _panel;
        private CanvasGroup _group;
        private Text _text;
        private Transform _target;
        private float _left;
        private float _shown;

        /// <summary>The line on show, or empty: for tests and probes.</summary>
        public string Line => _left > 0f ? _text.text : string.Empty;

        public void Initialize(RectTransform layer, Camera camera, HudKeepOut keepOut)
        {
            _area = layer;
            _camera = camera;
            _keepOut = keepOut;
            Image panel = UiKit.CreatePanel(layer, "HeroSpeech", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Backdrop);
            _panel = panel.rectTransform;
            _panel.sizeDelta = new Vector2(Width, Height);
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _text = UiKit.CreateFixedText(_panel, string.Empty, TextSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width - 24f, Height - 12f));
            _text.color = Speaker;
            _panel.gameObject.SetActive(false);
            keepOut.AddPriorityLabel(_panel);
        }

        /// <summary>Shows a line over a transform (the hero) for some real seconds.</summary>
        public void Say(Transform target, string line, float seconds)
        {
            if (target == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            _target = target;
            _text.text = line;
            _left = seconds;
            _shown = 0f;
            _panel.gameObject.SetActive(true);
            Place();
        }

        public void Hide()
        {
            _left = 0f;
            _panel.gameObject.SetActive(false);
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
            if (_left <= 0f || _target == null)
            {
                Hide();
                return;
            }

            _group.alpha = Mathf.Min(Mathf.Clamp01(_shown / FadeSeconds), Mathf.Clamp01(_left / FadeSeconds));
            Place();
        }

        private void Place()
        {
            Vector3 screen = _camera.WorldToScreenPoint(_target.position + Vector3.up * Lift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local);
            Rect area = _area.rect;
            float halfWidth = Width * 0.5f;
            local.x = Mathf.Clamp(local.x, area.xMin + halfWidth, area.xMax - halfWidth);
            local.y = Mathf.Clamp(local.y, area.yMin + Height, area.yMax - Height);
            _panel.anchoredPosition = local;
            var rect = new Rect(local - _panel.sizeDelta * 0.5f, _panel.sizeDelta);
            if (_keepOut.PlaceOverHud(rect, out Vector2 offset))
            {
                _panel.anchoredPosition = local + offset;
            }
            else
            {
                _group.alpha = 0f;
            }
        }
    }
}
