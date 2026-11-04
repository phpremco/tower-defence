using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using Game.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The planet map in two layers. The planet: a turning globe with one continent per region and a badge on each,
    /// no names on the surface. Entering a region flies the view onto that continent and shows its levels as small
    /// numbered markers joined by a glowing route, each with a bar per difficulty, lit once beaten. The stick, the
    /// arrow keys or the arrow buttons beside the globe turn the planet; the shoulder buttons step through the
    /// regions or the levels; A enters a region or chooses a level; B goes back a layer. Choosing a level turns and
    /// zooms the globe onto it and opens the level panel with the briefing and the difficulties.
    /// </summary>
    public sealed class WorldMapScreen : ScreenBase
    {
        private const string Title = "Choose a region";
        private const float GlobeSize = 1100f;
        private const int GlobeTexture = 2048;
        private const int CloudTexture = 1024;
        private static GlobePainter.Maps _globeMaps;
        private static Texture2D[] _globeClouds;
        private const int HaloTexture = 256;
        private const float HaloScale = 1.28f;
        private const float SpinDegreesPerSecond = 70f;
        private const float TiltDegreesPerSecond = 45f;
        private const float StepDeadzone = 0.5f;
        private const float StepRepeatSeconds = 0.32f;
        private const float BadgeSize = 64f;
        private const float BadgeRingSize = 94f;
        private const float MarkerSize = 52f;
        private const float MarkerZoomScale = 1.5f;
        private const float RingSize = 78f;
        private const float RingPulseRate = 2.6f;
        private const float RingPulseDepth = 0.12f;
        private const float CheckSize = 18f;
        private const float DoneBadgeSize = 28f;
        private const float LockSize = 22f;
        private const float BadgeIconScale = 1.3f;
        private const float BarWidth = 9f;
        private const float BarHeight = 5f;
        private const float BarGap = 3f;
        private const float BarDrop = -31f;
        private const float RouteSpacingDegrees = 1.6f;
        private const float DotSize = 8f;
        private const float RimFadeStart = 0.08f;
        private const float RimFadeEnd = 0.3f;
        private const float FocusMinFacing = 0.35f;
        private const float LayerFadeStart = 0.35f;
        private const float LayerFadeEnd = 0.75f;
        private const float SideButtonSize = 96f;
        private const float SideButtonOffset = 640f;
        private const float HoldSnapSeconds = 0.22f;
        private const float ActionWidth = 420f;
        private const float PanelWidth = 720f;
        private const float PanelHeight = 900f;
        private const float PanelRight = -70f;
        private const float DifficultyButtonWidth = 600f;
        private const float DifficultyTop = -420f;
        private const float DifficultyGap = 16f;
        private const int RegionNameSize = 30;
        private const int LevelNameSize = 52;
        private const int BriefingSize = 26;
        private const int CaptionSize = 34;
        private const int HintSize = 24;
        private const string PlanetHint = "Stick or arrow keys: turn the planet     LB / RB or Q / E: previous and next region     A / Enter: enter the region     B / Esc: back";
        private const string RegionHint = "Left / Right, LB / RB or Q / E: previous and next level     A / Enter: play     B / Esc: back to the planet     Click a level to select it, click again to play";
        private static readonly Vector2 ViewHome = new Vector2(0f, 56f);
        private static readonly Color LockedColor = new Color(0.4f, 0.43f, 0.5f);
        private static readonly Color LockedPlate = new Color(0.22f, 0.24f, 0.3f);

        // The Core's crack: dark lines over a faint ember glow, three strokes that zigzag like a fissure.
        private static readonly Color CrackColor = new Color(1f, 0.42f, 0.16f, 0.9f);
        private static readonly Color CrackGlow = new Color(0.9f, 0.25f, 0.08f, 0.22f);
        private const float CrackScale = 0.8f;
        private const float CrackWidth = 4f;
        private static readonly float[] CrackAngles = { 28f, -18f, 34f };
        private static readonly float[] CrackLengths = { 0.42f, 0.36f, 0.3f };
        private static readonly Vector2[] CrackOffsets = { new Vector2(-12f, 8f), new Vector2(4f, -2f), new Vector2(17f, -12f) };
        private static readonly Color HaloColor = new Color(0.45f, 0.8f, 1f, 0.55f);
        private static readonly Color RouteDone = new Color(0.7f, 0.95f, 1f, 0.9f);
        private static readonly Color RouteOpen = new Color(0.55f, 0.85f, 1f, 0.8f);
        private static readonly Color RouteLocked = new Color(0.62f, 0.68f, 0.8f, 0.6f);
        private static readonly Color PanelTint = new Color(0.08f, 0.13f, 0.24f, 0.96f);
        private static readonly Color Muted = new Color(0.7f, 0.8f, 0.95f);
        private static readonly Color Completed = new Color(0.4f, 0.95f, 0.5f);
        private static readonly Color BadgeBack = new Color(0.04f, 0.07f, 0.12f, 0.95f);
        private static readonly Color BarOff = new Color(0.1f, 0.12f, 0.18f, 0.9f);
        private static readonly Color[] BadgeOn =
        {
            new Color(0.4f, 0.95f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
        };

        // A marker on the surface: a region badge on the planet layer or a level marker on the region layer.
        private sealed class Marker
        {
            public int Index;
            public bool Open;
            public Vector3 Direction;
            public RectTransform Root;
            public CanvasGroup Group;
            public RectTransform Ring;
        }

        private sealed class RouteDot
        {
            public Vector3 Direction;
            public RectTransform Rect;
            public Image Image;
            public Color Colour;
            public bool Pulsing;
        }

        private readonly List<Marker> _badges = new List<Marker>();
        private readonly List<Marker> _markers = new List<Marker>();
        private readonly List<RouteDot> _route = new List<RouteDot>();
        private CampaignCatalog _catalog;
        private IInputProvider _input;
        private RectTransform _root;
        private RectTransform _view;
        private RectTransform _halo;
        private RawImage _haloImage;
        private PlanetGlobe _globe;
        private CanvasGroup _overview;
        private Text _caption;
        private Text _hint;
        private Button _planet;
        private Button _play;
        private Button _upgrades;
        private Button _back;
        private float _stepRepeat;
        private GameObject _regionPanel;
        private LoadoutPanelView _loadout;
        private int _region = -1;
        private bool _entered;
        private int _focused = -1;
        private int _chosen = -1;
        private float _heldSince = -1f;
        private int _heldDirection;
        private bool _rowFocused;

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
                _input.Confirmed += OnConfirm;
                _input.NextTower += () => Step(1);
                _input.PreviousTower += () => Step(-1);
            }

            _view = CreateRect("View", root, new Vector2(GlobeSize, GlobeSize));
            _view.anchoredPosition = ViewHome;
            BuildGlobe(_view);
            BuildRoute(_view);
            for (int i = 0; i < _catalog.Regions.Count; i++)
            {
                _badges.Add(CreateBadge(_view, i));
            }

            for (int i = 0; i < _catalog.Nodes.Count; i++)
            {
                _markers.Add(CreateMarker(_view, i));
            }

            // Everything around the globe fades away while the view flies onto a level, so the panel has the screen.
            RectTransform chrome = CreateRect("Chrome", root, Vector2.zero);
            chrome.anchorMin = Vector2.zero;
            chrome.anchorMax = Vector2.one;
            chrome.offsetMin = Vector2.zero;
            chrome.offsetMax = Vector2.zero;
            _overview = chrome.gameObject.AddComponent<CanvasGroup>();
            UiKit.CreateFixedText(chrome, Title, UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 100f));
            _caption = UiKit.CreateFixedText(chrome, string.Empty, CaptionSize, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1200f, 50f));
            _hint = UiKit.CreateFixedText(chrome, PlanetHint, HintSize, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(1700f, 34f));
            _hint.color = Muted;
            BuildSideButtons(chrome);
            BuildActionRow(chrome);

            // Open inside the region the player is working on, flying in from the whole planet.
            int start = FurthestOpenNode();
            _region = _catalog.Nodes[start].Region;
            _globe.SnapTo(_catalog.Regions[_region].Direction);
            EnterRegion(_region, start);
            SetFirstSelected(null);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancel;
                _input.Confirmed -= OnConfirm;
            }
        }

        private int FurthestOpenNode()
        {
            int start = 0;
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i].Open)
                {
                    start = i;
                }
            }

            return start;
        }

        // ---- building --------------------------------------------------------------------------------------------

        private void BuildGlobe(RectTransform view)
        {
            RawImage halo = CreateRawImage("Halo", view, PlanetPainter.PaintHalo(HaloTexture), HaloColor);
            _halo = halo.rectTransform;
            _haloImage = halo;

            // The generator bakes the maps; painting them here is the fallback for a catalog without them, and the
            // result lives on between visits to the screen because it takes a moment.
            if (_globeMaps == null && _catalog.HasGlobeMaps)
            {
                _globeMaps = new GlobePainter.Maps { Albedo = _catalog.GlobeAlbedo, Normal = _catalog.GlobeNormal, Gloss = _catalog.GlobeGloss, Emission = _catalog.GlobeLights };
                _globeClouds = _catalog.GlobeClouds;
            }

            if (_globeMaps == null)
            {
                _globeMaps = GlobePainter.Paint(GlobeTexture, GlobeTexture / 2, GlobePainter.RegionsOf(_catalog));
                _globeClouds = new[] { GlobePainter.PaintClouds(CloudTexture, CloudTexture / 2, 0f), GlobePainter.PaintClouds(CloudTexture, CloudTexture / 2, 200f) };
            }

            _globe = PlanetGlobe.Create(view, GlobeSize, _globeMaps, _globeClouds, _catalog.PlanetStyle, _catalog.PlanetTextures);
        }

        // A level wears its palette, so the lava caves glow orange on the map and the woods turquoise.
        private static Color LevelColour(LevelDefinition level)
        {
            if (level == null || level.Theme == null)
            {
                return LockedColor;
            }

            Color colour = Color.Lerp(level.Theme.GroundColor, level.Theme.AccentColor, 0.5f) * 1.15f;
            colour.a = 1f;
            return colour;
        }

        // Dots along the shortest route over the surface from each level to the next within a region. Their colour
        // tells the state of the leg: travelled, open (pulsing) or still locked.
        private void BuildRoute(RectTransform view)
        {
            for (int i = 1; i < _catalog.Nodes.Count; i++)
            {
                if (_catalog.Nodes[i].Region != _catalog.Nodes[i - 1].Region)
                {
                    continue;
                }

                Vector3 from = _catalog.Nodes[i - 1].Direction;
                Vector3 to = _catalog.Nodes[i].Direction;
                bool travelled = CampaignRules.IsCompleted(_catalog, i, Progress);
                bool open = CampaignRules.IsOpen(_catalog, i, Progress);
                Color colour = travelled ? RouteDone : open ? RouteOpen : RouteLocked;
                int count = Mathf.Max(2, Mathf.RoundToInt(PlanetCoordinates.AngleBetween(from, to) / RouteSpacingDegrees));
                for (int k = 1; k < count; k++)
                {
                    Image dot = CreateIcon("Dot", view, IconType.Disc, colour, DotSize);
                    _route.Add(new RouteDot { Direction = PlanetCoordinates.Along(from, to, k / (float)count), Rect = dot.rectTransform, Image = dot, Colour = colour, Pulsing = open && !travelled });
                }
            }
        }

        // A region badge: a round plate in the continent's colour, a tick once every level in it is beaten, a lock
        // while it cannot be entered yet. The caption below the globe names it and counts its levels.
        private Marker CreateBadge(RectTransform view, int index)
        {
            MapRegion region = _catalog.Regions[index];
            bool open = CampaignRules.IsRegionOpen(_catalog, index, Progress);
            int total = _catalog.NodeCount(index);
            bool done = total > 0 && CampaignRules.CompletedInRegion(_catalog, index, Progress) == total;
            Color colour = open ? region.Colour * 1.3f : LockedPlate;
            colour.a = 1f;

            RectTransform holder = CreateRect("Badge", view, new Vector2(BadgeSize, BadgeSize));
            var badge = new Marker { Index = index, Open = open, Direction = region.Direction, Root = holder, Group = holder.gameObject.AddComponent<CanvasGroup>() };
            if (region.Layer == MapLayer.Core)
            {
                BuildCrack(holder, badge, index);
                return badge;
            }

            Color ringColour = open ? Color.Lerp(colour, Color.white, 0.4f) : LockedColor;
            ringColour.a = 0.85f;
            badge.Ring = CreateIcon("Ring", holder, IconType.Ring, ringColour, BadgeRingSize).rectTransform;
            badge.Ring.gameObject.SetActive(false);

            Image plate = CreateIcon("Plate", holder, IconType.Disc, colour, BadgeSize);
            AddClick(plate, () => OnBadgeClicked(index));

            if (!open)
            {
                CreateIcon("Lock", holder, IconType.Lock, LockedColor, LockSize * BadgeIconScale);
            }
            else if (done)
            {
                CreateIcon("Done", holder, IconType.Check, new Color(0.05f, 0.1f, 0.08f), CheckSize * BadgeIconScale);
            }

            return badge;
        }

        // The Core lies under the surface, so it is no land and no plate: a thin dark crack with a faint ember glow.
        private void BuildCrack(RectTransform holder, Marker badge, int index)
        {
            Color ring = CrackColor;
            ring.a = 0.85f;
            badge.Ring = CreateIcon("Ring", holder, IconType.Ring, ring, BadgeRingSize * CrackScale).rectTransform;
            badge.Ring.gameObject.SetActive(false);
            Image glow = CreateIcon("Glow", holder, IconType.Disc, CrackGlow, BadgeSize * CrackScale);
            AddClick(glow, () => OnBadgeClicked(index));
            for (int i = 0; i < CrackAngles.Length; i++)
            {
                Image crack = CreateIcon("Crack", holder, IconType.Disc, CrackColor, BadgeSize);
                crack.sprite = null;
                crack.raycastTarget = false;
                crack.rectTransform.sizeDelta = new Vector2(BadgeSize * CrackLengths[i], CrackWidth);
                crack.rectTransform.anchoredPosition = CrackOffsets[i];
                crack.rectTransform.localRotation = Quaternion.Euler(0f, 0f, CrackAngles[i]);
            }
        }

        // A level marker: a hexagon plate in the level's colour with a tick once beaten or a lock while closed, and
        // four bars below for the difficulties, lit where that difficulty has been beaten. No text; the caption names it.
        private Marker CreateMarker(RectTransform view, int index)
        {
            MapNode node = _catalog.Nodes[index];
            bool open = CampaignRules.IsOpen(_catalog, index, Progress);
            Color colour = open ? LevelColour(node.Level) : LockedPlate;

            RectTransform holder = CreateRect("Marker", view, new Vector2(MarkerSize, MarkerSize));
            var marker = new Marker { Index = index, Open = open, Direction = node.Direction, Root = holder, Group = holder.gameObject.AddComponent<CanvasGroup>() };

            Color ringColour = open ? Color.Lerp(colour, Color.white, 0.4f) : LockedColor;
            ringColour.a = 0.85f;
            marker.Ring = CreateIcon("Ring", holder, IconType.Ring, ringColour, RingSize).rectTransform;
            marker.Ring.gameObject.SetActive(false);

            Image plate = CreateIcon("Plate", holder, IconType.Hex, colour, MarkerSize);
            AddClick(plate, () => OnMarkerClicked(index));

            // The number of the level, small and dark on the plate, so the order reads without any names.
            Text number = UiKit.CreateFixedText(holder, (index + 1).ToString(), 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MarkerSize, MarkerSize));
            number.color = open ? new Color(0.05f, 0.07f, 0.12f) : LockedColor;
            number.raycastTarget = false;
            Destroy(number.GetComponent<Outline>());

            if (!open)
            {
                number.enabled = false;
                CreateIcon("Lock", holder, IconType.Lock, LockedColor, LockSize);
            }
            else if (node.Level != null && Progress.IsLevelCompleted(node.Level.Id))
            {
                AddDoneBadge(holder, new Vector2(MarkerSize * 0.5f, MarkerSize * 0.5f), DoneBadgeSize);
            }

            int difficulties = _catalog.Difficulties.Count;
            float rowWidth = difficulties * BarWidth + (difficulties - 1) * BarGap;
            for (int d = 0; d < difficulties; d++)
            {
                bool done = node.Level != null && Progress.IsCompleted(node.Level.Id, _catalog.Difficulties[d].name);
                Image bar = CreateIcon("Bar", holder, IconType.Disc, done ? BadgeOn[Mathf.Min(d, BadgeOn.Length - 1)] : BarOff, BarWidth);
                bar.sprite = null;
                bar.rectTransform.sizeDelta = new Vector2(BarWidth, BarHeight);
                bar.rectTransform.anchoredPosition = new Vector2(-rowWidth * 0.5f + BarWidth * 0.5f + d * (BarWidth + BarGap), BarDrop);
            }

            return marker;
        }

        // A tick on a dark disc, so "beaten" reads over the ring, the plate and the continent alike.
        private static void AddDoneBadge(RectTransform holder, Vector2 position, float size)
        {
            Image disc = CreateIcon("DoneBadge", holder, IconType.Disc, BadgeBack, size);
            disc.rectTransform.anchoredPosition = position;
            Image check = CreateIcon("Done", holder, IconType.Check, Completed, size * 0.72f);
            check.rectTransform.anchoredPosition = position;
        }

        // Plates respond to the mouse without changing colour; the ring and the caption show the focus instead.
        private static void AddClick(Image plate, UnityEngine.Events.UnityAction onClick)
        {
            plate.raycastTarget = true;
            var button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colours = ColorBlock.defaultColorBlock;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colours.selectedColor = Color.white;
            colours.pressedColor = new Color(1.5f, 1.5f, 1.5f);
            colours.disabledColor = Color.white;
            colours.colorMultiplier = 1f;
            button.colors = colours;
            button.onClick.AddListener(onClick);
        }

        // Round arrow buttons either side of the globe for mouse users: hold to turn, tap to step.
        private void BuildSideButtons(RectTransform root)
        {
            SideButton(root, -SideButtonOffset, 180f, -1);
            SideButton(root, SideButtonOffset, 0f, 1);
        }

        private void SideButton(RectTransform root, float x, float arrowAngle, int direction)
        {
            Button button = UiKit.CreateIconButton(root, IconFactory.Get(IconType.Arrow), SideButtonSize, null);
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, ViewHome.y);
            rect.sizeDelta = new Vector2(SideButtonSize, SideButtonSize);
            button.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, arrowAngle);
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => { _heldSince = Time.unscaledTime; _heldDirection = direction; });
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => ReleaseSideButton(direction));
            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
        }

        private void ReleaseSideButton(int direction)
        {
            bool tap = _heldSince >= 0f && Time.unscaledTime - _heldSince < HoldSnapSeconds;
            _heldSince = -1f;
            _heldDirection = 0;
            if (tap)
            {
                Step(direction);
            }
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

            _planet = UiKit.CreateButton(row, "Planet", LeaveRegion);
            _play = UiKit.CreateButton(row, "Play level", PlayFocused);
            _upgrades = UiKit.CreateButton(row, "Upgrades", () => Go(SceneNames.Base));
            _back = UiKit.CreateButton(row, "Main menu", () => Go(SceneNames.MainMenu));
            foreach (Button button in row.GetComponentsInChildren<Button>())
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(ActionWidth, UiKit.ButtonHeight);
            }
        }

        // The row shows what fits the layer: on the planet upgrades and the menu, inside a region the way back to
        // the planet and the play button. Only the visible buttons navigate between each other; up leaves the row
        // and gives the globe the stick again.
        private void UpdateRow()
        {
            _planet.gameObject.SetActive(_entered);
            _play.gameObject.SetActive(_entered);
            _back.gameObject.SetActive(!_entered);
            _play.interactable = _focused >= 0 && _markers[_focused].Open;

            Button[] visible = _entered ? new[] { _planet, _play, _upgrades } : new[] { _upgrades, _back };
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? visible[i - 1] : null,
                    selectOnRight = i < visible.Length - 1 ? visible[i + 1] : null,
                };
            }
        }

        private bool IsRowButton(GameObject selected)
        {
            return selected == _planet.gameObject || selected == _play.gameObject || selected == _upgrades.gameObject || selected == _back.gameObject;
        }

        // Left and right step through the levels of a region: once on a push, then repeating while held.
        private void StepInput(float x, float dt)
        {
            if (Mathf.Abs(x) < StepDeadzone)
            {
                _stepRepeat = 0f;
                return;
            }

            _stepRepeat -= dt;
            if (_stepRepeat > 0f)
            {
                return;
            }

            _stepRepeat = StepRepeatSeconds;
            Step(x > 0f ? 1 : -1);
        }

        // ---- every frame -----------------------------------------------------------------------------------------

        private void Update()
        {
            if (_globe == null)
            {
                return;
            }

            bool browsing = _regionPanel == null && _loadout == null && _chosen < 0;
            if (browsing)
            {
                ReadInput();
            }

            PlaceBadges();
            PlaceMarkers();
            PlaceRoute();
            PlaceHalo();

            if (_chosen >= 0 && _globe.Zoom >= PlanetGlobe.LevelZoom && !_globe.Turning && _regionPanel == null && _loadout == null)
            {
                OpenRegionPanel();
            }

            _overview.alpha = 1f - LevelStage();
            _overview.interactable = browsing;
            _overview.blocksRaycasts = browsing;
        }

        // 0 on the planet, 1 once the view has flown onto a region.
        private float RegionStage() => Mathf.Clamp01(_globe.Zoom / PlanetGlobe.RegionZoom);

        // 0 on the region, 1 once the view has flown onto a level.
        private float LevelStage() => Mathf.Clamp01((_globe.Zoom - PlanetGlobe.RegionZoom) / (PlanetGlobe.LevelZoom - PlanetGlobe.RegionZoom));

        // The stick turns the globe unless the action row holds the focus; pushing up from the row hands it back.
        private void ReadInput()
        {
            // The left stick and d-pad come through CursorMove; the arrow keys, WASD and right stick through CameraPan.
            Vector2 move = _input != null ? _input.CursorMove + _input.CameraPan : Vector2.zero;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            _rowFocused = selected != null && IsRowButton(selected);
            if (_rowFocused)
            {
                if (move.y > 0.5f)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    _rowFocused = false;
                }

                return;
            }

            float dt = Time.unscaledDeltaTime;
            bool holding = _heldDirection != 0 && _heldSince >= 0f && Time.unscaledTime - _heldSince >= HoldSnapSeconds;
            if (holding)
            {
                move.x += _heldDirection;
            }

            if (_entered)
            {
                // Inside a region the levels are what matters: left and right walk them, nothing turns freely.
                StepInput(move.x, dt);
                return;
            }

            move = Vector2.ClampMagnitude(move, 1f);
            if (move.sqrMagnitude > 0.01f)
            {
                // Pushing right brings the east into view: the surface slides to the left, a positive turn about up.
                _globe.Spin(new Vector2(move.x * SpinDegreesPerSecond * dt, move.y * TiltDegreesPerSecond * dt));
                FocusRegion(Nearest(_badges, _region, -1));
            }
        }

        // The marker nearest the middle of the view among those facing the camera; inside a region only its levels count.
        private int Nearest(List<Marker> markers, int current, int region)
        {
            int best = current;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < markers.Count; i++)
            {
                if (region >= 0 && _catalog.Nodes[i].Region != region)
                {
                    continue;
                }

                Vector2 position = _globe.Project(markers[i].Direction, out float facing);
                if (facing < FocusMinFacing)
                {
                    continue;
                }

                float distance = position.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        private void PlaceBadges()
        {
            float layer = 1f - Mathf.Clamp01((RegionStage() - LayerFadeStart) / (LayerFadeEnd - LayerFadeStart));
            float pulse = 1f + RingPulseDepth * Mathf.Sin(Time.unscaledTime * RingPulseRate);
            for (int i = 0; i < _badges.Count; i++)
            {
                Marker badge = _badges[i];
                badge.Root.anchoredPosition = _globe.Project(badge.Direction, out float facing);
                float visible = Mathf.Clamp01((facing - RimFadeStart) / (RimFadeEnd - RimFadeStart)) * layer;
                badge.Group.alpha = visible;
                badge.Group.blocksRaycasts = visible > 0f;
                badge.Group.interactable = visible > 0f;
                badge.Root.localScale = Vector3.one * (0.8f + 0.2f * facing);
                badge.Ring.localScale = Vector3.one * pulse;
            }
        }

        private void PlaceMarkers()
        {
            float layer = Mathf.Clamp01((RegionStage() - LayerFadeStart) / (LayerFadeEnd - LayerFadeStart));
            float level = LevelStage();
            float scale = Mathf.Lerp(1f, MarkerZoomScale, level);
            float pulse = 1f + RingPulseDepth * Mathf.Sin(Time.unscaledTime * RingPulseRate);
            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i];
                marker.Root.anchoredPosition = _globe.Project(marker.Direction, out float facing);
                float visible = Mathf.Clamp01((facing - RimFadeStart) / (RimFadeEnd - RimFadeStart)) * layer;
                bool inRegion = _catalog.Nodes[i].Region == _region;
                bool shown = inRegion && visible > 0f && (_chosen < 0 || _chosen == i);
                // The other markers fade with the flight in; the chosen one stays as the anchor of the panel.
                marker.Group.alpha = shown ? visible * (_chosen == i ? 1f : 1f - level) : 0f;
                marker.Group.blocksRaycasts = shown;
                marker.Group.interactable = shown;
                marker.Root.localScale = Vector3.one * (scale * (0.8f + 0.2f * facing));
                marker.Ring.localScale = Vector3.one * pulse;
            }
        }

        private void PlaceRoute()
        {
            float layer = Mathf.Clamp01((RegionStage() - LayerFadeStart) / (LayerFadeEnd - LayerFadeStart));
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * RingPulseRate);
            for (int i = 0; i < _route.Count; i++)
            {
                RouteDot dot = _route[i];
                dot.Rect.anchoredPosition = _globe.Project(dot.Direction, out float facing);
                float visible = Mathf.Clamp01((facing - RimFadeStart) / (RimFadeEnd - RimFadeStart));
                Color colour = dot.Colour;
                colour.a *= visible * layer * (dot.Pulsing ? pulse : 1f) * (1f - LevelStage());
                dot.Image.color = colour;
            }
        }

        private void PlaceHalo()
        {
            Vector2 centre = _globe.Centre(out float radius);
            _halo.anchoredPosition = centre;
            _halo.sizeDelta = Vector2.one * (radius * 2f * HaloScale);
            Color halo = HaloColor;
            halo.a *= 1f - 0.6f * RegionStage();
            _haloImage.color = halo;
        }

        // ---- the planet layer --------------------------------------------------------------------------------------

        private void FocusRegion(int index)
        {
            if (index < 0 || index >= _badges.Count)
            {
                return;
            }

            for (int i = 0; i < _badges.Count; i++)
            {
                _badges[i].Ring.gameObject.SetActive(i == index);
            }

            _region = index;
            MapRegion region = _catalog.Regions[index];
            int total = _catalog.NodeCount(index);
            string status = total == 0 ? "   -   coming soon" : $"   -   {CampaignRules.CompletedInRegion(_catalog, index, Progress)} / {total} levels";
            _caption.text = region.DisplayName + status;
            _caption.color = _badges[index].Open ? Color.white : Muted;
            _globe.Highlight(index, region.Colour, _badges[index].Open, region.Direction, region.RadiusDegrees);
        }

        // Flies onto a region and shows its levels, focusing the one given (or the furthest open one in it).
        private void EnterRegion(int index, int focusNode = -1)
        {
            if (!_badges[index].Open)
            {
                return;
            }

            _entered = true;
            FocusRegion(index);
            foreach (Marker badge in _badges)
            {
                badge.Ring.gameObject.SetActive(false);
            }

            if (focusNode < 0)
            {
                focusNode = _catalog.FirstNode(index);
                for (int i = focusNode; i < _markers.Count && _catalog.Nodes[i].Region == index; i++)
                {
                    if (_markers[i].Open)
                    {
                        focusNode = i;
                    }
                }
            }

            FocusNode(focusNode);
            _globe.TurnTo(_catalog.Regions[index].Direction);
            _globe.ZoomTarget = PlanetGlobe.RegionZoom;
            _hint.text = RegionHint;
            UpdateRow();
        }

        // Back out to the whole planet, with the region just left still focused.
        private void LeaveRegion()
        {
            _entered = false;
            _focused = -1;
            foreach (Marker marker in _markers)
            {
                marker.Ring.gameObject.SetActive(false);
            }

            FocusRegion(_region);
            _globe.TurnTo(_catalog.Regions[_region].Direction);
            _globe.ZoomTarget = 0f;
            _hint.text = PlanetHint;
            UpdateRow();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnBadgeClicked(int index)
        {
            if (_entered || _chosen >= 0 || _regionPanel != null || _loadout != null)
            {
                return;
            }

            FocusRegion(index);
            EnterRegion(index);
        }

        // ---- the region layer --------------------------------------------------------------------------------------

        private void FocusNode(int index)
        {
            if (index < 0 || index >= _markers.Count)
            {
                return;
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                _markers[i].Ring.gameObject.SetActive(i == index);
            }

            _focused = index;
            MapNode node = _catalog.Nodes[index];
            Marker marker = _markers[index];
            string status = node.Level == null ? "   -   coming soon" : marker.Open ? string.Empty : "   -   locked";
            _caption.text = $"{index + 1}. {node.DisplayName}{status}";
            _caption.color = marker.Open ? Color.white : Muted;
            if (_play != null)
            {
                _play.interactable = marker.Open;
            }
        }

        // Shoulder buttons walk the regions on the planet, or the levels of a region once inside, and turn the
        // globe to what they land on.
        private void Step(int direction)
        {
            if (_regionPanel != null || _loadout != null || _chosen >= 0 || _rowFocused)
            {
                return;
            }

            if (!_entered)
            {
                int region = Mathf.Clamp(_region + direction, 0, _badges.Count - 1);
                FocusRegion(region);
                _globe.TurnTo(_catalog.Regions[region].Direction);
                return;
            }

            int next = Mathf.Clamp(_focused + direction, 0, _markers.Count - 1);
            if (_catalog.Nodes[next].Region != _region)
            {
                return;
            }

            FocusNode(next);
            _globe.TurnTo(_catalog.Nodes[next].Direction);
        }

        private void OnMarkerClicked(int index)
        {
            if (!_entered || _chosen >= 0 || _regionPanel != null || _loadout != null)
            {
                return;
            }

            // The first click selects a level and turns the globe to it; a click on the selected level plays it.
            if (index == _focused)
            {
                PlayFocused();
                return;
            }

            FocusNode(index);
            _globe.TurnTo(_catalog.Nodes[index].Direction);
        }

        private void PlayFocused()
        {
            if (_entered && _chosen < 0 && _regionPanel == null && _loadout == null && _focused >= 0)
            {
                Choose(_focused);
            }
        }

        private void OnConfirm()
        {
            // A mouse click is handled by whatever it lands on; it must not also play the level the keyboard focus is on.
            if (_input != null && _input.FromPointer)
            {
                return;
            }

            if (_chosen >= 0 || _regionPanel != null || _loadout != null || _rowFocused)
            {
                return;
            }

            if (!_entered)
            {
                EnterRegion(_region);
            }
            else
            {
                PlayFocused();
            }
        }

        // Choosing turns the globe onto the level and flies in; the panel opens once the flight has landed.
        private void Choose(int index)
        {
            if (!_markers[index].Open)
            {
                return;
            }

            _chosen = index;
            _globe.TurnTo(_catalog.Nodes[index].Direction);
            _globe.ZoomTarget = PlanetGlobe.LevelZoom;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // The level panel: the name of the region, the level, the briefing and the four difficulties with their marks.
        private void OpenRegionPanel()
        {
            LevelDefinition level = _catalog.Nodes[_chosen].Level;
            Image panel = UiKit.CreatePanel(_root, "RegionPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = new Vector2(PanelRight, 0f);
            _regionPanel = panel.gameObject;

            float width = PanelWidth - 80f;
            Text region = UiKit.CreateFixedText(rect, $"{_catalog.Regions[_catalog.Nodes[_chosen].Region].DisplayName}   -   {level.RegionName}", RegionNameSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(width, 40f));
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

            Button button = UiKit.CreateButton(panel, difficulty.DisplayName, () => OpenLoadout(level, difficulty));
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
            _chosen = -1;
            _globe.ZoomTarget = PlanetGlobe.RegionZoom;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // B closes what is open: the loadout, the panel, the flight, then the region; on the planet it first moves
        // the focus to the buttons, then leaves for the menu.
        private void OnCancel()
        {
            if (_loadout != null)
            {
                _loadout.Back();
            }
            else if (_regionPanel != null)
            {
                CloseRegion();
            }
            else if (_chosen >= 0)
            {
                _chosen = -1;
                _globe.ZoomTarget = PlanetGlobe.RegionZoom;
            }
            else if (_entered && !_rowFocused)
            {
                LeaveRegion();
            }
            else if (!_rowFocused)
            {
                UiKit.Select(_upgrades);
            }
            else
            {
                Go(SceneNames.MainMenu);
            }
        }

        // The difficulty chosen, the player picks the street weapons to take along; only then does the level start.
        private void OpenLoadout(LevelDefinition level, DifficultyProfile difficulty)
        {
            if (_regionPanel != null)
            {
                Destroy(_regionPanel);
                _regionPanel = null;
            }

            // Before the campaign has granted any gadget there is nothing to choose: straight into the level, empty-handed.
            if (!LoadoutPanelView.HasChoice(_catalog, Progress))
            {
                StartLevel(level, difficulty, new PathObjectDefinition[0]);
                return;
            }

            _loadout = gameObject.AddComponent<LoadoutPanelView>();
            _loadout.Open(_root, _catalog, level, difficulty, Progress, OnLoadoutBack, StartLevel);
        }

        private void OnLoadoutBack()
        {
            _loadout = null;
            OpenRegionPanel();
        }

        private static void StartLevel(LevelDefinition level, DifficultyProfile difficulty, IReadOnlyList<PathObjectDefinition> loadout)
        {
            Services.Register(new LevelSession(level, difficulty, true, loadout));
            Go(SceneNames.Level);
        }

        // ---- helpers ---------------------------------------------------------------------------------------------

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
