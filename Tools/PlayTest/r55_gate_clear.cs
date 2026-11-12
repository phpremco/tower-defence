// Round 55: does the Shattered pass the RuinGate cleanly? Bakes the posed gate and the posed giant (LOD0 only) and samples
// both surfaces into 0.06 m voxels in the gate's own frame; a voxel both surfaces fill is a place where they cut through
// each other. Contacts within 0.35 m of the gate's ground (feet on the plinth) are counted apart from the rest.
// Also reports the giant's size in its own heading frame and the angle between its heading and the gate's axis.
// When Tools/PlayTest/r55_gate_scale.txt holds a number, the gate is set to that scale first (a live look; data unchanged).
const float Voxel = 0.06f;
const float LowBand = 0.35f;
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
string scaleArg = System.IO.File.Exists("Tools/PlayTest/r55_gate_scale.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r55_gate_scale.txt").Trim() : "-";
if (scaleArg != "-") { gate.transform.localScale = UnityEngine.Vector3.one * float.Parse(scaleArg, System.Globalization.CultureInfo.InvariantCulture); }
// Tools/PlayTest/r55_gate_setback.txt: a live look at another set-back (metres behind the entrance cell along its axis).
string backArg = System.IO.File.Exists("Tools/PlayTest/r55_gate_setback.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r55_gate_setback.txt").Trim() : "-";
if (backArg != "-" && Game.Core.Services.TryGet(out Game.Levels.LevelContext entranceContext))
{
    var entrance = entranceContext.Level.Grid.CellToWorld(entranceContext.Level.BossEntrance.Cell);
    entrance.y = gate.transform.position.y;
    gate.transform.position = entrance - gate.transform.forward * float.Parse(backArg, System.Globalization.CultureInfo.InvariantCulture);
}

Game.Enemies.Enemy giant = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == "Shattered") { giant = enemy; break; }
}

var gatePos = gate.transform.position;
var toGate = UnityEngine.Quaternion.Inverse(gate.transform.rotation);
System.Func<UnityEngine.Vector3, long> key = p =>
{
    var l = toGate * (p - gatePos);
    long x = (long)UnityEngine.Mathf.FloorToInt(l.x / Voxel) + 100000;
    long y = (long)UnityEngine.Mathf.FloorToInt(l.y / Voxel) + 100000;
    long z = (long)UnityEngine.Mathf.FloorToInt(l.z / Voxel) + 100000;
    return (x << 40) | (y << 20) | z;
};
var baked = new UnityEngine.Mesh();
System.Action<UnityEngine.Renderer, System.Collections.Generic.HashSet<long>, System.Collections.Generic.List<UnityEngine.Vector3>> sample = (r, set, points) =>
{
    UnityEngine.Mesh mesh;
    if (r is UnityEngine.SkinnedMeshRenderer skinned) { skinned.BakeMesh(baked, true); mesh = baked; }
    else { var filter = r.GetComponent<UnityEngine.MeshFilter>(); if (filter == null || filter.sharedMesh == null) { return; } mesh = filter.sharedMesh; }
    var m = r.transform.localToWorldMatrix;
    var v = mesh.vertices;
    var w = new UnityEngine.Vector3[v.Length];
    for (int i = 0; i < v.Length; i++) { w[i] = m.MultiplyPoint3x4(v[i]); if (points != null) { points.Add(w[i]); } }
    var t = mesh.triangles;
    for (int i = 0; i < t.Length; i += 3)
    {
        var a = w[t[i]]; var b = w[t[i + 1]]; var c = w[t[i + 2]];
        float edge = UnityEngine.Mathf.Max((b - a).magnitude, UnityEngine.Mathf.Max((c - b).magnitude, (a - c).magnitude));
        int n = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt(edge / (Voxel * 0.5f)), 1, 400);
        for (int s = 0; s <= n; s++)
        {
            for (int q = 0; q <= n - s; q++)
            {
                float u = (float)s / n; float vv = (float)q / n;
                set.Add(key(a + (b - a) * u + (c - a) * vv));
            }
        }
    }
};
var gateSet = new System.Collections.Generic.HashSet<long>();
float gateTop = float.MinValue;
var gatePoints = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (var r in gate.GetComponentsInChildren<UnityEngine.Renderer>(false))
{
    if (r is UnityEngine.ParticleSystemRenderer || !r.enabled) { continue; }
    sample(r, gateSet, gatePoints);
}

