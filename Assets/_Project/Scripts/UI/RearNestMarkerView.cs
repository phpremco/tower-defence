using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The ping at a rift behind the defence gate while Voss warns about it (Remco 2026-11-10): a ring that keeps rippling
    /// out from the rift with the word over it while it is in view, an arrow at the screen edge towards it while it is not.
    /// It stays while the conversation shows and a few seconds after, then the rift itself (its glow) carries on.
    /// </summary>
    public sealed class RearNestMarkerView : MonoBehaviour
    {
        private const float EdgeMargin = 90f;
        private const float ArrowSize = 60f;
        private const float RingSize = 90f;
        private const float RingGrowth = 1.6f;
        private const float RingSeconds = 1.1f;
        private const float LabelWidth = 300f;
        private const float LabelHeight = 40f;
        private const float LabelGap = 56f;
        private const int LabelSize = 28;
        private const float MarkerLift = 0.5f;
        private const float LingerSeconds = 6f;
        private const string Label = "BEHIND THE GATE";
        private static readonly Color Warning = new Color(1f, 0.55f, 0.25f);

        private LevelContext _context;
        private Camera _camera;
        private RectTransform _area;
        private HudKeepOut _keepOut;
        private RectTransform _arrow;
        private RectTransform _ring;
        private Image _ringImage;
        private RectTransform _label;
        private Vector3 _target;
        private bool _active;
        private float _until;

        /// <summary>True while the ping shows (for probes and tests).</summary>
        public bool Showing => _active;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _camera = camera;
            _area = root;
            _keepOut = keepOut;

            _ringImage = CreateIcon(root, "RearNestRing", IconType.Ring, RingSize);
            _ring = _ringImage.rectTransform;
            _arrow = CreateIcon(root, "RearNestArrow", IconType.Arrow, ArrowSize).rectTransform;
            Text text = UiKit.CreateFixedText(root, Label, LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LabelWidth, LabelHeight));
            text.color = Warning;
            text.raycastTarget = false;
            _label = text.rectTransform;
            Show(false, false);
            context.RearNestWarned.Subscribe(OnWarned);
        }

        private void OnDestroy()
        {
            _context?.RearNestWarned.Unsubscribe(OnWarned);
        }

        private static Image CreateIcon(RectTransform root, string name, IconType type, float size)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(root, false);
            image.sprite = IconFactory.Get(type);
            image.color = Warning;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        private void OnWarned(Vector3 position)
        {
            _target = position;
            _active = true;
            _until = Time.unscaledTime + LingerSeconds;
        }

        private void LateUpdate()
        {
            if (!_active)
            {
                return;
            }

            // The clock is held while Voss speaks, so the linger counts from when the conversation is over, in real time.
            if (_context.StoryBusy)
            {
                _until = Time.unscaledTime + LingerSeconds;
            }

            if (Time.unscaledTime > _until || _context.Outcome != LevelOutcome.Running)
            {
                _active = false;
                Show(false, false);
                return;
            }

            Place();
        }

        private void Place()
        {
            Vector3 screen = _camera.WorldToScreenPoint(_target + Vector3.up * MarkerLift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local);
            Rect inner = _area.rect;
            inner.xMin += EdgeMargin;
            inner.xMax -= EdgeMargin;
            inner.yMin += EdgeMargin;
            inner.yMax -= EdgeMargin;
            bool inView = screen.z > 0f && inner.Contains(local);
            if (inView)
            {
                // In view: the ring ripples out from the rift, the word above it.
                float phase = Mathf.Repeat(Time.unscaledTime / RingSeconds, 1f);
                _ring.anchoredPosition = local;
                _ring.localScale = Vector3.one * Mathf.Lerp(1f, RingGrowth, phase);
                Color colour = Warning;
                colour.a = 1f - phase;
                _ringImage.color = colour;
                _label.anchoredPosition = local + Vector2.up * LabelGap;
                Show(false, true);
            }
            else
            {
                // Out of view: an arrow at the edge of the screen on the line towards the rift, the word inside it.
                Vector2 direction = screen.z > 0f ? local - inner.center : inner.center - local;
                if (direction.sqrMagnitude < 1f)
                {
                    direction = Vector2.up;
                }

                float reach = Mathf.Min(inner.width * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.x)), inner.height * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.y)));
                Vector2 edge = inner.center + direction * reach;
                _arrow.anchoredPosition = edge;
                _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                _label.anchoredPosition = edge - direction.normalized * LabelGap;
                Show(true, false);
            }

            // The HUD stays readable: a marker that would cover a panel moves off it, or waits while it cannot.
            if (!_keepOut.PlaceTogether(inView ? _ring : _arrow, _label))
            {
                Show(false, false);
            }
        }

        private void Show(bool arrow, bool ring)
        {
            SetActive(_arrow, arrow);
            SetActive(_ring, ring);
            SetActive(_label, arrow || ring);
        }

        private static void SetActive(RectTransform rect, bool on)
        {
            if (rect.gameObject.activeSelf != on)
            {
                rect.gameObject.SetActive(on);
            }
        }
    }
}
