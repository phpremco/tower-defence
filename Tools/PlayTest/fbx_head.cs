// Rest-pose z of the Head bone (and of the model's bounds) in every creature FBX: positive = head towards +Z.
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Models/Blender" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (t.name == "Head") { sb.Append(System.IO.Path.GetFileNameWithoutExtension(path) + " z=" + t.position.z.ToString("F2") + " " + System.IO.File.GetLastWriteTime(path).ToString("MM-dd HH:mm") + "; "); break; }
    }
}
return sb.ToString();
