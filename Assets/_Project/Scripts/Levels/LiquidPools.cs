using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The pools of a level: liquid cells grouped into connected bodies per kind, so a spawner can pick a pool and
    /// a cell inside it. Cells outside the grid belong to the pool but are never picked.
    /// </summary>
    public sealed class LiquidPools
    {
        private static readonly Vector2Int[] Steps = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

        private readonly Dictionary<TerrainKind, List<List<Vector2Int>>> _pools = new Dictionary<TerrainKind, List<List<Vector2Int>>>();
        private readonly TerrainMap _terrain;
        private readonly GridMap _grid;

        public LiquidPools(LevelDefinition level)
        {
            _terrain = level.Terrain;
            _grid = level.Grid;
            var seen = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> entry in _terrain.Cells)
            {
                if (!TerrainMap.IsLiquid(entry.Value) || seen.Contains(entry.Key))
                {
                    continue;
                }

                List<Vector2Int> pool = Flood(entry.Key, entry.Value, seen);
                // A pool that lies wholly outside the grid is scenery: nothing can come up in it.
                if (!AnyInsideGrid(pool))
                {
                    continue;
                }

                if (!_pools.TryGetValue(entry.Value, out List<List<Vector2Int>> list))
                {
                    list = new List<List<Vector2Int>>();
                    _pools[entry.Value] = list;
                }

                list.Add(pool);
            }
        }

        public int Count(TerrainKind kind) => _pools.TryGetValue(kind, out List<List<Vector2Int>> list) ? list.Count : 0;

        public IReadOnlyList<Vector2Int> Pool(TerrainKind kind, int index) => _pools[kind][index];

        /// <summary>The pool that holds a cell, or the nearest pool of that kind when the cell is dry. False without pools.</summary>
        public bool TryFindPool(TerrainKind kind, Vector2Int near, out IReadOnlyList<Vector2Int> pool)
        {
            pool = null;
            if (!_pools.TryGetValue(kind, out List<List<Vector2Int>> list) || list.Count == 0)
            {
                return false;
            }

            float best = float.MaxValue;
            foreach (List<Vector2Int> candidate in list)
            {
                foreach (Vector2Int cell in candidate)
                {
                    float distance = (cell - near).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        pool = candidate;
                    }
                }
            }

            return pool != null;
        }

        /// <summary>
        /// A cell of the pool to come up in: inside the grid and, when there is a choice, one with liquid on every
        /// side, so a creature surfaces in open water rather than on the shore. <paramref name="pick"/> selects
        /// among equally good cells.
        /// </summary>
        public Vector2Int SurfaceCell(IReadOnlyList<Vector2Int> pool, int pick)
        {
            int bestScore = -1;
            var candidates = new List<Vector2Int>();
            for (int i = 0; i < pool.Count; i++)
            {
                Vector2Int cell = pool[i];
                if (!_grid.Contains(cell))
                {
                    continue;
                }

                int score = 0;
                foreach (Vector2Int step in Steps)
                {
                    if (_terrain.IsLiquid(cell + step))
                    {
                        score++;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    candidates.Clear();
                }

                if (score == bestScore)
                {
                    candidates.Add(cell);
                }
            }

            return candidates.Count > 0 ? candidates[Mathf.Abs(pick) % candidates.Count] : pool[0];
        }

        public static TerrainKind KindOf(SpawnSource source)
        {
            switch (source)
            {
                case SpawnSource.Water:
                    return TerrainKind.Water;
                case SpawnSource.Lava:
                    return TerrainKind.Lava;
                case SpawnSource.Ice:
                    return TerrainKind.Ice;
                case SpawnSource.Bog:
                    return TerrainKind.Bog;
                default:
                    return TerrainKind.Quicksand;
            }
        }

        private bool AnyInsideGrid(List<Vector2Int> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (_grid.Contains(pool[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private List<Vector2Int> Flood(Vector2Int start, TerrainKind kind, HashSet<Vector2Int> seen)
        {
            var pool = new List<Vector2Int>();
            var open = new Queue<Vector2Int>();
            open.Enqueue(start);
            seen.Add(start);
            while (open.Count > 0)
            {
                Vector2Int cell = open.Dequeue();
                pool.Add(cell);
                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (!seen.Contains(next) && _terrain.KindAt(next) == kind)
                    {
                        seen.Add(next);
                        open.Enqueue(next);
                    }
                }
            }

            return pool;
        }
    }
}
