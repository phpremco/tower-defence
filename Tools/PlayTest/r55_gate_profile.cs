// Round 55: the RuinGate's opening as posed now, in its own frame (world metres, x across, z along its axis, out = +z):
// for each slice along the axis the soffit over the centre line (a ray up from the ground), the clear half-width at 0.5,
// 1.5, 2.5 and 3.5 m (rays out from the centre line), and the stone's top under the centre line (the plinth or groove).
// Also the Shattered's model frame: how far its body reaches behind and ahead of its pivot, its width and height, and
// its lowest points (feet) behind the pivot, so the set-back of the gate can be chosen.
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
var tris = new System.Collections.Generic.List<UnityEngine.Vector3>();
var baked = new UnityEngine.Mesh();
var toGate = UnityEngine.Quaternion.Inverse(gate.transform.rotation);
var origin = gate.transform.position;
foreach (var skinned in gate.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
{
    skinned.BakeMesh(baked, true);
    var m = skinned.transform.localToWorldMatrix;
    var v = baked.vertices;
    var t = baked.triangles;
    for (int i = 0; i < t.Length; i++) { tris.Add(toGate * (m.MultiplyPoint3x4(v[t[i]]) - origin)); }
}

System.Func<UnityEngine.Vector3, UnityEngine.Vector3, float> cast = (p, dir) =>
{
    float best = float.MaxValue;
    for (int i = 0; i < tris.Count; i += 3)
    {
        var e1 = tris[i + 1] - tris[i]; var e2 = tris[i + 2] - tris[i];
        var h = UnityEngine.Vector3.Cross(dir, e2); float a = UnityEngine.Vector3.Dot(e1, h);
        if (a > -1e-7f && a < 1e-7f) { continue; }
        float f = 1f / a; var s = p - tris[i]; float u = f * UnityEngine.Vector3.Dot(s, h);
        if (u < 0f || u > 1f) { continue; }
        var q = UnityEngine.Vector3.Cross(s, e1); float w = f * UnityEngine.Vector3.Dot(dir, q);
        if (w < 0f || u + w > 1f) { continue; }
        float d = f * UnityEngine.Vector3.Dot(e2, q);
        if (d > 0.001f && d < best) { best = d; }
    }

    return best;
};
var sb = new System.Text.StringBuilder();
sb.Append("gate scale ").Append(gate.transform.localScale.x.ToString("0.00")).Append(" opened ").Append((gate.Opened * 30f).ToString("0")).Append("f |");
for (float z = -2.5f; z <= 3.01f; z += 0.5f)
{
    float soffit = cast(new UnityEngine.Vector3(0f, 0.05f, z), UnityEngine.Vector3.up);
    float floor = 3f - cast(new UnityEngine.Vector3(0f, 3f, z), UnityEngine.Vector3.down);
    if (cast(new UnityEngine.Vector3(0f, 3f, z), UnityEngine.Vector3.down) > 100f) { floor = 0f; }
    sb.Append(" z").Append(z.ToString("0.0")).Append(": roof ").Append(soffit > 100f ? "-" : soffit.ToString("0.00"));
    sb.Append(" floor ").Append(floor.ToString("0.00")).Append(" half");
    foreach (float y in new[] { 0.5f, 1.5f, 2.5f, 3.5f })
    {
        float l = cast(new UnityEngine.Vector3(0f, y, z), UnityEngine.Vector3.left);
        float r = cast(new UnityEngine.Vector3(0f, y, z), UnityEngine.Vector3.right);
        float half = UnityEngine.Mathf.Min(l, r);
        sb.Append(' ').Append(half > 100f ? "-" : half.ToString("0.00"));
    }

    sb.Append(';');
}

UnityEngine.Object.Destroy(baked);
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shattered") { continue; }
    UnityEngine.Transform model = null;
    foreach (var child in enemy.GetComponentsInChildren<UnityEngine.Transform>(true)) { if (child.name.EndsWith("_model")) { model = child; break; } }
    if (model == null) { break; }
    // The model's -forward is where its head looks (r49_still.cs).
    var ahead = -model.forward; ahead.y = 0f; ahead.Normalize();
    var side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, ahead);
    var lod = enemy.GetComponentInChildren<UnityEngine.LODGroup>();
    var renderers = lod != null ? lod.GetLODs()[0].renderers : enemy.GetComponentsInChildren<UnityEngine.Renderer>(false);
    float back = 0f, front = 0f, wide = 0f, high = 0f, footBack = 0f;
    var pivot = enemy.transform.position;
    var mesh = new UnityEngine.Mesh();
    foreach (var r in renderers)
    {
        if (!(r is UnityEngine.SkinnedMeshRenderer sk) || !r.enabled || !r.gameObject.activeInHierarchy) { continue; }
        sk.BakeMesh(mesh, true);
        var m = sk.transform.localToWorldMatrix;
        foreach (var v in mesh.vertices)
        {
            var d = m.MultiplyPoint3x4(v) - pivot;
            float f = UnityEngine.Vector3.Dot(d, ahead);
            back = UnityEngine.Mathf.Max(back, -f); front = UnityEngine.Mathf.Max(front, f);
            wide = UnityEngine.Mathf.Max(wide, UnityEngine.Mathf.Abs(UnityEngine.Vector3.Dot(d, side)));
            high = UnityEngine.Mathf.Max(high, d.y);
            if (d.y < 0.25f) { footBack = UnityEngine.Mathf.Max(footBack, -f); }
        }
    }

    UnityEngine.Object.Destroy(mesh);
    var local = toGate * (pivot - origin);
    float axis = UnityEngine.Vector3.Angle(ahead, gate.transform.forward);
    sb.Append(" | giant ").Append(enemy.GetComponent<Game.Bosses.ContinentBossBrain>().State).Append(" pivot gate-local x ").Append(local.x.ToString("0.00")).Append(" z ").Append(local.z.ToString("0.00"));
    sb.Append(" head/axis ").Append(axis.ToString("0")).Append("deg behind ").Append(back.ToString("0.00")).Append(" ahead ").Append(front.ToString("0.00"));
    sb.Append(" half-width ").Append(wide.ToString("0.00")).Append(" height ").Append(high.ToString("0.00")).Append(" feet reach back ").Append(footBack.ToString("0.00"));
    break;
}

return sb.ToString();
