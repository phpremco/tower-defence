using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The hero's powers within reach at all times: a small column on the left of the screen with the shockwave and
    /// the "next post" order, each with its cooldown and its key. The player no longer has to find the hero in a
    /// fight and click it first; the full panel over the hero stays for the rest.
    /// </summary>
    public sealed class HeroQuickBar : MonoBehaviour
    {
        private const float Left = 18f;
        private const float IconSize = 72f;
        private const float Gap = 14f;
        private const float LabelHeight = 22f;
        private const int LabelSize = 18;
        private const float RefreshSeconds = 0.1f;
        private const string ShockwaveKey = "F";
        private const string PostKey = "H";
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.6f, 0.7f);

        private LevelContext _context;
        private Commander _hero;
        private RectTransform _root;
        private Button _shockwave;
        private Text _shockwaveLabel;
        private Text _postLabel;
        private float _nextRefresh;

        public void Initialize(LevelContext context, RectTransform parent)
        {
            _context = context;
            _hero = context.Hero;
            _root = new GameObject("HeroQuickBar", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.anchoredPosition = new Vector2(Left, 0f);
            float column = IconSize + LabelHeight;
            _root.sizeDelta = new Vector2(IconSize + 60f, column * 2f + Gap);

            _shockwave = Action(0, IconType.Shockwave, () => _hero.TryShockwave(), out _shockwaveLabel);
            Action(1, IconType.Hero, () => _hero.MoveTo((_hero.PostIndex + 1) % _hero.PostCount), out _postLabel);
            Refresh();
        }

        private Button Action(int index, IconType type, UnityEngine.Events.UnityAction action, out Text label)
        {
            float top = -index * (IconSize + LabelHeight + Gap);
            Button button = UiKit.CreateIconButton(_root, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(IconSize, IconSize);

            label = UiKit.CreateFixedText(_root, string.Empty, LabelSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, top - IconSize), new Vector2(IconSize + 60f, LabelHeight));
            label.color = TextColor;
            return button;
        }

        private void LateUpdate()
        {
            bool show = _hero != null && _context.Outcome == LevelOutcome.Running;
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
            bool ready = !_hero.IsDown && _hero.ShockwaveReady >= 1f;
            _shockwave.interactable = ready;
            _shockwaveLabel.text = ready ? $"Pulse  {ShockwaveKey}" : $"Pulse  {Cooldown(_hero.ShockwaveReady, _hero.Definition.ShockwaveCooldown)}s";
            _shockwaveLabel.color = ready ? TextColor : DimColor;
            _postLabel.text = $"Next post  {PostKey}";
            _postLabel.color = _hero.IsDown ? DimColor : TextColor;
        }

        private static int Cooldown(float ready, float total) => Mathf.CeilToInt((1f - ready) * total);
    }
}
