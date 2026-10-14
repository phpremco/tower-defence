using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The hero's controls: where it stands and how it is doing, a button that moves it to the next post and
    /// the two abilities with their cooldowns. Keys and gamepad buttons are shortcuts for the same actions.
    /// </summary>
    public sealed class HeroPanelView : MonoBehaviour
    {
        private const float Margin = 24f;
        private const float BottomBarHeight = 190f;
        private const float PanelWidth = 560f;
        private const float PanelHeight = 200f;
        private const float IconSize = 84f;
        private const float ColumnWidth = 180f;
        private const float Padding = 16f;
        private const int InfoSize = 26;
        private const int LabelSize = 22;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color DownColor = new Color(1f, 0.5f, 0.45f);

        private LevelContext _context;
        private Commander _hero;
        private RectTransform _panel;
        private Text _info;
        private Button _shockwave;
        private Button _repair;
        private Text _shockwaveLabel;
        private Text _repairLabel;
        private Font _font;

        public void Initialize(LevelContext context, RectTransform root, Font font)
        {
            _context = context;
            _hero = context.Hero;
            _font = font;
            if (_hero == null)
            {
                return;
            }

            var go = new GameObject("HeroPanel", typeof(RectTransform));
            _panel = go.GetComponent<RectTransform>();
            _panel.SetParent(root, false);
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;
            _panel.anchoredPosition = new Vector2(Margin, BottomBarHeight + 12f);
            _panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            UiKit.CreatePanel(_panel, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.09f, 0.18f, 0.85f));
            _info = CreateText("Info", new Vector2(Padding, PanelHeight - Padding - 40f), new Vector2(PanelWidth - Padding * 2f, 40f), InfoSize, TextAnchor.UpperLeft);

            CreateAction(0, IconType.Hero, () => _hero.MoveTo((_hero.PostIndex + 1) % _hero.PostCount), out _, out Text postLabel);
            postLabel.text = "Next post";
            _shockwave = CreateAction(1, IconType.Shockwave, () => _hero.TryShockwave(), out _, out _shockwaveLabel);
            _repair = CreateAction(2, IconType.Repair, () => _hero.TryEmergencyRepair(), out _, out _repairLabel);

            _hero.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Changed -= Refresh;
            }
        }

        private Button CreateAction(int index, IconType type, UnityEngine.Events.UnityAction action, out Button button, out Text label)
        {
            float x = Padding + index * ColumnWidth;
            Button created = UiKit.CreateIconButton(_panel, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            created.navigation = new Navigation { mode = Navigation.Mode.None };
            Destroy(created.GetComponent<LayoutElement>());
            RectTransform rect = created.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = new Vector2(x, 46f);

            label = CreateText("Label", new Vector2(x, 10f), new Vector2(ColumnWidth - 10f, 32f), LabelSize, TextAnchor.MiddleLeft);
            button = created;
            return created;
        }

        private Text CreateText(string objectName, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_panel, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

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

        private void Refresh()
        {
            string post = _context.HeroPostNames[_hero.PostIndex];
            if (_hero.IsDown)
            {
                _info.text = $"{_hero.Definition.DisplayName} is down  {Mathf.CeilToInt(_hero.ReviveRemaining)}s";
                _info.color = DownColor;
            }
            else
            {
                string state = _hero.IsWalking ? $"on the way to {post}" : $"at {post}";
                _info.text = $"{_hero.Definition.DisplayName} {state}  HP {Mathf.CeilToInt(_hero.Health.Current)}";
                _info.color = TextColor;
            }

            bool shockwaveReady = !_hero.IsDown && _hero.ShockwaveReady >= 1f;
            bool repairReady = !_hero.IsDown && !_hero.IsWalking && _hero.RepairReady >= 1f;
            _shockwave.interactable = shockwaveReady;
            _repair.interactable = repairReady;
            _shockwaveLabel.text = shockwaveReady ? "Shockwave" : $"Shockwave {Cooldown(_hero.ShockwaveReady, _hero.Definition.ShockwaveCooldown)}s";
            _repairLabel.text = repairReady ? "Repair post" : $"Repair {Cooldown(_hero.RepairReady, _hero.Definition.RepairCooldown)}s";
            _shockwaveLabel.color = shockwaveReady ? TextColor : DimColor;
            _repairLabel.color = repairReady ? TextColor : DimColor;
        }

        private static int Cooldown(float ready, float total) => Mathf.CeilToInt((1f - ready) * total);
    }
}
