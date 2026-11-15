using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 65 (Remco: "a tower on the edge of a plateau shoots through the ground at enemies below"): on every level, for
    /// every tower the level sells against the ground, on every raised cell it can be built on and at every upgrade level,
    /// the straight line from its lowest muzzle (the imported model's shot origin at rest, over the cell's middle) to every
    /// point of the road a walker can be on within its reach (the high-ground bonus included) stays above the drawn
    /// landscape (<see cref="TerrainField"/>). The same walk for towers on the plain is written to the report
    /// (Tools/PlayTest/r65_line_of_fire.txt) but not asserted: a gun on low ground behind a plateau still fires through it.
    /// The report also counts the nests whose creatures walk straight to the road across raised ground or its slope (they
    /// walk at road height, so they wade through it).
    /// </summary>
    public sealed class LineOfFireTests
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const string ReportPath = "Tools/PlayTest/r65_line_of_fire.txt";

        // A line may graze the ground by this much (the field is sampled, the mesh between its points is flat).
        private const float Graze = 0.01f;
        private const float SampleStep = 0.05f;
        // The last stretch before the target is the creature itself: the shot ends at its feet on the ground.
        private const float TargetSkip = 0.15f;
        // Ground this high counts as raised or sloping for the report's tightest line (a line always nears the road at its end).
        private const float RisingGround = 0.02f;
        // Without an imported model a tower fires from this height (Tower.MuzzleHeight).
        private const float PlainMuzzle = 1f;

        private readonly struct Gun
        {
            public Gun(string name, float range, float muzzle)
            {
                Name = name;
                Range = range;
                Muzzle = muzzle;
            }

            public string Name { get; }
            public float Range { get; }
            public float Muzzle { get; }
        }

        [Test]
        public void NoTowerOnRaisedGround_FiresThroughTheLandscape_AtAnythingOnTheRoadInItsReach()
        {
            var report = new StringBuilder();
            var failures = new List<string>();
            int lines = 0;
            foreach (LevelDefinition level in Levels())
            {
                var field = new TerrainField(level, new RoadShape(level));
                var build = new BuildGrid(level);
                List<Vector3> targets = RoadTargets(level);
                List<Gun> guns = Guns(level);
                float bonus = level.TerrainRules != null ? level.TerrainRules.HighGroundRange : 1f;
                int raisedCells = 0;
                float tightest = float.MaxValue;
                string tightestWhere = "-";
                foreach (KeyValuePair<Vector2Int, TerrainKind> pair in level.Terrain.Cells)
                {
                    if (pair.Value != TerrainKind.Raised || !build.CanBuild(pair.Key))
                    {
                        continue;
                    }

                    raisedCells++;
                    Vector3 foot = level.Grid.CellToWorld(pair.Key) + Vector3.up * level.Terrain.HeightAt(pair.Key);
                    foreach (Gun gun in guns)
                    {
                        float clearance = WorstClearance(field, foot + Vector3.up * gun.Muzzle, targets, gun.Range * bonus, out Vector3 worst, out float overRise, ref lines);
                        if (overRise < tightest)
                        {
                            tightest = overRise;
                            tightestWhere = $"{gun.Name} on {pair.Key}";
                        }

                        if (clearance < -Graze)
                        {
                            failures.Add($"{level.name}: {gun.Name} on the raised cell {pair.Key} fires {-clearance:0.000} under the ground at the road near {level.Grid.WorldToCell(worst)}");
                        }
                    }
                }

                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: raised build cells {1}, guns {2}, tightest line over the plateau and its slope {3:0.000} above the ground ({4}); on the plain: {5}; {6}",
                    level.name, raisedCells, guns.Count, raisedCells > 0 ? tightest : 0f, tightestWhere, PlainReport(level, field, build, targets, guns), NestReport(level, field)));
            }

            report.AppendLine(string.Format(CultureInfo.InvariantCulture, "lines walked from raised cells: {0}; failures: {1}", lines, failures.Count));
            foreach (string failure in failures)
            {
                report.AppendLine("  ! " + failure);
            }

            File.WriteAllText(ReportPath, report.ToString());
            Assert.Greater(lines, 0, "no line of fire was walked: no level has a raised build cell in reach of its road");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // The lowest clearance of the straight lines from a muzzle to every target within reach (flat distance, as the tower
        // measures it), the target it belongs to, and the lowest clearance over raised or sloping ground alone.
        private static float WorstClearance(TerrainField field, Vector3 muzzle, List<Vector3> targets, float reach, out Vector3 worstTarget, out float overRise, ref int lines)
        {
            float worst = float.MaxValue;
            overRise = float.MaxValue;
            worstTarget = muzzle;
            foreach (Vector3 target in targets)
            {
                float dx = target.x - muzzle.x;
                float dz = target.z - muzzle.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance > reach || distance < TargetSkip * 2f)
                {
                    continue;
                }

                lines++;
                for (float walked = 0f; walked < distance - TargetSkip; walked += SampleStep)
                {
                    float t = walked / distance;
                    float ground = field.HeightAt(muzzle.x + dx * t, muzzle.z + dz * t);
                    float clearance = Mathf.Lerp(muzzle.y, target.y, t) - ground;
                    if (ground > RisingGround)
                    {
                        overRise = Mathf.Min(overRise, clearance);
                    }

                    if (clearance < worst)
                    {
                        worst = clearance;
                        worstTarget = target;
                    }
                }
            }

            return worst;
        }

        // Towers on the plain whose line to some road point in reach passes under raised ground: counted, not asserted.
        private static string PlainReport(LevelDefinition level, TerrainField field, BuildGrid build, List<Vector3> targets, List<Gun> guns)
        {
            var raised = new List<Vector3>();
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in level.Terrain.Cells)
            {
                if (pair.Value == TerrainKind.Raised)
                {
                    raised.Add(level.Grid.CellToWorld(pair.Key));
                }
            }

            float longest = 0f;
            foreach (Gun gun in guns)
            {
                longest = Mathf.Max(longest, gun.Range);
            }

            int cells = 0;
            int blocked = 0;
            int unused = 0;
            var examples = new List<string>();
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!build.CanBuild(cell) || level.Terrain.KindAt(cell) == TerrainKind.Raised)
                    {
                        continue;
                    }

                    Vector3 foot = level.Grid.CellToWorld(cell);
                    if (!Near(foot, raised, longest))
                    {
                        continue;
                    }

                    cells++;
                    bool under = false;
                    foreach (Gun gun in guns)
                    {
                        under |= WorstClearance(field, foot + Vector3.up * gun.Muzzle, targets, gun.Range, out _, out _, ref unused) < -Graze;
                    }

                    if (under)
                    {
                        blocked++;
                        if (examples.Count < 6)
                        {
                            examples.Add(cell.ToString());
                        }
                    }
                }
            }

            return $"{blocked} of {cells} build cells within reach of raised ground have a line under it (e.g. {string.Join(" ", examples)})";
        }

        // Creatures out of a nest walk straight to the nearest road cell at road height: a leg that crosses raised ground or
        // its slope wades through it. Counted, not asserted (the walk never followed the landscape).
        private static string NestReport(LevelDefinition level, TerrainField field)
        {
            int wading = 0;
            float deepest = 0f;
            foreach (ElementNest nest in level.Nests)
            {
                Vector3 from = level.Grid.CellToWorld(nest.Cell);
                Vector3 to = OffRoadRoute.NearestRoadCellWorld(level, from);
                float length = Vector3.Distance(from, to);
                float highest = 0f;
                for (float walked = 0f; walked < length; walked += SampleStep)
                {
                    Vector3 at = Vector3.Lerp(from, to, walked / length);
                    highest = Mathf.Max(highest, field.PlateauAt(at.x, at.z) * TerrainMap.PlateauHeight);
                }

                wading += highest > RisingGround ? 1 : 0;
                deepest = Mathf.Max(deepest, highest);
            }

            return string.Format(CultureInfo.InvariantCulture, "nests whose walk to the road crosses raised ground or its slope: {0} of {1} (up to {2:0.00} deep)", wading, level.Nests.Length, deepest);
        }

        private static bool Near(Vector3 point, List<Vector3> others, float reach)
        {
            foreach (Vector3 other in others)
            {
                if ((other - point).sqrMagnitude <= reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        // Every point of the road a walker can stand on: each road cell's middle and as far to each side as walkers stray.
        private static List<Vector3> RoadTargets(LevelDefinition level)
        {
            float stray = EnemySpawner.StrayPastRowCells(level.RoadWidth) * level.CellSize;
            var targets = new List<Vector3>();
            foreach (Vector2Int cell in level.Road.Cells)
            {
                Vector3 middle = level.Grid.CellToWorld(cell);
                targets.Add(middle);
                targets.Add(middle + Vector3.right * stray);
                targets.Add(middle + Vector3.left * stray);
                targets.Add(middle + Vector3.forward * stray);
                targets.Add(middle + Vector3.back * stray);
            }

            return targets;
        }

        // Every tower the level sells that fires at the ground, at every upgrade level, with its lowest muzzle at rest.
        private static List<Gun> Guns(LevelDefinition level)
        {
            var guns = new List<Gun>();
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower == null || tower.IsSupport || (tower.Targets & TargetLayers.Ground) == 0)
                {
                    continue;
                }

                for (int i = 0; i <= tower.MaxLevelIndex; i++)
                {
                    guns.Add(new Gun($"{tower.name} L{i + 1}", tower.GetLevel(i).Range, MuzzleHeight(tower, i)));
                }
            }

            return guns;
        }

        /// <summary>The lowest shot origin of the tower's model at this level, at rest, over the tower's foot.</summary>
        public static float MuzzleHeight(TowerDefinition tower, int levelIndex)
        {
            ModelArtDefinition art = tower.Art != null ? tower.Art.ArtFor(levelIndex) : null;
            if (art == null || art.Model == null)
            {
                return PlainMuzzle;
            }

            float lowest = float.MaxValue;
            foreach (Transform node in art.Model.GetComponentsInChildren<Transform>(true))
            {
                if (node.name.StartsWith(tower.Art.MuzzlePrefix, System.StringComparison.Ordinal))
                {
                    lowest = Mathf.Min(lowest, node.position.y * art.Scale);
                }
            }

            return lowest < float.MaxValue ? lowest : PlainMuzzle;
        }

        private static IEnumerable<LevelDefinition> Levels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
