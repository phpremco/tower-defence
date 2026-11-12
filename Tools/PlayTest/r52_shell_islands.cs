// Round 52 (49G import): which Shell_n bone each of the Shattered's shell plates rides in each rig. Splits the vertices on
// Shell bones into connected islands (shared triangles, welded by position), and lists per island its bone, vertex count,
// centre (bind pose, model space) and the nearest Shell bone to that centre.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Shattered.asset");
var sb = new System.Text.StringBuilder();
foreach (var model in new[] { art.Model, art.Lod1 })
{
    var renderer = System.Array.Find(model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true), r => r.name == "Shattered" || r.name == "Shattered_Far");
    var mesh = renderer.sharedMesh; var bones = renderer.bones; var weights = mesh.boneWeights; var verts = mesh.vertices;
    int n = verts.Length;
    var parent = new int[n]; for (int i = 0; i < n; i++) parent[i] = i;
    System.Func<int, int> find = null; find = x => parent[x] == x ? x : (parent[x] = find(parent[x]));
    var tris = mesh.triangles;
    for (int t = 0; t < tris.Length; t += 3) { int a = find(tris[t]), b = find(tris[t + 1]), c = find(tris[t + 2]); parent[b] = a; parent[find(c)] = a; }
    var byPos = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
    for (int i = 0; i < n; i++) { var key = UnityEngine.Vector3Int.RoundToInt(verts[i] * 10000f); if (byPos.TryGetValue(key, out int o)) parent[find(i)] = find(o); else byPos[key] = i; }
    var groups = new System.Collections.Generic.Dictionary<int, (int count, UnityEngine.Vector3 sum, string bone)>();
    for (int i = 0; i < n; i++)
    {
        var w = weights[i];
        if (w.weight0 < 0.999f || bones[w.boneIndex0] == null || !bones[w.boneIndex0].name.StartsWith("Shell_")) continue;
        int root = find(i);
        groups.TryGetValue(root, out var g);
        groups[root] = (g.count + 1, g.sum + verts[i], bones[w.boneIndex0].name);
    }

    var bindPoses = mesh.bindposes;
    sb.Append(model.name).Append(": ");
    foreach (var g in groups.Values)
    {
        var centre = g.sum / g.count;
        string nearest = ""; float best = float.MaxValue;
        for (int b = 0; b < bones.Length; b++)
        {
            if (!bones[b].name.StartsWith("Shell_")) continue;
            var at = bindPoses[b].inverse.MultiplyPoint3x4(UnityEngine.Vector3.zero);
            float d = (at - centre).magnitude; if (d < best) { best = d; nearest = bones[b].name; }
        }

        sb.Append(g.bone).Append(' ').Append(g.count).Append("v c(").Append(centre.x.ToString("0.00")).Append(',').Append(centre.y.ToString("0.00")).Append(',').Append(centre.z.ToString("0.00")).Append(") near ").Append(nearest).Append(' ').Append(best.ToString("0.00")).Append("; ");
    }

    sb.Append(" || ");
}

return sb.ToString();
