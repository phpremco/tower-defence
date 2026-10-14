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
        private const float MapWidth = 1500f;
        private const float MapHeight = 560f;
        private const float NodeWidth = 330f;
        private const float NodeHeight = 150f;
        private const int NodeFontSize = 28;
        private const float ActionWidth = 480f;
        private const int PlanetTexture = 512;
        private const float PlanetSize = 1150f;
        private const float LineThickness = 8f;
        private const float BadgeSize = 34f;
        private const float BadgeGap = 8f;
        private const float DifficultyButtonWidth = 760f;
        private static readonly Color LineColor = new Color(0.4f, 0.9f, 1f, 0.7f);
        private static readonly Color BadgeOff = new Color(0.12f, 0.14f, 0.2f, 0.9f);
        private static readonly Color[] BadgeOn =
        {
            new Color(0.4f, 0.95f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
        };

        private CampaignCatalog _catalog;
        private RectTransform _root;
        private GameObject _difficultyPanel;
        private Button _returnFocus;
        private readonly List<Button> _nodeButtons = new List<Button>();

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog))
            {
                Go(SceneNames.Boot);
                return;
            }

            _root = root;
            UiKit.CreateFixedText(root, "Select a level", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 100f));

            var map = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
            map.SetParent(root, false);
            map.anchorMin = new Vector2(0.5f, 0.5f);
            map.anchorMax = new Vector2(0.5f, 0.5f);
            map.pivot = new Vector2(0.5f, 0.5f);
            map.sizeDelta = new Vector2(MapWidth, MapHeight);
            map.anchoredPosition = new Vector2(0f, 20f);

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

            BuildActionRow(root);
            SetFirstSelected(firstOpen != null ? firstOpen : _nodeButtons[0]);
        }

        private void BuildActionRow(RectTransform root)
        {
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

            UiKit.CreateButton(row, "Upgrades", () => Go(SceneNames.Base));
            UiKit.CreateButton(row, "Back", () => Go(SceneNames.MainMenu));
            foreach (Button button in row.GetComponentsInChildren<Button>())
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(ActionWidth, UiKit.ButtonHeight);
            }
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

        // A node shows the level name and four badges, one per difficulty, lit once that difficulty is completed.
        private Button CreateNode(RectTransform map, int index)
        {
            MapNode node = _catalog.Nodes[index];
            bool open = CampaignRules.IsOpen(_catalog, index, Progress);
            string status = node.Level == null ? "Coming soon" : open ? string.Empty : "Locked";
            string label = status.Length == 0 ? $"{index + 1}. {node.DisplayName}" : $"{index + 1}. {node.DisplayName}\n{status}";

            Button button = UiKit.CreateButton(map, label, open ? (UnityEngine.Events.UnityAction)(() => OpenDifficulties(index)) : null);
            button.interactable = open;

            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = node.MapPosition;
            rect.anchorMax = node.MapPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(NodeWidth, NodeHeight);
            rect.anchoredPosition = Vector2.zero;

            Text text = button.GetComponentInChildren<Text>();
            text.resizeTextMaxSize = NodeFontSize;
            text.alignment = TextAnchor.UpperCenter;
            text.rectTransform.offsetMin = new Vector2(12f, BadgeSize + 18f);
            text.rectTransform.offsetMax = new Vector2(-12f, -8f);

            if (node.Level != null)
            {
                BuildBadges(rect, node.Level);
            }

            return button;
        }

        private void BuildBadges(RectTransform parent, LevelDefinition level)
        {
            int count = _catalog.Difficulties.Count;
            float width = count * BadgeSize + (count - 1) * BadgeGap;
            for (int i = 0; i < count; i++)
            {
                bool done = Progress.IsCompleted(level.Id, _catalog.Difficulties[i].name);

                var go = new GameObject("Badge", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var image = go.AddComponent<Image>();
                UiKit.StyleSliced(image, UiKit.Skin != null ? UiKit.Skin.ButtonSquare : null);
                image.color = done ? BadgeOn[Mathf.Min(i, BadgeOn.Length - 1)] : BadgeOff;
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
                rect.anchoredPosition = new Vector2(-width * 0.5f + BadgeSize * 0.5f + i * (BadgeSize + BadgeGap), 10f);

                if (done)
                {
                    var tick = new GameObject("Check", typeof(RectTransform)).AddComponent<Image>();
                    tick.transform.SetParent(rect, false);
                    tick.sprite = IconFactory.Get(IconType.Check);
                    tick.color = new Color(0.05f, 0.1f, 0.15f);
                    tick.raycastTarget = false;
                    RectTransform tickRect = tick.rectTransform;
                    tickRect.anchorMin = Vector2.zero;
                    tickRect.anchorMax = Vector2.one;
                    tickRect.offsetMin = new Vector2(3f, 3f);
                    tickRect.offsetMax = new Vector2(-3f, -3f);
                }
            }
        }

        // Choosing a level opens the four difficulties with their completion marks and star ratings.
        private void OpenDifficulties(int index)
        {
            LevelDefinition level = _catalog.Nodes[index].Level;
            _returnFocus = _nodeButtons[index];

            Image dim = UiKit.CreatePanel(_root, "DifficultyDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
            dim.raycastTarget = true;
            _difficultyPanel = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "DifficultyPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.14f, 0.22f, 0.4f, 0.98f));
            panel.rectTransform.sizeDelta = new Vector2(DifficultyButtonWidth + 120f, 700f);

            UiKit.CreateFixedText(panel.rectTransform, level.DisplayName, 56, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(DifficultyButtonWidth, 80f));
            UiKit.CreateFixedText(panel.rectTransform, "Choose a difficulty", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(DifficultyButtonWidth, 50f));

            Button first = null;
            float y = -180f;
            for (int i = 0; i < _catalog.Difficulties.Count; i++)
            {
                DifficultyProfile difficulty = _catalog.Difficulties[i];
                Button button = CreateDifficultyButton(panel.rectTransform, level, difficulty, i, y);
                first = first != null ? first : button;
                y -= UiKit.ButtonHeight + 20f;
            }

            Button cancel = UiKit.CreateButton(panel.rectTransform, "Cancel", CloseDifficulties);
            Destroy(cancel.GetComponent<LayoutElement>());
            RectTransform cancelRect = cancel.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.sizeDelta = new Vector2(DifficultyButtonWidth, UiKit.ButtonHeight);
            cancelRect.anchoredPosition = new Vector2(0f, 24f);

            UiKit.Select(first);
        }

        private Button CreateDifficultyButton(RectTransform panel, LevelDefinition level, DifficultyProfile difficulty, int index, float y)
        {
            bool done = Progress.IsCompleted(level.Id, difficulty.name);
            int stars = Progress.GetStars(level.Id, difficulty.name);

            Button button = UiKit.CreateButton(panel, difficulty.DisplayName, () => StartLevel(level, difficulty));
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(DifficultyButtonWidth, UiKit.ButtonHeight);
            rect.anchoredPosition = new Vector2(0f, y);

            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(30f, 4f);
            label.rectTransform.offsetMax = new Vector2(-240f, -4f);

            // Three small stars on the right: lit for the stars earned on this difficulty.
            for (int s = 0; s < 3; s++)
            {
                var go = new GameObject("Star", typeof(RectTransform)).AddComponent<Image>();
                go.transform.SetParent(rect, false);
                go.sprite = IconFactory.Get(IconType.Star);
                go.color = s < stars ? new Color(1f, 0.85f, 0.25f) : new Color(0.12f, 0.16f, 0.26f);
                go.raycastTarget = false;
                RectTransform starRect = go.rectTransform;
                starRect.anchorMin = new Vector2(1f, 0.5f);
                starRect.anchorMax = new Vector2(1f, 0.5f);
                starRect.pivot = new Vector2(1f, 0.5f);
                starRect.sizeDelta = new Vector2(46f, 46f);
                starRect.anchoredPosition = new Vector2(-30f - (2 - s) * 52f, 0f);
            }

            if (done)
            {
                var tick = new GameObject("Done", typeof(RectTransform)).AddComponent<Image>();
                tick.transform.SetParent(rect, false);
                tick.sprite = IconFactory.Get(IconType.Check);
                tick.color = BadgeOn[Mathf.Min(index, BadgeOn.Length - 1)];
                tick.raycastTarget = false;
                RectTransform tickRect = tick.rectTransform;
                tickRect.anchorMin = new Vector2(0f, 0.5f);
                tickRect.anchorMax = new Vector2(0f, 0.5f);
                tickRect.pivot = new Vector2(0f, 0.5f);
                tickRect.sizeDelta = new Vector2(46f, 46f);
                tickRect.anchoredPosition = new Vector2(270f, 0f);
            }

            return button;
        }

        private void CloseDifficulties()
        {
            Destroy(_difficultyPanel);
            UiKit.Select(_returnFocus);
        }

        private static void StartLevel(LevelDefinition level, DifficultyProfile difficulty)
        {
            Services.Register(new LevelSession(level, difficulty));
            Go(SceneNames.Level);
        }
    }
}