foreach (var p in gatePoints) { gateTop = UnityEngine.Mathf.Max(gateTop, p.y - gatePos.y); }
var sb = new System.Text.StringBuilder();
sb.Append("gate scale ").Append(gate.transform.localScale.x.ToString("0.00")).Append(" top ").Append(gateTop.ToString("0.00"));
sb.Append(" opened ").Append((gate.Opened * 30f).ToString("0")).Append("f fwd ").Append(gate.transform.forward.ToString("0.00"));
if (giant == null) { UnityEngine.Object.Destroy(baked); return sb.Append(" | no giant").ToString(); }
var renderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
var lod = giant.GetComponentInChildren<UnityEngine.LODGroup>();
if (lod != null) { renderers.AddRange(lod.GetLODs()[0].renderers); }
else { renderers.AddRange(giant.GetComponentsInChildren<UnityEngine.Renderer>(false)); }
var giantSet = new System.Collections.Generic.HashSet<long>();
var giantPoints = new System.Collections.Generic.List<UnityEngine.Vector3>();
int used = 0;
foreach (var r in renderers)
{
    if (r == null || !r.enabled || r.forceRenderingOff || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer) { continue; }
    if (!(r is UnityEngine.SkinnedMeshRenderer) && !(r is UnityEngine.MeshRenderer)) { continue; }
    sample(r, giantSet, giantPoints);
    used++;
}

UnityEngine.Object.Destroy(baked);
// The model's -forward is where its head looks (r49_still.cs); the enemy root's own forward is not its heading.
var heading = giant.transform.forward;
foreach (var child in giant.GetComponentsInChildren<UnityEngine.Transform>(true)) { if (child.name.EndsWith("_model")) { heading = -child.forward; break; } }
heading.y = 0f; heading.Normalize();
var side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, heading);
float minF = float.MaxValue, maxF = float.MinValue, minS = float.MaxValue, maxS = float.MinValue, maxY = float.MinValue;
var root = giant.transform.position;
foreach (var p in giantPoints)
{
    var d = p - root;
    float f = UnityEngine.Vector3.Dot(d, heading); float s = UnityEngine.Vector3.Dot(d, side);
    minF = UnityEngine.Mathf.Min(minF, f); maxF = UnityEngine.Mathf.Max(maxF, f); minS = UnityEngine.Mathf.Min(minS, s); maxS = UnityEngine.Mathf.Max(maxS, s);
    maxY = UnityEngine.Mathf.Max(maxY, p.y - gatePos.y);
}

float angle = UnityEngine.Vector3.Angle(heading, new UnityEngine.Vector3(gate.transform.forward.x, 0f, gate.transform.forward.z));
var local = toGate * (root - gatePos);
sb.Append(" | giant ").Append(giant.GetComponent<Game.Bosses.ContinentBossBrain>() is var brain && brain != null ? brain.State : "?");
sb.Append(" at gate-local x ").Append(local.x.ToString("0.00")).Append(" z ").Append(local.z.ToString("0.00"));
sb.Append(" head/axis ").Append(angle.ToString("0")).Append("deg");
sb.Append(" size w ").Append((maxS - minS).ToString("0.00")).Append(" l ").Append((maxF - minF).ToString("0.00")).Append(" h ").Append(maxY.ToString("0.00"));
sb.Append(" (").Append(used).Append(" renderers)");
int low = 0, high = 0;
float hx0 = float.MaxValue, hx1 = float.MinValue, hy0 = float.MaxValue, hy1 = float.MinValue, hz0 = float.MaxValue, hz1 = float.MinValue;
foreach (long k in giantSet)
{
    if (!gateSet.Contains(k)) { continue; }
    float y = (((k >> 20) & 0xFFFFF) - 100000) * Voxel;
    if (y < LowBand) { low++; continue; }
    high++;
    float x = ((k >> 40) - 100000) * Voxel; float z = ((k & 0xFFFFF) - 100000) * Voxel;
    hx0 = UnityEngine.Mathf.Min(hx0, x); hx1 = UnityEngine.Mathf.Max(hx1, x); hy0 = UnityEngine.Mathf.Min(hy0, y); hy1 = UnityEngine.Mathf.Max(hy1, y); hz0 = UnityEngine.Mathf.Min(hz0, z); hz1 = UnityEngine.Mathf.Max(hz1, z);
}

sb.Append(" | cut voxels ").Append(high).Append(" (feet band ").Append(low).Append(')');
// Feet in the plinth: the top of the gate's stone under 0.6 m per ground column, against the giant's lowest surface there.
var floor = new System.Collections.Generic.Dictionary<long, long>();
foreach (long k in gateSet)
{
    long y = ((k >> 20) & 0xFFFFF) - 100000;
    if (y * Voxel >= 0.6f) { continue; }
    long column = k & ~(0xFFFFFL << 20);
    if (!floor.TryGetValue(column, out long top) || y > top) { floor[column] = y; }
}

long deepest = 0;
foreach (long k in giantSet)
{
    long y = ((k >> 20) & 0xFFFFF) - 100000;
    if (floor.TryGetValue(k & ~(0xFFFFFL << 20), out long top) && top - y > deepest) { deepest = top - y; }
}

sb.Append(" | deepest below the stone's top ").Append((deepest * Voxel).ToString("0.00")).Append(" m");
if (high > 0)
{
    sb.Append(" where x ").Append(hx0.ToString("0.0")).Append("..").Append(hx1.ToString("0.0")).Append(" y ").Append(hy0.ToString("0.0")).Append("..").Append(hy1.ToString("0.0"));
    sb.Append(" z ").Append(hz0.ToString("0.0")).Append("..").Append(hz1.ToString("0.0"));
}

return sb.ToString();
