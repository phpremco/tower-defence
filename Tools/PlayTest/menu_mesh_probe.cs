// Round menu 2026-11-04: node matrices, world bounds, triangle counts per sub-mesh and UV ranges of the menu FBX files.
var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "Assets/_Project/Art/Models/Blender/Menu_Island/Menu_Island.fbx", "Assets/_Project/Art/Models/Blender/Menu_NestMouth/Menu_NestMouth.fbx" })
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    foreach (var f in go.GetComponentsInChildren<UnityEngine.MeshFilter>())
    {
        var m = f.sharedMesh; var mat = f.transform.localToWorldMatrix;
        var b = m.bounds; var min = mat.MultiplyPoint3x4(b.min); var max = mat.MultiplyPoint3x4(b.max);
        sb.Append(f.name).Append(" rot=").Append(f.transform.localRotation.eulerAngles).Append(" scale=").Append(f.transform.lossyScale).Append(" world ").Append(min).Append("..").Append(max);
        for (int s = 0; s < m.subMeshCount; s++) sb.Append(" sub").Append(s).Append(" tris=").Append(m.GetSubMesh(s).indexCount / 3);
        var uv = new System.Collections.Generic.List<UnityEngine.Vector2>(); m.GetUVs(0, uv);
        float u0 = 9, u1 = -9, v0 = 9, v1 = -9; foreach (var p in uv) { u0 = UnityEngine.Mathf.Min(u0, p.x); u1 = UnityEngine.Mathf.Max(u1, p.x); v0 = UnityEngine.Mathf.Min(v0, p.y); v1 = UnityEngine.Mathf.Max(v1, p.y); }
        sb.Append(" uv ").Append(u0).Append("..").Append(u1).Append(" x ").Append(v0).Append("..").Append(v1).Append(" readable=").Append(m.isReadable).Append(" | ");
    }
}
return sb.ToString();
