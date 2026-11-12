// Round 52 (49G import): what is left of each of the first living Shardback's Shard_n clusters on screen. Bakes every
// skinned mesh of its model as drawn now and reports per cluster bone its scale and the extent of the baked vertices that
// ride it: a shed cluster collapses to a point (extent 0), with nothing stretched back to the body.
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shardback") continue;
    var sb = new System.Text.StringBuilder("armour " + enemy.Health.ArmorFraction.ToString("0.000") + ": ");
    foreach (var renderer in enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
    {
        if (!renderer.enabled) continue;
        var baked = new UnityEngine.Mesh();
        renderer.BakeMesh(baked, true);
        var verts = baked.vertices;
        var weights = renderer.sharedMesh.boneWeights;
        var bones = renderer.bones;
        var lo = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
        var hi = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
        for (int v = 0; v < verts.Length; v++)
        {
            int b = weights[v].boneIndex0;
            if (weights[v].weight0 < 0.999f || !bones[b].name.StartsWith("Shard_")) continue;
            lo[b] = lo.TryGetValue(b, out var l) ? UnityEngine.Vector3.Min(l, verts[v]) : verts[v];
            hi[b] = hi.TryGetValue(b, out var h) ? UnityEngine.Vector3.Max(h, verts[v]) : verts[v];
        }

        sb.Append(renderer.name).Append(" [");
        foreach (var b in lo.Keys)
        {
            sb.Append(bones[b].name).Append(" scale ").Append(bones[b].localScale.x.ToString("0.00")).Append(" extent ").Append((hi[b] - lo[b]).magnitude.ToString("0.000")).Append("; ");
        }

        sb.Append("] ");
        UnityEngine.Object.Destroy(baked);
    }

    return sb.ToString();
}

return "no living Shardback";
