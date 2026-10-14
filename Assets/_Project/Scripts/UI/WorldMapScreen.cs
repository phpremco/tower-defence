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
        private const float ActionWidth = 480f;
        private const int PlanetTexture = 512;
        private const float PlanetSize = 1150f;
        private const float LineThickness = 8f;
        private static readonly Color LineColor = new Color(0.4f, 0.9f, 1f, 0.7f);

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

            BuildPlanet(map);
            BuildConnections(map);

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

            var row = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(root, false);
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, 30f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _difficultyButton = UiKit.CreateButton(row, DifficultyLabel(), CycleDifficulty);
            UiKit.CreateButton(row, "Upgrades", () => Go(SceneNames.Base));
            UiKit.CreateButton(row, "Back", () => Go(SceneNames.MainMenu));
            foreach (Button button in row.GetComponentsInChildren<Button>())
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(ActionWidth, UiKit.ButtonHeight);
            }
            SetFirstSelected(firstOpen != null ? firstOpen : _difficultyButton);
        }

        // A procedural planet behind the level nodes: noisy land and sea colours with a bright atmosphere rim.
        private static void BuildPlanet(RectTransform map)
        {
            var texture = new Texture2D(PlanetTexture, PlanetTexture, TextureFormat.RGBA32, false);
            var pixels = new Color32[PlanetTexture * PlanetTexture];
            float half = PlanetTexture * 0.5f;
            for (int y = 0; y < PlanetTexture; y++)
            {
                for (int x = 0; x < PlanetTexture; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 1f)
                    {
                        pixels[y * PlanetTexture + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float n = Mathf.PerlinNoise(x * 0.012f + 3f, y * 0.012f + 7f) * 0.7f + Mathf.PerlinNoise(x * 0.04f, y * 0.04f) * 0.3f;
                    Color land = Color.Lerp(new Color(0.08f, 0.22f, 0.3f), new Color(0.35f, 0.5f, 0.3f), Smooth(0.35f, 0.65f, n));
                    land = Color.Lerp(land, new Color(0.55f, 0.25f, 0.12f), Smooth(0.62f, 0.8f, n) * 0.6f);
                    float shade = Mathf.Lerp(1f, 0.35f, (dx + dy + 2f) * 0.25f);
                    float rim = Smooth(0.82f, 1f, d);
                    Color final = Color.Lerp(land * shade, new Color(0.5f, 0.85f, 1f), rim * 0.8f);
                    float alpha = Mathf.Clamp01((1f - d) * 40f);
                    pixels[y * PlanetTexture + x] = new Color(final.r, final.g, final.b, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var go = new GameObject("Planet", typeof(RectTransform));
            go.transform.SetParent(map, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0.55f);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PlanetSize, PlanetSize);
            rect.anchoredPosition = Vector2.zero;
        }

        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        // Glowing lines between consecutive levels, so the order of the campaign is visible.
        private void BuildConnections(RectTransform map)
        {
            for (int i = 1; i < _catalog.Nodes.Count; i++)
            {
                Vector2 from = Vector2.Scale(_catalog.Nodes[i - 1].MapPosition - new Vector2(0.5f, 0.5f), map.sizeDelta);
                Vector2 to = Vector2.Scale(_catalog.Nodes[i].MapPosition - new Vector2(0.5f, 0.5f), map.sizeDelta);
                Vector2 delta = to - from;

                var go = new GameObject("Link", typeof(RectTransform));
                go.transform.SetParent(map, false);
                var image = go.AddComponent<Image>();
                image.color = LineColor;
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(delta.magnitude, LineThickness);
                rect.anchoredPosition = (from + to) * 0.5f;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
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



