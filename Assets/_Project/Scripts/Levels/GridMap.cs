using UnityEngine;

namespace Game.Levels
{
    /// <summary>Converts between grid cells and world positions. The grid is centred on the world origin.</summary>
    public readonly struct GridMap
    {
        private readonly Vector2Int _size;
        private readonly float _cellSize;

        public GridMap(Vector2Int size, float cellSize)
        {
            _size = size;
            _cellSize = cellSize;
        }

        public Vector2Int Size => _size;

        public float CellSize => _cellSize;

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < _size.x && cell.y < _size.y;

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float x = (cell.x - (_size.x - 1) * 0.5f) * _cellSize;
            float z = (cell.y - (_size.y - 1) * 0.5f) * _cellSize;
            return new Vector3(x, 0f, z);
        }
    }
}
