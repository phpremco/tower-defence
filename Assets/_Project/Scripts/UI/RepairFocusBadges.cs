using System.Collections.Generic;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A structure the player marked for the repair crew (round 88g, the "Repair focus" toggle on its menu) carries a
    /// small badge over its top for as long as the mark is on: a wrench in a ring, in the crew's cyan, so the player
    /// remembers which guns the repair drones go to first. The badge sits on the structure itself; one that would lie
    /// under a HUD panel is hidden rather than moved, because away from its tower it says nothing.
    /// </summary>
    public sealed class RepairFocusBadges : MonoBehaviour
    {
        // Just over the health bar of a tower as the camera sees it (the bar hangs at 1.7 units and is drawn towards the camera).
        private const float Lift = 1.5f;
        private const float Size = 40f;
        private const float RingInset = 2f;
        private const float WrenchInset = 10f;
        // A badge the keep-out had to move further than this is off its tower.
        private const float MovedLimit = 2f;
        private static readonly Color Backdrop = new Color(0.03f, 0.07f, 0.14f, 0.85f);
        private static readonly Color Tint = new Color(0.3f, 0.9f, 1f);

        private readonly Dictionary<Tower, RectTransform> _badges = new Dictionary<Tower, RectTransform>();
        private readonly List<Tower> _marked = new List<Tower>();
        private LevelContext _context;
        private RectTransform _root;
        private Camera _camera;
        private HudKeepOut _keepOut;
        private Canvas _canvas;

        /// <summary>How many badges show right now (for checks).</summary>
        public int Showing { get; private set; }

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _root = root;
            _camera = camera;
            _keepOut = keepOut;
            _canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            _context.RepairFocusChanged.Subscribe(OnChanged);
        }

        private void OnDestroy()
        {
            _context?.RepairFocusChanged.Unsubscribe(OnChanged);
        }

        private void OnChanged(Tower tower)
        {
            if (tower.RepairFocus && !_marked.Contains(tower))
            {
                _marked.Add(tower);
            }
        }

        private void LateUpdate()
        {
            int showing = 0;
            for (int i = _marked.Count - 1; i >= 0; i--)
            {
                Tower tower = _marked[i];
                // Sold, scrapped or combined (the object is gone), or the mark taken off: the badge goes with it.
                if (tower == null || !tower.RepairFocus)
                {
                    Drop(tower, i);
                    continue;
                }

                showing += Show(tower, BadgeFor(tower)) ? 1 : 0;
            }

            Showing = showing;
        }

        private void Drop(Tower tower, int index)
        {
            _marked.RemoveAt(index);
            if (!ReferenceEquals(tower, null) && _badges.TryGetValue(tower, out RectTransform badge))
            {
                _badges.Remove(tower);
                Destroy(badge.gameObject);
            }
        }

        private bool Show(Tower tower, RectTransform badge)
        {
            Vector3 screen = _camera.WorldToScreenPoint(tower.Position + Vector3.up * Lift);
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            bool projected = RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, uiCamera, out Vector2 local);
            Vector2 offset = Vector2.zero;
            bool visible = projected && screen.z > 0f && _context.Outcome == LevelOutcome.Running && _keepOut.Area.Contains(local)
                && _keepOut.Place(new Rect(local.x - Size * 0.5f, local.y, Size, Size), out offset)
                && offset.sqrMagnitude <= MovedLimit * MovedLimit;
            if (badge.gameObject.activeSelf != visible)
            {
                badge.gameObject.SetActive(visible);
            }

            if (visible)
            {
                badge.anchoredPosition = local;
            }

            return visible;
        }

        private RectTransform BadgeFor(Tower tower)
        {
            if (_badges.TryGetValue(tower, out RectTransform badge))
            {
                return badge;
            }

            badge = new GameObject("RepairFocusBadge", typeof(RectTransform)).GetComponent<RectTransform>();
            badge.SetParent(_root, false);
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.5f);
            badge.pivot = new Vector2(0.5f, 0f);
            badge.sizeDelta = new Vector2(Size, Size);
            Layer(badge, "Backdrop", IconType.Disc, Backdrop, 0f);
            Layer(badge, "Ring", IconType.Ring, Tint, RingInset);
            Layer(badge, "Wrench", IconType.Repair, Tint, WrenchInset);
            _badges[tower] = badge;
            return badge;
        }

        private static void Layer(RectTransform badge, string name, IconType icon, Color colour, float inset)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(badge, false);
            image.sprite = IconFactory.Get(icon);
            image.color = colour;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
