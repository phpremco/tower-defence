using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Which cells can hold a tower: any cell of the grid except the road and the blocked cells the level lists,
    /// and not a cell that already has a tower. The road stays free for path objects.
    /// </summary>
    public sealed class BuildGrid
    {
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

