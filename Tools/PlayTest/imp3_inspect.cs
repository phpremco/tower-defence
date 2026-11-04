// Import stage 3a: prints the node tree (renderers marked, bones with local position), and the clips of the FBX files
// named in Tools/PlayTest/imp3_inspect.txt (one asset path per line).
var sb = new System.Text.StringBuilder();
foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/imp3_inspect.txt"))
{
    string path = line.Trim();
    if (path.Length == 0) continue;
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    sb.Append("## ").Append(System.IO.Path.GetFileName(path)).Append(go == null ? " MISSING" : "").Append(" | ");
    if (go == null) continue;
    System.Action<UnityEngine.Transform, int> walk = null;
    walk = (t, depth) =>
    {
        var r = t.GetComponent<UnityEngine.Renderer>();
        sb.Append(t.name);
        if (r != null) { var smr = r as UnityEngine.SkinnedMeshRenderer; sb.Append(smr != null ? "[S " + smr.sharedMesh.triangles.Length / 3 + " b" + smr.bones.Length + "]" : "[M]"); }
        else sb.Append(t.localPosition.ToString("0.00"));
        if (t.childCount > 0) { sb.Append("{"); for (int i = 0; i < t.childCount; i++) { walk(t.GetChild(i), depth + 1); sb.Append(","); } sb.Append("}"); }
    };
    walk(go.transform, 0);
    sb.Append(" clips:");
    foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
        if (a is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview__")) sb.Append(c.name).Append("(").Append(c.length.ToString("0.00")).Append(c.isLooping ? "L" : "").Append(") ");
    sb.Append(" || ");
}
return sb.ToString();
