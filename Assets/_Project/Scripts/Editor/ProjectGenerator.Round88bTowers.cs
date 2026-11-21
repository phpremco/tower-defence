using System.Text;
using Game.Towers;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Round 88b without the full generator: the Cannon's blast cap per level (two creatures as built, three and four with
    /// its stars; Remco's playtest of 2026-11-20). Writes only the caps, from the constants the full generator uses
    /// (ProjectGenerator.Towers.cs), so both write the same data. Every other splash gun keeps the one cap it has.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string CannonId = "Cannon";

        /// <summary>Safe to run again: sets the values, never adds to them. Returns what stood before and what stands now.</summary>
        public static string ApplyBlastCaps()
        {
            var report = new StringBuilder();
            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{DataRoot}/Towers/{CannonId}.asset");
            if (tower == null)
            {
                return $"MISSING {CannonId}\n";
            }

            string before = BlastLine(tower);
            var so = new SerializedObject(tower);
            so.FindProperty("_splashTargets").intValue = CannonSplashTargets[0];
            SerializedProperty levels = so.FindProperty("_levels");
            for (int i = 0; i < levels.arraySize; i++)
            {
                int cap = CannonSplashTargets[UnityEngine.Mathf.Min(i, CannonSplashTargets.Length - 1)];
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("SplashTargets").intValue = cap;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tower);
            AssetDatabase.SaveAssets();
            report.Append($"{CannonId}: {before}  ->  {BlastLine(tower)}");
            string problem = tower.SplashTargetsProblem() ?? tower.StepProblem();
            report.Append(problem != null ? "  PROBLEM " + problem : string.Empty).Append('\n');
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { DataRoot + "/Towers" }))
            {
                var other = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (other != tower && other.SplashRadius > 0f)
                {
                    report.Append($"{other.name} (unchanged): {BlastLine(other)}\n");
                }
            }

            return report.ToString();
        }

        private static string BlastLine(TowerDefinition tower)
        {
            var text = new StringBuilder();
            for (int i = 0; i <= tower.MaxLevelIndex; i++)
            {
                int cap = tower.SplashTargetsAt(i);
                text.Append(i > 0 ? " / " : string.Empty).Append(cap > 0 ? cap.ToString() : "all");
            }

            return text.ToString();
        }
    }
}
