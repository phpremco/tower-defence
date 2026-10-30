using System.Collections.Generic;
using Game.Core;
using Game.PathObjects;
using Game.Platform;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The player's hands on the field. Towers go on any buildable cell. Confirming on an empty cell starts placing:
    /// a preview with its range follows the cursor and a second confirm builds it. Confirming on a tower opens its
    /// action icons (upgrade, repair, sell). Works with stick, d-pad, WASD and mouse.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        private const float StickThreshold = 0.5f;
        private const float StickRepeatSeconds = 0.16f;
        private const float PointerMovedPixels = 1f;
        private const float PointerActiveSeconds = 2f;
        private const float MarkerHeight = 0.06f;
        private const float MarkerCellFraction = 0.96f;

        // The marker on a build cell is the footprint of a pedestal, standing where the tower would stand.
        private const float MarkerFootprintCells = 0.7f;
        // Kept under 0.3, otherwise a shifted pedestal (0.7 cells wide) runs into a tower on the next cell.
        private const float RoadClearanceCells = 0.15f;

        // A pedestal shifted the full clearance hangs a little into the next cell, so when that cell is water,
        // a cliff edge, a rock or the edge of the map the tower only moves as far as its own cell allows.
        private const float FittedClearanceCells = 0.05f;
        private static readonly Vector2Int[] Neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        private const float MarkerEmission = 1.6f;

        // The cursor is a soft tint while browsing (a gamepad needs to see where it is) and firmer while placing.
        private const float BrowsingAlpha = 0.28f;
        private const float PlacingAlpha = 0.55f;
        private static readonly Color ValidColor = new Color(0.3f, 1f, 0.8f);
        private static readonly Color InvalidColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color TowerColor = new Color(1f, 0.9f, 0.35f);

        private readonly Dictionary<Vector2Int, Tower> _towers = new Dictionary<Vector2Int, Tower>();

        private static readonly Vector2Int[] NeighbourSteps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private readonly HashSet<Vector2Int> _pathObjectCells = new HashSet<Vector2Int>();
        private Tower _combinePartner;
        private readonly List<Tower> _blockades = new List<Tower>();

        private LevelContext _context;
        private IInputProvider _input;
        private Camera _camera;
        private Transform _parent;
        private BuildGrid _buildGrid;
        private GridMap _map;
        private TerrainMap _terrain;
        private Vector2Int _cursor;
        private Transform _marker;
        private Renderer _markerRenderer;
        private RangeIndicator _range;
        private Tower _ghost;
        private TowerDefinition _ghostDefinition;
        private string _cellMessage = string.Empty;
        private float _repeatTimer;
        private float _lastPointerMove = float.NegativeInfinity;
        private Vector2 _lastPointer;

        /// <summary>The level's fixed blockades, in the order the level lists them.</summary>
        public IReadOnlyList<Tower> Blockades => _blockades;

        public void Initialize(LevelContext context, IInputProvider input, Camera camera, Transform parent)
        {
            _context = context;
            _input = input;
            _camera = camera;
            _parent = parent;
            _map = context.Level.Grid;
            _terrain = context.Level.Terrain;
            _buildGrid = new BuildGrid(context.Level);
            _context.SelectedTower = context.AvailableTowers.Count > 0 ? context.AvailableTowers[0] : null;
            SpawnBlockades();

            float size = _map.CellSize;
            GameObject marker = PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube, ValidColor, parent, Vector3.zero, new Vector3(size * MarkerCellFraction, 0.03f, size * MarkerCellFraction), MarkerEmission);
            _marker = marker.transform;
            _markerRenderer = marker.GetComponent<Renderer>();
            _range = RangeIndicator.Create(parent);

            _cursor = new Vector2Int(_map.Size.x / 2, _map.Size.y / 2);
            _lastPointer = input.PointerPosition;

            _input.Confirmed += OnConfirmed;
            _input.Cancelled += OnCancelled;
            _input.NextTower += OnNextTower;
            _input.PreviousTower += OnPreviousTower;
            _input.UpgradePressed += OnUpgrade;
            _input.RepairPressed += OnRepair;
            _input.SellPressed += OnSell;
            _context.UpgradeRequested.Subscribe(OnUpgrade);
            _context.RepairRequested.Subscribe(OnRepair);
            _context.SellRequested.Subscribe(OnSell);
            _context.CombineRequested.Subscribe(OnCombine);
            _context.SelectionChanged.Subscribe(Refresh);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Confirmed -= OnConfirmed;
                _input.Cancelled -= OnCancelled;
                _input.NextTower -= OnNextTower;
                _input.PreviousTower -= OnPreviousTower;
                _input.UpgradePressed -= OnUpgrade;
                _input.RepairPressed -= OnRepair;
                _input.SellPressed -= OnSell;
            }

            if (_context != null)
            {
                _context.UpgradeRequested.Unsubscribe(OnUpgrade);
                _context.RepairRequested.Unsubscribe(OnRepair);
                _context.SellRequested.Unsubscribe(OnSell);
                _context.CombineRequested.Unsubscribe(OnCombine);
            _context.SelectionChanged.Unsubscribe(Refresh);
            }
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running || _context.Paused)
            {
                return;
            }

            // The mouse is tracked in every mode so a click away from an open menu lands on the cell under it.
            UpdatePointer();
            if (_context.Mode != PlacementMode.TowerMenu)
            {
                UpdateStick();
            }
        }

        private bool PointerActive => Time.unscaledTime - _lastPointerMove < PointerActiveSeconds;

        private void UpdateStick()
        {
            Vector2 move = _input.CursorMove;
            if (move.magnitude < StickThreshold)
            {
                _repeatTimer = 0f;
                return;
            }

            _repeatTimer -= Time.unscaledDeltaTime;
            if (_repeatTimer > 0f)
            {
                return;
            }

            _repeatTimer = StickRepeatSeconds;
            MoveCursor(GridCursor.Step(_cursor, move, _map));
        }

        private void UpdatePointer()
        {
            Vector2 pointer = _input.PointerPosition;
            if ((pointer - _lastPointer).sqrMagnitude < PointerMovedPixels * PointerMovedPixels)
            {
                return;
            }

            _lastPointer = pointer;
            _lastPointerMove = Time.unscaledTime;
            if (_context.PointerOverUi)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(pointer);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
            {
                Vector2Int cell = _map.WorldToCell(ray.GetPoint(distance));
                if (_map.Contains(cell))
                {
                    MoveCursor(cell);
                }
            }
        }

        private void MoveCursor(Vector2Int cell)
        {
            if (cell == _cursor)
            {
                return;
            }

            _cursor = cell;
            _cellMessage = string.Empty;
            Refresh();
        }

        private void OnConfirmed()
        {
            if (_context.Outcome != LevelOutcome.Running || _context.Paused || _context.PointerOverUi)
            {
                Debug.Log($"[Placement] confirm ignored: outcome={_context.Outcome} paused={_context.Paused} pointerOverUi={_context.PointerOverUi}");
                return;
            }

            Debug.Log($"[Placement] confirm: mode={_context.Mode} cell={_cursor} canBuild={_buildGrid.CanBuild(_cursor)} road={_buildGrid.IsRoad(_cursor)} blocked={_buildGrid.IsBlocked(_cursor)} selected={(_context.SelectedTower != null ? _context.SelectedTower.name : "none")} money={_context.Wallet.Money}");

            // A click away from the hero's spot closes its panel, then acts on the cell as usual.
            if (_context.HeroMenuPost >= 0 && HeroPostAt(_cursor) != _context.HeroMenuPost)
            {
                _context.HeroMenuPost = -1;
            }

            if (_context.BaseMenuOpen && !IsBaseCell(_cursor))
            {
                _context.BaseMenuOpen = false;
            }

            switch (_context.Mode)
            {
                case PlacementMode.Browsing:
                    ConfirmWhileBrowsing();
                    break;
                case PlacementMode.Placing:
                    TryPlace();
                    break;
                case PlacementMode.TowerMenu:
                    // Gamepad and keyboard act through the focused icon. A mouse click elsewhere closes the icons.
                    if (PointerActive)
                    {
                        CloseMenu();
                        ConfirmWhileBrowsing();
                    }

                    break;
            }
        }

        private void ConfirmWhileBrowsing()
        {
            if (_towers.TryGetValue(_cursor, out Tower tower))
            {
                _context.SelectedPlacedTower = tower;
                _context.Mode = PlacementMode.TowerMenu;
            }
            else if (HeroPostAt(_cursor) >= 0)
            {
                _context.HeroMenuPost = HeroPostAt(_cursor);
                _cellMessage = string.Empty;
            }
            else if (IsBaseCell(_cursor))
            {
                _context.BaseMenuOpen = true;
                _cellMessage = string.Empty;
            }
            else if (_context.SelectedPathObject != null && CanPlacePathObject(_context.SelectedPathObject, _cursor))
            {
                _cellMessage = string.Empty;
                _context.Mode = PlacementMode.Placing;
            }
            else if (_context.SelectedPathObject == null && _buildGrid.CanBuild(_cursor) && _context.SelectedTower != null)
            {
                _cellMessage = string.Empty;
                _context.Mode = PlacementMode.Placing;
            }
            else
            {
                _cellMessage = DescribeBlockedCell();
            }

            Refresh();
        }

        // The base stands at the end of the main road; the last rung of the road is its doorstep and opens its menu.
        private bool IsBaseCell(Vector2Int cell)
        {
            Vector2Int[] main = _context.Level.MainRoad;
            return main.Length > 0 && _context.Level.Road.RungContains(main[main.Length - 1], cell);
        }

        // The index of the hero post whose standing spot is this cell, or -1.
        private int HeroPostAt(Vector2Int cell)
        {
            Vector2Int[] cells = _context.HeroPostCells;
            for (int i = 0; i < cells.Length; i++)
            {
                if (_context.Level.Road.RungContains(cells[i], cell))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnCancelled()
        {
            if (_context.Paused)
            {
                return;
            }

            if (_context.HeroMenuPost >= 0 || _context.BaseMenuOpen)
            {
                _context.HeroMenuPost = -1;
                _context.BaseMenuOpen = false;
                Refresh();
                return;
            }

            switch (_context.Mode)
            {
                case PlacementMode.Placing:
                    _context.Mode = PlacementMode.Browsing;
                    Refresh();
                    break;
                case PlacementMode.TowerMenu:
                    CloseMenu();
                    break;
            }
        }

        private void CloseMenu()
        {
            _context.SelectedPlacedTower = null;
            _context.Mode = PlacementMode.Browsing;
            Refresh();
        }

        private void TryPlace()
        {
            if (_context.SelectedPathObject != null)
            {
                TryPlacePathObject(_context.SelectedPathObject);
                return;
            }

            TowerDefinition definition = _context.SelectedTower;
            if (definition == null || !_buildGrid.CanBuild(_cursor) || !_context.Wallet.TrySpend(definition.BuildCost))
            {
                Debug.Log($"[Placement] place refused: tower={(definition != null ? definition.name : "none")} canBuild={_buildGrid.CanBuild(_cursor)} money={_context.Wallet.Money}");
                return;
            }

            Tower tower = CreateTower(definition, _cursor, preview: false);
            _towers[_cursor] = tower;
            _buildGrid.Occupy(_cursor);
            _context.Mode = PlacementMode.Browsing;
            _context.TowerBuilt.Raise();
            Refresh();
        }

        // Blockades are part of the level, so they stand from the start and use the same menu as towers.
        private void SpawnBlockades()
        {
            RoadCorridor road = _context.Level.Road;
            foreach (BlockadeSpot spot in _context.Level.Blockades)
            {
                Tower blockade = CreateTower(spot.Blockade, spot.Cell, preview: false);
                Vector3 along = road.DirectionAt(spot.Cell);
                blockade.transform.rotation = Quaternion.LookRotation(along);
                _blockades.Add(blockade);

                // The gate spans the corridor, and the rung in front of it is the hero's place: both stay free of
                // anything else. Every cell of the gate's rung opens the gate's menu.
                var stand = spot.Cell - new Vector2Int(Mathf.RoundToInt(along.x), Mathf.RoundToInt(along.z));
                foreach (Vector2Int cell in road.Rung(spot.Cell))
                {
                    _towers[cell] = blockade;
                    _buildGrid.Occupy(cell);
                }

                foreach (Vector2Int cell in road.Rung(stand))
                {
                    _buildGrid.Occupy(cell);
                }
            }
        }

        // Raised ground lifts what stands on it; the grid itself stays flat.
        private Vector3 CellWorld(Vector2Int cell)
        {
            Vector3 world = _map.CellToWorld(cell);
            world.y = _terrain.HeightAt(cell);
            return world;
        }

        // A tower on a cell beside the road stands back from it, so a shoulder stays open between the walking
        // lane and the gun; the tower keeps its cell, only its spot inside the cell moves. Gates stay on the road.
        private Vector3 RoadClearance(Vector2Int cell)
        {
            Vector2Int away = Vector2Int.zero;
            for (int i = 0; i < Neighbours.Length; i++)
            {
                if (_buildGrid.IsRoad(cell + Neighbours[i]))
                {
                    away -= Neighbours[i];
                }
            }

            if (away == Vector2Int.zero)
            {
                return Vector3.zero;
            }

            float clearance = PlainGroundAround(cell, away) ? RoadClearanceCells : FittedClearanceCells;
            return new Vector3(away.x, 0f, away.y).normalized * (clearance * _map.CellSize);
        }

        // True when every cell the shifted pedestal could overhang is level ground like the tower's own cell.
        private bool PlainGroundAround(Vector2Int cell, Vector2Int away)
        {
            float height = _terrain.HeightAt(cell);
            for (int x = Mathf.Min(0, away.x); x <= Mathf.Max(0, away.x); x++)
            {
                for (int y = Mathf.Min(0, away.y); y <= Mathf.Max(0, away.y); y++)
                {
                    Vector2Int other = cell + new Vector2Int(x, y);
                    if (!_map.Contains(other) || _terrain.IsLiquid(other) || _buildGrid.IsBlocked(other) || !Mathf.Approximately(_terrain.HeightAt(other), height))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private Tower CreateTower(TowerDefinition definition, Vector2Int cell, bool preview)
        {
            var go = new GameObject(preview ? "TowerPreview" : $"Tower_{definition.DisplayName}");
            go.transform.SetParent(_parent, false);
            var tower = go.AddComponent<Tower>();
            if (definition.BlocksGroundEnemies)
            {
                // A gate stands on the centre line and spans the whole corridor.
                Vector3 centre = _context.Level.Road.CentreOf(cell);
                go.transform.position = new Vector3(centre.x, CellWorld(cell).y, centre.z);
                tower.Span = _context.Level.Road.Width * _map.CellSize;
            }
            else
            {
                go.transform.position = CellWorld(cell) + RoadClearance(cell);
            }

            go.AddComponent<TowerView>();
            tower.Initialize(definition, _context.Targets, _context.Projectiles, _context.ModifiersFor(definition));
            if (preview)
            {
                tower.enabled = false;
            }
            else
            {
                tower.Register(_context.Structures);
                tower.Wrecked += OnWrecked;
            }

            return tower;
        }

        private void OnWrecked(Tower tower) => _context.StructureWrecked.Raise(tower.Position);

        private void OnUpgrade()
        {
            Tower tower = _context.SelectedPlacedTower;
            if (_context.Mode != PlacementMode.TowerMenu || tower == null || !tower.CanUpgrade)
            {
                return;
            }

            if (_context.Wallet.TrySpend(tower.NextUpgradeCost))
            {
                tower.Upgrade();
                _context.TowerUpgraded.Raise();
                Refresh();
            }
        }

        private void OnRepair()
        {
            Tower tower = _context.SelectedPlacedTower;
            if (_context.Mode != PlacementMode.TowerMenu || tower == null || !tower.NeedsRepair)
            {
                return;
            }

            if (_context.Wallet.TrySpend(tower.RepairCost))
            {
                tower.RepairFully();
                _context.TowerRepaired.Raise();
                Refresh();
            }
        }

        private void OnSell()
        {
            Tower tower = _context.SelectedPlacedTower;
            if (_context.Mode != PlacementMode.TowerMenu || tower == null)
            {
                return;
            }

            if (!tower.CanBeSold)
            {
                // The sell shortcut is the "send hero here" shortcut on a blockade.
                _context.HeroSendRequested.Raise();
                return;
            }

            _context.Wallet.Add(tower.SellValue);
            _context.TowerSold.Raise();
            Vector2Int cell = _map.WorldToCell(tower.Position);
            _towers.Remove(cell);
            _buildGrid.Release(cell);
            tower.Retire();
            Destroy(tower.gameObject);
            CloseMenu();
        }

        private void OnNextTower() => CycleSelection(1);

        private void OnPreviousTower() => CycleSelection(-1);

        private void CycleSelection(int direction)
        {
            int count = _context.AvailableTowers.Count;
            if (count == 0)
            {
                return;
            }

            int current = 0;
            for (int i = 0; i < count; i++)
            {
                if (_context.AvailableTowers[i] == _context.SelectedTower)
                {
                    current = i;
                }
            }

            _context.SelectTower(_context.AvailableTowers[(current + direction + count) % count]);
            Refresh();
        }

        /// <summary>Updates the cursor marker, the placement preview, the range ring and the status text.</summary>
        private void Refresh()
        {
            Vector3 world = CellWorld(_cursor);
            bool hasTower = _towers.TryGetValue(_cursor, out Tower hovered);
            PlaceMarker(world, hasTower, hovered);
            bool placing = _context.Mode == PlacementMode.Placing;
            TowerDefinition selected = _context.SelectedTower;
            PathObjectDefinition pathObject = _context.SelectedPathObject;
            bool cellFree = pathObject != null ? CanPlacePathObject(pathObject, _cursor) : _buildGrid.CanBuild(_cursor);
            int price = pathObject != null ? pathObject.Cost : selected != null ? selected.BuildCost : 0;
            bool valid = !hasTower && cellFree && (pathObject != null || selected != null) && _context.Wallet.CanAfford(price);

            _marker.gameObject.SetActive(_context.Mode != PlacementMode.TowerMenu);
            Color markerColor = hasTower ? TowerColor : (placing ? valid : cellFree) ? ValidColor : InvalidColor;
            _markerRenderer.sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(markerColor, placing ? PlacingAlpha : BrowsingAlpha, MarkerEmission);

            UpdateOffer();
            // The preview and its range ring stand where the tower itself would stand: shifted away from the road.
            Vector3 stand = _buildGrid.IsRoad(_cursor) ? world : world + RoadClearance(_cursor);
            UpdateGhost(placing && pathObject == null, selected, stand);
            UpdateRange(placing, pathObject != null ? null : selected, valid, hasTower, hovered, stand);
            if (placing && pathObject != null)
            {
                _range.Show(world, pathObject.Radius, valid ? ValidColor : InvalidColor);
            }

            _context.Status = placing ? (pathObject != null ? PathObjectStatus(pathObject, valid) : PlacingStatus(selected, valid)) : _cellMessage;
            _context.Changed.Raise();
        }

        // On a build cell the marker shows exactly where the tower will stand (shifted away from the road, like the
        // tower itself); on the road it covers the whole cell, which is what a path object takes.
        private void PlaceMarker(Vector3 world, bool hasTower, Tower hovered)
        {
            bool wholeCell = _buildGrid.IsRoad(_cursor);
            Vector3 at = wholeCell ? world : hasTower ? hovered.Position : world + RoadClearance(_cursor);
            float size = _map.CellSize * (wholeCell ? MarkerCellFraction : MarkerFootprintCells);
            _marker.position = new Vector3(at.x, world.y + MarkerHeight, at.z);
            _marker.localScale = new Vector3(size, _marker.localScale.y, size);
        }

        private void UpdateGhost(bool placing, TowerDefinition selected, Vector3 world)
        {
            if (!placing || selected == null)
            {
                if (_ghost != null)
                {
                    Destroy(_ghost.gameObject);
                    _ghost = null;
                    _ghostDefinition = null;
                }

                return;
            }

            if (_ghost != null && _ghostDefinition != selected)
            {
                Destroy(_ghost.gameObject);
                _ghost = null;
            }

            if (_ghost == null)
            {
                _ghost = CreateTower(selected, _cursor, preview: true);
                _ghostDefinition = selected;
            }

            _ghost.transform.position = world;
        }

        private void UpdateRange(bool placing, TowerDefinition selected, bool valid, bool hasTower, Tower hovered, Vector3 world)
        {
            if (placing && selected != null)
            {
                float range = selected.GetLevel(0).Range * _context.ModifiersFor(selected).TowerRangeMultiplier;
                _range.Show(world, range, valid ? ValidColor : InvalidColor);
            }
            else if (_context.Mode == PlacementMode.TowerMenu && _context.SelectedPlacedTower != null)
            {
                _range.Show(_context.SelectedPlacedTower.Position, _context.SelectedPlacedTower.Range, TowerColor);
            }
            else if (hasTower)
            {
                _range.Show(hovered.Position, hovered.Range, TowerColor);
            }
            else
            {
                _range.Hide();
            }
        }

        // Road objects need a free road cell (blockades and the hero's cells count as taken), the others a free build cell.
        private bool CanPlacePathObject(PathObjectDefinition definition, Vector2Int cell)
        {
            if (_towers.ContainsKey(cell) || _pathObjectCells.Contains(cell))
            {
                return false;
            }

            if (definition.Placement == PathObjectPlacement.Road)
            {
                return _buildGrid.IsRoad(cell) && !_buildGrid.IsOccupied(cell);
            }

            return _buildGrid.CanBuild(cell);
        }

        private void TryPlacePathObject(PathObjectDefinition definition)
        {
            if (!CanPlacePathObject(definition, _cursor) || !_context.Wallet.TrySpend(definition.Cost))
            {
                return;
            }

            var go = new GameObject($"PathObject_{definition.DisplayName}");
            go.transform.SetParent(_parent, false);
            go.transform.position = CellWorld(_cursor);

            Vector3[] road = new Vector3[0];
            int next = 0;
            if (definition.Kind == PathObjectKind.Roller && _context.Level.Road.TryLocate(_cursor, out Vector3[] route, out int nextIndex))
            {
                road = route;
                next = nextIndex;
            }

            var pathObject = go.AddComponent<PathObject>();
            pathObject.Initialize(definition, _context.Targets, road, next);
            go.AddComponent<PathObjectView>().Initialize(pathObject);

            Vector2Int cell = _cursor;
            _pathObjectCells.Add(cell);
            pathObject.Triggered += (position, radius) => _context.Projectiles.Burst(position, radius);
            pathObject.Finished += finished =>
            {
                _pathObjectCells.Remove(cell);
                Destroy(finished.gameObject);
            };

            _context.Mode = PlacementMode.Browsing;
            _context.PathObjectPlaced.Raise();
            Refresh();
        }

        private string PathObjectStatus(PathObjectDefinition definition, bool valid)
        {
            if (valid)
            {
                return $"{definition.DisplayName} ({definition.Cost})";
            }

            return CanPlacePathObject(definition, _cursor) ? "Not enough money" : "Cannot place here";
        }

        // Works out whether the selected tower has a neighbour it can be combined with, so the menu can offer it.
        private void UpdateOffer()
        {
            _combinePartner = null;
            _context.CombineOffer = null;
            Tower selected = _context.SelectedPlacedTower;
            if (_context.Mode != PlacementMode.TowerMenu || selected == null || selected.Definition.BlocksGroundEnemies || selected.IsWrecked)
            {
                return;
            }

            Vector2Int cell = _map.WorldToCell(selected.Position);
            foreach (Vector2Int step in NeighbourSteps)
            {
                if (!_towers.TryGetValue(cell + step, out Tower neighbour) || neighbour.IsWrecked || neighbour.Definition.BlocksGroundEnemies)
                {
                    continue;
                }

                foreach (CombineRecipe recipe in _context.Level.Recipes)
                {
                    if (recipe.Matches(selected.Definition, neighbour.Definition))
                    {
                        _combinePartner = neighbour;
                        _context.CombineOffer = recipe;
                        return;
                    }
                }
            }
        }

        // Both towers go and one hybrid takes the selected tower's place; the value of both carries over for selling.
        private void OnCombine()
        {
            Tower selected = _context.SelectedPlacedTower;
            CombineRecipe recipe = _context.CombineOffer;
            Tower partner = _combinePartner;
            if (_context.Mode != PlacementMode.TowerMenu || selected == null || partner == null || recipe == null || !_context.Wallet.TrySpend(recipe.Cost))
            {
                return;
            }

            Vector2Int cell = _map.WorldToCell(selected.Position);
            Vector2Int partnerCell = _map.WorldToCell(partner.Position);
            int inherited = selected.TotalSpent + partner.TotalSpent;

            RemoveTower(selected, cell, release: false);
            RemoveTower(partner, partnerCell, release: true);

            Tower hybrid = CreateTower(recipe.Result, cell, preview: false);
            hybrid.InheritedValue = inherited;
            _towers[cell] = hybrid;
            _context.TowerUpgraded.Raise();
            CloseMenu();
        }

        private void RemoveTower(Tower tower, Vector2Int cell, bool release)
        {
            _towers.Remove(cell);
            if (release)
            {
                _buildGrid.Release(cell);
            }

            tower.Retire();
            Destroy(tower.gameObject);
        }

        private string DescribeBlockedCell()
        {
            if (_context.SelectedPathObject != null)
            {
                return _context.SelectedPathObject.Placement == PathObjectPlacement.Road ? "This goes on the road" : "This goes next to the road";
            }

            if (_context.SelectedTower == null)
            {
                return "Pick a tower first";
            }

            if (_buildGrid.IsRoad(_cursor))
            {
                return "Towers cannot be built on the road";
            }

            return _buildGrid.IsBlocked(_cursor) ? "Something is in the way here" : "Cannot build here";
        }

        private string PlacingStatus(TowerDefinition selected, bool valid)
        {
            if (valid)
            {
                return $"{selected.DisplayName} ({selected.BuildCost})";
            }

            if (!_buildGrid.CanBuild(_cursor))
            {
                return "Cannot build here";
            }

            return "Not enough money";
        }
    }
}



