using System.Collections.Generic;
using Game.Core;
using Game.Platform;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Cursor over the build slots plus buying and upgrading towers. Works with stick, d-pad, keyboard and mouse;
    /// the same Confirm action builds or upgrades.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        private const float StickThreshold = 0.5f;
        private const float StickRepeatSeconds = 0.18f;
        private const float PointerSnapRadiusCells = 0.7f;
        private const float PointerMovedPixels = 1f;
        private const float CursorHeight = 0.05f;
        private static readonly Color CursorColor = new Color(1f, 0.95f, 0.4f);

        private readonly List<Vector3> _slotPositions = new List<Vector3>();

        private LevelContext _context;
        private IInputProvider _input;
        private Camera _camera;
        private Transform _towerParent;
        private Tower[] _towers;
        private Transform _cursorMarker;
        private int _cursor;
        private int _selectedIndex;
        private float _repeatTimer;
        private Vector2 _lastPointer;

        public void Initialize(LevelContext context, IInputProvider input, Camera camera, Transform towerParent)
        {
            _context = context;
            _input = input;
            _camera = camera;
            _towerParent = towerParent;

            GridMap grid = context.Level.Grid;
            foreach (Vector2Int cell in context.Level.BuildSlots)
            {
                _slotPositions.Add(grid.CellToWorld(cell));
            }

            _towers = new Tower[_slotPositions.Count];
            _context.SelectedTower = context.AvailableTowers.Count > 0 ? context.AvailableTowers[0] : null;

            float marker = grid.CellSize * 1.05f;
            _cursorMarker = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, CursorColor, towerParent, Vector3.zero, new Vector3(marker, 0.02f, marker)).transform;
            _lastPointer = input.PointerPosition;
            MoveCursor(0);

            _input.Confirmed += OnConfirmed;
            _input.NextTower += OnNextTower;
            _input.PreviousTower += OnPreviousTower;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Confirmed -= OnConfirmed;
                _input.NextTower -= OnNextTower;
                _input.PreviousTower -= OnPreviousTower;
            }
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            UpdateStickCursor();
            UpdatePointerCursor();
        }

        private void UpdateStickCursor()
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
            MoveCursor(SlotCursor.Next(_slotPositions, _cursor, move));
        }

        private void UpdatePointerCursor()
        {
            Vector2 pointer = _input.PointerPosition;
            if ((pointer - _lastPointer).sqrMagnitude < PointerMovedPixels * PointerMovedPixels)
            {
                return;
            }

            _lastPointer = pointer;
            Ray ray = _camera.ScreenPointToRay(pointer);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance))
            {
                return;
            }

            float snap = _context.Level.CellSize * PointerSnapRadiusCells;
            int slot = SlotCursor.Nearest(_slotPositions, ray.GetPoint(distance), snap);
            if (slot >= 0)
            {
                MoveCursor(slot);
            }
        }

        private void MoveCursor(int index)
        {
            _cursor = index;
            _cursorMarker.position = _slotPositions[index] + Vector3.up * CursorHeight;
            _context.HoveredTower = _towers[index];
            _context.Changed.Raise();
        }

        private void OnConfirmed()
        {
            if (_context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            Tower tower = _towers[_cursor];
            if (tower != null)
            {
                TryUpgrade(tower);
            }
            else
            {
                TryBuild();
            }

            _context.HoveredTower = _towers[_cursor];
            _context.Changed.Raise();
        }

        private void TryUpgrade(Tower tower)
        {
            if (tower.CanUpgrade && _context.Wallet.TrySpend(tower.NextUpgradeCost))
            {
                tower.Upgrade();
            }
        }

        private void TryBuild()
        {
            TowerDefinition definition = _context.SelectedTower;
            if (definition == null || !_context.Wallet.TrySpend(definition.BuildCost))
            {
                return;
            }

            var go = new GameObject($"Tower_{definition.DisplayName}");
            go.transform.SetParent(_towerParent, false);
            go.transform.position = _slotPositions[_cursor];
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(definition, _context.Targets, _context.Projectiles, _context.Modifiers);
            _towers[_cursor] = tower;
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

            _selectedIndex = (_selectedIndex + direction + count) % count;
            _context.SelectedTower = _context.AvailableTowers[_selectedIndex];
            _context.Changed.Raise();
        }
    }
}

