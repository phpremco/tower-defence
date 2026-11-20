using System.Text;
using Game.Towers;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Round 87 as a targeted step (the full generator is never run on Remco's project): the beam's role. It writes what
    /// the generator's "Combat rules", "Bestiary" and "Roster" steps now write for it and nothing else: the rules asset
    /// with its family table, every creature's family, the Laser's levels and sweep time, and the retarget time of the
    /// turrets (the Gun Turret and its hybrid, the Storm Rifle). Safe to run again.
    /// </summary>
    public static partial class ProjectGenerator
    {
        public static string ApplyBeamRole()
        {
            var report = new StringBuilder();
            CombatRulesAsset(report);
            report.Append(ApplyCreatureFamilies());
            report.Append(ApplyLevels("Laser", LaserLevels()));
            SetBeam(AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{DataRoot}/Towers/Laser.asset"), LaserSweepSeconds);
            foreach (string turret in new[] { "PulseRifle", "StormRifle" })
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{DataRoot}/Towers/{turret}.asset");
                SetRetarget(tower, TurretRetargetSeconds);
                EditorUtility.SetDirty(tower);
                report.Append(System.FormattableString.Invariant($"{turret}: retarget {tower.RetargetSeconds:0.##} s\n"));
            }

            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static void CombatRulesAsset(StringBuilder report)
        {
            Core.CombatRules rules = CreateCombatRules();
            report.Append("Family table:");
            foreach (Core.FamilyRule rule in rules.Families)
            {
                report.Append(System.FormattableString.Invariant($" {rule.Type} on {rule.Family} x{rule.Factor:0.##};"));
            }

            report.Append('\n');
        }

        // The numbers and the improved stat of every level from the generator's spec; the step names stay the keys the
        // asset holds (their English text is the localisation table's).
        private static string ApplyLevels(string id, TowerLevel[] levels)
        {
            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            if (tower == null)
            {
                return $"MISSING {id}\n";
            }

            string before = Line(tower);
            var so = new SerializedObject(tower);
            WriteLevels(so, id, levels, false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tower);
            string problem = tower.StepProblem();
            return $"{id}: {before}  ->  {Line(tower)}{(problem != null ? "  PROBLEM " + problem : string.Empty)}\n";
        }
    }
}
