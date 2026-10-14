using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Planet map: one button per level (locked, open or completed), a difficulty picker and a way to the upgrade screen.
    /// Selecting an open level starts it on the chosen difficulty.
    /// </summary>
    public sealed class WorldMapScreen : ScreenBase
    {
        private const float MapWidth = 1400f;
        private const float MapHeight = 560f;
        private const float NodeWidth = 300f;
        private const float NodeHeight = 110f;
        private const int NodeFontSize = 28;

        private CampaignCatalog _catalog;
        private int _difficultyIndex;
        private Button _difficultyButton;
        private readonly List<Button> _nodeButtons = new List<Button>();

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog))
            {
                Go(SceneNames.Boot);
                return;
            }

            UiKit.CreateFixedText(root, "Select a level", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 100f));

            var map = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
            map.SetParent(root, false);
            map.anchorMin = new Vector2(0.5f, 0.5f);
            map.anchorMax = new Vector2(0.5f, 0.5f);
            map.pivot = new Vector2(0.5f, 0.5f);
            map.sizeDelta = new Vector2(MapWidth, MapHeight);
            map.anchoredPosition = new Vector2(0f, 30f);

            Button firstOpen = null;
            for (int i = 0; i < _catalog.Nodes.Count; i++)
            {
                Button button = CreateNode(map, i);
                _nodeButtons.Add(button);
                if (firstOpen == null && button.interactable)
                {
                    firstOpen = button;
                }
            }

            RectTransform bottom = UiKit.CreateColumn(root, 16f);
            bottom.anchorMin = new Vector2(0.5f, 0f);
            bottom.anchorMax = new Vector2(0.5f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = new Vector2(0f, 20f);
            _difficultyButton = UiKit.CreateButton(bottom, DifficultyLabel(), CycleDifficulty);
            UiKit.CreateButton(bottom, "Upgrades", () => Go(SceneNames.Base));
            UiKit.CreateButton(bottom, "Back", () => Go(SceneNames.MainMenu));

            SetFirstSelected(firstOpen != null ? firstOpen : _difficultyButton);
        }

        private Button CreateNode(RectTransform map, int index)
        {
            MapNode node = _catalog.Nodes[index];
            bool open = CampaignRules.IsOpen(_catalog, index, Progress);
            bool completed = CampaignRules.IsCompleted(_catalog, index, Progress);

            string status = node.Level == null ? "Coming soon" : completed ? "Completed" : open ? "Open" : "Locked";
            Button button = UiKit.CreateButton(map, $"{index + 1}. {node.DisplayName}\n{status}", open ? (UnityEngine.Events.UnityAction)(() => StartLevel(index)) : null);
            button.interactable = open;

            var layout = button.GetComponent<LayoutElement>();
            Destroy(layout);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = node.MapPosition;
            rect.anchorMax = node.MapPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(NodeWidth, NodeHeight);
            rect.anchoredPosition = Vector2.zero;
            button.GetComponentInChildren<Text>().resizeTextMaxSize = NodeFontSize;
            return button;
        }

        private void CycleDifficulty()
        {
            _difficultyIndex = (_difficultyIndex + 1) % _catalog.Difficulties.Count;
            UiKit.SetLabel(_difficultyButton, DifficultyLabel());
        }

        private string DifficultyLabel() => $"Difficulty: {_catalog.Difficulties[_difficultyIndex].DisplayName}";

        private void StartLevel(int index)
        {
            LevelDefinition level = _catalog.Nodes[index].Level;
            Services.Register(new LevelSession(level, _catalog.Difficulties[_difficultyIndex]));
            Go(SceneNames.Level);
        }
    }
}

