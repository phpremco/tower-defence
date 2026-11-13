// Round 58: (1) the Shattered's own Easy health factor on its boss data (the value ProjectGenerator.Bosses writes,
// ShatteredEasyHealth), read from Tools/PlayTest/r58_easy_factor.txt; (2) the bar heights of the crest rows measured again
// (BlenderArtBuilder.RemeasureBars: the cycles' bar and each in-place fight clip's lift), before and after.
string path = "Assets/_Project/Data/Enemies/Bosses/ShatteredBoss.asset";
var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.BossDefinition>(path);
float factor = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r58_easy_factor.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var so = new UnityEditor.SerializedObject(boss);
var list = so.FindProperty("_difficultyHealth");
list.arraySize = 1;
list.GetArrayElementAtIndex(0).FindPropertyRelative("Difficulty").stringValue = "Easy";
list.GetArrayElementAtIndex(0).FindPropertyRelative("Factor").floatValue = factor;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(boss);
var sb = new System.Text.StringBuilder();
sb.Append("Shattered Easy x").Append(boss.HealthOn("Easy")).Append(" Medium x").Append(boss.HealthOn("Medium")).Append(" | ");
string[] rows = { "SealedOne", "Shattered" };
foreach (string row in rows)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + row + ".asset");
    sb.Append(row).Append(" bar before ").Append(art.Height.ToString("0.000")).Append("; ");
}
sb.Append("remeasured ").Append(Game.Editor.BlenderArtBuilder.RemeasureBars(rows)).Append(" | ");
foreach (string row in rows)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + row + ".asset");
    sb.Append(row).Append(" bar after ").Append(art.Height.ToString("0.000")).Append(": ");
    foreach (var clip in art.Clips)
    {
        if (clip.LiftsBar) { sb.Append(clip.Clip.name).Append(" [").Append(clip.BarTopAt(0f).ToString("0.00")).Append(" ").Append(clip.BarTopAt(90f).ToString("0.00")).Append(" ").Append(clip.BarTopAt(180f).ToString("0.00")).Append(" ").Append(clip.BarTopAt(270f).ToString("0.00")).Append("] "); }
    }
    sb.Append("; ");
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
