using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Opens when the player clicks a spot where the hero can stand. If the hero is there, the panel shows how it
    /// is doing, a button for the next post and the two abilities (the repair shows what it costs and is disabled
    /// when the player cannot pay). If the hero is elsewhere, the panel is a single "move here" icon.
    /// Keys and gamepad buttons are shortcuts for the same actions.
    /// </summary>
    public sealed class HeroPanelView : MonoBehaviour
    {
        private const float FullWidth = 580f;
        private const float FullHeight = 200f;
        private const float PromptWidth = 250f;
        private const float PromptHeight = 170f;
        private const float IconSize = 84f;
        private const float ColumnWidth = 185f;
        private const float Padding = 16f;
        private const float AboveHero = 70f;
        private const float BelowHero = 40f;
        private const float TopReserve = 130f;
        private const float BottomReserve = 200f;
        private const int InfoSize = 26;
        private const int LabelSize = 22;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color DownColor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);

        private LevelContext _context;
        private Commander _hero;
        private RectTransform _root;
        private Camera _camera;
        private RectTransform _panel;
        private Text _info;
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

            UiKit.CreatePanel(_panel, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.09f, 0.18f, 0.9f));
            _info = CreateText("Info", TextAnchor.UpperLeft, InfoSize);

            _postButton = CreateAction(0, IconType.Hero, OnPostPressed, out _postLabel);
            _shockwave = CreateAction(1, IconType.Shockwave, () => _hero.TryShockwave(), out _shockwaveLabel);
            _repair = CreateAction(2, IconType.Repair, () => _context.HeroRepairRequested.Raise(), out _repairLabel);

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

        private Button CreateAction(int index, IconType type, UnityEngine.Events.UnityAction action, out Text label)
        {
            float x = Padding + index * ColumnWidth;
            Button button = UiKit.CreateIconButton(_panel, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = new Vector2(x, 46f);

            label = CreateText("Label", TextAnchor.MiddleLeft, LabelSize);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = Vector2.zero;
            labelRect.anchoredPosition = new Vector2(x, 10f);
            labelRect.sizeDelta = new Vector2(ColumnWidth - 6f, 32f);
            return button;
        }

        private Text CreateText(string objectName, TextAnchor alignment, int fontSize)
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
            _info.rectTransform.anchorMin = new Vector2(0f, 1f);
            _info.rectTransform.anchorMax = new Vector2(0f, 1f);
            _info.rectTransform.pivot = new Vector2(0f, 1f);
            _info.rectTransform.anchoredPosition = new Vector2(Padding, -Padding);
            _info.rectTransform.sizeDelta = new Vector2(PromptWidth - Padding * 2f, 40f);
            _info.text = _context.HeroPostNames[post];
            _info.color = TextColor;

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
            _info.rectTransform.anchorMin = new Vector2(0f, 1f);
            _info.rectTransform.anchorMax = new Vector2(0f, 1f);
            _info.rectTransform.pivot = new Vector2(0f, 1f);
            _info.rectTransform.anchoredPosition = new Vector2(Padding, -Padding);
            _info.rectTransform.sizeDelta = new Vector2(FullWidth - Padding * 2f, 40f);

            _postButton.gameObject.SetActive(true);
            _shockwave.gameObject.SetActive(true);
            _repair.gameObject.SetActive(true);
            _shockwaveLabel.gameObject.SetActive(true);
            _repairLabel.gameObject.SetActive(true);

            string postName = _context.HeroPostNames[_hero.PostIndex];
            if (_hero.IsDown)
            {
                _info.text = $"{_hero.Definition.DisplayName} is down  {Mathf.CeilToInt(_hero.ReviveRemaining)}s";
                _info.color = DownColor;
            }
            else
            {
                string state = _hero.IsWalking ? $"on the way to {postName}" : $"at {postName}";
                _info.text = $"{_hero.Definition.DisplayName} {state}  HP {Mathf.CeilToInt(_hero.Health.Current)}";
                _info.color = TextColor;
            }

            _postLabel.text = "Next post";
            _postLabel.color = TextColor;

            bool shockwaveReady = !_hero.IsDown && _hero.ShockwaveReady >= 1f;
            _shockwave.interactable = shockwaveReady;
            _shockwaveLabel.text = shockwaveReady ? "Shockwave" : $"Shockwave {Cooldown(_hero.ShockwaveReady, _hero.Definition.ShockwaveCooldown)}s";
            _shockwaveLabel.color = shockwaveReady ? TextColor : DimColor;

            int cost = _hero.Definition.RepairCost;
            bool affordable = _context.Wallet.CanAfford(cost);
            bool repairReady = _hero.CanEmergencyRepair;
            _repair.interactable = repairReady && affordable;
            _repairLabel.text = repairReady ? $"Repair {cost}" : $"Repair {Cooldown(_hero.RepairReady, _hero.Definition.RepairCooldown)}s";
            _repairLabel.color = !repairReady ? DimColor : affordable ? TextColor : Unaffordable;
        }

        private static int Cooldown(float ready, float total) => Mathf.CeilToInt((1f - ready) * total);
    }
}
