// Round 53: is the baked-top measure scale-true? Instantiates the Shattered model at root scale 1 and 0.8, samples Walk at
// mid-cycle and reports the baked top (BakeMesh useScale true and false) and the renderer bounds top for each.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Shattered.asset");
var walk = art.Clips[art.IndexOf(Game.Core.ModelClipRole.Move)].Clip;
var sb = new System.Text.StringBuilder();
foreach (float s in new[] { 1f, 0.8f })
{
    var holder = new UnityEngine.GameObject("holder");
    holder.transform.localScale = UnityEngine.Vector3.one * s;
    var probe = UnityEngine.Object.Instantiate(art.Model, holder.transform, false);
    walk.SampleAnimation(probe, walk.length * 0.5f);
    var baked = new UnityEngine.Mesh();
    float topTrue = float.MinValue, topFalse = float.MinValue, bounds = float.MinValue;
    foreach (var r in probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        r.BakeMesh(baked, true);
        foreach (var v in baked.vertices) { topTrue = UnityEngine.Mathf.Max(topTrue, (r.transform.position + r.transform.rotation * v).y); }
        r.BakeMesh(baked, false);
        foreach (var v in baked.vertices) { topFalse = UnityEngine.Mathf.Max(topFalse, r.transform.TransformPoint(v).y); }
        bounds = UnityEngine.Mathf.Max(bounds, r.bounds.max.y);
        sb.Append(r.name).Append(" lossy ").Append(r.transform.lossyScale.y.ToString("0.000")).Append(' ');
    }
    sb.Append("| root scale ").Append(s).Append(": bake(true) top ").Append(topTrue.ToString("0.000")).Append(" bake(false)+TransformPoint ").Append(topFalse.ToString("0.000")).Append(" bounds ").Append(bounds.ToString("0.000")).Append(" || ");
    UnityEngine.Object.DestroyImmediate(baked);
    UnityEngine.Object.DestroyImmediate(holder);
}
return sb.ToString();
