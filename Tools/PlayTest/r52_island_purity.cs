// Round 52 (49G import): the parts that come off by bone scale (the Shardback's Shard_n clusters, the Shattered's Shell_n
// plates) must be pure islands in the imported meshes: every vertex wholly on one such bone or on none, and no triangle
// joining a part vertex to a body vertex (that triangle would stretch from the hidden part to the body and hang there).
// Checks every skinned mesh of both rigs of the rows below. One line per mesh: vertices on the parts, mixed vertices,
// bridging triangles, and the vertex count per part bone.
var rows = new[] { ("Shardback", "Shard_"), ("Shardback_Corrupted", "Shard_"), ("Shattered", "Shell_") };
var sb = new System.Text.StringBuilder();
foreach (var (id, prefix) in rows)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
    foreach (var model in new[] { art.Model, art.Lod1 })
    {
        if (model == null) continue;
        foreach (var renderer in model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
        {
            var mesh = renderer.sharedMesh;
            var bones = renderer.bones;
            var weights = mesh.boneWeights;
            var owner = new int[weights.Length];
            int onParts = 0, mixed = 0;
            var perBone = new System.Collections.Generic.SortedDictionary<string, int>();
            for (int v = 0; v < weights.Length; v++)
            {
                var w = weights[v];
                int[] idx = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                float[] wt = { w.weight0, w.weight1, w.weight2, w.weight3 };
                float part = 0f; int partBone = -1;
                for (int k = 0; k < 4; k++)
                {
                    if (wt[k] > 0f && bones[idx[k]] != null && bones[idx[k]].name.StartsWith(prefix)) { part += wt[k]; partBone = idx[k]; }
                }

                owner[v] = part > 0.999f ? partBone : -1;
                if (part > 0.999f) { onParts++; string n = bones[partBone].name; perBone[n] = perBone.TryGetValue(n, out int c) ? c + 1 : 1; }
                else if (part > 0.001f) { mixed++; owner[v] = -2; }
            }

            int bridges = 0;
            var tris = mesh.triangles;
            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = owner[tris[t]], b = owner[tris[t + 1]], c = owner[tris[t + 2]];
                if (a != b || b != c) bridges++;
            }

            sb.Append(id).Append('/').Append(renderer.name).Append(": parts ").Append(onParts).Append(" mixed ").Append(mixed).Append(" bridges ").Append(bridges).Append(" [");
            foreach (var pair in perBone) sb.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
            sb.Append("] || ");
        }
    }
}

return sb.ToString();
