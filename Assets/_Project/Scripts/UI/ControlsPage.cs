using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The controls page: every rebindable action with its gamepad and keyboard glyph, built from the live bindings and
    /// the pad family in the player's hands (Xbox, PlayStation or Nintendo). Picking a cell waits for a new button or
    /// key on that device (Esc cancels); a control already in use swaps with the old one, so nothing is left unbound.
    /// "Reset to defaults" drops every override. Overrides are kept in the settings file, written when the options close.
    /// The fixed controls (cursor, camera, zoom) are listed under the table with their glyphs, not rebindable.
    /// </summary>
    public sealed class ControlsPage : MonoBehaviour
    {
        private const int TextSize = 28;
        private const float Top = -140f;
        private const float RowHeight = 40f;
        private const float CellHeight = 36f;
        private const float GlyphHeight = 32f;
        private const float LabelLeft = -860f;
        private const float LabelWidth = 700f;
        private const float PadCentre = 110f;
        private const float KeyCentre = 560f;
        private const float CellWidth = 380f;
        private const float BottomButtonsY = 40f;
        private const float BottomButtonWidth = 520f;
        private const float BottomButtonGap = 30f;
        private const float HintY = 130f;
        private const float StartDelaySeconds = 0.15f;
        private const float EndDelaySeconds = 0.3f;
        private const float RebindTimeoutSeconds = 8f;
        private static readonly Color HeaderColour = new Color(1f, 0.85f, 0.4f);
        private static readonly Color ActionColour = new Color(0.75f, 0.85f, 1f);
        private static readonly Color HintColour = new Color(0.7f, 0.76f, 0.85f);

        // The order of the table: calling the next wave sits with the clock controls.
        private static readonly GameAction[] Order =
        {
            GameAction.Confirm, GameAction.Cancel, GameAction.Pause, GameAction.Speed, GameAction.CallWave,
            GameAction.NextTower, GameAction.PreviousTower, GameAction.Upgrade, GameAction.Repair, GameAction.Sell, GameAction.HeroPost,
            GameAction.HeroAbility1, GameAction.HeroAbility2, GameAction.Overview, GameAction.Codex,
        };

        private readonly List<Cell> _cells = new List<Cell>();
        private readonly List<FixedRow> _fixedRows = new List<FixedRow>();
        private IInputBindings _bindings;
        private IInputProvider _input;
        private SettingsService _settings;
        private Button _back;
        private float _quietUntil;

        /// <summary>The first cell, for the gamepad focus when the page opens.</summary>
        public Selectable First => _cells.Count > 0 ? _cells[0].Button : _back;

        /// <summary>True while a rebind waits for a key, and a moment after, so the key that ended it does nothing else.</summary>
        public bool Busy => (_bindings != null && _bindings.IsRebinding) || Time.unscaledTime < _quietUntil;

        /// <summary>Builds the page over <paramref name="root"/>, hidden; <paramref name="back"/> leaves it.</summary>
        public static ControlsPage Create(RectTransform root, UnityEngine.Events.UnityAction back)
        {
            var go = new GameObject("Controls", typeof(RectTransform));
            RectTransform page = go.GetComponent<RectTransform>();
            page.SetParent(root, false);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;
            var controls = go.AddComponent<ControlsPage>();
            controls.Build(page, back);
            go.SetActive(false);
            return controls;
        }

        private void Build(RectTransform page, UnityEngine.Events.UnityAction back)
        {
            Services.TryGet(out _bindings);
            Services.TryGet(out _input);
            Services.TryGet(out _settings);
            string hero = HeroNaming.Campaign();

            float y = Top;
            Header(page, y);
            foreach (GameAction action in Order)
            {
                y -= RowHeight;
                Text label = Label(page, string.Format(ActionName(action), hero), y, ActionColour);
                label.alignment = TextAnchor.MiddleLeft;
                _cells.Add(CreateCell(page, action, InputDeviceKind.Gamepad, PadCentre, y));
                _cells.Add(CreateCell(page, action, InputDeviceKind.KeyboardMouse, KeyCentre, y));
            }

            y -= RowHeight;
            _fixedRows.Add(CreateFixedRow(page, Loc.Get("controls.cursor"), new[] { "<Gamepad>/leftStick", "<Gamepad>/dpad" }, Loc.Get("controls.cursor_keys"), y));
            y -= RowHeight;
            _fixedRows.Add(CreateFixedRow(page, Loc.Get("controls.camera"), new[] { "<Gamepad>/rightStick" }, Loc.Get("controls.camera_keys"), y));
            y -= RowHeight;
            _fixedRows.Add(CreateFixedRow(page, Loc.Get("controls.zoom"), new[] { "<Gamepad>/leftStickPress", "<Gamepad>/rightStickPress" }, Loc.Get("controls.zoom_keys"), y));

            Text hint = UiKit.CreateFixedText(page, Loc.Get("controls.hint"), UiKit.ReadableSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, HintY), new Vector2(LabelWidth * 2f, RowHeight));
            hint.color = HintColour;
            Button reset = BottomButton(page, Loc.Get("controls.reset"), ResetBindings, -1f);
            _back = BottomButton(page, Loc.Get("common.back"), back, 1f);
            Link(reset, _back);
            Refresh();

            if (_bindings != null)
            {
                _bindings.BindingsChanged += Refresh;
            }

            if (_input != null)
            {
                _input.ActiveDeviceChanged += Refresh;
            }
        }

        private void OnDestroy()
        {
            if (_bindings != null)
            {
                _bindings.CancelRebind();
                _bindings.BindingsChanged -= Refresh;
            }

            if (_input != null)
            {
                _input.ActiveDeviceChanged -= Refresh;
            }

            SetMenuInput(true);
        }

        private void OnDisable()
        {
            _bindings?.CancelRebind();
        }

        private void Header(RectTransform page, float y)
        {
            Label(page, Loc.Get("controls.header_action"), y, HeaderColour).alignment = TextAnchor.MiddleLeft;
            Centred(page, Loc.Get("controls.header_pad"), PadCentre, y, HeaderColour);
            Centred(page, Loc.Get("controls.header_keys"), KeyCentre, y, HeaderColour);
        }

        private static Text Label(RectTransform page, string text, float y, Color colour)
        {
            Text label = UiKit.CreateFixedText(page, text, TextSize, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(LabelLeft + LabelWidth * 0.5f, y), new Vector2(LabelWidth, RowHeight));
            label.color = colour;
            return label;
        }

        private static Text Centred(RectTransform page, string text, float x, float y, Color colour)
        {
            Text label = UiKit.CreateFixedText(page, text, TextSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(CellWidth, RowHeight));
            label.color = colour;
            return label;
        }

        private Cell CreateCell(RectTransform page, GameAction action, InputDeviceKind device, float x, float y)
        {
            Button button = UiKit.CreateButton(page, string.Empty, null);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y - (RowHeight - CellHeight) * 0.5f);
            rect.sizeDelta = new Vector2(CellWidth, CellHeight);
            Text text = button.GetComponentInChildren<Text>();
            text.fontSize = TextSize;
            text.resizeTextMaxSize = TextSize;
            Image glyph = PromptGlyphs.CreateImage(rect, GlyphHeight);
            var cell = new Cell(action, device, button, text, glyph);
            button.onClick.AddListener(() => OnCellPressed(cell));
            return cell;
        }

        private FixedRow CreateFixedRow(RectTransform page, string name, string[] padPaths, string keys, float y)
        {
            Label(page, name, y, ActionColour);
            var holder = new GameObject("PadGlyphs", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(page, false);
            holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(PadCentre, y);
            holder.sizeDelta = new Vector2(CellWidth, RowHeight);
            var layout = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = BottomButtonGap * 0.3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var images = new Image[padPaths.Length];
            for (int i = 0; i < padPaths.Length; i++)
            {
                images[i] = PromptGlyphs.CreateImage(holder, GlyphHeight);
                images[i].gameObject.AddComponent<LayoutElement>();
            }

            Centred(page, keys, KeyCentre, y, Color.white);
            return new FixedRow(padPaths, images);
        }

        private static Button BottomButton(RectTransform page, string label, UnityEngine.Events.UnityAction action, float side)
        {
            Button button = UiKit.CreateButton(page, label, action);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(side * (BottomButtonWidth + BottomButtonGap) * 0.5f, BottomButtonsY);
            rect.sizeDelta = new Vector2(BottomButtonWidth, UiKit.ButtonHeight);
            return button;
        }

        // The automatic navigation finds the grid of cells; the two bottom buttons are linked to each other by hand.
        private void Link(Button reset, Button back)
        {
            Selectable lastPad = null;
            Selectable lastKey = null;
            foreach (Cell cell in _cells)
            {
                if (cell.Button.interactable)
                {
                    if (cell.Device == InputDeviceKind.Gamepad)
                    {
                        lastPad = cell.Button;
                    }
                    else
                    {
                        lastKey = cell.Button;
                    }
                }
            }

            reset.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = back, selectOnUp = lastPad != null ? lastPad : lastKey };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = reset, selectOnUp = lastKey != null ? lastKey : lastPad };
        }

        private void Refresh()
        {
            PadLayout layout = _bindings != null ? _bindings.ActivePadLayout : PadLayout.Xbox;
            foreach (Cell cell in _cells)
            {
                bool bound = _bindings != null && _bindings.CanRebind(cell.Action, cell.Device);
                cell.Button.interactable = bound;
                Sprite glyph = bound ? PromptGlyphs.For(cell.Action, cell.Device, layout) : null;
                PromptGlyphs.Show(cell.Glyph, glyph);
                string printed = bound && _input != null ? _input.PromptFor(cell.Action, cell.Device) : string.Empty;
                cell.Text.text = glyph != null ? string.Empty : printed.Length > 0 ? printed : Loc.Get("controls.none");
            }

            GlyphFamily family = GlyphFamilies.For(InputDeviceKind.Gamepad, layout);
            foreach (FixedRow row in _fixedRows)
            {
                for (int i = 0; i < row.Paths.Length; i++)
                {
                    PromptGlyphs.Show(row.Images[i], PromptGlyphs.ForControl(family, row.Paths[i], string.Empty));
                }
            }
        }

        private void OnCellPressed(Cell cell)
        {
            if (_bindings == null || Busy || !_bindings.CanRebind(cell.Action, cell.Device))
            {
                return;
            }

            StartCoroutine(Rebind(cell));
        }

        // The menu stops listening while the game waits for the key, so the press that picks it does not also click.
        private IEnumerator Rebind(Cell cell)
        {
            _quietUntil = float.MaxValue;
            SetMenuInput(false);
            PromptGlyphs.Show(cell.Glyph, null);
            cell.Text.text = PromptText.ForText(cell.Device == InputDeviceKind.Gamepad ? Loc.Get("controls.press_button") : Loc.Get("controls.press_key"));
            yield return new WaitForSecondsRealtime(StartDelaySeconds);

            bool done = false;
            if (_bindings.StartRebind(cell.Action, cell.Device, _ => done = true))
            {
                // The wait ends on its own after a while (timed here in real time; the Input System's own timeout misfired
                // in the unfocused editor), so a player who walked away is not stuck on the page.
                float giveUp = Time.unscaledTime + RebindTimeoutSeconds;
                while (!done)
                {
                    if (Time.unscaledTime > giveUp)
                    {
                        _bindings.CancelRebind();
                    }

                    yield return null;
                }

                _settings?.SetBindingOverrides(_bindings.SaveOverrides());
            }

            Refresh();
            _quietUntil = Time.unscaledTime + EndDelaySeconds;
            yield return new WaitForSecondsRealtime(EndDelaySeconds);
            SetMenuInput(true);
            UiKit.Select(cell.Button);
        }

        private void ResetBindings()
        {
            if (_bindings == null || Busy)
            {
                return;
            }

            _bindings.ResetBindings();
            _settings?.SetBindingOverrides(string.Empty);
        }

        private static void SetMenuInput(bool on)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = on;
            }
        }

        private static string ActionName(GameAction action)
        {
            switch (action)
            {
                case GameAction.Confirm: return Loc.Get("controls.action.confirm");
                case GameAction.Cancel: return Loc.Get("controls.action.cancel");
                case GameAction.Pause: return Loc.Get("controls.action.pause");
                case GameAction.Speed: return Loc.Get("controls.action.speed");
                case GameAction.NextTower: return Loc.Get("controls.action.next_tower");
                case GameAction.PreviousTower: return Loc.Get("controls.action.previous_tower");
                case GameAction.CallWave: return Loc.Get("controls.action.call_wave");
                case GameAction.Upgrade: return Loc.Get("controls.action.upgrade");
                case GameAction.Repair: return Loc.Get("controls.action.repair");
                case GameAction.Sell: return Loc.Get("controls.action.sell");
                case GameAction.HeroPost: return Loc.Get("controls.action.hero_post");
                case GameAction.HeroAbility1: return Loc.Get("controls.action.hero_ability1");
                case GameAction.HeroAbility2: return Loc.Get("controls.action.hero_ability2");
                case GameAction.Overview: return Loc.Get("controls.action.overview");
                default: return Loc.Get("controls.action.codex");
            }
        }

        private sealed class Cell
        {
            public Cell(GameAction action, InputDeviceKind device, Button button, Text text, Image glyph)
            {
                Action = action;
                Device = device;
                Button = button;
                Text = text;
                Glyph = glyph;
            }

            public GameAction Action { get; }

            public InputDeviceKind Device { get; }

            public Button Button { get; }

            public Text Text { get; }

            public Image Glyph { get; }
        }

        private sealed class FixedRow
        {
            public FixedRow(string[] paths, Image[] images)
            {
                Paths = paths;
                Images = images;
            }

            public string[] Paths { get; }

            public Image[] Images { get; }
        }
    }
}
