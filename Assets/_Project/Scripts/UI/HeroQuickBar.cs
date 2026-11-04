using System.Collections.Generic;
using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The hero's own panel on the left of the screen, styled like the drones panel so it reads as one thing: a
    /// "Hero" header with the hero's name and health, and under it one button per ability slot (from data) and the
    /// "next post" order, each with its cooldown, price and key. The player never has to find the hero in a fight and click it first;
    /// the full panel over the hero stays for the rest.
    /// </summary>
    public sealed class HeroQuickBar : MonoBehaviour
    {
        private const float Left = 18f;
        private const float Width = 176f;
        private const float Padding = 10f;
        private const float HeaderHeight = 30f;
        private const float NameHeight = 24f;
        private const float BarHeight = 10f;
        private const float BarGap = 6f;
        private const float IconSize = 68f;
        private const float LabelHeight = 24f;
        private const float RowGap = 10f;
        private const int HeaderSize = 22;
        private const int NameSize = 18;
        private const int LabelSize = 18;
        private const float RefreshSeconds = 0.1f;
        private const float LowHealthFraction = 0.35f;
        private const float MidHealthFraction = 0.65f;
        private const string PostKey = "H";
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color HeaderColor = new Color(0.7f, 0.8f, 0.95f);
        private static readonly Color DownColor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color PostTint = new Color(0.5f, 0.6f, 1f);
        // Every row hangs from the top centre, so icons and their labels line up in one centred column.
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        private LevelContext _context;
        private Commander _hero;
        private RectTransform _root;
        private Text _name;
        private Image _healthFill;
        private readonly List<HeroAbilityButton> _abilities = new List<HeroAbilityButton>();
        private Text _postLabel;
        private float _nextRefresh;

        public void Initialize(LevelContext context, RectTransform parent)
        {
            _context = context;
            _hero = context.Hero;
            if (_hero == null)
            {
                return;
            }

            _root = new GameObject("HeroQuickBar", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.anchoredPosition = new Vector2(Left, 0f);
            int slots = CountSlots();
            float rows = (IconSize + LabelHeight) * (slots + 1) + RowGap * slots;
            float height = Padding + HeaderHeight + NameHeight + BarGap + BarHeight + RowGap + rows + Padding;
            _root.sizeDelta = new Vector2(Width, height);
            UiKit.CreatePanel(_root, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Backdrop);

            float y = -Padding;
            Text header = UiKit.CreateFixedText(_root, "Hero", HeaderSize, TextAnchor.MiddleCenter, Top, new Vector2(0f, y), new Vector2(Width - Padding * 2f, HeaderHeight));
            // The same display face as the drones panel header, so the two panels read as a pair.
            header.font = UiKit.DefaultFont;
            header.color = HeaderColor;
            y -= HeaderHeight;
            _name = UiKit.CreateFixedText(_root, _hero.Definition.DisplayName, NameSize, TextAnchor.MiddleCenter, Top, new Vector2(0f, y), new Vector2(Width - Padding * 2f, NameHeight));
            y -= NameHeight + BarGap;
            _healthFill = HeroPanelStyle.CreateHealthBar(_root, new Vector2(Padding, y), new Vector2(Width - Padding * 2f, BarHeight));
            y -= BarHeight + RowGap;

            for (int slot = 0; slot < _hero.Abilities.Count; slot++)
            {
                HeroAbilityState state = _hero.Abilities[slot];
                if (state == null)
                {
                    continue;
                }

                int pressed = slot;
                Button button = Action(y, HeroAbilityHud.IconFor(state.Definition), state.Definition.Tint, () => _context.HeroAbilityRequested.Raise(pressed), out Text label);
                _abilities.Add(new HeroAbilityButton(slot, state, button, label));
                y -= IconSize + LabelHeight + RowGap;
            }

            Action(y, IconFactory.Get(IconType.Hero), PostTint, () => _context.HeroPostRequested.Raise((_hero.PostIndex + 1) % _hero.PostCount), out _postLabel);
            Refresh();
        }

        private int CountSlots()
        {
            int count = 0;
            for (int i = 0; i < _hero.Abilities.Count; i++)
            {
                if (_hero.Abilities[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private Button Action(float top, Sprite icon, Color tint, UnityEngine.Events.UnityAction action, out Text label)
        {
            Button button = UiKit.CreateIconButton(_root, icon, IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Destroy(button.GetComponent<LayoutElement>());
            HeroPanelStyle.Tint(button, tint);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = Top;
            rect.anchorMax = Top;
            rect.pivot = Top;
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(IconSize, IconSize);

            label = UiKit.CreateFixedText(_root, string.Empty, LabelSize, TextAnchor.MiddleCenter, Top, new Vector2(0f, top - IconSize), new Vector2(Width - Padding * 2f, LabelHeight));
            label.color = TextColor;
            return button;
        }

        private void LateUpdate()
        {
            if (_root == null)
            {
                return;
            }

            bool show = _context.Outcome == LevelOutcome.Running;
            if (_root.gameObject.activeSelf != show)
            {
                _root.gameObject.SetActive(show);
            }

            if (show && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_hero.IsDown)
            {
                int seconds = Mathf.CeilToInt(_hero.ReviveRemaining);
                _name.text = $"Down  {seconds / 60}:{seconds % 60:00}";
                _name.color = DownColor;
            }
            else
            {
                _name.text = _hero.Definition.DisplayName;
                _name.color = TextColor;
            }

            HeroPanelStyle.FillHealth(_healthFill, _hero.IsDown ? 0f : _hero.Health.Current / _hero.Health.Max, LowHealthFraction, MidHealthFraction);

            foreach (HeroAbilityButton ability in _abilities)
            {
                bool ready = _hero.CanUse(ability.Slot);
                bool affordable = HeroAbilityHud.Affordable(ability.State, _context.Wallet);
                ability.Button.interactable = ready && affordable;
                ability.Label.text = HeroAbilityHud.Label(ability.State, ability.Slot, true);
                ability.Label.color = !ready ? DimColor : affordable ? TextColor : Unaffordable;
            }

            _postLabel.text = $"Next post  {PostKey}";
            _postLabel.color = _hero.IsDown ? DimColor : TextColor;
        }
    }
}
