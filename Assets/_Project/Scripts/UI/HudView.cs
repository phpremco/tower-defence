using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD built in code. A slim strip at the top holds the stats (base, wave) and the speed, pause and
    /// skip icons; the card bar sits at the very bottom with the money and the status line just above it. The
    /// drones are bought from the base's own menu. Key and gamepad help lives in the options menu. It only reads
    /// LevelContext, except for passing on clicks.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const int FontSize = 30;
        private const int SmallFontSize = 24;
        private const float Margin = 20f;
        private const float LineHeight = 44f;
        private const float TopBarHeight = 72f;
        private const float TopTextY = -16f;
        private const float HeroTextGap = 40f;
        private const float BottomBarHeight = 156f;
        private const float BarBottom = 8f;
        private const float AboveBarY = BottomBarHeight + 6f;
        private const float BarButtonWidth = 136f;
        private const float BarButtonHeight = 140f;
        private const float BarSpacing = 10f;
        private const float MinBarButtonWidth = 100f;
        private const float IconSize = 56f;
        private const float CardInset = 5f;
        private const float CardPriceHeight = 28f;
        private const float BadgeRow = 26f;
        private const float BadgeSize = 24f;
        private const float BadgeGap = 4f;
        private static readonly Color GroundBadge = new Color(1f, 0.78f, 0.35f);
        private static readonly Color AirBadge = new Color(0.55f, 0.85f, 1f);
        private static readonly Color SlowBadge = new Color(0.75f, 1f, 1f);
        private static readonly Color LockedPicture = new Color(0.35f, 0.35f, 0.4f, 0.8f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Dim = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);
        private static readonly Color BossWarning = new Color(1f, 0.45f, 0.4f);
        private static readonly Color SpeedActive = new Color(1f, 0.85f, 0.35f);
        private static readonly Color NormalFill = new Color(0.1f, 0.18f, 0.3f, 0.92f);
        private static readonly Color SelectedFill = new Color(0.22f, 0.5f, 0.9f, 0.96f);
        private static readonly Color LockedFill = new Color(0.07f, 0.07f, 0.09f, 0.9f);
        private static readonly Color Backdrop = new Color(0.05f, 0.09f, 0.16f, 0.7f);

        private readonly StringBuilder _builder = new StringBuilder(128);
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private PointerEventData _pointerData;
        private Game.Platform.IInputProvider _input;

        private LevelContext _context;
        private TimeController _time;
        private Text _waveText;
        private Text _baseText;
        private Text _heroText;
        private int _heroSecondsShown = -1;
        private Text _moneyText;
        private Text _statusText;
        private Image _speedIcon;
        private Font _font;
        private TowerButton[] _towerButtons;
        private bool _picturesChanged;
        private PathObjectButton[] _pathObjectButtons;

        private struct TowerButton
        {
            public TowerDefinition Tower;
            public Image Fill;
            public Image Picture;
            public Text Label;
            public Image[] Badges;
        }

        private struct PathObjectButton
        {
            public PathObjectDefinition Definition;
            public Image Fill;
            public Image Picture;
            public Text Label;
        }

        public void Initialize(LevelContext context, TimeController time, Camera camera)
        {
            _context = context;
            _time = time;
            _input = Services.Get<Game.Platform.IInputProvider>();
            _font = UiKit.DefaultFont;
            UiKit.EnsureEventSystem();

            RectTransform safe = UiKit.CreateRoot(transform, false);
            AddBackdrops(safe);
            BuildTexts(safe);
            BuildTowerBar(safe);
            BuildIcons(safe);
            gameObject.AddComponent<BossMarkerView>().Initialize(context, safe, camera);
            gameObject.AddComponent<BossHealthView>().Initialize(context, safe);
            gameObject.AddComponent<BaseMenuView>().Initialize(context, safe, camera, _font);
            gameObject.AddComponent<HeroPanelView>().Initialize(context, safe, camera, _font);
            gameObject.AddComponent<HeroQuickBar>().Initialize(context, safe);
            gameObject.AddComponent<TowerMenuView>().Initialize(context, safe, camera, _font);
            // After the panels, so the first-hit warning draws over them wherever the base lies on screen.
            gameObject.AddComponent<BaseAlertView>().Initialize(context, safe, camera);
            gameObject.AddComponent<ResultView>().Initialize(context, safe);
            gameObject.AddComponent<PauseMenuView>().Initialize(context, safe, time, _input);
            gameObject.AddComponent<BriefingView>().Initialize(context, safe, time);

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
            if (_picturesChanged)
            {
                _picturesChanged = false;
                RefreshTowerBar();
                RefreshPathObjects();
            }

            RefreshHero();

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

        // A downed hero counts down in the stats strip, minute and seconds, so the player knows how long the post is bare.
        // The text is only rebuilt when the shown second changes.
        private void RefreshHero()
        {
            Game.Hero.Commander hero = _context.Hero;
            int seconds = hero != null && hero.IsDown ? Mathf.CeilToInt(hero.ReviveRemaining) : -1;
            if (seconds == _heroSecondsShown)
            {
                return;
            }

            _heroSecondsShown = seconds;
            _heroText.text = seconds < 0 ? string.Empty : $"Hero down {seconds / 60}:{seconds % 60:00}";
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void OnBaseChanged(float _) => Refresh();

        // Dark translucent strips behind the text so it stays readable over any terrain.
        private static void AddBackdrops(RectTransform safe)
        {
            UiKit.CreatePanel(safe, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopBarHeight), Vector2.zero, Backdrop);
            UiKit.CreatePanel(safe, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BottomBarHeight), Backdrop);
        }

        private void BuildTexts(RectTransform safe)
        {
            _baseText = CreateText(safe, "Base", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, TopTextY), FontSize);
            _heroText = CreateText(safe, "Hero", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, TopTextY), FontSize);
            _heroText.color = BossWarning;
            _heroText.text = string.Empty;
            _waveText = CreateText(safe, "Wave", TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TopTextY), FontSize);
            _moneyText = CreateText(safe, "Money", TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(Margin, AboveBarY), FontSize);
            _statusText = CreateText(safe, "Status", TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, AboveBarY), SmallFontSize);
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
            row.anchoredPosition = new Vector2(-Margin, -(TopBarHeight - IconSize) * 0.5f);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = BarSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button speed = AddIcon(row, IconType.Speed, _time.CycleSpeed);
            _speedIcon = speed.transform.Find("Icon").GetComponent<Image>();
            AddIcon(row, IconType.Pause, _time.TogglePause);
            if (_context.SkipWaveAllowed)
            {
                AddIcon(row, IconType.SkipWave, () => _context.SkipWaveRequested.Raise());
            }
        }

        private Button AddIcon(RectTransform row, IconType type, UnityEngine.Events.UnityAction action)
        {
            Button button = UiKit.CreateIconButton(row, IconFactory.Get(type), IconSize, () =>
            {
                action();
                EventSystem.current.SetSelectedGameObject(null);
            });
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private void BuildTowerBar(RectTransform safe)
        {
            var barGo = new GameObject("TowerBar", typeof(RectTransform));
            var bar = barGo.GetComponent<RectTransform>();
            bar.SetParent(safe, false);
            bar.anchorMin = new Vector2(0.5f, 0f);
            bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, BarBottom);

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
            // Eight towers and several path objects must fit one row, so the cards shrink as the bar fills.
            int total = count + _context.AvailablePathObjects.Count;
            float fitted = (ReferenceWidth - Margin * 2f - BarSpacing * Mathf.Max(0, total - 1)) / Mathf.Max(1, total);
            float buttonWidth = Mathf.Clamp(fitted, MinBarButtonWidth, BarButtonWidth);
            _towerButtons = new TowerButton[count];
            for (int i = 0; i < count; i++)
            {
                TowerDefinition tower = _context.AllTowers[i];
                Button button = Card(bar, buttonWidth, tower.DisplayName, () => OnTowerClicked(tower), out Image picture, out Text label);
                _towerButtons[i] = new TowerButton { Tower = tower, Fill = button.GetComponent<Image>(), Picture = picture, Label = label, Badges = Badges(button.transform, tower) };
                if (TowerModelBuilder.HasModel(tower))
                {
                    ModelIconRenderer.Request(tower, stage => TowerModelBuilder.Build(stage, tower, 0), sprite => ShowPicture(picture, sprite));
                }
            }

            IReadOnlyList<PathObjectDefinition> objects = _context.AvailablePathObjects;
            _pathObjectButtons = new PathObjectButton[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                PathObjectDefinition definition = objects[i];
                Button button = Card(bar, buttonWidth, definition.DisplayName, () => OnPathObjectClicked(definition), out Image picture, out Text label);
                _pathObjectButtons[i] = new PathObjectButton { Definition = definition, Fill = button.GetComponent<Image>(), Picture = picture, Label = label };
                // The badge says what the street weapon does: snowflake, trap, blast, wheel, buggy or net.
                PathObjectBadge.For(definition.Effect, out IconType effectIcon, out Color effectColour);
                Badge(button.transform, effectIcon, effectColour, 0);
                if (definition.Damage > 0f)
                {
                    DamageTag((RectTransform)button.transform, definition.Damage);
                }
                if (definition.Look != PathObjectLook.None)
                {
                    ModelIconRenderer.Request(definition, stage => PathObjectModels.Build(stage, definition), sprite => ShowPicture(picture, sprite));
                }
            }
        }

        // A card in three bands: a row of badges at the top, the picture in the middle and the price along the
        // bottom. The name only shows until the picture arrives (a frame or two after the HUD is built).
        private Button Card(RectTransform bar, float width, string fallback, UnityEngine.Events.UnityAction onClick, out Image picture, out Text label)
        {
            Button button = UiKit.CreateButton(bar, fallback, onClick);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = BarButtonHeight;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, BarButtonHeight);

            var pictureGo = new GameObject("Picture", typeof(RectTransform));
            pictureGo.transform.SetParent(button.transform, false);
            picture = pictureGo.AddComponent<Image>();
            picture.raycastTarget = false;
            picture.preserveAspect = true;
            picture.enabled = false;
            RectTransform pictureRect = picture.rectTransform;
            pictureRect.anchorMin = Vector2.zero;
            pictureRect.anchorMax = Vector2.one;
            pictureRect.offsetMin = new Vector2(CardInset, CardPriceHeight);
            pictureRect.offsetMax = new Vector2(-CardInset, -(CardInset + BadgeRow));

            label = button.GetComponentInChildren<Text>();
            label.resizeTextMaxSize = SmallFontSize;
            label.transform.SetAsLastSibling();
            return button;
        }

        // Small icons in the top band of a card tell what the tower hits: ground, air, and whether it slows.
        private static Image[] Badges(Transform card, TowerDefinition tower)
        {
            var badges = new List<Image>(3);
            if (!tower.IsSupport && (tower.Targets & TargetLayers.Ground) != 0)
            {
                badges.Add(Badge(card, IconType.Ground, GroundBadge, badges.Count));
            }

            if (!tower.IsSupport && (tower.Targets & TargetLayers.Air) != 0)
            {
                badges.Add(Badge(card, IconType.Air, AirBadge, badges.Count));
            }

            if (tower.SlowFactor < 1f)
            {
                badges.Add(Badge(card, IconType.Slow, SlowBadge, badges.Count));
            }

            return badges.ToArray();
        }

        // A one-shot trap is bought for one number, so the card says how hard it hits, next to the effect badge.
        private static void DamageTag(RectTransform card, float damage)
        {
            Text tag = UiKit.CreateFixedText(card, $"{damage:0} DMG", SmallFontSize, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-CardInset, -CardInset), new Vector2(90f, BadgeRow));
            tag.color = new Color(1f, 0.75f, 0.35f);
            tag.raycastTarget = false;
        }

        private static Image Badge(Transform card, IconType type, Color colour, int index)
        {
            var go = new GameObject(type.ToString(), typeof(RectTransform));
            go.transform.SetParent(card, false);
            var image = go.AddComponent<Image>();
            image.sprite = IconFactory.Get(type);
            image.color = colour;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            rect.anchoredPosition = new Vector2(CardInset + index * (BadgeSize + BadgeGap), -CardInset);
            return image;
        }

        private void ShowPicture(Image picture, Sprite sprite)
        {
            if (picture == null)
            {
                return;
            }

            picture.sprite = sprite;
            picture.enabled = true;
            // A cached picture arrives while the bar is still being built, so the refresh waits for the next frame.
            _picturesChanged = true;
        }

        // Once the picture is there the label shrinks to a price strip along the bottom of the card.
        private static void PriceStrip(Text label, bool hasPicture)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = hasPicture ? new Vector2(1f, 0f) : Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = hasPicture ? new Vector2(-4f, CardPriceHeight) : new Vector2(-4f, -(CardInset + BadgeRow));
            label.alignment = hasPicture ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
        }

        private void OnPathObjectClicked(PathObjectDefinition definition)
        {
            _context.SelectPathObject(definition);
            EventSystem.current.SetSelectedGameObject(null);
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
            rect.sizeDelta = new Vector2(ReferenceWidth * 0.5f, LineHeight);

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
            _waveText.color = _context.BossWave && _context.NextWaveCountdown <= 0f ? BossWarning : TextColor;
            _baseText.text = $"Base {Mathf.CeilToInt(_context.BaseHealth.Current)}/{Mathf.CeilToInt(_context.BaseHealth.Max)}";
            // The hero timer follows the base text on the same line, however wide the numbers get.
            _heroText.rectTransform.anchoredPosition = new Vector2(Margin + _baseText.preferredWidth + HeroTextGap, TopTextY);
            _moneyText.text = $"Money {_context.Wallet.Money}";
            _statusText.text = _context.Status;
            RefreshSpeed();
            RefreshTowerBar();
            RefreshPathObjects();
        }

        // The speed icon shows one, two or three chevrons for the current factor, and lights up when the game runs fast.
        private void RefreshSpeed()
        {
            float factor = _time == null ? 1f : Mathf.Max(1f, _time.CurrentSpeed);
            bool fast = factor > 1f;
            _speedIcon.sprite = IconFactory.Get(factor >= 3f ? IconType.SpeedTriple : fast ? IconType.SpeedDouble : IconType.Speed);
            _speedIcon.color = fast ? SpeedActive : Color.white;
        }

        private void RefreshTowerBar()
        {
            for (int i = 0; i < _towerButtons.Length; i++)
            {
                TowerButton entry = _towerButtons[i];
                bool unlocked = _context.IsUnlocked(entry.Tower);
                bool affordable = _context.Wallet.CanAfford(entry.Tower.BuildCost);
                bool selected = entry.Tower == _context.SelectedTower && _context.SelectedPathObject == null;

                bool pictured = entry.Picture.enabled;
                entry.Fill.color = !unlocked ? LockedFill : selected ? SelectedFill : NormalFill;
                entry.Picture.color = !unlocked ? LockedPicture : Color.white;
                foreach (Image badge in entry.Badges)
                {
                    badge.enabled = unlocked;
                }

                entry.Label.color = !unlocked ? Dim : affordable ? TextColor : Unaffordable;
                string price = unlocked ? entry.Tower.BuildCost.ToString() : "Locked";
                entry.Label.text = pictured ? price : $"{entry.Tower.DisplayName}\n{price}";
                PriceStrip(entry.Label, pictured);
            }
        }

        private void RefreshPathObjects()
        {
            for (int i = 0; i < _pathObjectButtons.Length; i++)
            {
                PathObjectButton entry = _pathObjectButtons[i];
                bool affordable = _context.Wallet.CanAfford(entry.Definition.Cost);
                bool selected = entry.Definition == _context.SelectedPathObject;

                bool pictured = entry.Picture.enabled;
                entry.Fill.color = selected ? SelectedFill : NormalFill;
                entry.Label.color = affordable ? TextColor : Unaffordable;
                entry.Label.text = pictured ? entry.Definition.Cost.ToString() : $"{entry.Definition.DisplayName}\n{entry.Definition.Cost}";
                PriceStrip(entry.Label, pictured);
            }
        }

        private string WaveLine()
        {
            _builder.Clear();
            _builder.Append("Wave ").Append(_context.WaveNumber).Append('/').Append(_context.WaveCount);
            if (_context.BossWave && _context.NextWaveCountdown <= 0f)
            {
                _builder.Append("  BOSS");
            }

            if (_context.NextWaveCountdown > 0f)
            {
                _builder.Append("  next in ").Append(Mathf.CeilToInt(_context.NextWaveCountdown)).Append('s');
            }

            return _builder.ToString();
        }
    }
}
