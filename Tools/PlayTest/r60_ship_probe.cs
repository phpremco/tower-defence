// Round 60: lists the mothership FBX hierarchy and the mesh islands lying where the round-19 clamshell bay doors are
// (edit mode, no scene needed).
var sb = new System.Text.StringBuilder();
var inv = System.Globalization.CultureInfo.InvariantCulture;
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership.fbx");
foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    if (t.name.StartsWith("Light_")) continue;
    sb.Append(t.name).Append(" p=").Append(t.localPosition.ToString("F3")).Append(" r=").Append(t.localEulerAngles.ToString("F1")).Append(" s=").Append(t.localScale.ToString("F3")).Append(" | ");
}
foreach (var smr in go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
{
    var m = smr.sharedMesh;
    sb.Append("\nSMR ").Append(smr.name).Append(" verts=").Append(m.vertexCount).Append(" sub=").Append(m.subMeshCount).Append(" bones=").Append(smr.bones.Length).Append(" bounds=").Append(m.bounds.ToString("F3"));
    for (int b = 0; b < smr.bones.Length; b++) sb.Append(" bone").Append(b).Append("=").Append(smr.bones[b].name);
    var bp = m.bindposes; for (int b = 0; b < bp.Length; b++) sb.Append("\n bind").Append(b).Append(" ").Append(bp[b].ToString("F3").Replace("\n", " "));
}
var bay = go.transform.Find("Bay") ?? System.Array.Find(go.GetComponentsInChildren<UnityEngine.Transform>(true), x => x.name == "Bay");
if (bay != null) sb.Append("\nBay world ").Append(bay.position.ToString("F3")).Append(" fwd ").Append(bay.forward.ToString("F3"));
return sb.ToString();
