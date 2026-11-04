// Night 2: sets serialized fields on a data asset from Tools/PlayTest/n2_set.txt (first line the asset path, then
// "field value" lines, floats only) and saves it.
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/n2_set.txt");
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lines[0].Trim());
var so = new UnityEditor.SerializedObject(asset);
var sb = new System.Text.StringBuilder();
for (int i = 1; i < lines.Length; i++) { var p = lines[i].Trim().Split(' '); if (p.Length < 2) continue; var prop = so.FindProperty(p[0]); if (prop == null) { sb.Append("no " + p[0] + "; "); continue; } prop.floatValue = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture); sb.Append(p[0] + "=" + p[1] + "; "); }
so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(asset); UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
