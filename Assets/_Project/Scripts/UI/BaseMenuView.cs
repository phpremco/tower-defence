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

        private LevelContext _context;
        private RectTransform _root;
        private Font _font;
        private RectTransform _panel;
        private DroneButton[] _buttons;
        private bool _wasFocused;

        private struct DroneButton
        {
            public DroneDefinition Drone;
            public Button Button;
            public Text Label;
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
            Button button = UiKit.CreateIconButton(_panel, null, CardHeight, () => OnClicked(drone));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, Padding + LabelHeight);
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            Destroy(button.GetComponent<LayoutElement>());

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

            var entry = new DroneButton { Drone = drone, Button = button };
            entry.Label = CreateLabel(new Vector2(0f, 0f), new Vector2(x + CardWidth * 0.5f, Padding + LabelHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(CardWidth + CardGap, LabelHeight), LabelSize);
            return entry;
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
            _panel.gameObject.SetActive(running);
            if (!running)
            {
                return;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                Refresh(_buttons[i]);
            }

            bool focused = _context.BaseMenuOpen;
            if (focused && !_wasFocused)
            {
                FocusFirstAvailable();
            }
            else if (!focused && _wasFocused)
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
