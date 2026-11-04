// Import stage 4a: the node transforms inside a few environment FBX files (is the axis change on the node or in the mesh?).
var sb = new System.Text.StringBuilder();
string[] paths = { "Env/RoadEdge_Straight/RoadEdge_Straight", "Env/Cliff_OuterCorner/Cliff_OuterCorner", "Env/Flora_Frond/Flora_Frond", "Crystal_A/Crystal_A", "Burrow_A/Burrow_A", "Env/Decor_Spire/Decor_Spire" };
foreach (string p in paths)
{
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Models/Blender/" + p + ".fbx");
    if (model == null) { sb.Append(p).Append(" missing\n"); continue; }
    sb.Append(p).Append(" root r").Append(model.transform.localEulerAngles.ToString("0")).Append(" s").Append(model.transform.localScale.ToString("0.00"));
    foreach (var f in model.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        var t = f.transform;
        var m = model.transform.worldToLocalMatrix * t.localToWorldMatrix;
        sb.Append(" | ").Append(t.name).Append(" p").Append(t.localPosition.ToString("0.00")).Append(" r").Append(t.localEulerAngles.ToString("0")).Append(" s").Append(t.localScale.ToString("0.00"));
        var b = f.sharedMesh.bounds;
        var c = m.MultiplyPoint3x4(b.center);
        sb.Append(" worldCentre ").Append(c.ToString("0.00"));
    }
    foreach (var s in model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) sb.Append(" skinned ").Append(s.name);
    sb.Append('\n');
}
return sb.ToString();
