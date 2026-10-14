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
            CheckWaves(level, errors);
            return errors.Count == 0;
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
            var road = new HashSet<Vector2Int>(RoadCells(level));
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

            if (level.GridSize.x * level.GridSize.y - road.Count - seen.Count <= 0)
            {
                errors.Add("Level has no buildable cells.");
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

            bool airEnemy = group.Enemy.Layer == TargetLayers.Air;
            if (!airEnemy && level.Paths[group.PathIndex].Flying)
            {
                errors.Add($"Wave '{wave.name}' sends ground enemy {group.Enemy.name} along flying path {group.PathIndex}.");
            }
        }

        /// <summary>Every cell any road path crosses, including the cells between waypoints.</summary>
        public static IEnumerable<Vector2Int> RoadCells(LevelDefinition level)
        {
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                foreach (Vector2Int cell in LandPathCells(path.Waypoints))
                {
                    yield return cell;
                }
            }
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
