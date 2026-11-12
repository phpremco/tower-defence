// Round 53: re-measures the bar height of every catalog row that names crest bones (BlenderArtEntry.BarCrestBones) with
// the crest-aware measure, as the art builder now writes it, and writes the new _height into the row's art asset when
// Tools/PlayTest/r53_remeasure.txt says "write" ("dry" only reports). No model is rebuilt.
bool write = System.IO.File.Exists("Tools/PlayTest/r53_remeasure.txt") && System.IO.File.ReadAllText("Tools/PlayTest/r53_remeasure.txt").Trim() == "write";
var sb = new System.Text.StringBuilder(write ? "write: " : "dry: ");
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures)
{
    if (entry.BarCrestBones == null || entry.BarCrestBones.Length == 0) { continue; }
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + entry.Id + ".asset");
    if (art == null) { sb.Append(entry.Id).Append(" no art; "); continue; }
    float plain = Game.Editor.CreatureBarHeight.Measure(art.Model, art);
    float crest = Game.Editor.CreatureBarHeight.Measure(art.Model, art, entry.BarCrestBones);
    sb.Append(entry.Id).Append(" stored ").Append(art.Height.ToString("0.000")).Append(" plain ").Append(plain.ToString("0.000")).Append(" crest ").Append(crest.ToString("0.000"));
    if (write)
    {
        var so = new UnityEditor.SerializedObject(art);
        so.FindProperty("_height").floatValue = crest;
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.EditorUtility.SetDirty(art);
        sb.Append(" written");
    }
    sb.Append("; ");
}
if (write) { UnityEditor.AssetDatabase.SaveAssets(); }
return sb.ToString();
