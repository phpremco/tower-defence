using Game.Drones;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The base's own menu, opened by confirming on its doorstep: the base and energy readout and one card per
    /// drone kind with its picture, energy price and how many are out. A card is greyed out when energy is short
    /// or that kind is at its limit. It floats above the base like the tower menu and only reads LevelContext.
    /// </summary>
    public sealed class BaseMenuView : MonoBehaviour
    {
        private const float IconSize = 110f;
        private const float Spacing = 150f;
        private const float LabelHeight = 64f;
        private const float LabelGap = 8f;
        private const int LabelSize = 24;
        private const float WorldHeight = 3.2f;
        private const float RefreshSeconds = 0.25f;
        private const float BackdropPadding = 14f;
        private const float MinPanelWidth = 520f;
        private const float TopFraction = 0.1f;
        private const float BelowOffset = 280f;
        private const float PictureScale = 2.6f;
        private const float PictureLift = 0.55f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Unavailable = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color TooExpensive = new Color(1f, 0.5f, 0.45f);
        private static readonly Color CardTint = new Color(0.16f, 0.26f, 0.42f);

        private LevelContext _context;
        private RectTransform _root;
        private Camera _camera;
        private Font _font;
        private RectTransform _panel;
        private float _panelWidth;
        private Text _info;
        private DroneButton[] _buttons;
        private bool _wasOpen;
        private float _nextRefresh;

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
            _camera = camera;
            _font = font;
            Build();
            _context.Changed.Subscribe(Refresh);
            _context.DronesChanged.Subscribe(Refresh);
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
        }

        // Money changes while the menu is open, so the readout follows it a few times a second.
        private void Update()
        {
            if (_wasOpen && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }

        private void Build()
        {
            DroneDefinition[] drones = _context.Level.Drones;
            _panelWidth = Mathf.Max(MinPanelWidth, drones.Length * (IconSize + Spacing));

            var go = new GameObject("BaseMenu", typeof(RectTransform));
            _panel = go.GetComponent<RectTransform>();
            _panel.SetParent(_root, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(_panelWidth, IconSize + LabelHeight * 2.5f + LabelGap * 2f);

            UiKit.CreatePanel(
                _panel, "Backdrop", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-BackdropPadding, -BackdropPadding), new Vector2(BackdropPadding, BackdropPadding), new Color(0.05f, 0.09f, 0.18f, 0.9f));
            _info = CreateLabel(new Vector2(0.5f, 1f), new Vector2(0f, -LabelHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(_panelWidth, LabelHeight));

            _buttons = new DroneButton[drones.Length];
            for (int i = 0; i < drones.Length; i++)
            {
                _buttons[i] = CreateDroneButton(drones[i], i - (drones.Length - 1) * 0.5f);
            }

            _panel.gameObject.SetActive(false);
        }

        private DroneButton CreateDroneButton(DroneDefinition drone, float slot)
        {
            float x = slot * (IconSize + Spacing);
            Button button = UiKit.CreateIconButton(_panel, null, IconSize, () => OnClicked(drone));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, LabelHeight + LabelGap);
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
            entry.Label = CreateLabel(new Vector2(0.5f, 0f), new Vector2(x, LabelHeight * 0.5f), TextAnchor.MiddleCenter, new Vector2(IconSize + Spacing - 10f, LabelHeight));
            return entry;
        }

        // Drones are small, so the picture blows them up and lifts them into the frame the renderer uses for towers.
        private static GameObject BuildPicture(Transform stage, DroneDefinition drone)
        {
            GameObject root = DroneModelBuilder.Build(stage, drone).Root;
            root.transform.localScale = Vector3.one * PictureScale;
            root.transform.localPosition = Vector3.up * PictureLift;
            root.transform.localRotation = Quaternion.Euler(18f, 35f, 0f);
            return root;
        }

        private void OnClicked(DroneDefinition drone)
        {
            _context.DroneRequested.Raise(drone);
            EventSystem.current.SetSelectedGameObject(null);
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
            bool open = _context.BaseMenuOpen && _context.Outcome == LevelOutcome.Running;
            _panel.gameObject.SetActive(open);
            if (!open)
            {
                _wasOpen = false;
                return;
            }

            PositionAbove(LevelVisualBuilder.BasePosition(_context.Level));
            _info.text = $"Base {Mathf.CeilToInt(_context.BaseHealth.Current)}/{Mathf.CeilToInt(_context.BaseHealth.Max)}    Money {_context.Wallet.Money}";
            for (int i = 0; i < _buttons.Length; i++)
            {
                Refresh(_buttons[i]);
            }

            if (!_wasOpen)
            {
                _wasOpen = true;
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                FocusFirstAvailable();
            }
        }

        private void Refresh(DroneButton entry)
        {
            int active = _context.ActiveDrones(entry.Drone);
            bool full = active >= entry.Drone.MaxActive;
            bool affordable = _context.Wallet.Money >= entry.Drone.Cost;
            entry.Button.interactable = affordable && !full;
            entry.Label.text = full
                ? $"{entry.Drone.DisplayName}\nmax {active}/{entry.Drone.MaxActive}"
                : $"{entry.Drone.DisplayName}\n{entry.Drone.Cost} money  {active}/{entry.Drone.MaxActive}";
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

            if (first != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        // Above the base, or hanging below it when the base is near the top of the screen.
        private void PositionAbove(Vector3 basePosition)
        {
            Vector3 screen = _camera.WorldToScreenPoint(basePosition + Vector3.up * WorldHeight);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local))
            {
                return;
            }

            // Kept inside the screen sideways, so a base at the edge still shows its whole menu.
            float halfRoom = _root.rect.width * 0.5f - _panelWidth * 0.5f - BackdropPadding;
            local.x = Mathf.Clamp(local.x, -halfRoom, halfRoom);
            bool nearTop = local.y > _root.rect.height * TopFraction;
            _panel.pivot = new Vector2(0.5f, nearTop ? 1f : 0f);
            _panel.anchoredPosition = nearTop ? local - new Vector2(0f, BelowOffset) : local;
        }
    }
}
