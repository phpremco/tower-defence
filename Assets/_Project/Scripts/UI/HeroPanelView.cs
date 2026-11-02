using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Opens when the player clicks a spot where the hero can stand. If the hero is there, the panel is its card:
    /// name, where it is and its health bar, with a button for the next post and the two abilities (the repair
    /// shows what it costs and is disabled when the player cannot pay). If the hero is elsewhere, the panel is the
    /// post's name over a single "move here" icon. Keys and gamepad buttons are shortcuts for the same actions.
    /// </summary>
    public sealed class HeroPanelView : MonoBehaviour
    {
        private const float FullWidth = 600f;
        private const float FullHeight = 250f;
        private const float PromptWidth = 260f;
        private const float PromptHeight = 180f;
        private const float IconSize = 84f;
        private const float ColumnWidth = 190f;
        private const float Padding = 16f;
        private const float HeaderHeight = 32f;
        private const float StateHeight = 28f;
        private const float BarHeight = 10f;
        private const float BarGap = 6f;
        private const float ButtonBottom = 46f;
        private const float LabelBottom = 10f;
        private const float AboveHero = 70f;
        private const float BelowHero = 40f;
        private const float TopReserve = 90f;
        private const float BottomReserve = 165f;
        private const int HeaderSize = 24;
        private const int StateSize = 22;
        private const int LabelSize = 22;
        private const float LowHealthFraction = 0.35f;
        private const float MidHealthFraction = 0.65f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color HeaderColor = new Color(0.7f, 0.8f, 0.95f);
        private static readonly Color DownColor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color PostTint = new Color(0.5f, 0.6f, 1f);
        private static readonly Color ShockwaveTint = new Color(0.4f, 0.8f, 1f);
        private static readonly Color RepairTint = new Color(0.35f, 0.9f, 0.5f);

        private LevelContext _context;
        private Commander _hero;
        private RectTransform _root;
        private Camera _camera;
        private RectTransform _panel;
        private Text _header;
        private Text _state;
        private RectTransform _healthBar;
        private Image _healthFill;
        private Button _postButton;
        private Button _shockwave;
        private Button _repair;
        private Text _postLabel;
        private Text _shockwaveLabel;
        private Text _repairLabel;
        private Font _font;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, Font font)
        {
            _context = context;
            _hero = context.Hero;
            _root = root;
            _camera = camera;
            _font = font;
            if (_hero == null)
            {
                return;
            }

            var go = new GameObject("HeroPanel", typeof(RectTransform));
            _panel = go.GetComponent<RectTransform>();
            _panel.SetParent(root, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);

            UiKit.CreatePanel(_panel, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Backdrop);
            _header = CreateText("Header", TextAnchor.MiddleLeft, HeaderSize, -Padding, HeaderHeight);
            _header.color = HeaderColor;
            _state = CreateText("State", TextAnchor.MiddleLeft, StateSize, -Padding - HeaderHeight, StateHeight);
            _healthFill = HeroPanelStyle.CreateHealthBar(_panel, new Vector2(Padding, -Padding - HeaderHeight - StateHeight - BarGap), new Vector2(FullWidth - Padding * 2f, BarHeight));
            _healthBar = _healthFill.transform.parent.GetComponent<RectTransform>();

            _postButton = CreateAction(0, IconType.Hero, PostTint, OnPostPressed, out _postLabel);
            _shockwave = CreateAction(1, IconType.Shockwave, ShockwaveTint, () => _hero.TryShockwave(), out _shockwaveLabel);
            _repair = CreateAction(2, IconType.Repair, RepairTint, () => _context.HeroRepairRequested.Raise(), out _repairLabel);

            _hero.Changed += Refresh;
            _context.Changed.Subscribe(Refresh);
            _context.Wallet.Changed += OnMoneyChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hero == null)
            {
                return;
            }

            _hero.Changed -= Refresh;
            _context.Changed.Unsubscribe(Refresh);
            _context.Wallet.Changed -= OnMoneyChanged;
        }

        private void OnMoneyChanged(int _) => Refresh();

        private bool IsFull => _context.HeroMenuPost == _hero.PostIndex;

        // In prompt mode the one icon sends the hero to the clicked post. In full mode it cycles to the next post.
        private void OnPostPressed()
        {
            if (IsFull)
            {
                _hero.MoveTo((_hero.PostIndex + 1) % _hero.PostCount);
            }
            else
            {
                _hero.MoveTo(_context.HeroMenuPost);
                _context.HeroMenuPost = -1;
                _context.Changed.Raise();
            }
        }

        private Button CreateAction(int index, IconType type, Color tint, UnityEngine.Events.UnityAction action, out Text label)
        {
            float x = Padding + index * ColumnWidth;
            Button button = UiKit.CreateIconButton(_panel, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Destroy(button.GetComponent<LayoutElement>());
            HeroPanelStyle.Tint(button, tint);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = new Vector2(x, ButtonBottom);

            label = CreateText("Label", TextAnchor.MiddleLeft, LabelSize, 0f, 32f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = Vector2.zero;
            labelRect.anchoredPosition = new Vector2(x, LabelBottom);
            labelRect.sizeDelta = new Vector2(ColumnWidth - 6f, 32f);
            return button;
        }

        // A line of text hung from the panel's top-left corner, as wide as the panel allows.
        private Text CreateText(string objectName, TextAnchor alignment, int fontSize, float top, float height)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Padding, top);
            rect.sizeDelta = new Vector2(FullWidth - Padding * 2f, height);
            return text;
        }

        private void Update()
        {
            if (_panel == null || !_panel.gameObject.activeSelf)
            {
                return;
            }

            // Follows the hero's spot, above it or hanging below it when it is near the top of the field.
            Vector3 stand = _hero.StandAt(Mathf.Max(0, _context.HeroMenuPost));
            Vector3 screen = _camera.WorldToScreenPoint(stand);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local))
            {
                return;
            }

            Vector2 half = _root.rect.size * 0.5f;
            float width = _panel.sizeDelta.x;
            float height = _panel.sizeDelta.y;
            bool above = local.y + AboveHero + height < half.y - TopReserve;
            _panel.pivot = new Vector2(0.5f, above ? 0f : 1f);
            float x = Mathf.Clamp(local.x, -half.x + width * 0.5f + Padding, half.x - width * 0.5f - Padding);
            float y = above ? local.y + AboveHero : local.y - BelowHero;
            y = Mathf.Max(y, -half.y + BottomReserve + (above ? 0f : height));
            _panel.anchoredPosition = new Vector2(x, y);
        }

        private void Refresh()
        {
            int post = _context.HeroMenuPost;
            bool open = post >= 0 && post < _context.HeroPostNames.Length && _context.Outcome == LevelOutcome.Running;
            _panel.gameObject.SetActive(open);
            if (!open)
            {
                return;
            }

            if (IsFull)
            {
                RefreshFull();
            }
            else
            {
                RefreshPrompt(post);
            }
        }

        private void RefreshPrompt(int post)
        {
            _panel.sizeDelta = new Vector2(PromptWidth, PromptHeight);
            _header.rectTransform.sizeDelta = new Vector2(PromptWidth - Padding * 2f, HeaderHeight);
            _header.text = _context.HeroPostNames[post];
            _state.gameObject.SetActive(false);
            _healthBar.gameObject.SetActive(false);

            _postButton.gameObject.SetActive(true);
            _shockwave.gameObject.SetActive(false);
            _repair.gameObject.SetActive(false);
            _shockwaveLabel.gameObject.SetActive(false);
            _repairLabel.gameObject.SetActive(false);
            _postLabel.text = "Move hero here";
            _postLabel.color = TextColor;
        }

        private void RefreshFull()
        {
            _panel.sizeDelta = new Vector2(FullWidth, FullHeight);
            _header.rectTransform.sizeDelta = new Vector2(FullWidth - Padding * 2f, HeaderHeight);
            _header.text = _hero.Definition.DisplayName;
            _state.gameObject.SetActive(true);
            _healthBar.gameObject.SetActive(true);

            _postButton.gameObject.SetActive(true);
            _shockwave.gameObject.SetActive(true);
            _repair.gameObject.SetActive(true);
            _shockwaveLabel.gameObject.SetActive(true);
            _repairLabel.gameObject.SetActive(true);

            string postName = _context.HeroPostNames[_hero.PostIndex];
            if (_hero.IsDown)
            {
                int seconds = Mathf.CeilToInt(_hero.ReviveRemaining);
                _state.text = $"Down, back in {seconds / 60}:{seconds % 60:00}";
                _state.color = DownColor;
            }
            else
            {
                string where = _hero.IsWalking ? $"On the way to {postName}" : $"At {postName}";
                _state.text = $"{where}    HP {Mathf.CeilToInt(_hero.Health.Current)} / {Mathf.CeilToInt(_hero.Health.Max)}";
                _state.color = TextColor;
            }

            HeroPanelStyle.FillHealth(_healthFill, _hero.IsDown ? 0f : _hero.Health.Current / _hero.Health.Max, LowHealthFraction, MidHealthFraction);

            _postLabel.text = "Next post";
            _postLabel.color = _hero.IsDown ? DimColor : TextColor;

            bool shockwaveReady = !_hero.IsDown && _hero.ShockwaveReady >= 1f;
            _shockwave.interactable = shockwaveReady;
            _shockwaveLabel.text = shockwaveReady ? "Shockwave" : $"Shockwave {HeroPanelStyle.Cooldown(_hero.ShockwaveReady, _hero.Definition.ShockwaveCooldown)}s";
            _shockwaveLabel.color = shockwaveReady ? TextColor : DimColor;

            int cost = _hero.Definition.RepairCost;
            bool affordable = _context.Wallet.CanAfford(cost);
            bool repairReady = _hero.CanEmergencyRepair;
            _repair.interactable = repairReady && affordable;
            _repairLabel.text = repairReady ? $"Repair {cost}" : $"Repair {HeroPanelStyle.Cooldown(_hero.RepairReady, _hero.Definition.RepairCooldown)}s";
            _repairLabel.color = !repairReady ? DimColor : affordable ? TextColor : Unaffordable;
        }
    }
}
