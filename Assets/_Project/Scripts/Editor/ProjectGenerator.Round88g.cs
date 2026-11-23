using System.Text;
using Game.Levels;
using Game.Towers;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Round 88g in data, for a project the full generator is never run on: the share of a blow a gun takes per difficulty
    /// (Psycho 0.8, <see cref="BalanceSheet.Difficulties"/>) and the health every star adds to a gun
    /// (<see cref="BalanceSheet.GunHealthPerStar"/>, written into TowerLevel.HealthBonus of every gun asset). Safe to run
    /// again; the generator's own steps write the same. The tower levels are in the calibration's input hash: re-stamp
    /// after it.
    /// </summary>
    public static partial class ProjectGenerator
    {
        public static string ApplyRound88g()
        {
            var report = new StringBuilder();
            foreach (BalanceSheet.Difficulty sheet in BalanceSheet.Difficulties)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DataRoot}/Difficulty/{sheet.Id}.asset");
                if (profile == null)
                {
                    continue;
                }

                var so = new SerializedObject(profile);
                so.FindProperty("_towerDamageFactor").floatValue = sheet.TowerFactor;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                report.Append(System.FormattableString.Invariant($"{sheet.Id}: a gun takes x{sheet.TowerFactor:0.##} of a blow\n"));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { $"{DataRoot}/Towers" }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(tower);
                float starHealth = GunStarHealth(so);
                if (starHealth <= 0f)
                {
                    continue;
                }

                SerializedProperty levels = so.FindProperty("_levels");
                for (int i = 0; i < levels.arraySize; i++)
                {
                    levels.GetArrayElementAtIndex(i).FindPropertyRelative("HealthBonus").floatValue = i > 0 ? starHealth : 0f;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tower);
                float health = so.FindProperty("_maxHealth").floatValue;
                report.Append(System.FormattableString.Invariant($"{tower.name}: {health:0} + {starHealth:0} a star = {health + starHealth * (levels.arraySize - 1):0} at the top\n"));
            }

            AssetDatabase.SaveAssets();
            return report.ToString();
        }
    }
}
