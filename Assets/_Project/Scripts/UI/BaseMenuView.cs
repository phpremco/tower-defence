using Game.Drones;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The base panel in the bottom-right corner, always in view: one card per drone kind with its picture, its
    /// price and how many are out, greyed out when money is short or that kind is at its limit. A click buys.
    /// On a gamepad, confirming on the base's doorstep focuses the panel (the stick then walks the cards, A buys,
    /// B leaves), which is what <see cref="LevelContext.BaseMenuOpen"/> means. Only reads LevelContext.
    /// A level that offers no drones yet shows no panel. While a dialogue that introduces a drone kind plays (and for a
    /// while after, until that kind is bought) its card is framed in a pulsing light with a "NEW" tag across its top.
    /// </summary>
    public sealed class BaseMenuView : MonoBehaviour
    {
        private const float CardWidth = 112f;
        private const float CardHeight = 104f;
        private const float CardGap = 8f;
        private const float LabelHeight = 46f;
        private const float HeaderHeight = 30f;
        private const int LabelSize = 20;
        private const int HeaderSize = 22;
        private const float Padding = 10f;
        private const float RightMargin = 20f;
        private const float AboveBottomBar = 164f;
        private const float PictureScale = 2.6f;
        private const float PictureLift = 0.55f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color HeaderColor = new Color(0.7f, 0.8f, 0.95f);
        private static readonly Color Unavailable = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color TooExpensive = new Color(1f, 0.5f, 0.45f);
        private static readonly Color CardTint = new Color(0.16f, 0.26f, 0.42f);
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);

        // The introduction highlight: signal yellow like the colony's markings, breathing so it catches the eye.
        private const float HighlightLingerSeconds = 15f;
        private const float HighlightPulseRate = 4f;
        private const float HighlightMinAlpha = 0.35f;
        private const float HighlightPad = 7f;
        private const float NewTagHeight = 26f;
        private const int NewTagSize = 20;
        private const string NewTagText = "NEW";
        private static readonly Color HighlightColour = new Color(1f, 0.82f, 0.25f);

        private LevelContext _context;
        private RectTransform _root;
        private Font _font;
        private RectTransform _panel;

        /// <summary>The drones panel in the bottom-right corner, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;
        private DroneButton[] _buttons;
        private bool _wasFocused;
        private bool _tending;
        private int _openedFrame;
        private GameObject _lastCard;

        private struct DroneButton
        {
            public DroneDefinition Drone;
            public Button Button;
            public Text Label;
            public Image Highlight;
            public Text NewTag;

            /// <summary>Unscaled time until which the card stays lit; zero when it was never introduced.</summary>
            public float HighlightUntil;
        }

        public void Initialize(LevelContext context, RectTransform root, Camera camera, Font font)
        {
            _context = context;
            _root = root;
            _font = font;
            Build();
            _context.Changed.Subscribe(Refresh);
            _context.DronesChanged.Subscribe(Refresh);
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
            _context.DronesChanged.Unsubscribe(Refresh);
            _context.Wallet.Changed -= OnMoneyChanged;
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void Build()
        {
            DroneDefinition[] drones = _context.Level.Drones;
            float width = drones.Length * CardWidth + Mathf.Max(0, drones.Length - 1) * CardGap + Padding * 2f;
            float height = HeaderHeight + CardHeight + LabelHeight + Padding * 2f;

            var go = new GameObject("BasePanel", typeof(RectTransform));
            _panel = go.GetComponent<RectTransform>();
            _panel.SetParent(_root, false);
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 0f);
            _panel.pivot = new Vector2(1f, 0f);
            _panel.anchoredPosition = new Vector2(-RightMargin, AboveBottomBar);
            _panel.sizeDelta = new Vector2(width, height);

            UiKit.CreatePanel(_panel, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Backdrop);
            Text header = CreateLabel(new Vector2(0f, 1f), new Vector2(Padding, -Padding - HeaderHeight * 0.5f), TextAnchor.MiddleLeft, new Vector2(width - Padding * 2f, HeaderHeight), HeaderSize);
            header.rectTransform.pivot = new Vector2(0f, 0.5f);
            header.text = "Drones";
            header.color = HeaderColor;

            _buttons = new DroneButton[drones.Length];
            for (int i = 0; i < drones.Length; i++)
            {
                _buttons[i] = CreateDroneButton(drones[i], Padding + i * (CardWidth + CardGap));
            }

            // The stick walks the cards left and right once the panel has focus.
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _buttons[i - 1].Button : null,
                    selectOnRight = i + 1 < _buttons.Length ? _buttons[i + 1].Button : null,
                };
            }
        }

        private DroneButton CreateDroneButton(DroneDefinition drone, float x)
        {
            // Built before the card so it draws behind it: only the rim around the card shows.
            Image highlight = UiKit.CreatePanel(_panel, "Highlight", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, HighlightColour);
            RectTransform glow = highlight.rectTransform;
            glow.pivot = Vector2.zero;
            glow.anchoredPosition = new Vector2(x - HighlightPad, Padding + LabelHeight - HighlightPad);
            glow.sizeDelta = new Vector2(CardWidth + HighlightPad * 2f, CardHeight + HighlightPad * 2f);
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);

            Button button = UiKit.CreateIconButton(_panel, null, CardHeight, () => OnClicked(drone));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, Padding + LabelHeight);
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            Destroy(button.GetComponent<LayoutElement>());

            UiKit.UseTint(button);
            ColorBlock colors = button.colors;
            colors.normalColor = CardTint;
            colors.highlightedColor = Color.Lerp(CardTint, Color.white, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.white;
            colors.disabledColor = new Color(0.12f, 0.12f, 0.14f);
            button.colors = colors;

            // The card shows the drone itself, rendered once from its model like the tower cards.
            var icon = button.transform.Find("Icon").GetComponent<Image>();
            icon.enabled = false;
            ModelIconRenderer.Request(drone, stage => BuildPicture(stage, drone), sprite =>
            {
                if (icon != null)
                {
                    icon.sprite = sprite;
                    icon.enabled = true;
                }
            });

            var entry = new DroneButton { Drone = drone, Button = button, Highlight = highlight };
            entry.Label = CreateLabel(new Vector2(0f, 0f), new Vector2(x + CardWidth * 0.5f, Padding + LabelHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(CardWidth + CardGap, LabelHeight), LabelSize);
            entry.NewTag = CreateLabel(new Vector2(0f, 0f), new Vector2(x + CardWidth * 0.5f, Padding + LabelHeight + CardHeight - NewTagHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(CardWidth, NewTagHeight), NewTagSize);
            entry.NewTag.text = NewTagText;
            entry.NewTag.color = HighlightColour;
            entry.NewTag.fontStyle = FontStyle.Bold;
            entry.NewTag.gameObject.SetActive(false);
            return entry;
        }

        // Polled, because the dialogue player has no "started" event and a card must light up the frame its introduction
        // shows. No allocations: the current dialogue is compared against each card's drone.
        private void Update()
        {
            if (_buttons == null || _buttons.Length == 0)
            {
                return;
            }

            TendFocus();

            DialogueDefinition current = _context.Dialogue != null ? _context.Dialogue.Current : null;
            float now = Time.unscaledTime;
            float pulse = Mathf.Lerp(HighlightMinAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(now * HighlightPulseRate));
            for (int i = 0; i < _buttons.Length; i++)
            {
                // Lit while the introduction talks about it, then until the player buys one or the linger runs out.
                bool introducing = current != null && current.Introduces(_buttons[i].Drone);
                if (introducing)
                {
                    _buttons[i].HighlightUntil = now + HighlightLingerSeconds;
                }

                bool lit = introducing || (now < _buttons[i].HighlightUntil && _context.ActiveDrones(_buttons[i].Drone) == 0);
                if (!lit)
                {
                    _buttons[i].HighlightUntil = 0f;
                }

                SetHighlight(_buttons[i], lit, pulse);
            }
        }

        private static void SetHighlight(DroneButton entry, bool lit, float pulse)
        {
            if (entry.Highlight.gameObject.activeSelf != lit)
            {
                entry.Highlight.gameObject.SetActive(lit);
                entry.NewTag.gameObject.SetActive(lit);
            }

            if (lit)
            {
                Color colour = HighlightColour;
                colour.a = pulse;
                entry.Highlight.color = colour;
            }
        }

        // Drones are small, so the picture blows them up and lifts them into the frame the renderer uses for towers.
        private static GameObject BuildPicture(Transform stage, DroneDefinition drone)
        {
            GameObject root = DroneModelBuilder.BuildDisplay(stage, drone).Root;
            root.transform.localScale = Vector3.one * PictureScale;
            root.transform.localPosition = Vector3.up * PictureLift;
            root.transform.localRotation = Quaternion.Euler(18f, 35f, 0f);
            return root;
        }

        private void OnClicked(DroneDefinition drone)
        {
            _context.DroneRequested.Raise(drone);
            if (!_context.BaseMenuOpen)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private Text CreateLabel(Vector2 anchor, Vector2 position, TextAnchor alignment, Vector2 size, int fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform));
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
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private void Refresh()
        {
            bool running = _context.Outcome == LevelOutcome.Running;
            _panel.gameObject.SetActive(running && _buttons.Length > 0);
            if (!running)
            {
                return;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                Refresh(_buttons[i]);
            }

            bool focused = _context.BaseMenuOpen;
            if (!focused && _wasFocused)
            {
                ReleaseFocus();
            }

            _wasFocused = focused;
        }

        private void Refresh(DroneButton entry)
        {
            int active = _context.ActiveDrones(entry.Drone);
            int max = _context.MaxDrones(entry.Drone);
            bool full = active >= max;
            bool affordable = _context.Wallet.Money >= entry.Drone.Cost;
            entry.Button.interactable = affordable && !full;
            // The picture names the drone; the label only carries what changes: the price and how many are out.
            entry.Label.text = full ? $"max {active}/{max}" : $"{entry.Drone.Cost}   {active}/{max}";
            // Grey when the limit is reached, red when there is room but not enough money.
            entry.Label.color = full ? Unavailable : affordable ? TextColor : TooExpensive;
        }

        // A on the base doorstep opens the panel, and that press still counts as "submitted this frame" for the UI: a card
        // focused in the same frame was bought at once. The focus waits for the next frame. After that it stays on the
        // panel while it is open: a card that greys out under it (the last drone of its kind bought) drops the focus, and
        // it goes back to that card. While the pause menu, a dialogue or the codex holds the game, the focus is theirs.
        private void TendFocus()
        {
            if (!_context.BaseMenuOpen)
            {
                _tending = false;
                return;
            }

            // The opening is noticed here rather than in Refresh, so the frame count is right however the panel was opened.
            if (!_tending)
            {
                _tending = true;
                _openedFrame = Time.frameCount;
                _lastCard = null;
            }

            EventSystem events = EventSystem.current;
            if (events == null || Time.frameCount <= _openedFrame || _context.InputBlocked)
            {
                return;
            }

            GameObject current = events.currentSelectedGameObject;
            if (IsCard(current))
            {
                _lastCard = current;
            }
            else if (_lastCard != null && _lastCard.activeInHierarchy)
            {
                events.SetSelectedGameObject(_lastCard);
            }
            else
            {
                FocusFirstAvailable();
            }
        }

        private bool IsCard(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy)
            {
                return false;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (candidate == _buttons[i].Button.gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        private void FocusFirstAvailable()
        {
            Button first = null;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (first == null || (!first.interactable && _buttons[i].Button.interactable))
                {
                    first = _buttons[i].Button;
                }
            }

            if (first != null)
            {
                UiKit.Select(first);
            }
        }

        private void ReleaseFocus()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (selected == _buttons[i].Button.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    return;
                }
            }
        }
    }
}
