using System;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// "Keep these display settings?" after a window or resolution change: Keep stores the new mode, Revert or ten
    /// seconds without an answer put the old one back, so a mode the monitor cannot show never strands the player.
    /// Counts in unscaled time (the menu may run at time scale 0).
    /// </summary>
    public sealed class DisplayConfirmPrompt : MonoBehaviour
    {
        public const float RevertSeconds = 10f;

        private const float PanelWidth = 900f;
        private const float PanelHeight = 330f;
        private const float ButtonGap = 24f;
        private const float ButtonRowWidth = 340f;
        private const float TitleTop = -30f;
        private const float CountdownTop = -100f;
        private const float LineHeight = 60f;
        private const float SideMargin = 60f;
        private const float ButtonBottom = 36f;
        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.85f);
        private static readonly Color PanelTint = new Color(0.08f, 0.11f, 0.17f, 0.98f);

        private Action _keep;
        private Action _revert;
        private Text _countdown;
        private float _remaining;
        private int _shownSeconds = -1;
        private bool _answered;

        /// <summary>Opens the prompt over <paramref name="root"/>; exactly one of the two callbacks runs.</summary>
        public static DisplayConfirmPrompt Open(RectTransform root, Action keep, Action revert)
        {
            var go = new GameObject("DisplayConfirm", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var prompt = go.AddComponent<DisplayConfirmPrompt>();
            prompt._keep = keep;
            prompt._revert = revert;
            prompt._remaining = RevertSeconds;
            prompt.Build(rect);
            return prompt;
        }

        /// <summary>The seconds left before the old mode comes back; for probes and tests.</summary>
        public float Remaining => _remaining;

        /// <summary>B or Esc on the prompt: the safe answer.</summary>
        public void Revert() => Answer(false);

        private void Build(RectTransform rect)
        {
            Image dim = UiKit.CreatePanel(rect, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            dim.raycastTarget = true;
            Image panel = UiKit.CreatePanel(rect, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-PanelWidth * 0.5f, -PanelHeight * 0.5f), new Vector2(PanelWidth * 0.5f, PanelHeight * 0.5f), PanelTint);
            RectTransform box = panel.rectTransform;
            UiKit.CreateFixedText(box, Loc.Get("display.keep_title"), UiKit.BodySize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(PanelWidth - SideMargin, LineHeight));
            _countdown = UiKit.CreateFixedText(box, string.Empty, UiKit.ReadableSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, CountdownTop), new Vector2(PanelWidth - SideMargin, LineHeight));
            Button keep = Place(UiKit.CreateButton(box, Loc.Get("display.keep"), () => Answer(true)), -1f);
            Place(UiKit.CreateButton(box, Loc.Get("display.revert"), () => Answer(false)), 1f);
            UiKit.Select(keep);
            Tick(0f);
        }

        private static Button Place(Button button, float side)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(ButtonRowWidth, UiKit.ButtonHeight);
            rect.anchoredPosition = new Vector2(side * (ButtonRowWidth * 0.5f + ButtonGap * 0.5f), ButtonBottom);
            return button;
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void Tick(float delta)
        {
            if (_answered)
            {
                return;
            }

            _remaining -= delta;
            if (_remaining <= 0f)
            {
                Answer(false);
                return;
            }

            int seconds = Mathf.CeilToInt(_remaining);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _countdown.text = Loc.Format("display.keep_countdown", seconds);
            }
        }

        private void Answer(bool keep)
        {
            if (_answered)
            {
                return;
            }

            _answered = true;
            (keep ? _keep : _revert)?.Invoke();
            Destroy(gameObject);
        }
    }
}
