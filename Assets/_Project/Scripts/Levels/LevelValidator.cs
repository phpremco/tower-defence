using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Checks a level for mistakes before it is played: path reaches the base, slots are on valid cells,
    /// and every referenced definition exists. Used by the level bootstrapper and by tests.
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
            CheckLandPath(level, grid, errors);
            CheckAirPath(level, grid, errors);
            CheckSlots(level, grid, errors);
            CheckTowers(level, errors);
            CheckWaves(level, errors);
            return errors.Count == 0;
        }

        private static void CheckLandPath(LevelDefinition level, GridMap grid, List<string> errors)
        {
            Vector2Int[] path = level.LandPath;
            if (path.Length < 2)
            {
                errors.Add("Land path needs at least two waypoints.");
                return;
            }

            for (int i = 0; i < path.Length; i++)
            {
                if (!grid.Contains(path[i]))
                {
                    errors.Add($"Land path waypoint {i} {path[i]} is outside the grid.");
                }

                if (i > 0 && path[i].x != path[i - 1].x && path[i].y != path[i - 1].y)
                {
                    errors.Add($"Land path segment {i - 1}-{i} is not axis aligned.");
                }
            }
        }

        private static void CheckAirPath(LevelDefinition level, GridMap grid, List<string> errors)
        {
            Vector2Int[] path = level.AirPath;
            for (int i = 0; i < path.Length; i++)
            {
                if (!grid.Contains(path[i]))
                {
                    errors.Add($"Air path waypoint {i} {path[i]} is outside the grid.");
                }
            }
        }

        private static void CheckSlots(LevelDefinition level, GridMap grid, List<string> errors)
        {
            var pathCells = new HashSet<Vector2Int>(LandPathCells(level.LandPath));
            var seen = new HashSet<Vector2Int>();

            foreach (Vector2Int slot in level.BuildSlots)
            {
                if (!grid.Contains(slot))
                {
                    errors.Add($"Build slot {slot} is outside the grid.");
                }
                else if (pathCells.Contains(slot))
                {
                    errors.Add($"Build slot {slot} is on the path.");
                }

                if (!seen.Add(slot))
                {
                    errors.Add($"Build slot {slot} is listed twice.");
                }
            }

            if (level.BuildSlots.Length == 0)
            {
                errors.Add("Level has no build slots.");
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

            bool needsAirPath = false;

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
                        if (group.Enemy == null)
                        {
                            errors.Add($"Wave '{wave.name}' has a group without an enemy.");
                        }
                        else if (group.Enemy.Layer == TargetLayers.Air)
                        {
                            needsAirPath = true;
                        }
                    }
                }
            }

            if (needsAirPath && level.AirPath.Length < 2)
            {
                errors.Add("Waves contain air enemies but the air path has fewer than two waypoints.");
            }
        }

        /// <summary>Every cell the land path crosses, including the cells between waypoints.</summary>
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
                    // Not axis aligned: reported by CheckLandPath, and stepping would never reach the target.
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

