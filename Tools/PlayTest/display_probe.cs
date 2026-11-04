// Round menu 2026-11-04: the display stage meshes in the generated art: node rotation and the y range after the node.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.UI.DisplayStageArt>("Assets/_Project/Data/Art/UI/DisplayStageArt.asset");
var sb = new System.Text.StringBuilder();
System.Action<string, UnityEngine.Mesh, UnityEngine.Matrix4x4> one = (n, m, node) =>
{
    if (m == null) { sb.Append(n).Append(" none; "); return; }
    float y0 = 99, y1 = -99, r = 0; var v = m.vertices;
    foreach (var p in v) { var w = node.MultiplyPoint3x4(p); y0 = UnityEngine.Mathf.Min(y0, w.y); y1 = UnityEngine.Mathf.Max(y1, w.y); r = UnityEngine.Mathf.Max(r, new UnityEngine.Vector2(w.x, w.z).magnitude); }
    var up = 0f; var nn = m.normals; foreach (var q in nn) up += node.MultiplyVector(q).y;
    sb.Append(n).Append(" rot=").Append(node.rotation.eulerAngles).Append(" y ").Append(y0.ToString("0.000")).Append("..").Append(y1.ToString("0.000")).Append(" r=").Append(r.ToString("0.00")).Append(" meanNy=").Append((up / nn.Length).ToString("0.00")).Append(" sub=").Append(m.subMeshCount).Append("; ");
};
one("pedestal", art.Pedestal, art.PedestalNode); one("ring", art.PedestalRing, art.PedestalRingNode); one("halo", art.Halo, art.HaloNode);
return sb.ToString();
