// Round 53: which bones carry the Shattered's crest. Samples Walk at mid-cycle on a copy of the model and, for the vertices
// in the top band (above the threshold in Tools/PlayTest/r53_crest_band.txt, model units), lists per dominant bone the
// count, the highest y and the largest front/back (z) and side (x) offset from the model's axis.
float band = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r53_crest_band.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Shattered.asset");
var walk = art.Clips[art.IndexOf(Game.Core.ModelClipRole.Move)].Clip;
var probe = UnityEngine.Object.Instantiate(art.Model);
walk.SampleAnimation(probe, walk.length * 0.5f);
var stats = new System.Collections.Generic.Dictionary<string, UnityEngine.Vector4>();
var baked = new UnityEngine.Mesh();
float tan = UnityEngine.Mathf.Tan(44f * UnityEngine.Mathf.Deg2Rad);
float projected = float.MinValue; string projectedBone = "-";
foreach (var r in probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
{
    r.BakeMesh(baked, true);
    var m = r.transform.localToWorldMatrix;
    var v = baked.vertices; var w = r.sharedMesh.boneWeights;
    for (int i = 0; i < v.Length; i++)
    {
        var p = m.MultiplyPoint3x4(v[i]);
        string bone = r.bones[w[i].boneIndex0].name;
        float proj = p.y + UnityEngine.Mathf.Abs(p.z) * tan;
        if (proj > projected) { projected = proj; projectedBone = bone; }
        if (p.y < band) { continue; }
        stats.TryGetValue(bone, out var s);
        stats[bone] = new UnityEngine.Vector4(s.x + 1, UnityEngine.Mathf.Max(s.y, p.y), UnityEngine.Mathf.Max(s.z, UnityEngine.Mathf.Abs(p.z)), UnityEngine.Mathf.Max(s.w, UnityEngine.Mathf.Abs(p.x)));
    }
}
UnityEngine.Object.DestroyImmediate(baked);
UnityEngine.Object.DestroyImmediate(probe);
var sb = new System.Text.StringBuilder("max y+|z|tan44 " + projected.ToString("0.00") + " (" + projectedBone + ") | ");
foreach (var kv in stats) { sb.Append(kv.Key).Append(" n").Append(kv.Value.x).Append(" top ").Append(kv.Value.y.ToString("0.00")).Append(" |z| ").Append(kv.Value.z.ToString("0.00")).Append(" |x| ").Append(kv.Value.w.ToString("0.00")).Append("; "); }
return sb.ToString();
