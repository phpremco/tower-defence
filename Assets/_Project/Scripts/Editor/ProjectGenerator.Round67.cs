using System.Text;
using Game.Towers;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Round 67 as targeted steps (the full generator is never run on Remco's project): the combat rules asset with every
    /// weapon wired to it, and the towers' levels as named one-thing steps with their roles. Each writes exactly what the
    /// generator's own steps ("Combat rules", "Towers", "Roster") write, and is safe to run again.
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>The rules asset from the generator's tables, and every listed weapon stamped with its damage type and the rules.</summary>
        public static string ApplyCombatRules()
        {
            CreateCombatRules();
            string report = ApplyDamageTypes();
            AssetDatabase.SaveAssets();
            return report;
        }

        /// <summary>
        /// The generator's "Towers" and "Roster" steps alone: every tower's levels (the named steps), roles and role line.
        /// Returns one line per tower with its price and damage per second per level, before and after.
        /// </summary>
        public static string ApplyTowerSteps()
        {
            var before = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { DataRoot + "/Towers" }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                before[tower.name] = Line(tower);
            }

            var context = new GeneratorContext();
            GenerateCoreTowers(context);
            CreateRoster(context.Pulse, context.Cannon, context.Rocket);
            var report = new StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { DataRoot + "/Towers" }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                EditorUtility.SetDirty(tower);
                string problem = tower.StepProblem();
                report.Append(tower.name).Append(": ").Append(before[tower.name]).Append("  ->  ").Append(Line(tower)).Append(problem != null ? "  PROBLEM " + problem : string.Empty).Append('\n');
            }

            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        /// <summary>
        /// Writes the blade's looks per damage tier into the hero's gear art (orange halfway, red at the last tier): the
        /// asset takes the class's stages the first time and keeps its own numbers after that, like its other tunables.
        /// </summary>
        public static string ApplyBladeStages()
        {
            var art = AssetDatabase.LoadAssetAtPath<Game.Hero.HeroGearArtDefinition>(HeroGearArtPath);
            if (art == null)
            {
                return "MISSING " + HeroGearArtPath + "\n";
            }

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            var text = new StringBuilder("Blade stages:");
            foreach (Game.Hero.BladeAuraStage stage in art.BladeStages)
            {
                text.Append(System.FormattableString.Invariant($" from tier {stage.FromTier} #{UnityEngine.ColorUtility.ToHtmlStringRGB(stage.BladeColor)} x{stage.Strength:0.##};"));
            }

            return text.Append('\n').ToString();
        }

        private static string Line(TowerDefinition tower)
        {
            var text = new StringBuilder();
            for (int i = 0; i <= tower.MaxLevelIndex; i++)
            {
                TowerLevel level = tower.GetLevel(i);
                text.Append(System.FormattableString.Invariant($"[{level.Cost}c {level.Damage:0.##}/{level.FireInterval:0.###}s r{level.Range:0.#} dps {(level.FireInterval > 0f ? level.Damage / level.FireInterval : 0f):0.##}{(string.IsNullOrEmpty(level.StepName) ? string.Empty : " '" + level.StepName + "'")}]"));
            }

            return text.ToString();
        }
    }
}
