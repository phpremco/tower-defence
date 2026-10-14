using System.Collections.Generic;
using Game.Core;
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
        private const float MarkerEmission = 2f;
        private static readonly Color ValidColor = new Color(0.3f, 1f, 0.8f);
        private static readonly Color InvalidColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color TowerColor = new Color(1f, 0.9f, 0.35f);

        private readonly Dictionary<Vector2Int, Tower> _towers = new Dictionary<Vector2Int, Tower>();

        private LevelContext _context;
        private IInputProvider _input;
        private Camera _camera;
        private Transform _parent;
        private BuildGrid _buildGrid;
        private GridMap _map;
        private Vector2Int _cursor;
        private Transform _marker;
        private Renderer _markerRenderer;
        private RangeIndicator _range;
        private Tower _ghost;
        private TowerDefinition _ghostDefinition;
        private float _repeatTimer;
        private float _lastPointerMove = float.NegativeInfinity;
        private Vector2 _lastPointer;

        public void Initialize(LevelContext context, IInputProvider input, Camera camera, Transform parent)
        {
            _context = context;
            _input = input;
            _camera = camera;
            _parent = parent;
            _map = context.Level.Grid;
            _buildGrid = new BuildGrid(context.Level);
            _context.SelectedTower = context.AvailableTowers.Count > 0 ? context.AvailableTowers[0] : null;

            float size = _map.CellSize;
            GameObject marker = PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube, ValidColor, parent, Vector3.zero, new Vector3(size * 0.96f, 0.03f, size * 0.96f), MarkerEmission);
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
            }
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running || _context.Mode == PlacementMode.TowerMenu)
            {
                return;
            }

            UpdateStick();
            UpdatePointer();
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
            Refresh();
        }

        private void OnConfirmed()
        {
            if (_context.Outcome != LevelOutcome.Running || _context.PointerOverUi)
            {
                return;
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
            else if (_buildGrid.CanBuild(_cursor) && _context.SelectedTower != null)
            {
                _context.Mode = PlacementMode.Placing;
            }

            Refresh();
        }

        private void OnCancelled()
        {
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
            TowerDefinition definition = _context.SelectedTower;
            if (definition == null || !_buildGrid.CanBuild(_cursor) || !_context.Wallet.TrySpend(definition.BuildCost))
            {
                return;
            }

            Tower tower = CreateTower(definition, _cursor, preview: false);
            _towers[_cursor] = tower;
            _buildGrid.Occupy(_cursor);
            _context.Mode = PlacementMode.Browsing;
            Refresh();
        }

        private Tower CreateTower(TowerDefinition definition, Vector2Int cell, bool preview)
        {
            var go = new GameObject(preview ? "TowerPreview" : $"Tower_{definition.DisplayName}");
            go.transform.SetParent(_parent, false);
            go.transform.position = _map.CellToWorld(cell);
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(definition, _context.Targets, _context.Projectiles, _context.ModifiersFor(definition));
            if (preview)
            {
                tower.enabled = false;
            }
            else
            {
                tower.Register(_context.Structures);
            }

            return tower;
        }

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

            _context.Wallet.Add(tower.SellValue);
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
            Vector3 world = _map.CellToWorld(_cursor);
            _marker.position = world + Vector3.up * MarkerHeight;

            bool hasTower = _towers.TryGetValue(_cursor, out Tower hovered);
            bool placing = _context.Mode == PlacementMode.Placing;
            TowerDefinition selected = _context.SelectedTower;
            bool valid = !hasTower && _buildGrid.CanBuild(_cursor) && selected != null && _context.Wallet.CanAfford(selected.BuildCost);

            _marker.gameObject.SetActive(_context.Mode != PlacementMode.TowerMenu);
            _markerRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(hasTower ? TowerColor : valid || !placing ? ValidColor : InvalidColor, MarkerEmission);

            UpdateGhost(placing, selected, world);
            UpdateRange(placing, selected, valid, hasTower, hovered, world);

            _context.Status = placing ? PlacingStatus(selected, valid) : string.Empty;
            _context.Changed.Raise();
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
