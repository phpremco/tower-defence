// Round 50: lists the bones of the ray and bat art (near model and far model) with their rest positions in model space,
// to pick the socket a beam leaves from.
var rows = new[] { "RayBoss", "Skyray", "BatBoss" };
var sb = new System.Text.StringBuilder();
foreach (var id in rows)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
    foreach (var model in new[] { art.Model, art.Lod1 })
    {
        if (model == null) continue;
        sb.Append(id + "/" + model.name + ": ");
        foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            if (t.GetComponent<UnityEngine.Renderer>() != null || t == model.transform) continue;
            var p = model.transform.InverseTransformPoint(t.position);
            sb.Append(t.name + "(" + p.x.ToString("0.00") + "," + p.y.ToString("0.00") + "," + p.z.ToString("0.00") + ") ");
        }
        sb.Append(" || ");
    }
}

return sb.ToString();
