// Round 55: does anything standing on the field (decor, broken remains, props) cut through the Shattered? Samples the
// posed giant (LOD0) and every other mesh whose bounds touch it into 0.06 m world voxels and lists the objects that share
// voxels with it (the ground and road skins, wider than 8 m, are left out; so is the RuinGate, measured by r55_gate_clear.cs).
const float Voxel = 0.06f;
Game.Enemies.Enemy giant = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == "Shattered") { giant = enemy; break; }
}

if (giant == null) { return "no giant"; }
System.Func<UnityEngine.Vector3, long> key = p =>
{
    long x = (long)UnityEngine.Mathf.FloorToInt(p.x / Voxel) + 100000;
    long y = (long)UnityEngine.Mathf.FloorToInt(p.y / Voxel) + 100000;
    long z = (long)UnityEngine.Mathf.FloorToInt(p.z / Voxel) + 100000;
    return (x << 40) | (y << 20) | z;
};
var baked = new UnityEngine.Mesh();
System.Action<UnityEngine.Renderer, System.Collections.Generic.HashSet<long>> sample = (r, set) =>
{
    UnityEngine.Mesh mesh;
    if (r is UnityEngine.SkinnedMeshRenderer skinned) { skinned.BakeMesh(baked, true); mesh = baked; }
    else { var filter = r.GetComponent<UnityEngine.MeshFilter>(); if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) { return; } mesh = filter.sharedMesh; }
    var m = r.transform.localToWorldMatrix;
    var v = mesh.vertices;
    var w = new UnityEngine.Vector3[v.Length];
    for (int i = 0; i < v.Length; i++) { w[i] = m.MultiplyPoint3x4(v[i]); }
    var t = mesh.triangles;
    for (int i = 0; i < t.Length; i += 3)
    {
        var a = w[t[i]]; var b = w[t[i + 1]]; var c = w[t[i + 2]];
        float edge = UnityEngine.Mathf.Max((b - a).magnitude, UnityEngine.Mathf.Max((c - b).magnitude, (a - c).magnitude));
        int n = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt(edge / (Voxel * 0.5f)), 1, 400);
        for (int s = 0; s <= n; s++) { for (int q = 0; q <= n - s; q++) { set.Add(key(a + (b - a) * ((float)s / n) + (c - a) * ((float)q / n))); } }
    }
};
var own = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(giant.GetComponentsInChildren<UnityEngine.Renderer>(true));
var lod = giant.GetComponentInChildren<UnityEngine.LODGroup>();
var renderers = lod != null ? lod.GetLODs()[0].renderers : giant.GetComponentsInChildren<UnityEngine.Renderer>(false);
var giantSet = new System.Collections.Generic.HashSet<long>();
UnityEngine.Bounds box = default; bool first = true;
foreach (var r in renderers)
{
    if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer) { continue; }
    sample(r, giantSet);
    if (first) { box = r.bounds; first = false; } else { box.Encapsulate(r.bounds); }
}

var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
var gateRenderers = gate != null ? new System.Collections.Generic.HashSet<UnityEngine.Renderer>(gate.GetComponentsInChildren<UnityEngine.Renderer>(true)) : new System.Collections.Generic.HashSet<UnityEngine.Renderer>();
var sb = new System.Text.StringBuilder();
int checkedCount = 0, unreadable = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (own.Contains(r) || gateRenderers.Contains(r) || !r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer) { continue; }
    if (!(r is UnityEngine.MeshRenderer) && !(r is UnityEngine.SkinnedMeshRenderer)) { continue; }
    var b = r.bounds;
    if (b.size.x > 8f || b.size.z > 8f || !b.Intersects(box)) { continue; }
    var f = r.GetComponent<UnityEngine.MeshFilter>();
    if (r is UnityEngine.MeshRenderer && (f == null || f.sharedMesh == null || !f.sharedMesh.isReadable)) { unreadable++; sb.Append(" [unreadable ").Append(r.name).Append(']'); continue; }
    checkedCount++;
    var set = new System.Collections.Generic.HashSet<long>();
    sample(r, set);
    int shared = 0;
    foreach (long k in set) { if (giantSet.Contains(k)) { shared++; } }
    if (shared == 0) { continue; }
    var t = r.transform; string path = t.name;
    while (t.parent != null && t.parent.parent != null) { t = t.parent; path = t.name + "/" + path; }
    sb.Append(" | ").Append(path.Length > 80 ? path.Substring(0, 80) : path).Append(" @").Append(b.center.ToString("0.0")).Append(" cut ").Append(shared);
}

// The instanced decoration (EnvironmentInstancer's batches, read by reflection as r54_decor_probe.cs does): every piece
// within 5 m of the giant, its mesh placed by its matrix.
var instancer = UnityEngine.Object.FindAnyObjectByType<Game.Levels.EnvironmentInstancer>();
int pieces = 0;
if (instancer != null)
{
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var batches = (System.Collections.IList)typeof(Game.Levels.EnvironmentInstancer).GetField("_batches", flags).GetValue(instancer);
    foreach (var batch in batches)
    {
        var bt = batch.GetType();
        var mesh = (UnityEngine.Mesh)bt.GetField("Mesh").GetValue(batch);
        var all = (UnityEngine.Matrix4x4[])bt.GetField("Matrices").GetValue(batch);
        if (all == null || mesh == null) { continue; }
        if (!mesh.isReadable) { sb.Append(" [unreadable instanced ").Append(mesh.name).Append(']'); continue; }
        var v = mesh.vertices;
        var t = mesh.triangles;
        foreach (var m in all)
        {
            var at = m.GetPosition();
            if (new UnityEngine.Vector2(at.x - box.center.x, at.z - box.center.z).magnitude > 5f) { continue; }
            pieces++;
            var set = new System.Collections.Generic.HashSet<long>();
            var w = new UnityEngine.Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) { w[i] = m.MultiplyPoint3x4(v[i]); }
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = w[t[i]]; var b = w[t[i + 1]]; var c = w[t[i + 2]];
                float edge = UnityEngine.Mathf.Max((b - a).magnitude, UnityEngine.Mathf.Max((c - b).magnitude, (a - c).magnitude));
                int n = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt(edge / (Voxel * 0.5f)), 1, 400);
                for (int s = 0; s <= n; s++) { for (int q = 0; q <= n - s; q++) { set.Add(key(a + (b - a) * ((float)s / n) + (c - a) * ((float)q / n))); } }
            }

            int shared = 0;
            foreach (long k in set) { if (giantSet.Contains(k)) { shared++; } }
            if (shared > 0) { sb.Append(" | instanced ").Append(mesh.name).Append(" @").Append(at.ToString("0.0")).Append(" cut ").Append(shared); }
        }
    }
}

UnityEngine.Object.Destroy(baked);
sb.Insert(0, ", " + pieces + " instanced pieces within 5 m");
return "giant at " + giant.transform.position.ToString("0.00") + ", " + checkedCount + " meshes near it" + sb.ToString();
