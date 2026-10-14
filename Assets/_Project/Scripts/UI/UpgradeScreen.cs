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
    /// The base. A list of towers (and the base itself) on the left; the selected one gets its own panel on the right
    /// with its stats per level and its permanent upgrades, bought with research points.
    /// Towers the player has not unlocked yet are greyed out.
    /// </summary>
    public sealed class UpgradeScreen : ScreenBase
    {
        private const float ListX = 80f;
        private const float ListWidth = 460f;
        private const float DetailX = 600f;
        private const float DetailWidth = 1100f;
        private const float RowHeight = 100f;
        private const float RowSpacing = 18f;
        private const int StatsSize = 30;

        private readonly List<Group> _groups = new List<Group>();
        private CampaignCatalog _campaign;
        private UpgradeCatalog _catalog;
        private Text _pointsText;

        private sealed class Group
        {
            public string Name;
            public TowerDefinition Tower;
            public Button ListButton;
            public GameObject Panel;
            public readonly List<Button> Rows = new List<Button>();
            public readonly List<UpgradeDefinition> Upgrades = new List<UpgradeDefinition>();
        }

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog) || !Services.TryGet(out _campaign))
            {
                Go(SceneNames.Boot);
                return;
            }

            UiKit.CreateFixedText(root, "Base upgrades", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1200f, 100f));
            _pointsText = UiKit.CreateFixedText(root, string.Empty, UiKit.BodySize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(1200f, 60f));

            RectTransform list = CreateStack(root, new Vector2(0f, 0.5f), new Vector2(ListX, -20f), ListWidth, RowSpacing);
            foreach (string name in GroupNames())
            {
                Group group = BuildGroup(root, list, name);
                _groups.Add(group);
            }

            Button back = UiKit.CreateButton(root, "Back to map", () => Go(SceneNames.WorldMap));
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0f, 0f);
            backRect.anchoredPosition = new Vector2(ListX, 30f);
            backRect.sizeDelta = new Vector2(ListWidth, UiKit.ButtonHeight);
            Destroy(back.GetComponent<LayoutElement>());

            Show(_groups[0]);
            Refresh();
            SetFirstSelected(_groups[0].ListButton);
        }

        private Group BuildGroup(RectTransform root, RectTransform list, string name)
        {
            var group = new Group { Name = name, Tower = FindTower(name) };

            group.ListButton = UiKit.CreateButton(list, name, () => Show(group));
            group.ListButton.GetComponent<LayoutElement>().preferredWidth = ListWidth;
            group.ListButton.gameObject.AddComponent<SelectNotifier>().Selected = () => Show(group);

            RectTransform panel = CreateStack(root, new Vector2(0f, 0.5f), new Vector2(DetailX, -20f), DetailWidth, RowSpacing);
            group.Panel = panel.gameObject;

            Text stats = UiKit.CreateText(panel, StatsFor(group), StatsSize, TextAnchor.UpperLeft);
            stats.horizontalOverflow = HorizontalWrapMode.Overflow;
            var statsElement = stats.gameObject.AddComponent<LayoutElement>();
            statsElement.preferredHeight = group.Tower != null ? 190f : 60f;
            statsElement.preferredWidth = DetailWidth;

            foreach (UpgradeDefinition upgrade in _catalog.Upgrades)
            {
                if (upgrade.GroupName != name)
                {
                    continue;
                }

                UpgradeDefinition captured = upgrade;
                Button row = UiKit.CreateButton(panel, string.Empty, () => Buy(captured));
                var element = row.GetComponent<LayoutElement>();
                element.preferredWidth = DetailWidth;
                element.preferredHeight = RowHeight;
                group.Rows.Add(row);
                group.Upgrades.Add(upgrade);
            }

            panel.gameObject.SetActive(false);
            return group;
        }

        private static RectTransform CreateStack(RectTransform parent, Vector2 anchor, Vector2 position, float width, float spacing)
        {
            var go = new GameObject("Stack", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 0f);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
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
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            Progress.TryBuyUpgrade(upgrade);
            Refresh();
        }

        private void Refresh()
        {
            _pointsText.text = $"Research points: {Progress.Data.ResearchPoints}    Power: {Progress.Data.PowerCurrency}";

            foreach (Group group in _groups)
            {
                bool locked = group.Tower != null && !Progress.IsTowerUnlocked(group.Tower.Id);
                UiKit.SetLabel(group.ListButton, locked ? $"{group.Name}  (locked)" : group.Name);

                for (int i = 0; i < group.Rows.Count; i++)
                {
                    UpgradeDefinition upgrade = group.Upgrades[i];
                    int level = Progress.GetUpgradeLevel(upgrade.Id);
                    string pips = Pips(level, upgrade.MaxLevel);
                    string cost = level >= upgrade.MaxLevel ? "MAX" : $"cost {upgrade.CostForNextLevel(level)}";

                    group.Rows[i].interactable = !locked;
                    UiKit.SetLabel(group.Rows[i], locked ? $"{upgrade.DisplayName}\nLocked until the weapon is found" : $"{upgrade.DisplayName}\n{pips}   {cost}");
                }
            }
        }

        private static string Pips(int level, int max)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < max; i++)
            {
                builder.Append(i < level ? "[#]" : "[ ]");
            }

            return builder.ToString();
        }

        private static string StatsFor(Group group)
        {
            TowerDefinition tower = group.Tower;
            if (tower == null)
            {
                return "Improvements for your base and its blockades.";
            }

            var builder = new StringBuilder();
            builder.Append(tower.DisplayName).Append("   targets ").Append(Targets(tower)).Append("   health ").Append(Mathf.RoundToInt(tower.MaxHealth));
            if (tower.SplashRadius > 0f)
            {
                builder.Append("   splash ").Append(tower.SplashRadius.ToString("0.0"));
            }

            for (int i = 0; i <= tower.MaxLevelIndex; i++)
            {
                TowerLevel level = tower.GetLevel(i);
                builder.Append("\nLevel ").Append(i + 1).Append(":  damage ").Append(level.Damage.ToString("0"))
                    .Append("   range ").Append(level.Range.ToString("0.0"))
                    .Append("   shots/s ").Append((1f / level.FireInterval).ToString("0.0"))
                    .Append("   cost ").Append(level.Cost);
            }

            return builder.ToString();
        }

        private static string Targets(TowerDefinition tower)
        {
            bool ground = (tower.Targets & TargetLayers.Ground) != 0;
            bool air = (tower.Targets & TargetLayers.Air) != 0;
            return ground && air ? "ground and air" : air ? "air" : "ground";
        }
    }
}
