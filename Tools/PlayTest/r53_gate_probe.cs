// Round 53: where the RuinGate stands against the field: its drawn footprint (renderer bounds, x/z), the boss entrance
// cell, and every other drawn object whose bounds overlap the gate's footprint (spawn gates, nests, decor, structures),
// with their root's name, so an arch rising through a mound or a route gate shows up as a line here.
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
var own = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(gate.GetComponentsInChildren<UnityEngine.Renderer>(true));
UnityEngine.Bounds box = default;
bool first = true;
foreach (var r in own)
{
    if (!r.enabled || r is UnityEngine.ParticleSystemRenderer) { continue; }
    if (first) { box = r.bounds; first = false; } else { box.Encapsulate(r.bounds); }
}

var sb = new System.Text.StringBuilder();
sb.Append("gate at ").Append(gate.transform.position.ToString("0.00")).Append(" fwd ").Append(gate.transform.forward.ToString("0.00"));
sb.Append(" bounds min ").Append(box.min.ToString("0.00")).Append(" max ").Append(box.max.ToString("0.00"));
if (Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    var level = context.Level;
    sb.Append(" | level ").Append(level.name).Append(" entrance ").Append(level.BossEntrance.Kind).Append(' ').Append(level.BossEntrance.Cell);
    sb.Append(" world ").Append(level.Grid.CellToWorld(level.BossEntrance.Cell).ToString("0.00"));
    foreach (var nest in level.Nests) { sb.Append(" nest ").Append(nest.Cell).Append(' ').Append(level.Grid.CellToWorld(nest.Cell).ToString("0.00")); }
    sb.Append(" road0 ").Append(level.MainRoad[0]).Append(' ').Append(level.Grid.CellToWorld(level.MainRoad[0]).ToString("0.00"));
}

var flat = new UnityEngine.Bounds(box.center, new UnityEngine.Vector3(box.size.x, 1000f, box.size.z));
var hits = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (own.Contains(r) || !r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer) { continue; }
    var b = r.bounds;
    // Ground, terrain and road skins span the field; only objects standing on it count.
    if (b.size.x > 8f || b.size.z > 8f || b.max.y < 0.15f) { continue; }
    if (!flat.Intersects(b)) { continue; }
    var t = r.transform;
    string path = t.name;
    while (t.parent != null && t.parent.parent != null) { t = t.parent; path = t.name + "/" + path; }
    if (path.Length > 90) { path = path.Substring(0, 90); }
    hits[path + " @" + r.bounds.center.ToString("0.0") + " top " + r.bounds.max.y.ToString("0.0")] = 1;
}

sb.Append(" | overlaps ").Append(hits.Count).Append(": ");
foreach (var k in hits.Keys) { sb.Append(k).Append("; "); }
return sb.ToString();
