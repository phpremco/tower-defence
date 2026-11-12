// Round 49 import (set 3b): dry run of the cycle-aware bar height (Game.Editor.CreatureBarHeight) for every enemy art:
// stored height, the new measure, and the cycle's median and highest top. Writes Logs/r49s3_bar_heights.txt; returns the
// arts whose measure would change by more than 1 %.
var sb = new System.Text.StringBuilder();
var changed = new System.Text.StringBuilder();
var seen = new System.Collections.Generic.HashSet<string>();
sb.AppendLine("art | boss | stored | new | median | max");
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition"))
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (def == null || def.Art == null || def.Art.Model == null || !seen.Add(def.Art.name)) continue;
    var art = def.Art;
    var tops = Game.Editor.CreatureBarHeight.CycleTops(art.Model, art);
    float measure = Game.Editor.CreatureBarHeight.Measure(art.Model, art);
    tops.Sort();
    float median = tops.Count > 0 ? tops[tops.Count / 2] : float.NaN;
    float max = tops.Count > 0 ? tops[tops.Count - 1] : float.NaN;
    string row = $"{art.name} | {def.IsBoss} | {art.Height:0.000} | {measure:0.000} | {median:0.000} | {max:0.000}";
    sb.AppendLine(row);
    if (UnityEngine.Mathf.Abs(measure - art.Height) > art.Height * 0.01f) changed.Append(row).Append("; ");
}

System.IO.File.WriteAllText("Logs/r49s3_bar_heights.txt", sb.ToString());
return changed.Length == 0 ? "none changed" : changed.ToString();
