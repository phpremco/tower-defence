using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Which cells can hold a tower: any cell of the grid except the road, the blocked cells the level lists, the ground
    /// under the base model and under the gate that rises at the boss entrance, and not a cell that already has a tower. The road stays free for path objects.
    /// </summary>
    public sealed class BuildGrid
    {
        // How far past the base model a cell middle must lie to take a tower.
        private const float BaseMarginCells = 0.25f;

        private readonly GridMap _grid;
                private readonly HashSet<Vector2Int> _blocked;
        private readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();
        private readonly TerrainMap _terrain;
        private readonly RoadCorridor _road;

        public BuildGrid(LevelDefinition level)
        {
            _grid = level.Grid;
            _terrain = level.Terrain;
            _road = level.Road;
            _blocked = new HashSet<Vector2Int>(level.BlockedCells);
            _blocked.UnionWith(level.MineCells);
            foreach (ElementNest nest in level.Nests)
            {
                _blocked.Add(nest.Cell);
            }

            // Round 55: where the ruin gate will rise at the boss entrance, so no tower stands through it.
            if (level.BossEntrance.Footprint != null)
            {
                _blocked.UnionWith(level.BossEntrance.Footprint);
            }

            BlockBase(level);
        }

        // No tower stands inside the base: every cell whose middle the base model covers (a little grown, since a tower
        // reaches past its middle) is kept free. The base's own guns stand on its mounts instead.
        private void BlockBase(LevelDefinition level)
        {
            BaseArtDefinition art = level.Theme != null ? level.Theme.BaseArt : null;
            if (art == null || level.MainRoad.Length == 0)
            {
                return;
            }

            Vector3 centre = LevelVisualBuilder.BasePosition(level);
            Quaternion facing = LevelVisualBuilder.BaseFacing(level);
            float margin = BaseMarginCells * _grid.CellSize;
            Vector2 min = art.FootprintMin;
            Vector2 max = art.FootprintMax;
            float reach = Mathf.Max(Mathf.Max(-min.x, max.x), Mathf.Max(-min.y, max.y)) + margin;
            int cells = Mathf.CeilToInt(reach / _grid.CellSize) + 1;
            Vector2Int middle = _grid.WorldToCell(centre);
            for (int x = -cells; x <= cells; x++)
            {
                for (int y = -cells; y <= cells; y++)
                {
                    Vector2Int cell = middle + new Vector2Int(x, y);
                    if (_grid.Contains(cell) && !IsRoad(cell) && art.Covers(_grid.CellToWorld(cell), centre, facing, margin))
                    {
                        _blocked.Add(cell);
                    }
                }
            }
        }

        public bool IsRoad(Vector2Int cell) => _road.IsRoad(cell);

        public bool IsBlocked(Vector2Int cell) => _blocked.Contains(cell);

        public bool IsOccupied(Vector2Int cell) => _occupied.Contains(cell);

        public bool IsLiquid(Vector2Int cell) => _terrain.IsLiquid(cell);

        /// <summary>True when the cell is a free, buildable tile.</summary>
        public bool CanBuild(Vector2Int cell) =>
            _grid.Contains(cell) && !IsRoad(cell) && !IsBlocked(cell) && !IsLiquid(cell) && !IsOccupied(cell);

        public void Occupy(Vector2Int cell) => _occupied.Add(cell);

        public void Release(Vector2Int cell) => _occupied.Remove(cell);
    }
}

