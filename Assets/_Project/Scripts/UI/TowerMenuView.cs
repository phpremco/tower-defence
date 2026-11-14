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
        // Room the HUD bars take at the top and bottom of the screen, and the gap between the menu and the tower.
        private const float TopReserve = 90f;
        private const float BottomReserve = 165f;
        private const float AnchorGap = 16f;
        private const float FallbackHeight = 1.4f;
        private const float RefreshSeconds = 0.25f;
        private const float BackdropPadding = 14f;
        private const float PanelWidth = 1000f;
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

        /// <summary>The open tower menu, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;
        private readonly System.Collections.Generic.List<Renderer> _renderers = new System.Collections.Generic.List<Renderer>();
        private Tower _measuredTower;
        private int _measuredLevel;
        private bool _measuredWreck;
        private float _drawnHeight;
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
        private int _openedFrame;
        private GameObject _lastIcon;

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
            if (_wasOpen)
            {
                TendFocus();
            }

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
            text.fontSize = Mathf.Max(LabelSize, UiKit.ReadableSize);
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
            _sellPrice.text = tower.CanBeSold ? (tower.IsWrecked ? "Scrap\n+" : "Sell\n+") + tower.SellValue : IsHeroHere(tower) ? $"{HeroNaming.Of(_context)}\nhere" : $"Send\n{HeroNaming.Of(_context)}";

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

            // A wrecked gate comes back at once when paid for; a wrecked tower is scrapped and built again.
            string wreck = tower.Definition.BlocksGroundEnemies ? "wrecked - rebuild restores it at once, full health, same level" : "wrecked";
            string state = tower.IsWrecked ? wreck : $"HP {Mathf.CeilToInt(tower.Health.Current)}/{Mathf.CeilToInt(tower.Health.Max)}";
            // What it does against ground and sky, from its data: a gun that aims up but barely hurts flyers says so here.
            string reach = TargetRead.Label(tower.Definition);
            _info.text = $"{tower.Definition.DisplayName}  {(tower.CanUpgrade ? $"Lv {tower.LevelIndex + 1}" : "MAX")}  {state}{(reach.Length > 0 ? "  -  " + reach : string.Empty)}" + UpgradePreview(tower);

            if (!_wasOpen)
            {
                _wasOpen = true;
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                _openedFrame = Time.frameCount;
                _lastIcon = null;
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

        // The press that opened the menu (A or Enter on the tower) still counts as "submitted this frame" for the UI, so
        // an icon focused in that frame was bought at once: the focus waits for the next frame. After that the focus is
        // kept on the strip: an icon that greys out under it (a tower upgraded to the top, money spent) drops the focus,
        // and it goes back to that same icon rather than jumping on to Sell, so a quick second press never sells. While
        // the pause menu, a dialogue or the codex holds the game, the focus is theirs.
        private void TendFocus()
        {
            EventSystem events = EventSystem.current;
            if (events == null || Time.frameCount <= _openedFrame || _context.InputBlocked)
            {
                return;
            }

            GameObject current = events.currentSelectedGameObject;
            if (IsIcon(current))
            {
                _lastIcon = current;
                return;
            }

            events.SetSelectedGameObject(_lastIcon != null && _lastIcon.activeInHierarchy ? _lastIcon : FirstAvailable().gameObject);
        }

        private bool IsIcon(GameObject candidate)
        {
            return candidate != null && candidate.activeInHierarchy &&
                (candidate == _upgrade.gameObject || candidate == _repair.gameObject || candidate == _sell.gameObject || candidate == _combine.gameObject);
        }

        private Button FirstAvailable() => _upgrade.interactable ? _upgrade : _repair.interactable ? _repair : _sell;

        // The menu hangs just over the tower's drawn top when there is room under the top bar, else just under its
        // foot, kept clear of the HUD bars; both sides follow the model's real size on screen at any zoom.
        private void PositionAbove(Tower tower)
        {
            float height = DrawnHeight(tower);
            if (!ToLocal(tower.Position + Vector3.up * height, out Vector2 top) || !ToLocal(tower.Position, out Vector2 foot))
            {
                return;
            }

            // Kept inside the screen sideways, so a base or tower at the edge still shows its whole menu.
            float halfRoom = _root.rect.width * 0.5f - PanelWidth * 0.5f - BackdropPadding;
            float x = Mathf.Clamp(top.x, -halfRoom, halfRoom);
            float half = _root.rect.height * 0.5f;
            float panel = _panel.sizeDelta.y + BackdropPadding * 2f;
            bool above = top.y + AnchorGap + panel <= half - TopReserve;
            float y = above ? top.y + AnchorGap + BackdropPadding : foot.y - AnchorGap - BackdropPadding;
            y = above ? y : Mathf.Max(y, -half + BottomReserve + panel - BackdropPadding);
            _panel.pivot = new Vector2(0.5f, above ? 0f : 1f);
            _panel.anchoredPosition = new Vector2(x, y);
        }

        private bool ToLocal(Vector3 world, out Vector2 local)
        {
            Vector3 screen = _camera.WorldToScreenPoint(world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out local);
        }

        // The top of the tower's visible model above its foot, measured once per tower and level (a model swaps on an
        // upgrade or a wreck), never every frame.
        private float DrawnHeight(Tower tower)
        {
            if (tower == _measuredTower && tower.LevelIndex == _measuredLevel && tower.IsWrecked == _measuredWreck && _drawnHeight > 0f)
            {
                return _drawnHeight;
            }

            _measuredTower = tower;
            _measuredLevel = tower.LevelIndex;
            _measuredWreck = tower.IsWrecked;
            _drawnHeight = 0f;
            tower.GetComponentsInChildren(false, _renderers);
            foreach (Renderer renderer in _renderers)
            {
                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && renderer.enabled)
                {
                    _drawnHeight = Mathf.Max(_drawnHeight, renderer.bounds.max.y - tower.Position.y);
                }
            }

            return _drawnHeight > 0f ? _drawnHeight : FallbackHeight;
        }
    }
}




