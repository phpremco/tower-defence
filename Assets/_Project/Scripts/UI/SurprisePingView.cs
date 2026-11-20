using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The ping on the screen when a surprise comes out (round 88a, Remco missed a group that slipped in while he was not
    /// watching the radar): for a few seconds a ring ripples out from the spawn point with the word over it while it is in
    /// view, and an arrow at the screen edge points at it while it is not. A few can show at once, one per spawn point.
    /// It needs no pointer and no button: it shows by itself and goes by itself. The radar pings the same place
    /// (<see cref="RadarView"/>).
    /// </summary>
    public sealed class SurprisePingView : MonoBehaviour
    {
        private const int Markers = 3;
        private const float ShowSeconds = 5f;
        private const float EdgeMargin = 90f;
        private const float ArrowSize = 56f;
        private const float RingSize = 90f;
        private const float RingGrowth = 1.7f;
        private const float RingSeconds = 0.9f;
        private const float LabelWidth = 260f;
        private const float LabelHeight = 40f;
        private const float LabelGap = 54f;
        private const int LabelSize = 26;
        private const float MarkerLift = 0.5f;
        // The same spawn point pinged again while its marker still shows keeps that marker (points closer than this, in world units).
        private const float SamePointDistance = 1f;
        // While Voss warns about a rift behind the gate, that rift wears her marker (RearNestMarkerView): no second one over it.
        private const float RearWarningSeconds = 30f;
        // Amber: apart from the boss marker's red and the rear rift's orange.
        private static readonly Color Warning = new Color(1f, 0.82f, 0.2f);

        private sealed class Marker
        {
            public RectTransform Arrow;
            public RectTransform Ring;
            public Image RingImage;
            public RectTransform Label;
            public Vector3 Target;
            public float Since;
            public float Until = float.NegativeInfinity;
        }

        private readonly Marker[] _markers = new Marker[Markers];
        private LevelContext _context;
        private Camera _camera;
        private RectTransform _area;
        private HudKeepOut _keepOut;
        private Vector3 _rearPoint;
        private float _rearUntil = float.NegativeInfinity;

        /// <summary>How many pings show right now (for probes and tests).</summary>
        public int Showing
        {
            get
            {
                int showing = 0;
                foreach (Marker marker in _markers)
                {
                    showing += Time.unscaledTime <= marker.Until ? 1 : 0;
                }

                return showing;
            }
        }

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _camera = camera;
            _area = root;
            _keepOut = keepOut;
            for (int i = 0; i < Markers; i++)
            {
                var marker = new Marker();
                marker.RingImage = CreateIcon(root, "SurpriseRing", IconType.Ring, RingSize);
                marker.Ring = marker.RingImage.rectTransform;
                marker.Arrow = CreateIcon(root, "SurpriseArrow", IconType.Arrow, ArrowSize).rectTransform;
                Text text = UiKit.CreateFixedText(root, Loc.Get("hud.surprise_marker"), LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LabelWidth, LabelHeight));
                text.color = Warning;
                text.raycastTarget = false;
                marker.Label = text.rectTransform;
                _markers[i] = marker;
                Show(marker, false, false);
            }

            context.SurpriseSpotted.Subscribe(OnSpotted);
            context.RearNestWarned.Subscribe(OnRearNestWarned);
        }

        private void OnDestroy()
        {
            _context?.SurpriseSpotted.Unsubscribe(OnSpotted);
            _context?.RearNestWarned.Unsubscribe(OnRearNestWarned);
        }

        private void OnRearNestWarned(Vector3 position)
        {
            _rearPoint = position;
            _rearUntil = Time.unscaledTime + RearWarningSeconds;
            foreach (Marker marker in _markers)
            {
                if (IsSamePoint(marker.Target, position))
                {
                    marker.Until = float.NegativeInfinity;
                }
            }
        }

        private static bool IsSamePoint(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < SamePointDistance * SamePointDistance;

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

        // The marker of the same point is renewed; otherwise a free one is taken, or the one that has shown longest.
        private void OnSpotted(Vector3 position)
        {
            float now = Time.unscaledTime;
            if (now <= _rearUntil && IsSamePoint(_rearPoint, position))
            {
                return;
            }

            Marker chosen = _markers[0];
            foreach (Marker marker in _markers)
            {
                if (now <= marker.Until && IsSamePoint(marker.Target, position))
                {
                    chosen = marker;
                    break;
                }

                // A free marker ended long ago; among busy ones the one that ends soonest has shown longest.
                if (marker.Until < chosen.Until)
                {
                    chosen = marker;
                }
            }

            chosen.Target = position;
            chosen.Since = now;
            chosen.Until = now + ShowSeconds;
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            bool running = _context.Outcome == LevelOutcome.Running;
            foreach (Marker marker in _markers)
            {
                if (now > marker.Until || !running)
                {
                    marker.Until = float.NegativeInfinity;
                    Show(marker, false, false);
                    continue;
                }

                Place(marker, now);
            }
        }

        private void Place(Marker marker, float now)
        {
            Vector3 screen = _camera.WorldToScreenPoint(marker.Target + Vector3.up * MarkerLift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local);
            Rect inner = _area.rect;
            inner.xMin += EdgeMargin;
            inner.xMax -= EdgeMargin;
            inner.yMin += EdgeMargin;
            inner.yMax -= EdgeMargin;
            bool inView = screen.z > 0f && inner.Contains(local);
            if (inView)
            {
                float phase = Mathf.Repeat((now - marker.Since) / RingSeconds, 1f);
                marker.Ring.anchoredPosition = local;
                marker.Ring.localScale = Vector3.one * Mathf.Lerp(1f, RingGrowth, phase);
                Color colour = Warning;
                colour.a = 1f - phase;
                marker.RingImage.color = colour;
                marker.Label.anchoredPosition = local + Vector2.up * LabelGap;
                Show(marker, false, true);
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
                marker.Arrow.anchoredPosition = edge;
                marker.Arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                marker.Label.anchoredPosition = edge - direction.normalized * LabelGap;
                Show(marker, true, false);
            }

            // The HUD stays readable: a marker that would cover a panel moves off it, or waits while it cannot.
            if (!_keepOut.PlaceTogether(inView ? marker.Ring : marker.Arrow, marker.Label))
            {
                Show(marker, false, false);
            }
        }

        private static void Show(Marker marker, bool arrow, bool ring)
        {
            SetActive(marker.Arrow, arrow);
            SetActive(marker.Ring, ring);
            SetActive(marker.Label, arrow || ring);
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
