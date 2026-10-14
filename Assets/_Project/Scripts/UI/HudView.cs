using System.Text;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Placeholder HUD built in code: wave and base at the top, money bottom left, tower bar bottom centre.
    /// It only reads LevelContext and never changes gameplay.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const int FontSize = 34;
        private const int BigFontSize = 44;
        private const float Margin = 24f;
        private const float LineHeight = 52f;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);

        private readonly StringBuilder _builder = new StringBuilder(128);

        private LevelContext _context;
        private TimeController _time;
        private Text _waveText;
        private Text _baseText;
        private Text _moneyText;
        private Text _towerText;
        private Text _hoverText;
        private Text _outcomeText;
        private Font _font;

        public void Initialize(LevelContext context, TimeController time)
        {
            _context = context;
            _time = time;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();

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

        private void OnMoneyChanged(int _) => Refresh();

        private void OnBaseChanged(float _) => Refresh();

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            // The safe area keeps the HUD readable on TVs and handhelds with overscan or notches.
            var safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasGo.transform, false);
            Rect area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;

            _waveText = CreateText(safe, "Wave", TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -Margin), FontSize);
            _baseText = CreateText(safe, "Base", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, -Margin), FontSize);
            _moneyText = CreateText(safe, "Money", TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(Margin, Margin), FontSize);
            _towerText = CreateText(safe, "Tower", TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, Margin + LineHeight), FontSize);
            _hoverText = CreateText(safe, "Hover", TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, Margin), FontSize);
            _outcomeText = CreateText(safe, "Outcome", TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, BigFontSize);
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void Refresh()
        {
            _waveText.text = WaveLine();
            _baseText.text = $"Base {Mathf.CeilToInt(_context.BaseHealth.Current)}/{Mathf.CeilToInt(_context.BaseHealth.Max)}";
            _moneyText.text = $"Money {_context.Wallet.Money}";
            _towerText.text = SelectedTowerLine();
            _hoverText.text = HoverLine();
            _outcomeText.text = OutcomeLine();
        }

        private string WaveLine()
        {
            _builder.Clear();
            _builder.Append("Wave ").Append(_context.WaveNumber).Append('/').Append(_context.WaveCount);
            if (_context.NextWaveCountdown > 0f)
            {
                _builder.Append("  next in ").Append(Mathf.CeilToInt(_context.NextWaveCountdown)).Append('s');
            }

            if (_time != null && !Mathf.Approximately(_time.CurrentSpeed, 1f))
            {
                _builder.Append("  speed x").Append(_time.CurrentSpeed);
            }

            return _builder.ToString();
        }

        private string SelectedTowerLine()
        {
            TowerDefinition tower = _context.SelectedTower;
            return tower == null ? string.Empty : $"Selected: {tower.DisplayName} ({tower.BuildCost})";
        }

        private string HoverLine()
        {
            Tower tower = _context.HoveredTower;
            if (tower == null)
            {
                return "Empty slot: confirm to build";
            }

            return tower.CanUpgrade
                ? $"{tower.Definition.DisplayName} {tower.LevelIndex + 1}/{TowerDefinition.LevelCount}: confirm to upgrade ({tower.NextUpgradeCost})"
                : $"{tower.Definition.DisplayName} {tower.LevelIndex + 1}/{TowerDefinition.LevelCount}: max level";
        }

        private string WonLine(LevelResult result)
        {
            _builder.Clear();
            _builder.Append("Victory\n");
            _builder.Append("Research points +").Append(result.ResearchPoints);
            if (result.PowerCurrency > 0)
            {
                _builder.Append("   Power +").Append(result.PowerCurrency);
            }

            if (result.NewTower != null)
            {
                string source = result.Source == UnlockSource.StolenAlienTech ? "stolen alien tech" : "intel from the home planet";
                _builder.Append("\nNew weapon: ").Append(result.NewTower.DisplayName).Append(" (").Append(source).Append(")");
                if (!string.IsNullOrEmpty(result.Story))
                {
                    _builder.Append("\n").Append(result.Story);
                }
            }

            _builder.Append("\nPress confirm to continue");
            return _builder.ToString();
        }

        private string OutcomeLine()
        {
            LevelResult result = _context.Result;
            switch (_context.Outcome)
            {
                case LevelOutcome.Won:
                    return WonLine(result);
                case LevelOutcome.Lost:
                    return "Defeat\nPress confirm to return to the map";
                default:
                    return string.Empty;
            }
        }
    }
}


