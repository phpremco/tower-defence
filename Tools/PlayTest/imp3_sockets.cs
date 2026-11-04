// Import stage 3a: world positions (model at the origin, unrotated) and up/forward axes of chosen nodes in the FBX files
// listed in Tools/PlayTest/imp3_inspect.txt; the second line lists the node names.
string[] lines = System.IO.File.ReadAllLines("Tools/PlayTest/imp3_inspect.txt");
string[] names = lines[lines.Length - 1].Split(',');
var sb = new System.Text.StringBuilder();
for (int l = 0; l < lines.Length - 1; l++)
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(lines[l].Trim());
    if (go == null) { sb.Append("missing ").Append(lines[l]); continue; }
    var inst = UnityEngine.Object.Instantiate(go);
    sb.Append("## ").Append(go.name).Append(": ");
    foreach (var t in inst.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        foreach (string n in names)
            if (t.name == n.Trim()) sb.Append(t.name).Append(" p").Append(t.position.ToString("0.00")).Append(" up").Append(t.up.ToString("0.0")).Append(" fw").Append(t.forward.ToString("0.0")).Append("; ");
    }
    var b = new UnityEngine.Bounds(); bool first = true;
    foreach (var r in inst.GetComponentsInChildren<UnityEngine.Renderer>(true)) { sb.Append(r.name).Append(" b").Append(r.bounds.center.ToString("0.00")).Append(r.bounds.size.ToString("0.00")).Append("; "); }
    UnityEngine.Object.DestroyImmediate(inst);
    sb.Append(" || ");
}
return sb.ToString();
