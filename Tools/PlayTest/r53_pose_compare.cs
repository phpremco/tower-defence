// Round 53: why the Shattered's drawn top in play differs from the clip-sampled bar height. For the first living
// Shattered: the near rig's top (model units, over the model root), the Core_* / Crys_* / Pelvis bones as posed now, then
// the same after sampling its Walk clip at mid-cycle onto the near rig with the Animators off (they are switched back on).
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shattered") { continue; }
    UnityEngine.Transform modelRoot = null;
    foreach (var t in enemy.GetComponentsInChildren<UnityEngine.Transform>()) { if (t.name.EndsWith("_model")) { modelRoot = t; break; } }
    var near = new System.Collections.Generic.List<UnityEngine.SkinnedMeshRenderer>();
    foreach (var r in enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()) { if (r.enabled && !r.name.EndsWith("_Far")) { near.Add(r); } }
    System.Func<string> measure = () =>
    {
        var s = new System.Text.StringBuilder();
        float top = float.MinValue; string bone = "-";
        var baked = new UnityEngine.Mesh();
        foreach (var r in near)
        {
            r.BakeMesh(baked, true);
            var v = baked.vertices; var w = r.sharedMesh.boneWeights;
            for (int i = 0; i < v.Length; i++)
            {
                float y = r.transform.localToWorldMatrix.MultiplyPoint3x4(v[i]).y;
                if (y > top) { top = y; bone = r.bones[w[i].boneIndex0].name; }
            }
        }
        UnityEngine.Object.Destroy(baked);
        float s0 = modelRoot.lossyScale.y;
        s.Append("top ").Append(((top - modelRoot.position.y) / s0).ToString("0.000")).Append(" model (").Append(bone).Append(") ");
        foreach (var r in near)
        {
            foreach (var b in r.bones)
            {
                if (b.name.StartsWith("Core_") || b.name == "Pelvis" || b.name == "Root") { s.Append(b.name).Append(" y ").Append(((b.position.y - modelRoot.position.y) / s0).ToString("0.00")).Append(" s ").Append(b.localScale.x.ToString("0.00")).Append("; "); }
            }
            break;
        }
        s.Append("rootRot ").Append(modelRoot.rotation.eulerAngles.ToString("0")).Append(' ');
        return s.ToString();
    };
    var sb = new System.Text.StringBuilder("now: " + measure());
    var art = enemy.Definition.Art;
    var walk = art.Clips[art.IndexOf(Game.Core.ModelClipRole.Move)].Clip;
    var animators = enemy.GetComponentsInChildren<UnityEngine.Animator>(true);
    foreach (var a in animators) { a.enabled = false; walk.SampleAnimation(a.gameObject, walk.length * 0.5f); }
    sb.Append(" | sampled Walk 0.5: ").Append(measure());
    foreach (var a in animators) { a.enabled = true; }
    foreach (var a in animators) { sb.Append(" animatorOn ").Append(a.gameObject.name).Append(" scale ").Append(a.transform.localScale.y.ToString("0.00")); }
    return sb.ToString();
}

return "no living Shattered";
