using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for raised ground (round 65, Remco: "a tower on the edge of a plateau shoots through the ground"):
    /// the terrain rules asset every level points at (the reach of high ground, the sight rule), and the step that shapes
    /// a level's plateaus by <see cref="PlateauSkirt"/>: raised ground keeps one cell clear of everything that must stay
    /// level, the ring round it is written as the level's slope cells (no tower stands there), and the raised cells
    /// from which a gun would fire at the road through the plateau's own lip become blocked cells. Runs on a level as it
    /// is, so the map step of the generator and the round's apply script do exactly the same thing; safe to run again.
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>Reach of a tower on raised ground (Remco, 2026-11-15: ten percent).</summary>
        public const float HighGroundRange = 1.1f;

        /// <summary>
        /// The share of the way to a road cell within which a gun's line must leave the plateau. The lowest muzzle of the
        /// roster (the Cannon's, 0.34 over its pad) clears the rounded lip of a plateau of
        /// <see cref="TerrainMap.PlateauHeight"/> a little past half-way: a cell with one more raised cell between it
        /// and the ring still sees the road beyond (1.5 of 2.8 cells, the nearest a walker comes), a cell with two does
        /// not (2.5 of 3.8). LineOfFireTests measures the real muzzles against the drawn landscape.
        /// </summary>
        public const float PlateauSightRatio = 0.54f;

        /// <summary>The one terrain rules asset, created or brought up to the generator's numbers.</summary>
        internal static TerrainRules EnsureTerrainRules()
        {
            EnsureFolder($"{DataRoot}/Balance");
            var rules = AssetDatabase.LoadAssetAtPath<TerrainRules>(TerrainRules.AssetPath);
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<TerrainRules>();
                AssetDatabase.CreateAsset(rules, TerrainRules.AssetPath);
            }

            var so = new SerializedObject(rules);
            so.FindProperty("_highGroundRange").floatValue = HighGroundRange;
            so.FindProperty("_sightRatio").floatValue = PlateauSightRatio;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rules);
            return rules;
        }

        /// <summary>
        /// Shapes the level's raised ground by the plateau rules and points it at the terrain rules. Returns what changed
        /// in a line: raised cells taken down, slope cells, raised cells closed for towers.
        /// </summary>
        internal static string ApplyPlateauSkirt(LevelDefinition level)
        {
            TerrainRules rules = EnsureTerrainRules();
            var so = new SerializedObject(level);
            so.FindProperty("_terrainRules").objectReferenceValue = rules;
            so.ApplyModifiedPropertiesWithoutUndo();

            // A fresh copy to read from: the asset caches its road and terrain, which an earlier write may have changed.
            LevelDefinition copy = Object.Instantiate(level);
            try
            {
                var level0 = new HashSet<Vector2Int>(copy.Road.Cells);
                level0.UnionWith(copy.MineCells);
                level0.UnionWith(copy.Nests.Select(nest => nest.Cell));
                if (copy.BossEntrance.Footprint != null)
                {
                    level0.UnionWith(copy.BossEntrance.Footprint);
                }

                TerrainPatch[] before = copy.TerrainPatches;
                TerrainPatch[] kept = PlateauSkirt.KeepClear(before, copy.GridSize, level0.Contains);
                var terrain = new TerrainMap(kept);
                Vector2Int[] ring = PlateauSkirt.Ring(terrain, copy.GridSize);
                float reach = LevelValidator.GroundReach(copy) * rules.HighGroundRange;
                List<Vector2Int> shadowed = PlateauSkirt.Shadowed(terrain, copy.Grid, copy.Road.Cells, EnemySpawner.StrayPastRowCells(copy.RoadWidth), reach, rules.SightRatio);
                // The scatter never blocks a terrain cell, so a blocked cell on raised ground is always one of these: an
                // earlier run's are dropped first and the step gives the same level however often it runs.
                Vector2Int[] scattered = copy.BlockedCells.Where(cell => copy.Terrain.KindAt(cell) != TerrainKind.Raised).ToArray();
                Vector2Int[] blocked = scattered.Union(shadowed).ToArray();

                so.Update();
                SerializedProperty patches = so.FindProperty("_terrain");
                patches.arraySize = kept.Length;
                for (int i = 0; i < kept.Length; i++)
                {
                    SerializedProperty element = patches.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Cell").vector2IntValue = kept[i].Cell;
                    element.FindPropertyRelative("Kind").enumValueIndex = (int)kept[i].Kind;
                }

                WriteCells(so.FindProperty("_slopeCells"), ring);
                WriteCells(so.FindProperty("_blockedCells"), blocked);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(level);
                int raised = kept.Count(patch => patch.Kind == TerrainKind.Raised);
                return $"{level.name}: raised {before.Count(patch => patch.Kind == TerrainKind.Raised)} -> {raised} ({before.Length - kept.Length} taken down beside the road, a liquid, a mine or a nest), slope cells {ring.Length}, raised cells closed by the sight rule {shadowed.Count}, raised build cells {raised - shadowed.Count}";
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        /// <summary>
        /// Round 65 on the levels as they are, without the full generator: every campaign level gets its plateaus shaped
        /// and its ground painted again (the painter draws the new slope), and the arena copies that borrow its map are
        /// rebuilt from it, as after a relayout (they share its painted ground and shape their own plateaus).
        /// </summary>
        /// <param name="onlyKey">One level key (C1_01) to do alone, or null for every level.</param>
        public static string ApplyPlateausToEveryLevel(bool bake, string onlyKey = null)
        {
            var report = new System.Text.StringBuilder();
            foreach (CampaignLevelSpec spec in LevelSpecs.All)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{DataRoot}/Levels/Level_{spec.Key}.asset");
                if (level == null || (onlyKey != null && spec.Key != onlyKey))
                {
                    continue;
                }

                report.AppendLine(ApplyPlateauSkirt(level));
                if (bake && level.Theme != null)
                {
                    BakeTerrain(level);
                    RememberBake(level);
                }

                report.AppendLine($"  arena copies rebuilt: {RebuildArenaCopiesOf(level.name)}");
            }

            AssetDatabase.SaveAssets();
            return report.ToString();
        }
    }
}
