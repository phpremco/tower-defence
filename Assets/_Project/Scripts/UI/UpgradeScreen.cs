using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>The base: spend research points on permanent upgrades, then return to the map.</summary>
    public sealed class UpgradeScreen : ScreenBase
    {
        private readonly List<Button> _buttons = new List<Button>();

        private UpgradeCatalog _catalog;
        private Text _pointsText;

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog))
            {
                Go(SceneNames.Boot);
                return;
            }

            UiKit.CreateFixedText(root, "Base upgrades", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 100f));
            _pointsText = UiKit.CreateFixedText(root, string.Empty, UiKit.BodySize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1200f, 60f));

            RectTransform column = UiKit.CreateColumn(root, 16f);
            foreach (UpgradeDefinition upgrade in _catalog.Upgrades)
            {
                UpgradeDefinition captured = upgrade;
                Button button = UiKit.CreateButton(column, string.Empty, () => Buy(captured));
                _buttons.Add(button);
            }

            Button back = UiKit.CreateButton(column, "Back to map", () => Go(SceneNames.WorldMap));
            Refresh();
            SetFirstSelected(_buttons.Count > 0 ? _buttons[0] : back);
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            Progress.TryBuyUpgrade(upgrade);
            Refresh();
        }

        private void Refresh()
        {
            _pointsText.text = $"Research points: {Progress.Data.ResearchPoints}    Power: {Progress.Data.PowerCurrency}";
            for (int i = 0; i < _buttons.Count; i++)
            {
                UpgradeDefinition upgrade = _catalog.Upgrades[i];
                int level = Progress.GetUpgradeLevel(upgrade.Id);
                string cost = level >= upgrade.MaxLevel ? "max" : upgrade.CostForNextLevel(level).ToString();
                UiKit.SetLabel(_buttons[i], $"{upgrade.DisplayName}  {level}/{upgrade.MaxLevel}  ({cost})");
            }
        }
    }
}
