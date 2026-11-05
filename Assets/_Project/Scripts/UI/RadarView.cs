using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The HUD radar (Remco 2026-11-05), dressed like the perimeter scan of the opening cutscene: the round scope picture
    /// with its turning sweep, and inside it the level drawn small (field, road, base, gates and towers as their own
    /// symbols), enemies as red contacts that flare as the sweep passes (fliers as hollow rings, bosses big and
    /// pulsing), the spawn origins only once one has opened, and the rectangle the camera shows. The map turns with the
    /// camera's yaw, so up on the radar is up on the screen. A click on it sends the camera there. Contacts are pooled and
    /// refreshed a few times a second; nothing is allocated per frame.
    /// </summary>
    public sealed class RadarView : MonoBehaviour, IPointerClickHandler
    {
        private const float BaseSize = 270f;
        // README round 25: the scope picture is drawn 1.087 times the scope size (its radius is 471 of 1024 px).
        private const float PictureScale = 1.087f;
        // Share of the scope's radius the field's half diagonal may fill, so the corners stay off the rim.
        private const float MapFill = 0.9f;
        private const float RefreshSeconds = 0.1f;
        private const float SweepDegreesPerSecond = 120f;
        private const float PingFade = 1.1f;
        private const float PingFloor = 0.45f;
        private const int CellPixels = 4;
        private const float ContactSize = 10f;
        private const float FlierSize = 11f;
        private const float BossSize = 22f;
        private const float BossPulse = 0.18f;
        private const float BossPulseSpeed = 5f;
        private const float TowerSize = 6f;
        private const float GateLength = 13f;
        private const float GateThickness = 4f;
        private const float BaseIconSize = 22f;
        private const float OriginIconSize = 16f;
        private const float FrameThickness = 1.5f;
        private const int StartPool = 48;

        private static readonly Color SweepColour = new Color(0.4f, 1f, 0.85f, 0.6f);
        private static readonly Color GroundColour = new Color(1f, 0.2f, 0.12f);
        private static readonly Color FlierColour = new Color(1f, 0.55f, 0.25f);
        private static readonly Color BossColour = new Color(1f, 0.1f, 0.15f);
        private static readonly Color TowerColour = new Color(0.45f, 0.85f, 1f, 0.95f);
        private static readonly Color WreckColour = new Color(0.5f, 0.55f, 0.6f, 0.6f);
        private static readonly Color GateColour = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color BaseColour = new Color(0.55f, 0.95f, 1f, 1f);
        private static readonly Color OriginColour = new Color(1f, 0.4f, 0.2f, 1f);
        private static readonly Color ViewColour = new Color(0.75f, 1f, 0.95f, 0.85f);
        private static readonly Color FieldPixel = new Color(0.1f, 0.3f, 0.27f, 0.55f);
        private static readonly Color RockPixel = new Color(0.04f, 0.12f, 0.11f, 0.6f);
        private static readonly Color RoadPixel = new Color(0.3f, 0.85f, 0.72f, 0.75f);
        private static readonly Color EdgePixel = new Color(0.4f, 1f, 0.85f, 0.9f);

        private LevelContext _context;
        private Camera _camera;
        private FixedCameraRig _rig;
        private CameraControls _controls;
        private RectTransform _root;
        private RectTransform _map;
        private RectTransform _sweep;
        private RectTransform _view;
        private RectTransform _symbols;
        private Texture2D _mapTexture;
        private Vector2 _fieldSize;
        private float _radius;
        private float _yaw;
        private float _pitch;
        private float _sweepDegrees;
        private float _sinceRefresh = float.PositiveInfinity;
        private readonly List<Blip> _contacts = new List<Blip>(StartPool);
        private readonly List<Blip> _structures = new List<Blip>(StartPool);
        private readonly List<Blip> _origins = new List<Blip>(8);
        private int _contactsUsed;
        private int _structuresUsed;
        private Blip _base;

        private sealed class Blip
        {
            public RectTransform Rect;
            public Image Image;
            public Color Colour;
            public float Size;
            public bool Boss;
        }

        /// <summary>The radar's panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _root;

        /// <summary>Builds the radar panel in the top-right corner of the safe area, the view living on the panel itself (it takes the clicks).</summary>
        public static RadarView Create(LevelContext context, RectTransform safe, Camera camera, Vector2 topRightOffset)
        {
            RectTransform root = NewRect("Radar", safe);
            var radar = root.gameObject.AddComponent<RadarView>();
            radar.Initialize(context, camera, topRightOffset);
            return radar;
        }

        private void Initialize(LevelContext context, Camera camera, Vector2 topRightOffset)
        {
            _context = context;
            _camera = camera;
            _rig = camera != null ? camera.GetComponent<FixedCameraRig>() : null;
            _controls = Services.TryGet(out CameraControls controls) ? controls : new CameraControls();
            LevelDefinition level = context.Level;
            _fieldSize = level.WorldSize;
            _yaw = level.Camera.YawDegrees;
            _pitch = level.Camera.PitchDegrees;
            _radius = BaseSize * 0.5f;

            _root = (RectTransform)transform;
            _root.anchorMin = Vector2.one;
            _root.anchorMax = Vector2.one;
            _root.pivot = Vector2.one;
            _root.anchoredPosition = topRightOffset;
            _root.sizeDelta = new Vector2(BaseSize, BaseSize);
            BuildScope();
            BuildMap(level);
            BuildSweep();
            _controls.Changed += ApplyOptions;
            ApplyOptions();
        }

        private void OnDestroy()
        {
            if (_controls != null)
            {
                _controls.Changed -= ApplyOptions;
            }

            if (_mapTexture != null)
            {
                Destroy(_mapTexture);
            }
        }

        private void ApplyOptions()
        {
            _root.gameObject.SetActive(_controls.RadarOn);
            _root.localScale = Vector3.one * _controls.RadarScale;
        }

        // A click on the radar sends the camera to that spot of the field (it glides there, keeping the zoom).
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Vector3 world = RadarMath.RadarToWorld(local, _fieldSize, _radius * MapFill);
            _context.CameraFocusRequested.Raise(OverviewFraming.KeepOnField(world, _fieldSize * 0.5f, 0f));
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _sweepDegrees = Mathf.Repeat(_sweepDegrees + SweepDegreesPerSecond * dt, 360f);
            _sweep.localRotation = Quaternion.Euler(0f, 0f, -_sweepDegrees);
            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshSeconds)
            {
                _sinceRefresh = 0f;
                RefreshStructures();
                RefreshContacts();
                RefreshOrigins();
                RefreshView();
            }

            Ping();
        }

        // ---------------------------------------------------------------- building

        private void BuildScope()
        {
            UiArtSet art = UiArtSet.Active;
            RawImage scope = NewRect("Scope", _root).gameObject.AddComponent<RawImage>();
            scope.texture = art != null ? art.Radar : null;
            scope.color = art != null && art.Radar != null ? Color.white : new Color(0.06f, 0.14f, 0.14f, 0.9f);
            Fill(scope.rectTransform, PictureScale);
            // The scope takes the clicks, and a button on the panel tells the HUD the pointer is over something live.
            scope.raycastTarget = true;
            Button button = _root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private void BuildMap(LevelDefinition level)
        {
            _map = NewRect("Map", _root);
            Vector2 half = RadarMath.FitHalfExtents(_fieldSize, _radius * MapFill);
            _map.sizeDelta = half * 2f;
            _map.localRotation = Quaternion.Euler(0f, 0f, _yaw);
            RawImage ground = NewRect("Ground", _map).gameObject.AddComponent<RawImage>();
            _mapTexture = PaintMap(level);
            ground.texture = _mapTexture;
            ground.raycastTarget = false;
            Fill(ground.rectTransform, 1f);
            _symbols = NewRect("Symbols", _map);
            Fill(_symbols, 1f);
            _view = BuildFrame(_map);
            _base = NewBlip(_symbols, IconFactory.Get(IconType.Base), BaseColour, BaseIconSize);
            _base.Rect.gameObject.SetActive(false);
        }

        private void BuildSweep()
        {
            Image sweep = NewRect("Sweep", _root).gameObject.AddComponent<Image>();
            sweep.sprite = DialogueArt.RadarSweep;
            sweep.color = SweepColour;
            sweep.raycastTarget = false;
            sweep.enabled = sweep.sprite != null;
            _sweep = sweep.rectTransform;
            Fill(_sweep, PictureScale);
        }

        // The field once, a few pixels a cell: road bright, open ground dim, rock and pools darker, the border lit.
        private static Texture2D PaintMap(LevelDefinition level)
        {
            Vector2Int size = level.GridSize;
            var grid = new BuildGrid(level);
            RoadCorridor road = level.Road;
            int width = size.x * CellPixels;
            int height = size.y * CellPixels;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x / CellPixels, y / CellPixels);
                    bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    Color colour = edge ? EdgePixel : road.IsRoad(cell) ? RoadPixel : grid.CanBuild(cell) ? FieldPixel : RockPixel;
                    pixels[y * width + x] = colour;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "RadarMap", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // The camera's view: four thin lines; their parent is sized and turned to the view's ground footprint.
        private static RectTransform BuildFrame(RectTransform parent)
        {
            RectTransform frame = NewRect("View", parent);
            Line(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -FrameThickness), Vector2.zero);
            Line(frame, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, FrameThickness));
            Line(frame, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(FrameThickness, 0f));
            Line(frame, new Vector2(1f, 0f), Vector2.one, new Vector2(-FrameThickness, 0f), Vector2.zero);
            return frame;
        }

        private static void Line(RectTransform frame, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            Image line = NewRect("Line", frame).gameObject.AddComponent<Image>();
            line.color = ViewColour;
            line.raycastTarget = false;
            RectTransform rect = line.rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // ---------------------------------------------------------------- refreshing

        private void RefreshStructures()
        {
            _structuresUsed = 0;
            bool baseShown = false;
            StructureRegistry structures = _context.Structures;
            for (int i = 0; i < structures.Count; i++)
            {
                IStructure structure = structures[i];
                if (structure.IsBase)
                {
                    Place(_base, structure.Position, BaseColour, BaseIconSize);
                    baseShown = true;
                    continue;
                }

                if (!(structure is Tower tower))
                {
                    continue;
                }

                bool gate = tower.Definition.BlocksGroundEnemies;
                Blip blip = Take(_structures, ref _structuresUsed, null);
                Color colour = tower.IsWrecked ? WreckColour : gate ? GateColour : TowerColour;
                Place(blip, tower.Position, colour, TowerSize);
                if (gate)
                {
                    // A gate lies across the road: a bar at right angles to the road's direction.
                    Vector3 along = _context.Level.Road.DirectionAt(_context.Level.Grid.WorldToCell(tower.Position));
                    blip.Rect.sizeDelta = new Vector2(GateLength, GateThickness);
                    blip.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.z, along.x) * Mathf.Rad2Deg + 90f);
                }
                else
                {
                    blip.Rect.localRotation = Quaternion.identity;
                }
            }

            if (_base.Rect.gameObject.activeSelf != baseShown)
            {
                _base.Rect.gameObject.SetActive(baseShown);
            }

            Hide(_structures, _structuresUsed);
        }

        private void RefreshContacts()
        {
            _contactsUsed = 0;
            TargetRegistry targets = _context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!(targets[i] is Enemy enemy) || !enemy.IsAlive)
                {
                    continue;
                }

                EnemyDefinition definition = enemy.Definition;
                bool boss = definition.IsBoss;
                bool flier = definition.Flies;
                Blip blip = Take(_contacts, ref _contactsUsed, DialogueArt.RadarContact);
                // A flier is a hollow ring, so it reads apart from the walkers even where they overlap.
                Sprite sprite = flier && !boss ? IconFactory.Get(IconType.Ring) : DialogueArt.RadarContact;
                if (blip.Image.sprite != sprite)
                {
                    blip.Image.sprite = sprite;
                }

                if (blip.Boss && !boss)
                {
                    blip.Rect.localScale = Vector3.one;
                }

                blip.Boss = boss;
                Place(blip, enemy.Position, boss ? BossColour : flier ? FlierColour : GroundColour, boss ? BossSize : flier ? FlierSize : ContactSize);
            }

            Hide(_contacts, _contactsUsed);
        }

        // An origin shows only once it has opened: the radar never tells where something will come out before it does.
        private void RefreshOrigins()
        {
            OriginDirector origins = _context.Origins;
            if (origins == null)
            {
                return;
            }

            ElementNest[] nests = _context.Level.Nests;
            for (int i = 0; i < origins.Count && i < nests.Length; i++)
            {
                while (_origins.Count <= i)
                {
                    Blip made = NewBlip(_symbols, IconFactory.Get(IconType.EnemyOrigin), OriginColour, OriginIconSize);
                    made.Rect.gameObject.SetActive(false);
                    _origins.Add(made);
                }

                bool open = origins.StateOf(i) == OriginState.Open;
                Blip blip = _origins[i];
                if (open && !blip.Rect.gameObject.activeSelf)
                {
                    Place(blip, _context.Level.Grid.CellToWorld(nests[i].Cell), OriginColour, OriginIconSize);
                }
                else if (!open && blip.Rect.gameObject.activeSelf)
                {
                    blip.Rect.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshView()
        {
            if (_rig == null)
            {
                return;
            }

            float scale = RadarMath.Scale(_fieldSize, _radius * MapFill);
            Vector2 footprint = RadarMath.ViewFootprint(_camera.orthographicSize, _camera.aspect, _pitch);
            // Zoomed out the view covers more than the field; the frame keeps to the field so it never runs past the scope.
            Vector2 half = _map.sizeDelta * 0.5f;
            Vector2 size = Vector2.Min(footprint * scale, half * 2f);
            Vector2 centre = RadarMath.WorldToRadar(_rig.Focus, _fieldSize, _radius * MapFill);
            Vector2 room = half - size * 0.5f;
            _view.anchoredPosition = new Vector2(Mathf.Clamp(centre.x, -room.x, room.x), Mathf.Clamp(centre.y, -room.y, room.y));
            _view.sizeDelta = size;
            // The footprint lies along the camera's right and forward, turned by the yaw on the map, so on the screen
            // (the map turned back by the yaw) it is upright.
            _view.localRotation = Quaternion.Euler(0f, 0f, -_yaw);
        }

        // Contacts flare as the sweep passes and fade until the next pass; bosses also pulse.
        private void Ping()
        {
            float pulse = 1f + BossPulse * Mathf.Sin(Time.unscaledTime * BossPulseSpeed);
            for (int i = 0; i < _contactsUsed; i++)
            {
                Blip blip = _contacts[i];
                float ping = RadarMath.Ping(blip.Rect.anchoredPosition, _sweepDegrees + _yaw, SweepDegreesPerSecond, PingFade);
                Color colour = blip.Colour;
                colour.a = PingFloor + (1f - PingFloor) * ping;
                blip.Image.color = colour;
                if (blip.Boss)
                {
                    blip.Rect.localScale = Vector3.one * pulse;
                }
            }
        }

        // ---------------------------------------------------------------- pooled symbols

        private Blip Take(List<Blip> pool, ref int used, Sprite sprite)
        {
            if (used >= pool.Count)
            {
                pool.Add(NewBlip(_symbols, sprite, Color.white, ContactSize));
            }

            Blip blip = pool[used++];
            if (!blip.Rect.gameObject.activeSelf)
            {
                blip.Rect.gameObject.SetActive(true);
            }

            return blip;
        }

        private static void Hide(List<Blip> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i].Rect.gameObject.activeSelf)
                {
                    pool[i].Rect.gameObject.SetActive(false);
                }
            }
        }

        private void Place(Blip blip, Vector3 world, Color colour, float size)
        {
            blip.Rect.anchoredPosition = RadarMath.WorldToRadar(world, _fieldSize, _radius * MapFill);
            if (!Mathf.Approximately(blip.Size, size))
            {
                blip.Size = size;
                blip.Rect.sizeDelta = new Vector2(size, size);
                blip.Rect.localScale = Vector3.one;
            }

            blip.Colour = colour;
            blip.Image.color = colour;
            if (!blip.Rect.gameObject.activeSelf)
            {
                blip.Rect.gameObject.SetActive(true);
            }
        }

        private static Blip NewBlip(RectTransform parent, Sprite sprite, Color colour, float size)
        {
            Image image = NewRect("Blip", parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return new Blip { Rect = image.rectTransform, Image = image, Colour = colour, Size = size };
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static void Fill(RectTransform rect, float scale)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 parent = rect.parent is RectTransform owner ? owner.sizeDelta : Vector2.zero;
            rect.sizeDelta = parent * scale;
        }
    }
}
