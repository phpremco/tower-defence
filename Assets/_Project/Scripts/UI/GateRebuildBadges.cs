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
    /// timer. Hidden while that gate's own menu is open, which shows the same price on its button.
    /// </summary>
    public sealed class GateRebuildBadges : MonoBehaviour
    {
        private const float Lift = 2.2f;
        private const float Width = 230f;
        private const float Height = 54f;
        private const float IconSize = 40f;
        private const float Inset = 10f;
        private const int TextSize = 26;
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color Affordable = new Color(0.9f, 0.95f, 1f);
        private static readonly Color TooExpensive = new Color(1f, 0.55f, 0.5f);

        private sealed class Badge
        {
            public RectTransform Rect;
            public Text Label;
        }

        private readonly Dictionary<Tower, Badge> _badges = new Dictionary<Tower, Badge>();
        private readonly List<Tower> _wrecked = new List<Tower>();
        private LevelContext _context;
        private RectTransform _root;
        private Camera _camera;

        public void Initialize(LevelContext context, RectTransform root, Camera camera)
        {
            _context = context;
            _root = root;
            _camera = camera;
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
            bool onScreen = RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            bool visible = onScreen && !menuOpen && _context.Outcome == LevelOutcome.Running && screen.z > 0f;
            badge.Rect.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            badge.Rect.anchoredPosition = local;
            int price = gate.RepairCost;
            badge.Label.text = $"Rebuild  {price}";
            badge.Label.color = _context.Wallet.CanAfford(price) ? Affordable : TooExpensive;
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
            badge = new Badge { Rect = rect, Label = label };
            _badges[gate] = badge;

            return badge;
        }
    }
}
