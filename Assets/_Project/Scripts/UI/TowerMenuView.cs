using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A small strip of icons (upgrade, repair, sell, each with its price) next to the selected tower.
    /// It sits beside the tower instead of covering the screen, and only reads the level context.
    /// </summary>
    public sealed class TowerMenuView : MonoBehaviour
    {
        private const float IconSize = 100f;
        private const float Spacing = 70f;
        private const float LabelHeight = 64f;
        private const float LabelGap = 8f;
        private const int LabelSize = 24;
        private const float WorldHeight = 2.7f;
        private const float RefreshSeconds = 0.25f;
        private const float BackdropPadding = 14f;
        private const float PanelWidth = 1000f;
        private const float TopFraction = 0.1f;
        private const float BelowOffset = 240f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Unavailable = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color TooExpensive = new Color(1f, 0.5f, 0.45f);
        private static readonly Color UpgradeTint = new Color(0.3f, 0.85f, 0.45f);
        private static readonly Color RepairTint = new Color(0.35f, 0.65f, 1f);
        private static readonly Color SellTint = new Color(1f, 0.78f, 0.25f);
        private static readonly Color CombineTint = new Color(0.8f, 0.5f, 1f);

        private LevelContext _context;
        private RectTransform _root;
        private Camera _camera;
        private Font _font;
        private RectTransform _panel;
        private Text _info;
        private Button _upgrade;
        private Button _repair;
        private Button _sell;
        private Text _upgradePrice;
        private Text _repairPrice;
        private Text _sellPrice;
        private Image _thirdIcon;
        private Button _combine;
        private Text _combinePrice;
        private bool _wasOpen;
        private float _nextRefresh;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, Font font)
        {
            _context = context;
            _root = root;
            _camera = camera;
            _font = font;
            Build();
            _context.Changed.Subscribe(Refresh);
            _context.Wallet.Changed += OnMoneyChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Changed.Unsubscribe(Refresh);
            _context.Wallet.Changed -= OnMoneyChanged;
        }

        private void Update()
        {
            if (_wasOpen && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void Build()
        {
            var go = new GameObject("TowerMenu", typeof(RectTransform));
            _panel = go.GetComponent<RectTransform>();
            _panel.SetParent(_root, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(PanelWidth, IconSize + LabelHeight * 3f + LabelGap * 2f);

            UiKit.CreatePanel(
                _panel, "Backdrop", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-BackdropPadding, -BackdropPadding), new Vector2(BackdropPadding, BackdropPadding), new Color(0.05f, 0.09f, 0.18f, 0.9f));
            _info = CreateLabel(new Vector2(0.5f, 1f), new Vector2(0f, -LabelHeight), TextAnchor.MiddleCenter, new Vector2(900f, LabelHeight * 2f));

            _upgrade = CreateIcon(IconType.Upgrade, 0, UpgradeTint, () => _context.UpgradeRequested.Raise(), out _upgradePrice);
            _repair = CreateIcon(IconType.Repair, 1, RepairTint, () => _context.RepairRequested.Raise(), out _repairPrice);
            _sell = CreateIcon(IconType.Sell, 2, SellTint, OnThirdAction, out _sellPrice);
            _thirdIcon = _sell.transform.Find("Icon").GetComponent<Image>();
            _combine = CreateIcon(IconType.Combine, 3, CombineTint, () => _context.CombineRequested.Raise(), out _combinePrice);
            _panel.gameObject.SetActive(false);
        }

        private void OnThirdAction()
        {
            Tower selected = _context.SelectedPlacedTower;
            if (selected != null && selected.CanBeSold)
            {
                _context.SellRequested.Raise();
            }
            else
            {
                _context.HeroSendRequested.Raise();
            }
        }

        private bool IsHeroHere(Tower tower)
        {
            return _context.Hero != null && _context.Hero.IsAt(tower.Position);
        }

        private Button CreateIcon(IconType type, int index, Color tint, UnityEngine.Events.UnityAction action, out Text price)
        {
            float x = (index - 1) * (IconSize + Spacing);
            Button button = UiKit.CreateIconButton(_panel, IconFactory.Get(type), IconSize, action);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, LabelHeight + LabelGap);
            Destroy(button.GetComponent<LayoutElement>());

            UiKit.UseTint(button);
            ColorBlock colors = button.colors;
            colors.normalColor = tint * 0.55f;
            colors.highlightedColor = tint;
            colors.selectedColor = tint;
            colors.pressedColor = tint;
            colors.disabledColor = new Color(0.12f, 0.12f, 0.14f);
            button.colors = colors;

            price = CreateLabel(new Vector2(0.5f, 0f), new Vector2(x, LabelHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(IconSize + Spacing - 10f, LabelHeight));
            return button;
        }

        private Text CreateLabel(Vector2 anchor, Vector2 position, TextAnchor alignment, Vector2 size)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = LabelSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private void Refresh()
        {
            Tower tower = _context.SelectedPlacedTower;
            bool open = _context.Mode == PlacementMode.TowerMenu && tower != null && _context.Outcome == LevelOutcome.Running;
            _panel.gameObject.SetActive(open);

            if (!open)
            {
                _wasOpen = false;
                return;
            }

            PositionAbove(tower);
            bool canUpgrade = tower.CanUpgrade && _context.Wallet.CanAfford(tower.NextUpgradeCost);
            bool canRepair = tower.NeedsRepair && _context.Wallet.CanAfford(tower.RepairCost);

            _upgrade.interactable = canUpgrade;
            _repair.interactable = canRepair;
            // The third icon sells a tower. A blockade cannot be sold, so it sends the hero there instead.
            bool hasHero = _context.Hero != null;
            _thirdIcon.sprite = IconFactory.Get(tower.CanBeSold ? IconType.Sell : IconType.Hero);
            _sell.interactable = tower.CanBeSold || (hasHero && !IsHeroHere(tower));
            _upgradePrice.text = tower.CanUpgrade ? $"Upgrade\n{tower.NextUpgradeCost}" : "MAX\nlevel";
            string repairVerb = tower.IsWrecked ? "Rebuild" : "Repair";
            _repairPrice.text = tower.NeedsRepair ? $"{repairVerb}\n{tower.RepairCost}" : "Repair\n-";
            _sellPrice.text = tower.CanBeSold ? (tower.IsWrecked ? "Scrap\n+" : "Sell\n+") + tower.SellValue : IsHeroHere(tower) ? "Hero\nhere" : "Send\nhero";

            // The combine icon only appears when a neighbouring tower forms a recipe with this one.
            CombineRecipe offer = _context.CombineOffer;
            _combine.gameObject.SetActive(offer != null);
            _combinePrice.gameObject.SetActive(offer != null);
            if (offer != null)
            {
                bool canCombine = _context.Wallet.CanAfford(offer.Cost);
                _combine.interactable = canCombine;
                // The result's name on the button, so the player knows what the two towers turn into.
                _combinePrice.text = $"{offer.Result.DisplayName}\n{offer.Cost}";
                _combinePrice.color = canCombine ? TextColor : TooExpensive;
            }
            // Grey when there is nothing to buy, red when there is but the player cannot pay yet.
            _upgradePrice.color = canUpgrade ? TextColor : tower.CanUpgrade ? TooExpensive : Unavailable;
            _repairPrice.color = canRepair ? TextColor : tower.NeedsRepair ? TooExpensive : Unavailable;

            string state = tower.IsWrecked ? "wrecked" : $"HP {Mathf.CeilToInt(tower.Health.Current)}/{Mathf.CeilToInt(tower.Health.Max)}";
            _info.text = $"{tower.Definition.DisplayName}  {(tower.CanUpgrade ? $"Lv {tower.LevelIndex + 1}" : "MAX")}  {state}" + UpgradePreview(tower);

            if (!_wasOpen)
            {
                _wasOpen = true;
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                FocusFirstAvailable();
            }
        }

        // Shows what the next upgrade changes, so the player can judge it before paying.
        private static string UpgradePreview(Tower tower)
        {
            if (!tower.CanUpgrade)
            {
                return string.Empty;
            }

            TowerLevel now = tower.Definition.GetLevel(tower.LevelIndex);
            TowerLevel next = tower.Definition.GetLevel(tower.LevelIndex + 1);
            string health = next.HealthBonus > 0f ? $"   +{next.HealthBonus:0} HP" : string.Empty;
            // A chilling tower is bought for its slow, so that is what its preview talks about.
            float slowNow = now.SlowSeconds > 0f ? now.SlowSeconds : tower.Definition.SlowSeconds;
            float slowNext = next.SlowSeconds > 0f ? next.SlowSeconds : tower.Definition.SlowSeconds;
            string effect = slowNext > 0f && Mathf.Approximately(now.Damage, next.Damage)
                ? $"slow {slowNow:0.0}s > {slowNext:0.0}s"
                : $"damage {now.Damage:0} > {next.Damage:0}";
            return $"\nNext: {effect}   range {now.Range:0.0} > {next.Range:0.0}   shots/s {1f / now.FireInterval:0.0} > {1f / next.FireInterval:0.0}{health}";
        }

        private void FocusFirstAvailable()
        {
            Button first = _upgrade.interactable ? _upgrade : _repair.interactable ? _repair : _sell;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        // Above the tower, or hanging below it when the tower is near the top of the screen.
        private void PositionAbove(Tower tower)
        {
            Vector3 screen = _camera.WorldToScreenPoint(tower.Position + Vector3.up * WorldHeight);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local))
            {
                return;
            }

            // Kept inside the screen sideways, so a base or tower at the edge still shows its whole menu.
            float halfRoom = _root.rect.width * 0.5f - PanelWidth * 0.5f - BackdropPadding;
            local.x = Mathf.Clamp(local.x, -halfRoom, halfRoom);
            bool nearTop = local.y > _root.rect.height * TopFraction;
            _panel.pivot = new Vector2(0.5f, nearTop ? 1f : 0f);
            _panel.anchoredPosition = nearTop ? local - new Vector2(0f, BelowOffset) : local;
        }
    }
}




