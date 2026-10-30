using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Checks a level for mistakes before it is played: paths stay on the grid and the road is axis aligned,
    /// blocked cells are valid, every spawn group has a path that suits its enemy, and every referenced definition exists.
    /// Used by the level bootstrapper and by tests.
    /// </summary>
    public static class LevelValidator
    {
        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static bool Validate(LevelDefinition level, List<string> errors)
        {
            errors.Clear();

            if (!level.IsValid(out string basicError))
            {
                errors.Add(basicError);
                return false;
            }

            GridMap grid = level.Grid;
            CheckPaths(level, grid, errors);
            CheckBlockedCells(level, grid, errors);
            CheckTowers(level, errors);
            CheckBlockades(level, errors);
            CheckTerrain(level, errors);
            CheckMines(level, grid, errors);
            CheckRoad(level, grid, errors);
            CheckRoadsReachBase(level, errors);
            CheckWaves(level, errors);
            CheckBossEntrance(level, grid, errors);
            CheckNests(level, grid, errors);
            return errors.Count == 0;
        }

        // Raised ground would lift the road and liquids would drown it; the cells around a blockade hold the hero.
        private static void CheckTerrain(LevelDefinition level, List<string> errors)
        {
            RoadCorridor road = level.Road;
            TerrainMap terrain = level.Terrain;

            foreach (KeyValuePair<Vector2Int, TerrainKind> entry in terrain.Cells)
            {
                if (entry.Value == TerrainKind.Ground)
                {
                    continue;
                }

                if (road.IsRoad(entry.Key))
                {
                    errors.Add($"Terrain {entry.Value} at {entry.Key} lies on the road.");
                }

                if (!TerrainMap.IsLiquid(entry.Value))
                {
                    continue;
                }

                foreach (BlockadeSpot spot in level.Blockades)
                {
                    if (Mathf.Abs(spot.Cell.x - entry.Key.x) <= 1 && Mathf.Abs(spot.Cell.y - entry.Key.y) <= 1)
                    {
                        errors.Add($"Liquid at {entry.Key} is next to the blockade at {spot.Cell}, where the hero stands.");
                    }
                }
            }
        }

        private static void CheckPaths(LevelDefinition level, GridMap grid, List<string> errors)
        {
            bool hasRoad = false;
            for (int p = 0; p < level.Paths.Length; p++)
            {
                EnemyPath path = level.Paths[p];
                string label = $"Path {p} '{path.Name}'";
                if (path.Waypoints == null || path.Waypoints.Length < 2)
                {
                    errors.Add($"{label} needs at least two waypoints.");
                    continue;
                }

                hasRoad |= !path.Flying;
                for (int i = 0; i < path.Waypoints.Length; i++)
                {
                    if (!grid.Contains(path.Waypoints[i]))
                    {
                        errors.Add($"{label} waypoint {i} {path.Waypoints[i]} is outside the grid.");
                    }

                    bool diagonal = i > 0 && path.Waypoints[i].x != path.Waypoints[i - 1].x && path.Waypoints[i].y != path.Waypoints[i - 1].y;
                    if (!path.Flying && diagonal)
                    {
                        errors.Add($"{label} segment {i - 1}-{i} is not axis aligned.");
                    }
                }
            }

            if (!hasRoad)
            {
                errors.Add("Level needs at least one road path.");
            }
        }

        private static void CheckBlockedCells(LevelDefinition level, GridMap grid, List<string> errors)
        {
            var seen = new HashSet<Vector2Int>();

            foreach (Vector2Int cell in level.BlockedCells)
            {
                if (!grid.Contains(cell))
                {
                    errors.Add($"Blocked cell {cell} is outside the grid.");
                }

                if (!seen.Add(cell))
                {
                    errors.Add($"Blocked cell {cell} is listed twice.");
                }
            }

            if (level.GridSize.x * level.GridSize.y - level.Road.Cells.Count - seen.Count <= 0)
            {
                errors.Add("Level has no buildable cells.");
            }
        }

        // A mining node on the road, on liquid or outside the grid could never be reached or built around.
        private static void CheckMines(LevelDefinition level, GridMap grid, List<string> errors)
        {
            foreach (Vector2Int cell in level.MineCells)
            {
                if (!grid.Contains(cell) || level.Road.IsRoad(cell) || level.Terrain.IsLiquid(cell))
                {
                    errors.Add($"Mining node at {cell} is outside the grid, on the road or on liquid.");
                }
            }
        }

        // Every road must end where the main road ends, at the base. A side road that stops at a junction would
        // make its enemies count as having reached the base without walking there.
        private static void CheckRoadsReachBase(LevelDefinition level, List<string> errors)
        {
            Vector2Int[] main = level.MainRoad;
            if (main.Length == 0)
            {
                return;
            }

            Vector2Int baseCell = main[main.Length - 1];
            for (int p = 0; p < level.Paths.Length; p++)
            {
                EnemyPath path = level.Paths[p];
                if (!path.Flying && path.Waypoints != null && path.Waypoints.Length > 0 && path.Waypoints[path.Waypoints.Length - 1] != baseCell)
                {
                    errors.Add($"Road path {p} '{path.Name}' does not end at the base {baseCell}.");
                }
            }
        }

        private static void CheckTowers(LevelDefinition level, List<string> errors)
        {
            if (level.AvailableTowers.Length == 0)
            {
                errors.Add("Level has no available towers.");
            }

            foreach (var tower in level.AvailableTowers)
            {
                if (tower == null)
                {
                    errors.Add("Available towers contains an empty entry.");
                }
            }
        }

        private static void CheckBlockades(LevelDefinition level, List<string> errors)
        {
            RoadCorridor road = level.Road;
            var seen = new HashSet<Vector2Int>();

            foreach (BlockadeSpot spot in level.Blockades)
            {
                if (spot.Blockade == null || !spot.Blockade.BlocksGroundEnemies)
                {
                    errors.Add($"Blockade at {spot.Cell} has no blockade definition.");
                }

                // The gate is centred and turned from its cell, which only works on the row the route runs along.
                if (road.RowOf(spot.Cell) != 0 || road.IsPlaza(spot.Cell))
                {
                    errors.Add($"Blockade at {spot.Cell} is not on the first row of a road, or lies on a plaza.");
                }

                if (!seen.Add(spot.Cell))
                {
                    errors.Add($"Blockade at {spot.Cell} is listed twice.");
                }
            }
        }

        private static void CheckWaves(LevelDefinition level, List<string> errors)
        {
            if (level.DifficultyWaves.Length == 0)
            {
                errors.Add("Level has no waves.");
            }

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                if (set.Difficulty == null || set.Waves == null || set.Waves.Length == 0)
                {
                    errors.Add("A difficulty entry has no difficulty or no waves.");
                    continue;
                }

                foreach (WaveDefinition wave in set.Waves)
                {
                    if (wave == null)
                    {
                        errors.Add($"Difficulty '{set.Difficulty.name}' has an empty wave entry.");
                        continue;
                    }

                    foreach (SpawnGroup group in wave.Groups)
                    {
                        CheckGroup(level, wave, group, errors);
                    }
                }
            }
        }

        private static void CheckGroup(LevelDefinition level, WaveDefinition wave, SpawnGroup group, List<string> errors)
        {
            if (group.Enemy == null)
            {
                errors.Add($"Wave '{wave.name}' has a group without an enemy.");
                return;
            }

            if (group.PathIndex < 0 || group.PathIndex >= level.Paths.Length)
            {
                errors.Add($"Wave '{wave.name}' sends {group.Enemy.name} along path {group.PathIndex}, which does not exist.");
                return;
            }

            bool airEnemy = group.Enemy.Flies;
            if (!airEnemy && level.Paths[group.PathIndex].Flying)
            {
                errors.Add($"Wave '{wave.name}' sends ground enemy {group.Enemy.name} along flying path {group.PathIndex}.");
            }

            if (group.Source == SpawnSource.Gate)
            {
                return;
            }

            if (group.Source == SpawnSource.Nest)
            {
                CheckNestGroup(level, wave, group, errors);
                return;
            }

            // A pool spawn needs a pool of that kind to come out of, and only walkers climb out of pools.
            if (airEnemy)
            {
                errors.Add($"Wave '{wave.name}' sends flyer {group.Enemy.name} out of a {group.Source} pool.");
            }

            if (new LiquidPools(level).Count(LiquidPools.KindOf(group.Source)) == 0)
            {
                errors.Add($"Wave '{wave.name}' sends {group.Enemy.name} out of a {group.Source} pool, but the level has none.");
            }
        }

        // A nest spawn needs a creature with an element and a nest of that element to come out of.
        private static void CheckNestGroup(LevelDefinition level, WaveDefinition wave, SpawnGroup group, List<string> errors)
        {
            if (group.Enemy.Element == Element.None)
            {
                errors.Add($"Wave '{wave.name}' sends {group.Enemy.name} out of a nest, but it has no element.");
                return;
            }

            foreach (ElementNest nest in level.Nests)
            {
                if (nest.Element == group.Enemy.Element)
                {
                    return;
                }
            }

            errors.Add($"Wave '{wave.name}' sends {group.Enemy.name} out of a {group.Enemy.Element} nest, but the level has none.");
        }

        // A nest lies on the grid, off the road, in terrain that suits its element: water, lava and sand nests in
        // that liquid, the rest on dry ground.
        private static void CheckNests(LevelDefinition level, GridMap grid, List<string> errors)
        {
            foreach (ElementNest nest in level.Nests)
            {
                if (!grid.Contains(nest.Cell) || level.Road.IsRoad(nest.Cell))
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} is off the grid or on the road.");
                    continue;
                }

                TerrainKind kind = level.Terrain.KindAt(nest.Cell);
                TerrainKind wanted = NestTerrain(nest.Element);
                bool liquidNest = TerrainMap.IsLiquid(wanted);
                if (liquidNest ? kind != wanted : TerrainMap.IsLiquid(kind))
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} sits in {kind}, not in {wanted}.");
                }
            }
        }

        /// <summary>The terrain a nest of an element belongs in; plain ground for elements without a liquid.</summary>
        public static TerrainKind NestTerrain(Element element)
        {
            switch (element)
            {
                case Element.Water:
                    return TerrainKind.Water;
                case Element.Fire:
                    return TerrainKind.Lava;
                case Element.Sand:
                    return TerrainKind.Quicksand;
                default:
                    return TerrainKind.Ground;
            }
        }

        // A pool entrance must name a liquid cell; a ground entrance a plain cell off the road, inside the grid.
        private static void CheckBossEntrance(LevelDefinition level, GridMap grid, List<string> errors)
        {
            BossEntrance entrance = level.BossEntrance;
            switch (entrance.Kind)
            {
                case EntranceKind.Pool:
                    if (!level.Terrain.IsLiquid(entrance.Cell))
                    {
                        errors.Add($"Boss entrance names pool cell {entrance.Cell}, which holds no liquid.");
                    }

                    break;
                case EntranceKind.Ground:
                    if (!grid.Contains(entrance.Cell) || level.Road.IsRoad(entrance.Cell) || level.Terrain.IsLiquid(entrance.Cell))
                    {
                        errors.Add($"Boss entrance names ground cell {entrance.Cell}, which is off the grid, on the road or liquid.");
                    }

                    break;
            }
        }

        // Every road cell, second rows included, must be on the grid, and a plaza must touch a road through its own
        // cells, or enemies could never walk onto it.
        private static void CheckRoad(LevelDefinition level, GridMap grid, List<string> errors)
        {
            RoadCorridor road = level.Road;
            foreach (Vector2Int cell in road.Cells)
            {
                if (!grid.Contains(cell))
                {
                    errors.Add($"Road cell {cell} is outside the grid.");
                }
            }

            var reached = new HashSet<Vector2Int>();
            var open = new Queue<Vector2Int>();
            foreach (Vector2Int cell in road.PlazaCells)
            {
                if (road.RowOf(cell) >= 0 || TouchesRoad(road, cell))
                {
                    reached.Add(cell);
                    open.Enqueue(cell);
                }
            }

            while (open.Count > 0)
            {
                Vector2Int cell = open.Dequeue();
                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (road.IsPlaza(next) && reached.Add(next))
                    {
                        open.Enqueue(next);
                    }
                }
            }

            foreach (Vector2Int cell in road.PlazaCells)
            {
                if (!reached.Contains(cell))
                {
                    errors.Add($"Plaza cell {cell} is not connected to a road.");
                }
            }
        }

        private static bool TouchesRoad(RoadCorridor road, Vector2Int cell)
        {
            foreach (Vector2Int step in Steps)
            {
                if (road.RowOf(cell + step) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every cell one road path crosses, including the cells between waypoints.</summary>
        public static IEnumerable<Vector2Int> LandPathCells(Vector2Int[] path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                if (i == 0)
                {
                    yield return path[0];
                    continue;
                }

                Vector2Int from = path[i - 1];
                Vector2Int to = path[i];
                if (from.x != to.x && from.y != to.y)
                {
                    // Not axis aligned: reported by CheckPaths, and stepping would never reach the target.
                    yield return to;
                    continue;
                }

                var step = new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));
                Vector2Int cell = from;

                while (cell != to)
                {
                    cell += step;
                    yield return cell;
                }
            }
        }
    }
}
