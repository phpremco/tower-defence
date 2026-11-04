using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The base. Every upgradable thing is a tile in a grid on the left that fits on one screen: the towers in one
    /// block, the commander, the base and the three drones in another. The chosen tile opens the panel on the
    /// right with a turning model, its stats and its permanent upgrades, each showing the value now and after the
    /// next level. Buying one is celebrated by <see cref="UpgradeCelebration"/>. Towers not found yet are greyed.
    /// </summary>
    public sealed class UpgradeScreen : ScreenBase
    {
        private const float Margin = 60f;
        private const float GridTop = 150f;
        private const float GridWidth = 880f;
        private const int Columns = 5;
        private const float TileSize = 164f;
        private const float TileGap = 15f;
        private const float SectionGap = 52f;
        private const float HeadingHeight = 36f;
        private const float TileIconSize = 104f;
        private const float TileProgressHeight = 8f;
        private const float TileStarSize = 34f;
        private const int TileNameSize = 18;
        private const float PanelX = 990f;
        private const float PanelTop = 150f;
        private const float PanelWidth = 870f;
        private const float PanelHeight = 800f;
        private const float PanelInset = 30f;
        private const float PreviewSize = 300f;
        private const float PreviewTop = 150f;
        private const float StatsX = 360f;
        private const float StatRowHeight = 56f;
        private const float StatLabelWidth = 180f;
        private const float BarWidth = 160f;
        private const float BarHeight = 22f;
        private const float StatValueWidth = 120f;
        private const float RowsTop = 478f;
        private const float RowHeight = 82f;
        private const float RowGap = 10f;
        private const float RowNameWidth = 265f;
        private const float RowChangeWidth = 230f;
        private const float RowCostWidth = 160f;
        private const float PipSize = 26f;
        private const float PipGap = 8f;
        private const int MaxRows = 4;
        private const int HeaderSize = 46;
        private const int SubtitleSize = 22;
        private const int StatSize = 23;
        private const int RowSize = 23;
        private const int CurrencySize = 30;
        private const float BackWidth = 420f;
        private const float BaseIconScale = 0.5f;
        private const float DroneIconScale = 2.6f;
        private const float DroneIconLift = 0.55f;
        private const string TowersHeading = "Towers";
        private const string OthersHeading = "Commander, base and drones";
        private const string ResearchLabel = "Research";
        private const string PowerLabel = "Power";
        private static readonly Color PanelTint = new Color(0.1f, 0.16f, 0.3f, 0.95f);
        private static readonly Color TileTint = new Color(0.14f, 0.22f, 0.4f, 1f);
        private static readonly Color TileChosen = new Color(0.25f, 0.5f, 0.85f, 1f);
        private static readonly Color TileLocked = new Color(0.12f, 0.14f, 0.2f, 1f);
        private static readonly Color PreviewTint = new Color(0.05f, 0.08f, 0.13f, 1f);
        private static readonly Color PipOn = new Color(0.45f, 0.95f, 1f);
        private static readonly Color PipPower = new Color(1f, 0.75f, 0.3f);
        private static readonly Color PipOff = new Color(0.12f, 0.16f, 0.26f);
        private static readonly Color Muted = new Color(0.65f, 0.72f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
        private static readonly Color Better = new Color(0.55f, 1f, 0.6f);
        private static readonly Color TooPoor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color LockedIcon = new Color(0.35f, 0.38f, 0.45f, 1f);

        private enum SubjectKind
        {
            Tower,
            Commander,
            Base,
            Drone,
        }

        private sealed class StatLine
        {
            public string Name;
            public float Base;
            public bool HasTarget;
            public UpgradeTarget Target;
            public bool Flat;
            public string Format;
            public string Suffix;
            public float Reference;
        }

        private sealed class Subject
        {
            public string Name;
            public string Description;
            public SubjectKind Kind;
            public TowerDefinition Tower;
            public DroneDefinition Drone;
            public HeroDefinition Hero;
            public Color Glow;
            public BaseArtDefinition BaseArt;
            public bool Locked;
            public readonly List<UpgradeDefinition> Upgrades = new List<UpgradeDefinition>();
            public readonly List<StatLine> Stats = new List<StatLine>();
            public Button Tile;
            public Image TileBack;
            public Image TileIcon;
            public RectTransform TileProgress;
            public Image TileStar;
        }

        private sealed class StatWidget
        {
            public GameObject Root;
            public Text Name;
            public RectTransform Fill;
            public Text Value;
        }

        private sealed class RowWidget
        {
            public Button Button;
            public Text Name;
            public Text Change;
            public Text Cost;
            public Image[] Pips;
            public UpgradeDefinition Upgrade;
        }

        private readonly List<Subject> _subjects = new List<Subject>();
        private readonly List<StatWidget> _statWidgets = new List<StatWidget>();
        private readonly List<RowWidget> _rows = new List<RowWidget>();
        private CampaignCatalog _campaign;
        private UpgradeCatalog _catalog;
        private IInputProvider _input;
        private UpgradeCelebration _celebration;
        private Text _researchText;
        private Text _powerText;
        private Text _header;
        private Text _subtitle;
        private RectTransform _previewFrame;
        private ModelPreview _preview;
        private Subject _selected;
        private float _maxTowerDamage;
        private float _maxTowerRange;
        private float _maxTowerRate;
        private float _maxTowerHealth;

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog) || !Services.TryGet(out _campaign))
            {
                Go(SceneNames.Boot);
                return;
            }

            if (Services.TryGet(out _input))
            {
                _input.Cancelled += OnCancel;
            }

            _celebration = gameObject.AddComponent<UpgradeCelebration>();
            MeasureTowerMaxima();
            CollectSubjects();

            Text title = UiKit.CreateFixedText(root, "Base upgrades", UiKit.TitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, -28f), new Vector2(900f, 90f));
            title.fontSize = 56;
            _researchText = UiKit.CreateFixedText(root, string.Empty, CurrencySize, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-Margin, -40f), new Vector2(400f, 44f));
            _powerText = UiKit.CreateFixedText(root, string.Empty, CurrencySize, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-Margin, -84f), new Vector2(400f, 44f));
            _researchText.color = PipOn;
            _powerText.color = PipPower;

            BuildGrid(root);
            BuildPanel(root);

            Button back = UiKit.CreateButton(root, "Back to map", () => Go(SceneNames.WorldMap));
            Destroy(back.GetComponent<LayoutElement>());
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0f, 0f);
            backRect.anchoredPosition = new Vector2(Margin, 40f);
            backRect.sizeDelta = new Vector2(BackWidth, UiKit.ButtonHeight);

            Select(_subjects[0]);
            RefreshAll();
            SetFirstSelected(_subjects[0].Tile);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancel;
            }
        }

        private void OnCancel() => Go(SceneNames.WorldMap);

        // ---- subjects -------------------------------------------------------------------------------------------

        // One subject per upgrade group: a tower, the commander, the base or a drone, each with the stats the
        // upgrades act on. The catalog order decides the order of the tiles.
        private void CollectSubjects()
        {
            var byGroup = new Dictionary<string, Subject>();
            var others = new List<Subject>();
            foreach (UpgradeDefinition upgrade in _catalog.Upgrades)
            {
                // Upgrades of towers out of the campaign stay saved but are not offered until their continent comes.
                if (!upgrade.IsInCampaign)
                {
                    continue;
                }

                if (!byGroup.TryGetValue(upgrade.GroupName, out Subject subject))
                {
                    subject = CreateSubject(upgrade);
                    byGroup.Add(upgrade.GroupName, subject);
                    if (subject.Kind == SubjectKind.Tower)
                    {
                        _subjects.Add(subject);
                    }
                    else
                    {
                        others.Add(subject);
                    }
                }

                subject.Upgrades.Add(upgrade);
                if (string.IsNullOrEmpty(subject.Description))
                {
                    subject.Description = upgrade.GroupDescription;
                }
            }

            // Commander and base first, then the drones, whatever order the catalog lists them in.
            others.Sort((a, b) => Order(a.Kind).CompareTo(Order(b.Kind)));
            _subjects.AddRange(others);
            foreach (Subject subject in _subjects)
            {
                AddStats(subject);
            }
        }

        private static int Order(SubjectKind kind) => kind == SubjectKind.Commander ? 0 : kind == SubjectKind.Base ? 1 : 2;

        private Subject CreateSubject(UpgradeDefinition upgrade)
        {
            var subject = new Subject { Name = upgrade.GroupName };
            if (!string.IsNullOrEmpty(upgrade.TowerId))
            {
                subject.Kind = SubjectKind.Tower;
                subject.Tower = FindTower(upgrade.TowerId);
                subject.Glow = subject.Tower != null ? subject.Tower.Color : Color.white;
            }
            else if (!string.IsNullOrEmpty(upgrade.DroneId))
            {
                subject.Kind = SubjectKind.Drone;
                subject.Drone = FindDrone(upgrade.DroneId);
                subject.Glow = subject.Drone != null ? subject.Drone.Color : Color.white;
            }
            else if (upgrade.Target == UpgradeTarget.HeroDamage || upgrade.Target == UpgradeTarget.HeroHealth)
            {
                subject.Kind = SubjectKind.Commander;
                subject.Hero = FindHero();
                subject.Glow = subject.Hero != null ? subject.Hero.Color : Gold;
            }
            else
            {
                subject.Kind = SubjectKind.Base;
                LevelDefinition first = FirstLevel();
                subject.Glow = first != null && first.Theme != null ? first.Theme.BaseColor : PipOn;
                subject.BaseArt = first != null && first.Theme != null ? first.Theme.BaseArt : null;
            }

            return subject;
        }

        private void AddStats(Subject subject)
        {
            switch (subject.Kind)
            {
                case SubjectKind.Tower when subject.Tower != null:
                {
                    TowerLevel top = subject.Tower.GetLevel(subject.Tower.MaxLevelIndex);
                    subject.Stats.Add(Stat("Damage", top.Damage, UpgradeTarget.TowerDamage, "0", string.Empty, _maxTowerDamage));
                    subject.Stats.Add(Stat("Range", top.Range, UpgradeTarget.TowerRange, "0.0", string.Empty, _maxTowerRange));
                    subject.Stats.Add(Stat("Fire rate", 1f / top.FireInterval, UpgradeTarget.TowerFireRate, "0.0", "/s", _maxTowerRate));
                    subject.Stats.Add(Stat("Health", subject.Tower.MaxHealth, UpgradeTarget.TowerHealth, "0", string.Empty, _maxTowerHealth));
                    break;
                }

                case SubjectKind.Commander when subject.Hero != null:
                    subject.Stats.Add(Stat("Armour", subject.Hero.MaxHealth, UpgradeTarget.HeroHealth, "0", string.Empty));
                    subject.Stats.Add(Stat("Blade", subject.Hero.Damage, UpgradeTarget.HeroDamage, "0", string.Empty));
                    subject.Stats.Add(Plain("Reach", subject.Hero.Reach, "0.0"));
                    subject.Stats.Add(Plain("Speed", subject.Hero.WalkSpeed, "0.0"));
                    break;

                case SubjectKind.Base:
                {
                    LevelDefinition first = FirstLevel();
                    TowerDefinition gate = FindGate();
                    subject.Stats.Add(Flat("Base armour", first != null ? first.BaseHealth : 0f, UpgradeTarget.BaseHealth, "0"));
                    subject.Stats.Add(Flat("Gun level", 0f, UpgradeTarget.BaseWeapon, "0"));
                    if (gate != null)
                    {
                        subject.Stats.Add(Stat("Gate health", gate.MaxHealth, UpgradeTarget.BlockadeHealth, "0", string.Empty));
                    }

                    break;
                }

                case SubjectKind.Drone when subject.Drone != null:
                {
                    DroneDefinition drone = subject.Drone;
                    subject.Stats.Add(Stat("Speed", drone.Speed, UpgradeTarget.DroneSpeed, "0.0", string.Empty));
                    subject.Stats.Add(Stat("Armour", drone.MaxHealth, UpgradeTarget.DroneArmour, "0", string.Empty));
                    switch (drone.Kind)
                    {
                        case DroneKind.Repair:
                            subject.Stats.Add(Stat("Repair", drone.RepairPerSecond, UpgradeTarget.DroneOutput, "0.0", "/s"));
                            break;
                        case DroneKind.Harvest:
                            subject.Stats.Add(Stat("Per trip", drone.HarvestValue, UpgradeTarget.DroneOutput, "0", string.Empty));
                            break;
                        default:
                            subject.Stats.Add(Stat("Damage", drone.Damage, UpgradeTarget.DroneOutput, "0.0", string.Empty));
                            subject.Stats.Add(Stat("Fire rate", 1f / drone.FireInterval, UpgradeTarget.DroneTempo, "0.0", "/s"));
                            break;
                    }

                    subject.Stats.Add(Flat("At once", drone.MaxActive, UpgradeTarget.DroneCount, "0"));
                    break;
                }
            }

            // Bars are measured against the value the fully upgraded subject reaches, unless the stat has its own
            // reference such as the strongest tower of the campaign.
            foreach (StatLine stat in subject.Stats)
            {
                if (stat.Reference <= 0f)
                {
                    stat.Reference = Mathf.Max(0.001f, ValueOf(subject, stat, null, maxed: true));
                }
            }
        }

        private static StatLine Stat(string name, float value, UpgradeTarget target, string format, string suffix, float reference = 0f)
        {
            return new StatLine { Name = name, Base = value, HasTarget = true, Target = target, Format = format, Suffix = suffix, Reference = reference };
        }

        private static StatLine Flat(string name, float value, UpgradeTarget target, string format)
        {
            return new StatLine { Name = name, Base = value, HasTarget = true, Target = target, Flat = true, Format = format, Suffix = string.Empty };
        }

        private static StatLine Plain(string name, float value, string format)
        {
            return new StatLine { Name = name, Base = value, Format = format, Suffix = string.Empty };
        }

        // The stat's value with the bought upgrades applied. "Extra" adds one more level of that upgrade, for the
        // value after the purchase; "maxed" assumes every upgrade of the subject at its top.
        private float ValueOf(Subject subject, StatLine stat, UpgradeDefinition extra, bool maxed = false)
        {
            if (!stat.HasTarget)
            {
                return stat.Base;
            }

            float bonus = 0f;
            foreach (UpgradeDefinition upgrade in subject.Upgrades)
            {
                if (upgrade.Target != stat.Target)
                {
                    continue;
                }

                int level = maxed ? upgrade.MaxLevel : Progress.GetUpgradeLevel(upgrade.Id) + (upgrade == extra ? 1 : 0);
                bonus += level * upgrade.BonusPerLevel;
            }

            return stat.Flat ? stat.Base + bonus : stat.Base * (1f + bonus);
        }

        private StatLine StatFor(Subject subject, UpgradeDefinition upgrade)
        {
            foreach (StatLine stat in subject.Stats)
            {
                if (stat.HasTarget && stat.Target == upgrade.Target)
                {
                    return stat;
                }
            }

            return null;
        }

        // ---- the grid -------------------------------------------------------------------------------------------

        private void BuildGrid(RectTransform root)
        {
            float y = GridTop;
            y = BuildSection(root, TowersHeading, s => s.Kind == SubjectKind.Tower, y);
            y += SectionGap;
            BuildSection(root, OthersHeading, s => s.Kind != SubjectKind.Tower, y);
        }

        private float BuildSection(RectTransform root, string heading, System.Predicate<Subject> filter, float y)
        {
            Text label = UiKit.CreateFixedText(root, heading, SubtitleSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(Margin, -y), new Vector2(GridWidth, HeadingHeight));
            label.color = Muted;
            y += HeadingHeight + 8f;

            int index = 0;
            foreach (Subject subject in _subjects)
            {
                if (!filter(subject))
                {
                    continue;
                }

                int column = index % Columns;
                int row = index / Columns;
                BuildTile(root, subject, new Vector2(Margin + column * (TileSize + TileGap), -(y + row * (TileSize + TileGap))));
                index++;
            }

            int rows = Mathf.CeilToInt(index / (float)Columns);
            return y + rows * (TileSize + TileGap) - TileGap;
        }

        private void BuildTile(RectTransform root, Subject subject, Vector2 position)
        {
            Button tile = UiKit.CreateButton(root, string.Empty, () => Select(subject));
            Destroy(tile.GetComponent<LayoutElement>());
            RectTransform rect = tile.GetComponent<RectTransform>();
            PlaceTopLeft(rect, position, new Vector2(TileSize, TileSize));
            subject.Tile = tile;
            subject.TileBack = tile.GetComponent<Image>();
            Destroy(tile.GetComponentInChildren<Text>().gameObject);

            var icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(rect, false);
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -10f);
            iconRect.sizeDelta = new Vector2(TileIconSize, TileIconSize);
            subject.TileIcon = icon;
            RequestIcon(subject, icon);

            Text name = UiKit.CreateFixedText(rect, subject.Name, TileNameSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(TileSize - 12f, 44f));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = TileNameSize;
            name.raycastTarget = false;

            RectTransform bar = UiKit.CreateBar(rect, UiKit.Skin != null ? UiKit.Skin.BarBlue : null, 0f);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(10f, 5f);
            bar.offsetMax = new Vector2(-10f, 5f + TileProgressHeight);
            subject.TileProgress = (RectTransform)bar.GetChild(0);

            Image star = CreateIcon("Star", rect, IconType.Star, Gold, TileStarSize);
            star.rectTransform.anchorMin = new Vector2(1f, 1f);
            star.rectTransform.anchorMax = new Vector2(1f, 1f);
            star.rectTransform.anchoredPosition = new Vector2(-6f - TileStarSize * 0.5f, -6f - TileStarSize * 0.5f);
            star.enabled = false;
            subject.TileStar = star;
        }

        // The tile shows the thing itself, rendered once from the same model the level uses.
        private void RequestIcon(Subject subject, Image icon)
        {
            System.Func<Transform, GameObject> build = null;
            object key = null;
            switch (subject.Kind)
            {
                case SubjectKind.Tower when subject.Tower != null:
                    key = subject.Tower;
                    build = stage => TowerModelBuilder.Build(stage, subject.Tower, 0);
                    break;
                case SubjectKind.Drone when subject.Drone != null:
                    key = subject.Drone;
                    build = stage =>
                    {
                        GameObject model = DroneModelBuilder.BuildDisplay(stage, subject.Drone).Root;
                        model.transform.localScale = Vector3.one * DroneIconScale;
                        model.transform.localPosition = Vector3.up * DroneIconLift;
                        model.transform.localRotation = Quaternion.Euler(18f, 35f, 0f);
                        return model;
                    };
                    break;
                case SubjectKind.Commander when subject.Hero != null:
                    key = subject.Hero;
                    build = stage => HeroView.BuildDisplayFigure(stage, subject.Hero);
                    break;
                case SubjectKind.Base:
                    key = "Base";
                    build = stage =>
                    {
                        GameObject model = LevelVisualBuilder.BuildBaseDisplay(stage, subject.Glow, subject.BaseArt);
                        model.transform.localScale = Vector3.one * BaseIconScale;
                        return model;
                    };
                    break;
            }

            if (build == null)
            {
                return;
            }

            ModelIconRenderer.Request(key, build, sprite =>
            {
                if (icon != null)
                {
                    icon.sprite = sprite;
                    icon.enabled = true;
                }
            });
        }

        // ---- the detail panel ----------------------------------------------------------------------------------

        private void BuildPanel(RectTransform root)
        {
            Image panel = UiKit.CreatePanel(root, "Detail", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, PanelTint);
            RectTransform rect = panel.rectTransform;
            PlaceTopLeft(rect, new Vector2(PanelX, -PanelTop), new Vector2(PanelWidth, PanelHeight));

            _header = UiKit.CreateFixedText(rect, string.Empty, HeaderSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(PanelInset, -22f), new Vector2(PanelWidth - PanelInset * 2f, 60f));
            _subtitle = UiKit.CreateFixedText(rect, string.Empty, SubtitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(PanelInset, -80f), new Vector2(PanelWidth - PanelInset * 2f, 60f));
            _subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _subtitle.color = Muted;

            Image frame = UiKit.CreatePanel(rect, "Preview frame", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, PreviewTint);
            PlaceTopLeft(frame.rectTransform, new Vector2(PanelInset, -PreviewTop), new Vector2(PreviewSize, PreviewSize));
            _previewFrame = frame.rectTransform;
            _preview = ModelPreview.Create(_previewFrame, new Vector2(PreviewSize - 12f, PreviewSize - 12f));
            RectTransform previewRect = _preview.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = Vector2.zero;

            for (int i = 0; i < 5; i++)
            {
                _statWidgets.Add(BuildStatWidget(rect, i));
            }

            for (int i = 0; i < MaxRows; i++)
            {
                _rows.Add(BuildRow(rect, i));
            }
        }

        private StatWidget BuildStatWidget(RectTransform panel, int index)
        {
            var root = new GameObject("Stat", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(panel, false);
            PlaceTopLeft(root, new Vector2(StatsX, -(PreviewTop + index * StatRowHeight)), new Vector2(PanelWidth - StatsX - PanelInset, StatRowHeight));

            Text name = UiKit.CreateFixedText(root, string.Empty, StatSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(StatLabelWidth, StatRowHeight));
            name.color = Muted;

            RectTransform bar = UiKit.CreateBar(root, UiKit.Skin != null ? UiKit.Skin.BarYellow : null, 0f);
            PlaceTopLeft(bar, new Vector2(StatLabelWidth + 6f, -(StatRowHeight - BarHeight) * 0.5f), new Vector2(BarWidth, BarHeight));

            Text value = UiKit.CreateFixedText(root, string.Empty, StatSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(StatLabelWidth + BarWidth + 18f, 0f), new Vector2(StatValueWidth, StatRowHeight));
            return new StatWidget { Root = root.gameObject, Name = name, Fill = (RectTransform)bar.GetChild(0), Value = value };
        }

        private RowWidget BuildRow(RectTransform panel, int index)
        {
            var row = new RowWidget();
            Button button = UiKit.CreateButton(panel, string.Empty, () => Buy(row));
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            float width = PanelWidth - PanelInset * 2f;
            PlaceTopLeft(rect, new Vector2(PanelInset, -(RowsTop + index * (RowHeight + RowGap))), new Vector2(width, RowHeight));
            Destroy(button.GetComponentInChildren<Text>().gameObject);

            row.Button = button;
            row.Name = UiKit.CreateFixedText(rect, string.Empty, RowSize, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(RowNameWidth, RowHeight));
            row.Change = UiKit.CreateFixedText(rect, string.Empty, RowSize, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(24f + RowNameWidth, 0f), new Vector2(RowChangeWidth, RowHeight));
            row.Cost = UiKit.CreateFixedText(rect, string.Empty, RowSize, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-24f - 3 * (PipSize + PipGap) - 16f, 0f), new Vector2(RowCostWidth, RowHeight));
            row.Name.raycastTarget = false;
            row.Change.raycastTarget = false;
            row.Cost.raycastTarget = false;

            row.Pips = new Image[3];
            for (int i = 0; i < row.Pips.Length; i++)
            {
                Image pip = CreateIcon("Pip", rect, IconType.Disc, PipOff, PipSize);
                pip.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                pip.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                pip.rectTransform.anchoredPosition = new Vector2(-24f - PipSize * 0.5f - (row.Pips.Length - 1 - i) * (PipSize + PipGap), 0f);
                row.Pips[i] = pip;
            }

            return row;
        }

        // ---- state ----------------------------------------------------------------------------------------------

        private void Select(Subject subject)
        {
            _selected = subject;
            foreach (Subject other in _subjects)
            {
                other.TileBack.color = other == subject ? TileChosen : other.Locked ? TileLocked : TileTint;
            }

            _header.text = subject.Name;
            _header.color = subject.Locked ? Muted : Color.Lerp(subject.Glow, Color.white, 0.35f);
            _subtitle.text = subject.Locked ? "Not found yet. Beat the level that holds this weapon." : Subtitle(subject);
            ShowPreview(subject);
            RefreshPanel();
        }

        private string Subtitle(Subject subject)
        {
            if (subject.Kind == SubjectKind.Tower && subject.Tower != null)
            {
                bool ground = (subject.Tower.Targets & TargetLayers.Ground) != 0;
                bool air = (subject.Tower.Targets & TargetLayers.Air) != 0;
                return "Shoots " + (ground && air ? "ground and air" : air ? "air" : "ground");
            }

            return subject.Description ?? string.Empty;
        }

        private void ShowPreview(Subject subject)
        {
            switch (subject.Kind)
            {
                case SubjectKind.Tower when subject.Tower != null:
                    _preview.ShowTower(subject.Tower);
                    break;
                case SubjectKind.Commander when subject.Hero != null:
                    _preview.ShowHero(subject.Hero);
                    break;
                case SubjectKind.Drone when subject.Drone != null:
                    _preview.ShowDrone(subject.Drone);
                    break;
                case SubjectKind.Base:
                    _preview.ShowBase(subject.Glow, subject.BaseArt);
                    break;
            }
        }

        private void RefreshAll()
        {
            _researchText.text = $"{ResearchLabel} {Progress.Data.ResearchPoints}";
            _powerText.text = $"{PowerLabel} {Progress.Data.PowerCurrency}";
            foreach (Subject subject in _subjects)
            {
                subject.Locked = subject.Tower != null && !Progress.IsTowerUnlocked(subject.Tower.Id);
                subject.TileIcon.color = subject.Locked ? LockedIcon : Color.white;
                subject.TileBack.color = subject == _selected ? TileChosen : subject.Locked ? TileLocked : TileTint;
                int bought = 0;
                int total = 0;
                foreach (UpgradeDefinition upgrade in subject.Upgrades)
                {
                    bought += Progress.GetUpgradeLevel(upgrade.Id);
                    total += upgrade.MaxLevel;
                }

                subject.TileProgress.anchorMax = new Vector2(total > 0 ? bought / (float)total : 0f, 1f);
                subject.TileStar.enabled = total > 0 && bought >= total;
            }

            RefreshPanel();
        }

        private void RefreshPanel()
        {
            Subject subject = _selected;
            for (int i = 0; i < _statWidgets.Count; i++)
            {
                StatWidget widget = _statWidgets[i];
                bool shown = i < subject.Stats.Count;
                widget.Root.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                StatLine stat = subject.Stats[i];
                float value = ValueOf(subject, stat, null);
                widget.Name.text = stat.Name;
                widget.Fill.anchorMax = new Vector2(Mathf.Clamp01(value / stat.Reference), 1f);
                widget.Value.text = value.ToString(stat.Format) + stat.Suffix;
                widget.Value.color = Color.white;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                RowWidget row = _rows[i];
                bool shown = i < subject.Upgrades.Count;
                row.Button.gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                row.Upgrade = subject.Upgrades[i];
                RefreshRow(row, subject);
            }
        }

        private void RefreshRow(RowWidget row, Subject subject)
        {
            UpgradeDefinition upgrade = row.Upgrade;
            int level = Progress.GetUpgradeLevel(upgrade.Id);
            bool maxed = level >= upgrade.MaxLevel;
            int cost = maxed ? 0 : upgrade.CostForNextLevel(level);
            bool affordable = !maxed && Progress.CanAfford(upgrade, cost);
            bool power = upgrade.Currency == UpgradeCurrency.PowerCurrency;

            row.Button.interactable = !subject.Locked && !maxed && affordable;
            row.Name.text = upgrade.DisplayName;
            row.Name.color = subject.Locked ? Muted : Color.white;

            StatLine stat = StatFor(subject, upgrade);
            if (maxed)
            {
                row.Change.text = stat != null ? ValueOf(subject, stat, null).ToString(stat.Format) + stat.Suffix + "   at the top" : "At the top";
                row.Change.color = Gold;
            }
            else if (stat != null)
            {
                row.Change.text = $"{ValueOf(subject, stat, null).ToString(stat.Format)}{stat.Suffix}  >  {ValueOf(subject, stat, upgrade).ToString(stat.Format)}{stat.Suffix}";
                row.Change.color = Better;
            }
            else
            {
                row.Change.text = $"+{Mathf.RoundToInt(upgrade.BonusPerLevel * 100f)}% per level";
                row.Change.color = Better;
            }

            row.Cost.text = maxed ? string.Empty : $"{cost} {(power ? "power" : "research")}";
            row.Cost.color = maxed ? Muted : affordable ? (power ? PipPower : PipOn) : TooPoor;

            Color on = power ? PipPower : PipOn;
            for (int i = 0; i < row.Pips.Length; i++)
            {
                bool exists = i < upgrade.MaxLevel;
                row.Pips[i].enabled = exists;
                row.Pips[i].color = i < level ? on : PipOff;
            }
        }

        // Buying rebuilds the numbers at once; the celebration then animates from the old values to the new ones.
        private void Buy(RowWidget row)
        {
            Subject subject = _selected;
            UpgradeDefinition upgrade = row.Upgrade;
            StatLine stat = StatFor(subject, upgrade);
            bool power = upgrade.Currency == UpgradeCurrency.PowerCurrency;
            int pointsBefore = power ? Progress.Data.PowerCurrency : Progress.Data.ResearchPoints;
            float valueBefore = stat != null ? ValueOf(subject, stat, null) : 0f;
            bool masteredBefore = subject.TileStar.enabled;

            if (!Progress.TryBuyUpgrade(upgrade))
            {
                return;
            }

            int level = Progress.GetUpgradeLevel(upgrade.Id);
            RefreshAll();
            if (!row.Button.interactable)
            {
                UiKit.Select(NextSelectable(row));
            }

            var cue = new UpgradeCelebration.Cue
            {
                PreviewFrame = _previewFrame,
                Preview = _preview,
                Pip = row.Pips[Mathf.Clamp(level - 1, 0, row.Pips.Length - 1)],
                PipColor = power ? PipPower : PipOn,
                Points = power ? _powerText : _researchText,
                PointsFrom = pointsBefore,
                PointsTo = power ? Progress.Data.PowerCurrency : Progress.Data.ResearchPoints,
                PointsLabel = power ? PowerLabel : ResearchLabel,
                Mastered = subject.TileStar.enabled && !masteredBefore,
                TileStar = subject.TileStar,
            };

            if (stat != null)
            {
                int index = subject.Stats.IndexOf(stat);
                StatWidget widget = _statWidgets[index];
                cue.BarFill = widget.Fill;
                cue.BarFrom = Mathf.Clamp01(valueBefore / stat.Reference);
                cue.BarTo = Mathf.Clamp01(ValueOf(subject, stat, null) / stat.Reference);
                cue.Value = widget.Value;
                cue.ValueFrom = valueBefore;
                cue.ValueTo = ValueOf(subject, stat, null);
                cue.ValueFormat = stat.Format;
                cue.ValueSuffix = stat.Suffix;
            }

            _celebration.Play(cue);
        }

        // When the bought row turns grey the gamepad focus moves to the next buyable row, or back to the tile.
        private Selectable NextSelectable(RowWidget from)
        {
            foreach (RowWidget row in _rows)
            {
                if (row != from && row.Button.gameObject.activeSelf && row.Button.interactable)
                {
                    return row.Button;
                }
            }

            return _selected.Tile;
        }

        // ---- lookups --------------------------------------------------------------------------------------------

        private void MeasureTowerMaxima()
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (TowerDefinition tower in node.Level.AvailableTowers)
                {
                    TowerLevel top = tower.GetLevel(tower.MaxLevelIndex);
                    _maxTowerDamage = Mathf.Max(_maxTowerDamage, top.Damage);
                    _maxTowerRange = Mathf.Max(_maxTowerRange, top.Range);
                    _maxTowerRate = Mathf.Max(_maxTowerRate, 1f / top.FireInterval);
                    _maxTowerHealth = Mathf.Max(_maxTowerHealth, tower.MaxHealth);
                }
            }

            // Leave room above the strongest tower for what the upgrades add to it.
            const float Headroom = 1.3f;
            _maxTowerDamage = Mathf.Max(1f, _maxTowerDamage) * Headroom;
            _maxTowerRange = Mathf.Max(1f, _maxTowerRange) * Headroom;
            _maxTowerRate = Mathf.Max(1f, _maxTowerRate) * Headroom;
            _maxTowerHealth = Mathf.Max(1f, _maxTowerHealth);
        }

        private LevelDefinition FirstLevel()
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level != null)
                {
                    return node.Level;
                }
            }

            return null;
        }

        private TowerDefinition FindTower(string id)
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (TowerDefinition tower in node.Level.AvailableTowers)
                {
                    if (tower != null && tower.Id == id)
                    {
                        return tower;
                    }
                }

                // Hybrids are never on a level's build bar; they come out of its recipes.
                foreach (CombineRecipe recipe in node.Level.Recipes)
                {
                    if (recipe != null && recipe.Result != null && recipe.Result.Id == id)
                    {
                        return recipe.Result;
                    }
                }
            }

            return null;
        }

        private TowerDefinition FindGate()
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level != null && node.Level.Blockades.Length > 0 && node.Level.Blockades[0].Blockade != null)
                {
                    return node.Level.Blockades[0].Blockade;
                }
            }

            return null;
        }

        private HeroDefinition FindHero()
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level != null && node.Level.Hero != null)
                {
                    return node.Level.Hero;
                }
            }

            return null;
        }

        private DroneDefinition FindDrone(string id)
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (DroneDefinition drone in new[] { node.Level.RepairDrone, node.Level.HarvestDrone, node.Level.AttackDrone })
                {
                    if (drone != null && drone.name == id)
                    {
                        return drone;
                    }
                }
            }

            return null;
        }

        // ---- helpers --------------------------------------------------------------------------------------------

        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image CreateIcon(string name, RectTransform parent, IconType icon, Color colour, float size)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = IconFactory.Get(icon);
            image.color = colour;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            return image;
        }
    }
}
