using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Teaches the base alarm once per level: the first time the base takes damage an arrow points at it with the
    /// words "BASE UNDER ATTACK!", at the screen edge while the base lies out of view and hanging over it while it is
    /// in view. After a few seconds it is gone, and from then on the alarm sound alone carries the meaning.
    /// </summary>
    public sealed class BaseAlertView : MonoBehaviour
    {
        private const float EdgeMargin = 150f;
        private const float ArrowSize = 60f;
        private const float LabelWidth = 420f;
        private const float LabelHeight = 46f;
        private const float LabelGap = 46f;
        private const int LabelSize = 32;
        private const float MarkerLift = 3.2f;
        private const float ShowSeconds = 4.5f;
        private const float PulseRate = 6f;
        private const float PulseDepth = 0.14f;
        private const float BlinkRate = 3f;
        private const string Label = "BASE UNDER ATTACK!";
        private static readonly Color Warning = new Color(1f, 0.4f, 0.32f);

        private LevelContext _context;
        private Camera _camera;
        private RectTransform _area;
        private HudKeepOut _keepOut;
        private RectTransform _arrow;
        private Text _text;
        private RectTransform _label;
        private float _lastHealth;
        private float _until = float.NegativeInfinity;
        private bool _shown;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _keepOut = keepOut;
            _camera = camera;
            _area = root;
            _lastHealth = context.BaseHealth.Current;

            var arrow = new GameObject("BaseAlertArrow", typeof(RectTransform)).AddComponent<Image>();
            arrow.transform.SetParent(root, false);
            arrow.sprite = IconFactory.Get(IconType.Arrow);
            arrow.color = Warning;
            arrow.raycastTarget = false;
            _arrow = arrow.rectTransform;
            _arrow.anchorMin = new Vector2(0.5f, 0.5f);
            _arrow.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);

            _text = UiKit.CreateFixedText(root, Label, LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LabelWidth, LabelHeight));
            _text.color = Warning;
            _label = _text.rectTransform;
            Show(false);
        }

        private void LateUpdate()
        {
            float current = _context.BaseHealth.Current;
            if (!_shown && current < _lastHealth && _context.Outcome == LevelOutcome.Running)
            {
                // Only the first hit teaches; later hits rely on the sound the player has now learned.
                _shown = true;
                _until = Time.unscaledTime + ShowSeconds;
            }

            _lastHealth = current;
            if (Time.unscaledTime >= _until || _context.Outcome != LevelOutcome.Running || _context.Base == null)
            {
                Show(false);
                return;
            }

            Place(_context.Base.Position);
        }

        private void Place(Vector3 target)
        {
            Vector3 screen = _camera.WorldToScreenPoint(target + Vector3.up * MarkerLift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local);
            Rect inner = _area.rect;
            inner.xMin += EdgeMargin;
            inner.xMax -= EdgeMargin;
            inner.yMin += EdgeMargin;
            inner.yMax -= EdgeMargin;

            Show(true);
            if (screen.z > 0f && inner.Contains(local))
            {
                _arrow.anchoredPosition = local;
                _arrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
                _label.anchoredPosition = local + Vector2.up * LabelGap;
            }
            else
            {
                Vector2 direction = screen.z > 0f ? local - inner.center : inner.center - local;
                if (direction.sqrMagnitude < 1f)
                {
                    direction = Vector2.up;
                }

                float reach = Mathf.Min(inner.width * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.x)), inner.height * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.y)));
                Vector2 edge = inner.center + direction * reach;
                _arrow.anchoredPosition = edge;
                _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                // A wide label needs more room than the arrow when it hangs beside it at a left or right edge.
                Vector2 inward = direction.normalized;
                _label.anchoredPosition = edge - inward * (LabelGap + Mathf.Abs(inward.x) * LabelWidth * 0.5f);
            }

            // The HUD stays readable: the warning moves off any panel it would cover, or waits while it cannot.
            if (!_keepOut.PlaceTogether(_arrow, _label))
            {
                Show(false);
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * PulseRate);
            _arrow.localScale = Vector3.one * (1f + PulseDepth * wave);
            // The words blink in step with the alarm so the eye catches them even at the screen edge.
            _text.color = Color.Lerp(Warning, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * BlinkRate * Mathf.PI));
        }

        private void Show(bool on)
        {
            if (_arrow.gameObject.activeSelf != on)
            {
                _arrow.gameObject.SetActive(on);
                _label.gameObject.SetActive(on);
            }
        }
    }
}
