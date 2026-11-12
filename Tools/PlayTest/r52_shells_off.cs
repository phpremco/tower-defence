// Round 52 (49G import): scales the first living Shattered's Shell_n bones to zero in both rigs, as SplitSignature does once
// the giant has rejoined (its armour broken away), so a still shows what stays behind. Use on a posed creature (the
// Animators are off, so the clip does not put the plates back). Reports the bones hidden and, per drawn mesh, the extent of
// the baked vertices still on Shell bones (0 = every plate gone, nothing stretched back to the core).
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shattered") continue;
    int hidden = 0;
    foreach (var bone in enemy.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (!bone.name.StartsWith("Shell_")) continue;
        bone.localScale = UnityEngine.Vector3.zero;
        hidden++;
    }

    var sb = new System.Text.StringBuilder("hid " + hidden + " shell bones; ");
    foreach (var renderer in enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
    {
        if (!renderer.enabled) continue;
        var baked = new UnityEngine.Mesh();
        renderer.BakeMesh(baked, true);
        var verts = baked.vertices;
        var weights = renderer.sharedMesh.boneWeights;
        var bones = renderer.bones;
        var spread = new System.Collections.Generic.Dictionary<string, float>();
        var first = new System.Collections.Generic.Dictionary<string, UnityEngine.Vector3>();
        for (int v = 0; v < verts.Length; v++)
        {
            var bone = bones[weights[v].boneIndex0];
            if (weights[v].weight0 < 0.999f || !bone.name.StartsWith("Shell_")) continue;
            if (!first.TryGetValue(bone.name, out var at)) { first[bone.name] = verts[v]; spread[bone.name] = 0f; continue; }
            spread[bone.name] = UnityEngine.Mathf.Max(spread[bone.name], (verts[v] - at).magnitude);
        }

        sb.Append(renderer.name).Append(" [");
        foreach (var pair in spread) sb.Append(pair.Key).Append(' ').Append(pair.Value.ToString("0.000")).Append("; ");
        sb.Append("] ");
        UnityEngine.Object.Destroy(baked);
    }

    return sb.ToString();
}

return "no living Shattered";
