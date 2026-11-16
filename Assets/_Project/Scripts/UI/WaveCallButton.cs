using Game.Core;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The HUD's "call next wave" icon (round 73): lit while the level accepts a call, with the button glyph of the call on
    /// the device in use as a badge, and a short "+X early" that rises under the wave line when a call paid a bonus. Reads the
    /// level's shared state; the click only raises the request, the level controller applies the rules.
    /// </summary>
    public sealed class WaveCallButton : MonoBehaviour
    {
        private const float BadgeHeight = 32f;
        private const float BadgeOverhang = 8f;
        private const float DimAlpha = 0.35f;
        private const int BonusFontSize = 30;
        private const float BonusSeconds = 1.8f;
        private const float BonusRise = 24f;
        private static readonly Vector2 BonusBox = new Vector2(400f, 44f);
        private static readonly Color BonusColour = new Color(1f, 0.85f, 0.35f);

        private LevelContext _context;
        private IInputProvider _input;
        private IInputBindings _bindings;
        private Button _button;
        private Image _icon;
        private Image _badge;
        private Text _bonus;
        private Vector2 _bonusHome;
        private float _bonusAge = float.MaxValue;
        private bool _shownCallable;

        /// <summary>The bonus text, for tests and stills.</summary>
        public string BonusText => _bonus != null && _bonus.enabled ? _bonus.text : string.Empty;

        public Button Button => _button;

        /// <param name="bonusAnchor">Where the bonus text starts, on the safe area's top centre (under the wave line).</param>
        public void Initialize(LevelContext context, Button button, RectTransform safe, Font font, Vector2 bonusAnchor)
        {
            _context = context;
            _button = button;
            _icon = button.transform.Find("Icon").GetComponent<Image>();
            _badge = PromptGlyphs.CreateImage(button.GetComponent<RectTransform>(), BadgeHeight, "Badge");
            RectTransform badge = _badge.rectTransform;
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 0f);
            badge.anchoredPosition = new Vector2(BadgeOverhang, -BadgeOverhang);
            _bonus = CreateBonusText(safe, font, bonusAnchor);
            _bonusHome = bonusAnchor;

            Services.TryGet(out _input);
            Services.TryGet(out _bindings);
            if (_input != null)
            {
                _input.ActiveDeviceChanged += RefreshGlyph;
            }

            if (_bindings != null)
            {
                _bindings.BindingsChanged += RefreshGlyph;
            }

            context.WaveCalledEarly.Subscribe(OnCalled);
            RefreshGlyph();
            Apply(context.CanCallWave);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.ActiveDeviceChanged -= RefreshGlyph;
            }

            if (_bindings != null)
            {
                _bindings.BindingsChanged -= RefreshGlyph;
            }

            _context?.WaveCalledEarly.Unsubscribe(OnCalled);
        }

        private void Update()
        {
            if (_context == null)
            {
                return;
            }

            bool callable = _context.CanCallWave;
            if (callable != _shownCallable)
            {
                Apply(callable);
            }

            AnimateBonus();
        }

        private void Apply(bool callable)
        {
            _shownCallable = callable;
            _button.interactable = callable;
            _icon.color = new Color(1f, 1f, 1f, callable ? 1f : DimAlpha);
            if (_badge.sprite != null)
            {
                _badge.color = new Color(1f, 1f, 1f, callable ? 1f : DimAlpha);
            }
        }

        // Only a device switch, a new pad family or a rebind changes the badge.
        private void RefreshGlyph()
        {
            PromptGlyphs.Show(_badge, PromptGlyphs.For(GameAction.CallWave));
            if (_button != null)
            {
                Apply(_shownCallable);
            }
        }

        private void OnCalled(int bonus)
        {
            if (bonus <= 0)
            {
                return;
            }

            _bonus.text = Loc.Format("hud.early_bonus", bonus);
            _bonus.enabled = true;
            _bonusAge = 0f;
            AnimateBonus();
        }

        // Real seconds, so the text reads the same at triple speed.
        private void AnimateBonus()
        {
            if (!_bonus.enabled)
            {
                return;
            }

            _bonusAge += Time.unscaledDeltaTime;
            float t = _bonusAge / BonusSeconds;
            if (t >= 1f)
            {
                _bonus.enabled = false;
                return;
            }

            _bonus.rectTransform.anchoredPosition = _bonusHome + Vector2.up * (BonusRise * t);
            _bonus.color = new Color(BonusColour.r, BonusColour.g, BonusColour.b, 1f - t * t);
        }

        private static Text CreateBonusText(RectTransform safe, Font font, Vector2 anchor)
        {
            var go = new GameObject("EarlyBonus", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(safe, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchor;
            rect.sizeDelta = BonusBox;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = BonusFontSize;
            text.alignment = TextAnchor.UpperCenter;
            text.color = BonusColour;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.enabled = false;
            return text;
        }
    }
}
