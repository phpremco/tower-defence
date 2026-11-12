// Round 53: for the first living enemy named in Tools/PlayTest/enemy_arg.txt, the health bar against the drawn top in play:
// bar height, baked top of the near-LOD skinned meshes, the bone that carries the topmost vertex, the model root's scale,
// whether the Animators run, and the Crys_n / Shell_n bone scales (the phase look).
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyView>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = view.GetComponent<Game.Enemies.Enemy>();
    if (enemy == null || !enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    float barY = float.NaN;
    foreach (var node in view.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (node.name == "HealthBar") { barY = node.position.y; break; }
    }

    float top = float.MinValue;
    string topBone = "-";
    string topRenderer = "-";
    var baked = new UnityEngine.Mesh();
    foreach (var renderer in view.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        if (!renderer.enabled || renderer.name.EndsWith("_Far")) { continue; }
        renderer.BakeMesh(baked, true);
        var toWorld = renderer.transform.localToWorldMatrix; // round 53: the whole matrix, scale included
        var verts = baked.vertices;
        var weights = renderer.sharedMesh.boneWeights;
        for (int i = 0; i < verts.Length; i++)
        {
            float y = toWorld.MultiplyPoint3x4(verts[i]).y;
            if (y > top) { top = y; topBone = renderer.bones[weights[i].boneIndex0].name; topRenderer = renderer.name; }
        }
    }

    UnityEngine.Object.Destroy(baked);
    var sb = new System.Text.StringBuilder();
    sb.Append($"{wanted}: ground {view.transform.position.y:0.00} bar {barY:0.00} top {top:0.00} ({topRenderer}/{topBone}) barAboveTop {barY - top:0.00} health {enemy.Health.Fraction:0.00} ");
    foreach (var t in view.GetComponentsInChildren<UnityEngine.Transform>())
    {
        if (t.name.EndsWith("_model")) { sb.Append("modelScale ").Append(t.lossyScale.y.ToString("0.000")).Append(' '); break; }
    }

    foreach (var a in view.GetComponentsInChildren<UnityEngine.Animator>(true)) { sb.Append("anim ").Append(a.enabled).Append(' '); }
    var seen = new System.Collections.Generic.HashSet<string>();
    foreach (var b in view.GetComponentsInChildren<UnityEngine.Transform>())
    {
        if ((b.name.StartsWith("Crys_") || b.name.StartsWith("Shell_")) && seen.Add(b.name)) { sb.Append(b.name).Append('=').Append(b.localScale.x.ToString("0.00")).Append(' '); }
    }

    return sb.ToString();
}

return "no living " + wanted;
