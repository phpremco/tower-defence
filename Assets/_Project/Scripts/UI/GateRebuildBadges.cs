using System.Collections.Generic;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A wrecked gate says how to get it back: a small badge over the wreck with the repair icon and "Rebuild" with
    /// its price, white when the player can pay and red when not, until the gate stands again. Rebuilding is instant
    /// (the gate's menu, R or the repair icon: full health, its level kept), so the badge shows a price, not a
    /// timer. Hidden while that gate's own menu is open, which shows the same price on its button. A wreck under a
    /// HUD panel (the tower bar, say) keeps its badge just off that panel with a small arrow pointing back at it.
    /// </summary>
    public sealed class GateRebuildBadges : MonoBehaviour
    {
        private const float Lift = 2.2f;
        private const float Width = 230f;
        private const float Height = 54f;
        private const float IconSize = 40f;
        private const float Inset = 10f;
        private const int TextSize = 26;
        private const float TailSize = 30f;
        // Moves smaller than this are the badge nudged on screen, not pushed away from its wreck.
        private const float TailThreshold = 24f;
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color Affordable = new Color(0.9f, 0.95f, 1f);
        private static readonly Color TooExpensive = new Color(1f, 0.55f, 0.5f);

        private sealed class Badge
        {
            public RectTransform Rect;
            public Text Label;
            public RectTransform Tail;
        }

        private readonly Dictionary<Tower, Badge> _badges = new Dictionary<Tower, Badge>();
        private readonly List<Tower> _wrecked = new List<Tower>();
        private LevelContext _context;
        private RectTransform _root;
        private Camera _camera;
        private HudKeepOut _keepOut;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _root = root;
            _camera = camera;
            _keepOut = keepOut;
            _context.TowerWrecked.Subscribe(OnWrecked);
        }

        private void OnDestroy()
        {
            _context?.TowerWrecked.Unsubscribe(OnWrecked);
        }

        private void OnWrecked(Tower tower)
        {
            if (tower.Definition.BlocksGroundEnemies && !_wrecked.Contains(tower))
            {
                _wrecked.Add(tower);
            }
        }

        private void LateUpdate()
        {
            for (int i = _wrecked.Count - 1; i >= 0; i--)
            {
                Tower gate = _wrecked[i];
                if (gate == null)
                {
                    _wrecked.RemoveAt(i);
                    continue;
                }

                Badge badge = BadgeFor(gate);
                if (!gate.IsWrecked)
                {
                    badge.Rect.gameObject.SetActive(false);
                    _wrecked.RemoveAt(i);
                    continue;
                }

                Show(gate, badge);
            }
        }

        private void Show(Tower gate, Badge badge)
        {
            bool menuOpen = _context.Mode == PlacementMode.TowerMenu && _context.SelectedPlacedTower == gate;
            Vector3 screen = _camera.WorldToScreenPoint(gate.Position + Vector3.up * Lift);
            bool projected = RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            // A wreck out of view gets no badge; one in view but under a HUD panel gets its badge just off that panel.
            // The room asked for includes the tail's strip under the badge, so the arrow stays clear of the panel too.
            bool inView = projected && screen.z > 0f && _keepOut.Area.Contains(local);
            Vector2 offset = Vector2.zero;
            bool visible = inView && !menuOpen && _context.Outcome == LevelOutcome.Running
                && _keepOut.Place(new Rect(local.x - Width * 0.5f, local.y - TailSize, Width, Height + TailSize), out offset);
            badge.Rect.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            badge.Rect.anchoredPosition = local + offset;
            PointTail(badge, offset);
            int price = gate.RepairCost;
            badge.Label.text = $"Rebuild  {price}";
            badge.Label.color = _context.Wallet.CanAfford(price) ? Affordable : TooExpensive;
        }

        // A badge pushed off a panel loses its place over the wreck, so a small arrow on its edge points back at it.
        private static void PointTail(Badge badge, Vector2 offset)
        {
            bool moved = offset.sqrMagnitude > TailThreshold * TailThreshold;
            badge.Tail.gameObject.SetActive(moved);
            if (!moved)
            {
                return;
            }

            Vector2 back = -offset.normalized;
            Vector2 centre = new Vector2(0f, Height * 0.5f);
            Vector2 half = new Vector2(Width * 0.5f, Height * 0.5f);
            float reach = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(back.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(back.y)));
            badge.Tail.anchoredPosition = centre + back * (reach + TailSize * 0.5f);
            badge.Tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(back.y, back.x) * Mathf.Rad2Deg);
        }

        private Badge BadgeFor(Tower gate)
        {
            if (_badges.TryGetValue(gate, out Badge badge))
            {
                return badge;
            }

            var rect = new GameObject("GateRebuildBadge", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(Width, Height);
            UiKit.CreatePanel(rect, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Backdrop);

            var icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(rect, false);
            icon.sprite = IconFactory.Get(IconType.Repair);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(Inset, 0f);

            Text label = UiKit.CreateFixedText(rect, string.Empty, TextSize, TextAnchor.MiddleCenter, new Vector2(1f, 0.5f), new Vector2(-Inset, 0f), new Vector2(Width - IconSize - Inset * 3f, Height - Inset));
            label.raycastTarget = false;

            var tail = new GameObject("Tail", typeof(RectTransform)).AddComponent<Image>();
            tail.transform.SetParent(rect, false);
            tail.sprite = IconFactory.Get(IconType.Arrow);
            tail.color = Affordable;
            tail.raycastTarget = false;
            RectTransform tailRect = tail.rectTransform;
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.sizeDelta = new Vector2(TailSize, TailSize);
            tail.gameObject.SetActive(false);

            badge = new Badge { Rect = rect, Label = label, Tail = tailRect };
            _badges[gate] = badge;

            return badge;
        }
    }
}
