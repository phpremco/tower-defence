// Runs only the generator's difficulty step (BalanceSheet.Difficulties -> Data/Difficulty/*.asset) and saves, for a
// balance change that touches nothing but the profiles while the full generator must not run (it syncs the Blender
// exports from ArtSource). Same code as the "Difficulties" step of Tools/Generate/Scenes and Starter Data.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var generator = typeof(Game.Editor.ProjectGenerator);
var contextType = generator.GetNestedType("GeneratorContext", System.Reflection.BindingFlags.NonPublic);
var context = System.Activator.CreateInstance(contextType, true);
generator.GetMethod("GenerateDifficulties", flags).Invoke(null, new[] { context });
UnityEditor.AssetDatabase.SaveAssets();
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:DifficultyProfile", new[] { "Assets/_Project/Data/Difficulty" }))
{
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.DifficultyProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    sb.Append(profile.name).Append(": income ").Append(profile.IncomeMultiplier).Append(", start money ").Append(profile.StartingMoneyMultiplier).Append(", boss health ").Append(profile.BossHealthShare).Append('\n');
}

return sb.ToString();
