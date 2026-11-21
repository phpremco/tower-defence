using System.Collections.Generic;
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
    /// Where they sit is <see cref="MarkerLayout"/>'s rule (round 88b): inside the safe area, off every HUD panel the
    /// <see cref="HudKeepOut"/> knows, off Voss's rear-rift marker and off each other; this view only measures and applies.
    /// It needs no pointer and no button: it shows by itself and goes by itself. The radar pings the same place
    /// (<see cref="RadarView"/>).
    /// </summary>
    public sealed class SurprisePingView : MonoBehaviour
    {
        private const int Markers = 3;
        private const float ShowSeconds = 5f;
        private const float ArrowSize = 56f;
        private const float RingSize = 90f;
        private const float RingGrowth = 1.7f;
        private const float RingSeconds = 0.9f;
        private const float LabelWidth = 260f;
        private const float LabelHeight = 40f;
        // From the middle of a ring to the middle of its word, over or under it.
        private const float RingLabelOffset = 54f;
        // Between an edge arrow and its word, and between two markers.
        private const float ArrowLabelGap = 2f;
        private const float MarkerSpacing = 8f;
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
        private readonly MarkerLayout _layout = new MarkerLayout();
        private readonly List<Rect> _obstacles = new List<Rect>();
        private Canvas _canvas;
        private Vector2 _labelSize;
        private float _arrowBox;
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
            _canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            // The arrow turns: its box is the square that holds it at any angle.
            _arrowBox = new Vector2(ArrowSize, ArrowSize).magnitude;
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
                // The word's own size, not its roomy box: the block that must stay clear is what is drawn.
                _labelSize = new Vector2(Mathf.Min(LabelWidth, text.preferredWidth), LabelHeight);
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
            bool collected = false;
            foreach (Marker marker in _markers)
            {
                if (now > marker.Until || !running)
                {
                    marker.Until = float.NegativeInfinity;
                    Show(marker, false, false);
                    continue;
                }

                if (!collected)
                {
                    collected = true;
                    _obstacles.Clear();
                    _keepOut.CollectObstacles(_obstacles);
                }

                Place(marker, now);
            }
        }

        // Markers are placed one after the other and each becomes an obstacle for the next, so they never share a spot.
        private void Place(Marker marker, float now)
        {
            // A still or a console build may draw the HUD through a camera: the point is read the way the canvas is drawn.
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector3 screen = _camera.WorldToScreenPoint(marker.Target + Vector3.up * MarkerLift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, uiCamera, out Vector2 local);
            Rect area = _area.rect;
            if (screen.z <= 0f)
            {
                // Behind the camera the projection is mirrored: the place lies the other way.
                local = area.center * 2f - local;
            }

            // In view means the ring can be seen whole: on screen and under no panel.
            var ring = new Rect(local - new Vector2(RingSize, RingSize) * 0.5f, new Vector2(RingSize, RingSize));
            if (screen.z > 0f && MarkerLayout.IsFree(ring, area, _keepOut.Zones))
            {
                PlaceRing(marker, local, ring, area, now);
            }
            else if (_layout.PlaceEdge(local, _arrowBox, _labelSize, ArrowLabelGap, area, _obstacles, out EdgeMarkerPlacement placed))
            {
                marker.Arrow.anchoredPosition = placed.Arrow - area.center;
                marker.Arrow.localRotation = Quaternion.Euler(0f, 0f, placed.Angle);
                marker.Label.anchoredPosition = placed.Label - area.center;
                _obstacles.Add(Spaced(placed.Block));
                Show(marker, true, false);
            }
            else
            {
                Show(marker, false, false);
            }
        }

        private void PlaceRing(Marker marker, Vector2 local, Rect ring, Rect area, float now)
        {
            float phase = Mathf.Repeat((now - marker.Since) / RingSeconds, 1f);
            marker.Ring.anchoredPosition = local - area.center;
            marker.Ring.localScale = Vector3.one * Mathf.Lerp(1f, RingGrowth, phase);
            Color colour = Warning;
            colour.a = 1f - phase;
            marker.RingImage.color = colour;
            bool worded = _layout.PlaceRingLabel(local, RingLabelOffset, _labelSize, area, _obstacles, out Vector2 label);
            if (worded)
            {
                marker.Label.anchoredPosition = label - area.center;
                _obstacles.Add(Spaced(new Rect(label - _labelSize * 0.5f, _labelSize)));
            }

            _obstacles.Add(Spaced(ring));
            SetActive(marker.Arrow, false);
            SetActive(marker.Ring, true);
            SetActive(marker.Label, worded);
        }

        private static Rect Spaced(Rect rect)
        {
            return Rect.MinMaxRect(rect.xMin - MarkerSpacing, rect.yMin - MarkerSpacing, rect.xMax + MarkerSpacing, rect.yMax + MarkerSpacing);
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
