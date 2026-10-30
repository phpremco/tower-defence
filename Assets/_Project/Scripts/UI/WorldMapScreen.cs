using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The planet map: every level is a region on the surface with a beacon on it. Choosing a beacon zooms onto its
    /// region and opens the region panel with the briefing and the difficulties; cancelling zooms back out.
    /// </summary>
    public sealed class WorldMapScreen : ScreenBase
    {
        private const string Title = "Choose a region";
        private const float MapWidth = 1500f;
        private const float MapHeight = 560f;
        private const float PlanetSize = 1500f;
        private const float HaloScale = 1.22f;
        private const int SurfaceTexture = 1024;
        private const int CloudTexture = 1024;
        private const int HaloTexture = 256;
        private const float RegionRadius = 0.3f;
        private const float CloudTurnDegreesPerSecond = 0.8f;
        private const float BeaconSize = 54f;
        private const float RingSize = 100f;
        private const float RingPulseRate = 2.6f;
        private const float RingPulseDepth = 0.14f;
        private const float CheckSize = 24f;
        private const float LabelWidth = 340f;
        private const float LabelHeight = 70f;
        private const float LabelDrop = -36f;
        private const int LabelSize = 24;
        private const float DotSpacing = 24f;
        private const float DotSize = 7f;
        private const float ZoomScale = 2.1f;
        private const float ZoomSeconds = 0.55f;
        private const float ZoomFocusX = -380f;
        private const float ActionWidth = 480f;
        private const float PanelWidth = 720f;
        private const float PanelHeight = 900f;
        private const float PanelRight = -70f;
        private const float DifficultyButtonWidth = 600f;
        private const float DifficultyTop = -420f;
        private const float DifficultyGap = 16f;
        private const int RegionNameSize = 30;
        private const int LevelNameSize = 52;
        private const int BriefingSize = 26;
        private static readonly Vector2 ViewHome = new Vector2(0f, -40f);
        private static readonly Color RouteColor = new Color(0.55f, 0.85f, 1f, 0.45f);
        private static readonly Color LockedColor = new Color(0.35f, 0.38f, 0.45f);
        private static readonly Color HaloColor = new Color(0.45f, 0.8f, 1f, 0.5f);
        private static readonly Color CloudColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color PanelTint = new Color(0.08f, 0.13f, 0.24f, 0.96f);
        private static readonly Color Muted = new Color(0.7f, 0.8f, 0.95f);
        private static readonly Color Completed = new Color(0.4f, 0.95f, 0.5f);
        private static readonly Color[] BadgeOn =
        {
            new Color(0.4f, 0.95f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
        };

        private CampaignCatalog _catalog;
        private IInputProvider _input;
        private RectTransform _root;
        private RectTransform _view;
        private RectTransform _clouds;
        private CanvasGroup _route;
        private CanvasGroup _actions;
        private GameObject _regionPanel;
        private readonly List<Button> _beacons = new List<Button>();
        private readonly List<CanvasGroup> _beaconGroups = new List<CanvasGroup>();
        private readonly List<RectTransform> _rings = new List<RectTransform>();
        private int _selected = -1;
        private float _zoom;
        private float _zoomTarget;
        private Vector2 _zoomOffset;

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog))
            {
                Go(SceneNames.Boot);
                return;
            }

            _root = root;
            if (Services.TryGet(out _input))
            {
                _input.Cancelled += OnCancel;
            }

            _view = CreateRect("View", root, new Vector2(PlanetSize, PlanetSize));
            _view.anchoredPosition = ViewHome;
            BuildPlanet(_view);
            BuildRoute(_view);

            Button firstOpen = null;
            for (int i = 0; i < _catalog.Nodes.Count; i++)
            {
                Button beacon = CreateBeacon(_view, i);
                _beacons.Add(beacon);
                if (firstOpen == null && beacon.interactable)
                {
                    firstOpen = beacon;
                }
            }

            // Drawn after the globe, so the title stays readable over its bright top rim.
            UiKit.CreateFixedText(root, Title, UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 100f));
            BuildActionRow(root);
            SetFirstSelected(firstOpen != null ? firstOpen : _beacons[0]);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancel;
            }
        }

        private void Update()
        {
            if (_clouds == null)
            {
                return;
            }

            _clouds.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * CloudTurnDegreesPerSecond);
            for (int i = 0; i < _rings.Count; i++)
            {
                if (_rings[i] != null)
                {
                    _rings[i].localScale = Vector3.one * (1f + RingPulseDepth * Mathf.Sin(Time.unscaledTime * RingPulseRate + i));
                }
            }

            if (!Mathf.Approximately(_zoom, _zoomTarget))
            {
                _zoom = Mathf.MoveTowards(_zoom, _zoomTarget, Time.unscaledDeltaTime / ZoomSeconds);
                ApplyZoom();
            }
        }

        // The halo behind, the painted surface and the clouds turning over it. The planet is taller than the screen,
        // so it reads as a globe seen up close rather than a picture of one.
        private void BuildPlanet(RectTransform view)
        {
            RawImage halo = CreateRawImage("Halo", view, PlanetPainter.PaintHalo(HaloTexture), HaloColor);
            halo.rectTransform.sizeDelta = new Vector2(PlanetSize * HaloScale, PlanetSize * HaloScale);

            CreateRawImage("Surface", view, PlanetPainter.PaintSurface(SurfaceTexture, Regions(), new Vector2(MapWidth / PlanetSize, MapHeight / PlanetSize)), Color.white);
            _clouds = CreateRawImage("Clouds", view, PlanetPainter.PaintClouds(CloudTexture), CloudColor).rectTransform;
        }

        private List<PlanetPainter.Region> Regions()
        {
            var regions = new List<PlanetPainter.Region>();
            foreach (MapNode node in _catalog.Nodes)
            {
                regions.Add(new PlanetPainter.Region { Position = node.MapPosition, Colour = RegionColour(node.Level), Radius = RegionRadius });
            }

            return regions;
        }

        // A region wears its level's palette, so the lava caves glow orange on the map and the woods turquoise.
        private static Color RegionColour(LevelDefinition level)
        {
            if (level == null || level.Theme == null)
            {
                return LockedColor;
            }

            Color colour = Color.Lerp(level.Theme.GroundColor, level.Theme.AccentColor, 0.5f) * 1.15f;
            colour.a = 1f;
            return colour;
        }

        // Dotted lines between consecutive levels, so the order of the campaign stays readable without panels.
        private void BuildRoute(RectTransform view)
        {
            RectTransform route = CreateRect("Route", view, view.sizeDelta);
            _route = route.gameObject.AddComponent<CanvasGroup>();
            _route.blocksRaycasts = false;
            _route.interactable = false;
            for (int i = 1; i < _catalog.Nodes.Count; i++)
            {
                Vector2 from = MapPoint(i - 1);
                Vector2 to = MapPoint(i);
                int count = Mathf.Max(2, Mathf.FloorToInt((to - from).magnitude / DotSpacing));
                for (int k = 1; k < count; k++)
                {
                    Image dot = CreateIcon("Dot", route, IconType.Disc, RouteColor, DotSize);
                    dot.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k / (float)count);
                }
            }
        }

        private Vector2 MapPoint(int index) => Vector2.Scale(_catalog.Nodes[index].MapPosition - new Vector2(0.5f, 0.5f), new Vector2(MapWidth, MapHeight));

        // A beacon on the region: a pulsing ring while the level is open, a check once it has been beaten, and the name below.
        private Button CreateBeacon(RectTransform view, int index)
        {
            MapNode node = _catalog.Nodes[index];
            bool open = CampaignRules.IsOpen(_catalog, index, Progress);
            Color colour = open ? RegionColour(node.Level) : LockedColor;

            RectTransform holder = CreateRect("Beacon", view, new Vector2(BeaconSize, BeaconSize));
            holder.anchoredPosition = MapPoint(index);
            _beaconGroups.Add(holder.gameObject.AddComponent<CanvasGroup>());

            RectTransform ring = null;
            if (open)
            {
                Color ringColour = colour;
                ringColour.a = 0.75f;
                ring = CreateIcon("Ring", holder, IconType.Ring, ringColour, RingSize).rectTransform;
            }

            _rings.Add(ring);

            Image image = CreateIcon("Mark", holder, IconType.Beacon, Color.white, BeaconSize);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colours = ColorBlock.defaultColorBlock;
            colours.normalColor = colour;
            colours.highlightedColor = Color.Lerp(colour, Color.white, 0.5f);
            colours.selectedColor = Color.Lerp(colour, Color.white, 0.5f);
            colours.pressedColor = Color.white;
            colours.disabledColor = LockedColor;
            colours.colorMultiplier = 1f;
            button.colors = colours;
            button.interactable = open;
            if (open)
            {
                button.onClick.AddListener(() => Choose(index));
            }

            if (node.Level != null && Progress.IsLevelCompleted(node.Level.Id))
            {
                Image check = CreateIcon("Done", holder, IconType.Check, Completed, CheckSize);
                check.rectTransform.anchoredPosition = new Vector2(BeaconSize * 0.5f, BeaconSize * 0.5f);
            }

            string status = node.Level == null ? "\nComing soon" : open ? string.Empty : "\nLocked";
            Text label = UiKit.CreateFixedText(holder, $"{index + 1}. {node.DisplayName}{status}", LabelSize, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0f, LabelDrop), new Vector2(LabelWidth, LabelHeight));
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.color = open ? Color.white : Muted;
            return button;
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
            _actions = row.gameObject.AddComponent<CanvasGroup>();

            UiKit.CreateButton(row, "Upgrades", () => Go(SceneNames.Base));
            UiKit.CreateButton(row, "Back", () => Go(SceneNames.MainMenu));
            foreach (Button button in row.GetComponentsInChildren<Button>())
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(ActionWidth, UiKit.ButtonHeight);
            }
        }

        // Choosing a beacon flies the view onto its region; the panel opens once the flight has landed.
        private void Choose(int index)
        {
            _selected = index;
            _zoomTarget = 1f;
            _zoomOffset = new Vector2(ZoomFocusX, 0f) - MapPoint(index) * ZoomScale;
            SetBeaconsInteractable(false);
        }

        private void ApplyZoom()
        {
            float t = _zoom * _zoom * (3f - 2f * _zoom);
            _view.localScale = Vector3.one * (1f + t * (ZoomScale - 1f));
            _view.anchoredPosition = ViewHome + _zoomOffset * t;
            _route.alpha = 1f - t;
            for (int i = 0; i < _beaconGroups.Count; i++)
            {
                _beaconGroups[i].alpha = i == _selected ? 1f : 1f - t;
            }

            _actions.alpha = 1f - t;
            _actions.interactable = _zoom < 0.5f;
            _actions.blocksRaycasts = _zoom < 0.5f;

            if (_zoom >= 1f && _regionPanel == null)
            {
                OpenRegionPanel();
            }
            else if (_zoom <= 0f)
            {
                SetBeaconsInteractable(true);
                UiKit.Select(_beacons[_selected]);
                _selected = -1;
            }
        }

        private void SetBeaconsInteractable(bool on)
        {
            for (int i = 0; i < _beacons.Count; i++)
            {
                _beacons[i].interactable = on && CampaignRules.IsOpen(_catalog, i, Progress);
            }
        }

        // The region panel: the name of the region, its level, the briefing and the four difficulties with their marks.
        private void OpenRegionPanel()
        {
            LevelDefinition level = _catalog.Nodes[_selected].Level;
            Image panel = UiKit.CreatePanel(_root, "RegionPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = new Vector2(PanelRight, 0f);
            _regionPanel = panel.gameObject;

            float width = PanelWidth - 80f;
            Text region = UiKit.CreateFixedText(rect, level.RegionName, RegionNameSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(width, 40f));
            region.color = Muted;
            UiKit.CreateFixedText(rect, level.DisplayName, LevelNameSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(width, 70f));
            Text briefing = UiKit.CreateFixedText(rect, level.Briefing, BriefingSize, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(width, 190f));
            briefing.horizontalOverflow = HorizontalWrapMode.Wrap;
            briefing.color = Muted;
            UiKit.CreateFixedText(rect, "Choose a difficulty", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, DifficultyTop + 46f), new Vector2(width, 40f));

            Button first = null;
            float y = DifficultyTop;
            for (int i = 0; i < _catalog.Difficulties.Count; i++)
            {
                Button button = CreateDifficultyButton(rect, level, _catalog.Difficulties[i], i, y);
                first = first != null ? first : button;
                y -= UiKit.ButtonHeight + DifficultyGap;
            }

            Button back = UiKit.CreateButton(rect, "Back", CloseRegion);
            Destroy(back.GetComponent<LayoutElement>());
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(DifficultyButtonWidth, UiKit.ButtonHeight);
            backRect.anchoredPosition = new Vector2(0f, 24f);

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
            label.rectTransform.offsetMax = new Vector2(-220f, -4f);

            // Three small stars on the right: lit for the stars earned on this difficulty.
            for (int s = 0; s < 3; s++)
            {
                Image star = CreateIcon("Star", rect, IconType.Star, s < stars ? new Color(1f, 0.85f, 0.25f) : new Color(0.12f, 0.16f, 0.26f), 44f);
                star.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                star.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                star.rectTransform.anchoredPosition = new Vector2(-46f - (2 - s) * 50f, 0f);
            }

            if (done)
            {
                Image tick = CreateIcon("Done", rect, IconType.Check, BadgeOn[Mathf.Min(index, BadgeOn.Length - 1)], 44f);
                tick.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                tick.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                tick.rectTransform.anchoredPosition = new Vector2(250f, 0f);
            }

            return button;
        }

        private void CloseRegion()
        {
            if (_regionPanel == null)
            {
                return;
            }

            Destroy(_regionPanel);
            _regionPanel = null;
            _zoomTarget = 0f;
        }

        // B closes the region panel, and from the overview it goes back to the main menu like the Back button.
        private void OnCancel()
        {
            if (_regionPanel != null)
            {
                CloseRegion();
            }
            else if (_zoomTarget <= 0f && _zoom <= 0f)
            {
                Go(SceneNames.MainMenu);
            }
        }

        private static void StartLevel(LevelDefinition level, DifficultyProfile difficulty)
        {
            Services.Register(new LevelSession(level, difficulty, true));
            Go(SceneNames.Level);
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static RawImage CreateRawImage(string name, RectTransform parent, Texture2D texture, Color tint)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.color = tint;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = parent.sizeDelta;
            return image;
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
