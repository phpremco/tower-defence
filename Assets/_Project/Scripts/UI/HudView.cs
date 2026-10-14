using System.Text;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Placeholder HUD built in code: wave and base at the top, money bottom left, speed and pause icons top right
    /// and the tower bar at the bottom (locked and unaffordable towers are greyed out). Key and gamepad help is not
    /// shown here; it lives in the options menu. It only reads LevelContext, except for passing on clicks.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const int FontSize = 34;
        private const int SmallFontSize = 26;
        private const float Margin = 24f;
        private const float LineHeight = 52f;
        private const float BarButtonWidth = 250f;
        private const float BarButtonHeight = 110f;
        private const float BarSpacing = 14f;
        private const float IconSize = 76f;
        private const float TopBarHeight = 110f;
        private const float BottomBarHeight = 190f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Dim = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);
        private static readonly Color NormalFill = new Color(0.1f, 0.18f, 0.3f, 0.92f);
        private static readonly Color SelectedFill = new Color(0.22f, 0.5f, 0.9f, 0.96f);
        private static readonly Color LockedFill = new Color(0.07f, 0.07f, 0.09f, 0.9f);

        private readonly StringBuilder _builder = new StringBuilder(128);
        private readonly System.Collections.Generic.List<RaycastResult> _hits = new System.Collections.Generic.List<RaycastResult>();
        private PointerEventData _pointerData;
        private Game.Platform.IInputProvider _input;

        private LevelContext _context;
        private TimeController _time;
        private Text _waveText;
        private Text _baseText;
        private Text _moneyText;
        private Text _statusText;
        private Font _font;
        private TowerButton[] _towerButtons;

        private struct TowerButton
        {
            public TowerDefinition Tower;
            public Image Fill;
            public Text Label;
        }

        public void Initialize(LevelContext context, TimeController time, Camera camera)
        {
            _context = context;
            _time = time;
            _input = Game.Core.Services.Get<Game.Platform.IInputProvider>();
            _font = UiKit.DefaultFont;
            UiKit.EnsureEventSystem();

            RectTransform safe = UiKit.CreateRoot(transform, false);
            AddBackdrops(safe);
            BuildTexts(safe);
            BuildTowerBar(safe);
            BuildIcons(safe);
            gameObject.AddComponent<TowerMenuView>().Initialize(context, safe, camera, _font);
            gameObject.AddComponent<ResultView>().Initialize(context, safe);
            gameObject.AddComponent<PauseMenuView>().Initialize(context, safe, time, Game.Core.Services.Get<Game.Platform.IInputProvider>());

            _context.Changed.Subscribe(Refresh);
            _context.Wallet.Changed += OnMoneyChanged;
            _context.BaseHealth.Changed += OnBaseChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Changed.Unsubscribe(Refresh);
            _context.Wallet.Changed -= OnMoneyChanged;
            _context.BaseHealth.Changed -= OnBaseChanged;
        }

        // Only an interactive element (a button) under the pointer blocks the field. Plain text or backdrops never do.
        private void Update()
        {
            EventSystem current = EventSystem.current;
            if (current == null || _input == null)
            {
                _context.PointerOverUi = false;
                return;
            }

            if (_pointerData == null)
            {
                _pointerData = new PointerEventData(current);
            }

            _pointerData.position = _input.PointerPosition;
            _hits.Clear();
            current.RaycastAll(_pointerData, _hits);

            bool blocked = false;
            for (int i = 0; i < _hits.Count && !blocked; i++)
            {
                blocked = _hits[i].gameObject.GetComponentInParent<Selectable>() != null;
            }

            _context.PointerOverUi = blocked;
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void OnBaseChanged(float _) => Refresh();

        // Dark translucent bars behind the text so it stays readable over any terrain.
        private static void AddBackdrops(RectTransform safe)
        {
            var tint = new Color(0.05f, 0.09f, 0.16f, 0.7f);
            UiKit.CreatePanel(safe, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopBarHeight), Vector2.zero, tint);
            UiKit.CreatePanel(safe, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BottomBarHeight), tint);
        }

        private void BuildTexts(RectTransform safe)
        {
            _waveText = CreateText(safe, "Wave", TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -Margin), FontSize);
            _baseText = CreateText(safe, "Base", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, -Margin), FontSize);
            _moneyText = CreateText(safe, "Money", TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(Margin, Margin), FontSize);
            _statusText = CreateText(safe, "Status", TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, Margin), FontSize);
        }

        // Speed, pause and (in the test mode) skip wave. The keys remain as shortcuts.
        private void BuildIcons(RectTransform safe)
        {
            var go = new GameObject("Icons", typeof(RectTransform));
            var row = go.GetComponent<RectTransform>();
            row.SetParent(safe, false);
            row.anchorMin = new Vector2(1f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(1f, 1f);
            row.anchoredPosition = new Vector2(-Margin, -Margin);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = BarSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddIcon(row, IconType.Speed, _time.CycleSpeed);
            AddIcon(row, IconType.Pause, _time.TogglePause);
            if (_context.SkipWaveAllowed)
            {
                AddIcon(row, IconType.SkipWave, () => _context.SkipWaveRequested.Raise());
            }
        }

        private void AddIcon(RectTransform row, IconType type, UnityEngine.Events.UnityAction action)
        {
            Button button = UiKit.CreateIconButton(row, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private void BuildTowerBar(RectTransform safe)
        {
            var barGo = new GameObject("TowerBar", typeof(RectTransform));
            var bar = barGo.GetComponent<RectTransform>();
            bar.SetParent(safe, false);
            bar.anchorMin = new Vector2(0.5f, 0f);
            bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, Margin + LineHeight);

            var layout = barGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = BarSpacing;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            var fitter = barGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            int count = _context.AllTowers.Count;
            _towerButtons = new TowerButton[count];
            for (int i = 0; i < count; i++)
            {
                TowerDefinition tower = _context.AllTowers[i];
                Button button = UiKit.CreateButton(bar, tower.DisplayName, () => OnTowerClicked(tower));
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var element = button.GetComponent<LayoutElement>();
                element.preferredWidth = BarButtonWidth;
                element.preferredHeight = BarButtonHeight;
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(BarButtonWidth, BarButtonHeight);

                Text label = button.GetComponentInChildren<Text>();
                label.resizeTextMaxSize = SmallFontSize;
                _towerButtons[i] = new TowerButton { Tower = tower, Fill = button.GetComponent<Image>(), Label = label };
            }
        }

        private void OnTowerClicked(TowerDefinition tower)
        {
            _context.SelectTower(tower);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private Text CreateText(RectTransform parent, string objectName, TextAnchor alignment, Vector2 anchor, Vector2 offset, int size)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(ReferenceWidth * 0.5f, LineHeight * 2f);

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void Refresh()
        {
            _waveText.text = WaveLine();
            _baseText.text = $"Base {Mathf.CeilToInt(_context.BaseHealth.Current)}/{Mathf.CeilToInt(_context.BaseHealth.Max)}";
            _moneyText.text = $"Money {_context.Wallet.Money}";
            _statusText.text = _context.Status;
            RefreshTowerBar();
        }

        private void RefreshTowerBar()
        {
            for (int i = 0; i < _towerButtons.Length; i++)
            {
                TowerButton entry = _towerButtons[i];
                bool unlocked = _context.IsUnlocked(entry.Tower);
                bool affordable = _context.Wallet.CanAfford(entry.Tower.BuildCost);
                bool selected = entry.Tower == _context.SelectedTower;

                entry.Fill.color = !unlocked ? LockedFill : selected ? SelectedFill : NormalFill;
                entry.Label.color = !unlocked ? Dim : affordable ? TextColor : Unaffordable;
                entry.Label.text = unlocked ? $"{entry.Tower.DisplayName}\n{entry.Tower.BuildCost}" : $"{entry.Tower.DisplayName}\nLocked";
            }
        }

        private string WaveLine()
        {
            _builder.Clear();
            _builder.Append("Wave ").Append(_context.WaveNumber).Append('/').Append(_context.WaveCount);
            if (_context.NextWaveCountdown > 0f)
            {
                _builder.Append("  next in ").Append(Mathf.CeilToInt(_context.NextWaveCountdown)).Append('s');
            }

            if (_time != null && !Mathf.Approximately(_time.CurrentSpeed, 1f) && !_context.Paused)
            {
                _builder.Append("  speed x").Append(_time.CurrentSpeed);
            }

            return _builder.ToString();
        }
    }
}



