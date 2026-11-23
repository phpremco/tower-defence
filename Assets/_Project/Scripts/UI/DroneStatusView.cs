using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// What every drone the player owns is doing (round 88g, Remco: "I had two repair drones and could not tell what they
    /// were doing"): a strip on top of the Drones panel with one chip per drone, standing in the column over the card of
    /// its kind, so the card names the type. A chip is the drone's battery as a bar (what its hull has left; while it is
    /// knocked out, how far its charge has come back) in the colour of its state, with the state as a word: working,
    /// flying, charging or idle. The strip is only as tall as the largest crew and is gone while no drone is out.
    /// Choosing a chip (a click, or up from a card with the stick and confirm) glides the camera to that drone.
    /// Only reads the level context and the drones.
    /// </summary>
    public sealed class DroneStatusView : MonoBehaviour
    {
        private const float ChipHeight = 34f;
        private const float ChipGap = 4f;
        private const float Padding = 8f;
        private const float PanelGap = 6f;
        private const float Frame = 3f;
        private const float TextInset = 4f;
        // The bar moves when the battery moved by this much, so a steady drone costs nothing per frame.
        private const float BatteryStep = 0.004f;
        // A charging bar breathes, so it is told from a bar that is merely short.
        private const float ChargePulseRate = 5f;
        private const float ChargePulseDepth = 0.35f;
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color FrameColour = new Color(0.02f, 0.04f, 0.08f);
        private static readonly Color FrameChosen = new Color(1f, 0.82f, 0.25f);
        private static readonly Color Empty = new Color(0.1f, 0.13f, 0.2f);
        private static readonly Color TextColour = new Color(0.95f, 0.98f, 1f);
        private static readonly Color TextShade = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color WorkingColour = new Color(0.16f, 0.6f, 0.3f);
        private static readonly Color FlyingColour = new Color(0.18f, 0.42f, 0.85f);
        private static readonly Color ChargingColour = new Color(0.8f, 0.52f, 0.1f);
        private static readonly Color IdleColour = new Color(0.36f, 0.4f, 0.48f);

        private sealed class Chip
        {
            public Drone Drone;
            public Button Button;
            public RectTransform Fill;
            public Image FillImage;
            public Text Word;
            public int Column;
            public int Row;
            public float ShownBattery = -1f;
            public DroneState ShownState;
            public bool Stated;
        }

        private readonly List<Chip> _chips = new List<Chip>();
        private readonly string[] _words = new string[4];
        private LevelContext _context;
        private RectTransform _panel;
        private DroneDefinition[] _kinds;
        private Button[] _cards;
        private float[] _columnX;
        private float _columnWidth;
        private int[] _rows;

        /// <summary>The strip over the Drones panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;

        /// <summary>How far the strip reaches over the top of the Drones panel; zero while no drone is out.</summary>
        public float Height { get; private set; }

        /// <summary>How many drones have a chip (for checks).</summary>
        public int Count => _chips.Count;

        /// <summary>
        /// Builds the (still empty) strip on top of the Drones panel. The kinds, their cards and the left edge of each
        /// card's column come from the panel, so a chip always stands over the card of its kind.
        /// </summary>
        public void Initialize(LevelContext context, RectTransform dronesPanel, DroneDefinition[] kinds, Button[] cards, float[] columnX, float columnWidth)
        {
            _context = context;
            _kinds = kinds;
            _cards = cards;
            _columnX = columnX;
            _columnWidth = columnWidth;
            _rows = new int[kinds.Length];
            _words[(int)DroneState.Idle] = Loc.Get("drone.state.idle");
            _words[(int)DroneState.Flying] = Loc.Get("drone.state.flying");
            _words[(int)DroneState.Working] = Loc.Get("drone.state.working");
            _words[(int)DroneState.Charging] = Loc.Get("drone.state.charging");

            _panel = new GameObject("DroneStatus", typeof(RectTransform)).GetComponent<RectTransform>();
            _panel.SetParent(dronesPanel, false);
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = new Vector2(0f, PanelGap);
            UiKit.CreatePanel(_panel, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Backdrop);
            _panel.gameObject.SetActive(false);
            _context.DroneLaunched.Subscribe(OnLaunched);
        }

        private void OnDestroy()
        {
            _context?.DroneLaunched.Unsubscribe(OnLaunched);
        }

        /// <summary>True for a chip of the strip, so the Drones panel keeps the stick's focus on it.</summary>
        public bool IsChip(GameObject candidate)
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                if (_chips[i].Button.gameObject == candidate)
                {
                    return candidate.activeInHierarchy;
                }
            }

            return false;
        }

        private void OnLaunched(Drone drone)
        {
            int column = System.Array.IndexOf(_kinds, drone.Definition);
            if (column < 0)
            {
                return;
            }

            Chip chip = CreateChip(drone, column, _rows[column]);
            _rows[column]++;
            _chips.Add(chip);
            Resize();
            Link();
            Show(chip);
        }

        private Chip CreateChip(Drone drone, int column, int row)
        {
            var go = new GameObject("DroneChip", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_panel, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            // The first drone of a kind sits nearest its card; the next ones stack upwards.
            rect.anchoredPosition = new Vector2(_columnX[column], Padding + row * (ChipHeight + ChipGap));
            rect.sizeDelta = new Vector2(_columnWidth, ChipHeight);
            var frame = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = frame;
            ColorBlock colours = button.colors;
            colours.normalColor = FrameColour;
            colours.highlightedColor = FrameChosen;
            colours.selectedColor = FrameChosen;
            colours.pressedColor = Color.white;
            button.colors = colours;

            Image back = Solid(rect, "Back", Empty, Frame);
            Image fill = Solid(back.rectTransform, "Battery", IdleColour, 0f);

            Text word = UiKit.CreateFixedText(rect, string.Empty, UiKit.ReadableSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(_columnWidth - TextInset * 2f, ChipHeight));
            UiKit.KeepReadable(word);
            word.color = TextColour;
            word.raycastTarget = false;
            word.horizontalOverflow = HorizontalWrapMode.Overflow;
            word.verticalOverflow = VerticalWrapMode.Overflow;
            word.gameObject.AddComponent<Shadow>().effectColor = TextShade;

            var chip = new Chip { Drone = drone, Button = button, Fill = fill.rectTransform, FillImage = fill, Word = word, Column = column, Row = row };
            button.onClick.AddListener(() => OnChosen(chip));
            return chip;
        }

        // A flat block of one colour (no panel art: a battery bar is read by its length, so its edge must be exact).
        private static Image Solid(RectTransform parent, string name, Color colour, float inset)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = colour;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return image;
        }

        // The strip is as tall as the largest crew of one kind.
        private void Resize()
        {
            int rows = 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                rows = Mathf.Max(rows, _rows[i]);
            }

            Height = rows > 0 ? PanelGap + Padding * 2f + rows * ChipHeight + (rows - 1) * ChipGap : 0f;
            _panel.sizeDelta = new Vector2(0f, Mathf.Max(0f, Height - PanelGap));
            _panel.gameObject.SetActive(rows > 0);
        }

        // The stick walks from a card up its column and sideways to the chip beside it at the same height.
        private void Link()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                Chip chip = _chips[i];
                Chip below = Find(chip.Column, chip.Row - 1);
                chip.Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnDown = below != null ? below.Button : _cards[chip.Column],
                    selectOnUp = ButtonOf(Find(chip.Column, chip.Row + 1)),
                    selectOnLeft = ButtonOf(Beside(chip, -1)),
                    selectOnRight = ButtonOf(Beside(chip, 1)),
                };
            }

            for (int column = 0; column < _cards.Length; column++)
            {
                Navigation navigation = _cards[column].navigation;
                navigation.selectOnUp = ButtonOf(Find(column, 0));
                _cards[column].navigation = navigation;
            }
        }

        private static Button ButtonOf(Chip chip) => chip != null ? chip.Button : null;

        private Chip Find(int column, int row)
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                if (_chips[i].Column == column && _chips[i].Row == row)
                {
                    return _chips[i];
                }
            }

            return null;
        }

        // The nearest column that way with a chip, at the same height or the highest it has under it.
        private Chip Beside(Chip chip, int direction)
        {
            for (int column = chip.Column + direction; column >= 0 && column < _rows.Length; column += direction)
            {
                if (_rows[column] > 0)
                {
                    return Find(column, Mathf.Min(chip.Row, _rows[column] - 1));
                }
            }

            return null;
        }

        private void OnChosen(Chip chip)
        {
            if (chip.Drone != null)
            {
                _context.CameraFocusRequested.Raise(chip.Drone.Position);
            }

            // A mouse click must not leave the focus on the chip: the next key press would choose it again.
            if (!_context.BaseMenuOpen && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Update()
        {
            if (_chips.Count == 0 || !_panel.gameObject.activeInHierarchy)
            {
                return;
            }

            float pulse = 1f - ChargePulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * ChargePulseRate));
            for (int i = 0; i < _chips.Count; i++)
            {
                Chip chip = _chips[i];
                Show(chip);
                if (chip.ShownState == DroneState.Charging)
                {
                    chip.FillImage.color = ChargingColour * pulse + new Color(0f, 0f, 0f, 1f - pulse);
                }
            }
        }

        // Written only when something moved: the word and colour on a change of state, the bar on a change of battery.
        private static void ShowBattery(Chip chip, float battery)
        {
            // Full and empty are shown exactly; in between a step smaller than the eye sees is skipped.
            bool edge = battery <= 0f || battery >= 1f;
            if (edge ? Mathf.Approximately(battery, chip.ShownBattery) : Mathf.Abs(battery - chip.ShownBattery) < BatteryStep)
            {
                return;
            }

            chip.ShownBattery = battery;
            chip.Fill.anchorMax = new Vector2(Mathf.Clamp01(battery), 1f);
        }

        private void Show(Chip chip)
        {
            if (chip.Drone == null)
            {
                return;
            }

            ShowBattery(chip, chip.Drone.Battery);
            DroneState state = chip.Drone.State;
            if (chip.Stated && state == chip.ShownState)
            {
                return;
            }

            chip.Stated = true;
            chip.ShownState = state;
            chip.Word.text = _words[(int)state];
            chip.FillImage.color = ColourOf(state);
        }

        /// <summary>The colour that stands for a state on the strip.</summary>
        public static Color ColourOf(DroneState state)
        {
            switch (state)
            {
                case DroneState.Working:
                    return WorkingColour;
                case DroneState.Flying:
                    return FlyingColour;
                case DroneState.Charging:
                    return ChargingColour;
                default:
                    return IdleColour;
            }
        }
    }
}
