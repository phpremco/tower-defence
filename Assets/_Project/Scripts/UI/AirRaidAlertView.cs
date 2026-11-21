using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The warning of a level's first air raid (round 88c): a note under the top strip that says how many waves off the
    /// raid is. Slides down, stays long enough to read twice (real seconds, so it reads the same at any game speed), fades. It takes no input and is
    /// registered with the HUD keep-out so no world label sits under it.
    /// </summary>
    public sealed class AirRaidAlertView : MonoBehaviour
    {
        private const float Width = 760f;
        private const float Height = 64f;
        private const float SlideDistance = 24f;
        private const float FadeSeconds = 0.3f;
        private const float ShowSeconds = 9f;
        private const int TextSize = 26;
        private static readonly Color Backdrop = new Color(0.16f, 0.05f, 0.04f, 0.92f);
        private static readonly Color Ink = new Color(1f, 0.86f, 0.6f);

        private LevelContext _context;
        private RectTransform _panel;
        private CanvasGroup _group;
        private Text _text;
        private float _top;
        private float _left;
        private float _shown;

        /// <summary>The panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;

        /// <summary>True while the note shows.</summary>
        public bool IsShowing => _left > 0f;

        /// <summary>The line on the note (for probes and tests).</summary>
        public string Line => _text != null ? _text.text : string.Empty;

        /// <param name="top">How far under the top of the safe area the note hangs (under the top strip).</param>
        public void Initialize(LevelContext context, RectTransform safe, float top)
        {
            _context = context;
            _top = top;
            Image panel = UiKit.CreatePanel(safe, "AirRaidAlert", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Backdrop);
            _panel = panel.rectTransform;
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.sizeDelta = new Vector2(Width, Height);
            _panel.anchoredPosition = new Vector2(0f, -_top);
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _text = UiKit.CreateFixedText(_panel, string.Empty, TextSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width - 24f, Height - 8f));
            _text.color = Ink;
            _panel.gameObject.SetActive(false);
            _context.AirRaidWarned.Subscribe(OnWarned);
        }

        private void OnDestroy() => _context?.AirRaidWarned.Unsubscribe(OnWarned);

        private void OnWarned(AirRaidWarning warning)
        {
            string when = warning.WavesAhead <= 0 ? Loc.Get("hud.air_raid_now") : warning.WavesAhead == 1 ? Loc.Get("hud.air_raid_next") : Loc.Format("hud.air_raid_in", warning.WavesAhead);
            _text.text = Loc.Format("hud.air_raid_plain", when);
            _left = ShowSeconds;
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
            _panel.anchoredPosition = new Vector2(0f, -_top + SlideDistance * (1f - into));
        }
    }
}
