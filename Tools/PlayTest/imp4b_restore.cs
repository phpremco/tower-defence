// Import stage 4b: in edit mode, puts every campaign theme's in-memory environment back to Crystal Plains (after an
// interrupted imp4b_env.sh run) without saving; reports what each theme wore.
var report = new System.Text.StringBuilder();
var field = typeof(Game.Levels.VisualTheme).GetField("_environment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var plains = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>("Assets/_Project/Data/Art/Environment/CrystalPlains.asset");
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:VisualTheme"))
{
    var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.VisualTheme>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    var set = (Game.Levels.EnvironmentSet)field.GetValue(theme);
    if (set != null && set != plains) { report.Append(theme.name).Append(" wore ").Append(set.name).Append("; "); field.SetValue(theme, plains); }
}

return report.Length == 0 ? "all themes clean" : report.ToString();
