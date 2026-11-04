// Writes "field value" lines from Tools/PlayTest/fb2_set_tune.txt into the S-69-CRYS PlanetTextureSet asset (float fields) and saves it.
var set = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.PlanetTextureSet>("Assets/_Project/Data/Art/Planet/Planet_S69.asset");
if (set == null) return "no set";
var so = new UnityEditor.SerializedObject(set);
var sb = new System.Text.StringBuilder();
foreach (var raw in System.IO.File.ReadAllLines("Tools/PlayTest/fb2_set_tune.txt"))
{
    var p = raw.Trim().Split(' ');
    if (p.Length < 2 || p[0].StartsWith("#")) continue;
    var prop = so.FindProperty(p[0]);
    if (prop == null) { sb.Append("no " + p[0] + "; "); continue; }
    prop.floatValue = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
    sb.Append(p[0] + "=" + p[1] + "; ");
}
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(set);
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
