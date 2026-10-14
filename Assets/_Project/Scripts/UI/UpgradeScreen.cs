using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The base. A list of towers (and the base itself) on the left. The selected entry gets its own panel with a
    /// turning 3D model, stat bars from level 1 to level 3 and its permanent upgrades, bought with research points.
    /// Towers the player has not unlocked yet are greyed out.
    /// </summary>
    public sealed class UpgradeScreen : ScreenBase
    {
        private const float Margin = 60f;
        private const float TopOffset = 170f;
        private const float ListWidth = 420f;
        private const float ListRowHeight = 100f;
        private const float ListRowGap = 14f;
        private const float DetailX = 520f;
        private const float DetailWidth = 1340f;
        private const float DetailHeight = 760f;
        private const float PreviewSize = 440f;
        private const float StatsX = 520f;
        private const float StatsY = 150f;
        private const float StatRowHeight = 66f;
        private const float BarWidth = 420f;
        private const float BarHeight = 28f;
        private const float RowsX = 520f;
        private const float RowsY = 430f;
        private const float RowWidth = 780f;
        private const float RowHeight = 96f;
        private const float RowGap = 12f;
        private const float PipSize = 30f;
        private const float PipGap = 8f;
        private static readonly Color PanelTint = new Color(0.16f, 0.24f, 0.42f, 0.94f);
        private static readonly Color PipOn = new Color(0.45f, 0.95f, 1f);
        private static readonly Color PipOff = new Color(0.12f, 0.16f, 0.26f);
        private static readonly Color Muted = new Color(0.65f, 0.72f, 0.85f);

        private readonly List<Group> _groups = new List<Group>();
        private CampaignCatalog _campaign;
        private UpgradeCatalog _catalog;
        private Text _pointsText;
        private ModelPreview _preview;
        private float _maxDamage;
        private float _maxRange;
        private float _maxRate;
        private float _maxHealth;

        private sealed class Group
        {
            public string Name;
            public TowerDefinition Tower;
            public Button ListButton;
            public GameObject Panel;
            public readonly List<UpgradeRow> Rows = new List<UpgradeRow>();
        }

        private sealed class UpgradeRow
        {
            public UpgradeDefinition Upgrade;
            public Button Button;
            public Text Label;
            public Image[] Pips;
        }

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog) || !Services.TryGet(out _campaign))
            {
                Go(SceneNames.Boot);
                return;
            }

            MeasureStatMaxima();

            Text title = UiKit.CreateFixedText(root, "Base upgrades", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1200f, 100f));
            title.fontSize = 56;
            _pointsText = UiKit.CreateFixedText(root, string.Empty, 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1200f, 50f));

            _preview = null;
            float y = TopOffset;
            foreach (string name in GroupNames())
            {
                Group group = BuildGroup(root, name, y);
                _groups.Add(group);
                y += ListRowHeight + ListRowGap;
            }

            Button back = UiKit.CreateButton(root, "Back to map", () => Go(SceneNames.WorldMap));
            Destroy(back.GetComponent<LayoutElement>());
            PlaceTopLeft(back.GetComponent<RectTransform>(), new Vector2(Margin, 0f), new Vector2(ListWidth, UiKit.ButtonHeight));
            back.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            back.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0f);
            back.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(Margin, 40f);

            BuildSharedPreview(root);
            Show(_groups[0]);
            Refresh();
            SetFirstSelected(_groups[0].ListButton);
        }

        private Group BuildGroup(RectTransform root, string name, float y)
        {
            var group = new Group { Name = name, Tower = FindTower(name) };

            group.ListButton = UiKit.CreateButton(root, name, () => Show(group));
            Destroy(group.ListButton.GetComponent<LayoutElement>());
            PlaceTopLeft(group.ListButton.GetComponent<RectTransform>(), new Vector2(Margin, -y), new Vector2(ListWidth, ListRowHeight));

            Image panel = UiKit.CreatePanel(root, name + " panel", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, PanelTint);
            RectTransform panelRect = panel.rectTransform;
            PlaceTopLeft(panelRect, new Vector2(DetailX, -TopOffset), new Vector2(DetailWidth, DetailHeight));
            group.Panel = panel.gameObject;

            BuildDetails(panelRect, group);
            panel.gameObject.SetActive(false);
            return group;
        }

        private void BuildDetails(RectTransform panel, Group group)
        {
            TowerDefinition tower = group.Tower;
            Text header = UiKit.CreateFixedText(panel, group.Name, 58, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(1000f, 80f));
            header.color = PipOn;

            string subtitle = tower != null ? $"Targets {Targets(tower)}    Health {Mathf.RoundToInt(tower.MaxHealth)}" : "Improvements for your base and its blockades";
            Text sub = UiKit.CreateFixedText(panel, subtitle, 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -92f), new Vector2(1200f, 50f));
            sub.color = Muted;

            if (tower != null)
            {
                BuildStats(panel, tower);
            }

            float rowY = tower != null ? RowsY : StatsY;
            foreach (UpgradeDefinition upgrade in _catalog.Upgrades)
            {
                if (upgrade.GroupName != group.Name)
                {
                    continue;
                }

                group.Rows.Add(BuildRow(panel, upgrade, rowY));
                rowY += RowHeight + RowGap;
            }
        }

        // One shared preview for the whole screen: every group used to build its own rig at the same spot,
        // so each showed a pile of all the towers instead of the one being upgraded.
        private void BuildSharedPreview(RectTransform root)
        {
            Image frame = UiKit.CreatePanel(root, "Preview frame", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.2f, 1f));
            PlaceTopLeft(frame.rectTransform, new Vector2(DetailX + 30f, -(TopOffset + StatsY)), new Vector2(PreviewSize + 20f, PreviewSize + 20f));

            _preview = ModelPreview.Create(frame.rectTransform, new Vector2(PreviewSize, PreviewSize));
            RectTransform rect = _preview.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
        private GameObject BaseModel()
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level != null && node.Level.Theme != null && node.Level.Theme.BaseModel != null)
                {
                    return node.Level.Theme.BaseModel;
                }
            }

            return null;
        }

        private void BuildStats(RectTransform panel, TowerDefinition tower)
        {
            TowerLevel first = tower.GetLevel(0);
            TowerLevel last = tower.GetLevel(tower.MaxLevelIndex);
            float firstRate = 1f / first.FireInterval;
            float lastRate = 1f / last.FireInterval;

            AddStat(panel, 0, "Damage", first.Damage, last.Damage, _maxDamage, "0");
            AddStat(panel, 1, "Range", first.Range, last.Range, _maxRange, "0.0");
            AddStat(panel, 2, "Fire rate", firstRate, lastRate, _maxRate, "0.0");
            AddStat(panel, 3, "Health", tower.MaxHealth, tower.MaxHealth, _maxHealth, "0");
        }

        // Yellow bar = value at the top level, blue bar on top = value at level 1.
        private void AddStat(RectTransform panel, int index, string label, float level1, float level3, float max, string format)
        {
            float y = -(StatsY + index * StatRowHeight);
            Text name = UiKit.CreateFixedText(panel, label, 30, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(StatsX, y), new Vector2(190f, StatRowHeight));
            name.color = Muted;

            float barX = StatsX + 200f;
            RectTransform top = UiKit.CreateBar(panel, UiKit.Skin != null ? UiKit.Skin.BarYellow : null, level3 / max);
            PlaceTopLeft(top, new Vector2(barX, y - (StatRowHeight - BarHeight) * 0.5f), new Vector2(BarWidth, BarHeight));

            RectTransform low = UiKit.CreateBar(panel, UiKit.Skin != null ? UiKit.Skin.BarBlue : null, level1 / max);
            PlaceTopLeft(low, new Vector2(barX, y - (StatRowHeight - BarHeight) * 0.5f), new Vector2(BarWidth, BarHeight));
            low.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            string value = Mathf.Approximately(level1, level3) ? level1.ToString(format) : $"{level1.ToString(format)} > {level3.ToString(format)}";
            Text number = UiKit.CreateFixedText(panel, value, 30, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(barX + BarWidth + 20f, y), new Vector2(300f, StatRowHeight));
            number.color = Color.white;
        }

        private UpgradeRow BuildRow(RectTransform panel, UpgradeDefinition upgrade, float y)
        {
            Button button = UiKit.CreateButton(panel, string.Empty, () => Buy(upgrade));
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            PlaceTopLeft(rect, new Vector2(RowsX, -y), new Vector2(RowWidth, RowHeight));

            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.resizeTextMaxSize = 30;
            label.rectTransform.offsetMax = new Vector2(-(PipSize + PipGap) * upgrade.MaxLevel - 30f, -4f);
            label.rectTransform.offsetMin = new Vector2(24f, 4f);

            var row = new UpgradeRow { Upgrade = upgrade, Button = button, Label = label, Pips = new Image[upgrade.MaxLevel] };
            for (int i = 0; i < upgrade.MaxLevel; i++)
            {
                var pip = new GameObject("Pip", typeof(RectTransform)).AddComponent<Image>();
                pip.transform.SetParent(rect, false);
                pip.raycastTarget = false;
                RectTransform pipRect = pip.rectTransform;
                pipRect.anchorMin = new Vector2(1f, 0.5f);
                pipRect.anchorMax = new Vector2(1f, 0.5f);
                pipRect.pivot = new Vector2(1f, 0.5f);
                pipRect.sizeDelta = new Vector2(PipSize, PipSize);
                pipRect.anchoredPosition = new Vector2(-24f - (upgrade.MaxLevel - 1 - i) * (PipSize + PipGap), 0f);
                row.Pips[i] = pip;
            }

            return row;
        }

        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void MeasureStatMaxima()
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
                    _maxDamage = Mathf.Max(_maxDamage, top.Damage);
                    _maxRange = Mathf.Max(_maxRange, top.Range);
                    _maxRate = Mathf.Max(_maxRate, 1f / top.FireInterval);
                    _maxHealth = Mathf.Max(_maxHealth, tower.MaxHealth);
                }
            }

            _maxDamage = Mathf.Max(1f, _maxDamage);
            _maxRange = Mathf.Max(1f, _maxRange);
            _maxRate = Mathf.Max(1f, _maxRate);
            _maxHealth = Mathf.Max(1f, _maxHealth);
        }

        private IEnumerable<string> GroupNames()
        {
            var seen = new List<string>();
            foreach (UpgradeDefinition upgrade in _catalog.Upgrades)
            {
                if (!seen.Contains(upgrade.GroupName))
                {
                    seen.Add(upgrade.GroupName);
                }
            }

            return seen;
        }

        private TowerDefinition FindTower(string groupName)
        {
            foreach (MapNode node in _campaign.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (TowerDefinition tower in node.Level.AvailableTowers)
                {
                    if (tower != null && tower.DisplayName == groupName)
                    {
                        return tower;
                    }
                }
            }

            return null;
        }

        private void Show(Group selected)
        {
            foreach (Group group in _groups)
            {
                group.Panel.SetActive(group == selected);
            }

            if (_preview != null)
            {
                GameObject model = selected.Tower != null ? selected.Tower.Model : BaseModel();
                Color tint = selected.Tower != null ? Color.Lerp(Color.white, selected.Tower.Color, 0.55f) : Color.white;
                float scale = selected.Tower != null ? selected.Tower.ModelScale : 1f;
                _preview.Show(model, scale, tint);
            }
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            Progress.TryBuyUpgrade(upgrade);
            Refresh();
        }

        private void Refresh()
        {
            _pointsText.text = $"Research points {Progress.Data.ResearchPoints}     Power {Progress.Data.PowerCurrency}";

            foreach (Group group in _groups)
            {
                bool locked = group.Tower != null && !Progress.IsTowerUnlocked(group.Tower.Id);
                UiKit.SetLabel(group.ListButton, locked ? $"{group.Name}\nlocked" : group.Name);

                foreach (UpgradeRow row in group.Rows)
                {
                    int level = Progress.GetUpgradeLevel(row.Upgrade.Id);
                    bool maxed = level >= row.Upgrade.MaxLevel;
                    int cost = maxed ? 0 : row.Upgrade.CostForNextLevel(level);
                    bool affordable = Progress.Data.ResearchPoints >= cost;

                    row.Button.interactable = !locked && !maxed && affordable;
                    row.Label.text = locked ? $"{row.Upgrade.DisplayName}\nlocked until the weapon is found"
                        : maxed ? $"{row.Upgrade.DisplayName}\nmaximum level"
                        : $"{row.Upgrade.DisplayName}\ncost {cost}";

                    for (int i = 0; i < row.Pips.Length; i++)
                    {
                        row.Pips[i].color = i < level ? PipOn : PipOff;
                    }
                }
            }
        }

        private static string Targets(TowerDefinition tower)
        {
            bool ground = (tower.Targets & TargetLayers.Ground) != 0;
            bool air = (tower.Targets & TargetLayers.Air) != 0;
            return ground && air ? "ground and air" : air ? "air" : "ground";
        }
    }
}




