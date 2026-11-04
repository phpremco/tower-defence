// Import stage 4a: sets tunables of the Crystal Plains environment set from Tools/PlayTest/imp4_tune.txt, one
// "field value" pair per line (float fields only, e.g. "_paintGlow 0.55"), and saves the asset. Play Mode stopped.
var set = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>("Assets/_Project/Data/Art/Environment/CrystalPlains.asset");
if (set == null) return "no set";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var so = new UnityEditor.SerializedObject(set);
var report = new System.Text.StringBuilder();
foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/imp4_tune.txt"))
{
    string[] parts = line.Trim().Split(' ');
    if (parts.Length < 2) continue;
    var property = so.FindProperty(parts[0]);
    if (property == null) { report.Append("no ").Append(parts[0]).Append("; "); continue; }
    if (property.propertyType == UnityEditor.SerializedPropertyType.Vector2) property.vector2Value = new UnityEngine.Vector2(float.Parse(parts[1], inv), float.Parse(parts[2], inv));
    else if (property.propertyType == UnityEditor.SerializedPropertyType.Integer) property.intValue = int.Parse(parts[1], inv);
    else property.floatValue = float.Parse(parts[1], inv);
    report.Append(parts[0]).Append('=').Append(string.Join(",", parts, 1, parts.Length - 1)).Append("; ");
}
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(set);
UnityEditor.AssetDatabase.SaveAssets();
return report.ToString();
