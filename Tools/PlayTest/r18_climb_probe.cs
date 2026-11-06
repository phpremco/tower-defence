// Round 18: where Gravemaw's rock climb and structure climb land with the clips' end offsets, on the level it is fought
// on (run in play mode once the boss is out). Rocks: for every climbable rock cell next to open ground, how deep the rock
// runs straight on from that face, against what Climb_Up + Climb_Down need (Climb_Up's travel past its face plus
// Climb_Down's face distance). Base: how far past the base's centre and its footprint Climb_Up's end pivot lands.
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var field = (Game.Bosses.BossField)typeof(Game.Bosses.ContinentBossBrain).GetField("_field", flags).GetValue(brain);
var enemy = brain.GetComponent<Game.Enemies.Enemy>();
var art = enemy.Definition.Art;
float upF = 0f, upA = 0f, downF = 0f, downA = 0f;
foreach (var c in art.Clips)
{
    if (c.Role == Game.Core.ModelClipRole.ClimbUp) { upF = c.EndOffset.x; upA = c.AnchorForward; }
    if (c.Role == Game.Core.ModelClipRole.ClimbDown) { downF = c.EndOffset.x; downA = c.AnchorForward; }
}
var view = typeof(Game.Bosses.ContinentBossBrain).GetField("_view", flags).GetValue(brain);
float scale = (float)view.GetType().GetProperty("ModelScale", flags | System.Reflection.BindingFlags.Public).GetValue(view);
upF *= scale; upA *= scale; downF *= scale; downA *= scale;
float need = (upF - upA) + downA;
var climb = field.Climb;
var grid = field.Grid;
float cell = grid.CellSize;
var dirs = new[] { new UnityEngine.Vector2Int(1, 0), new UnityEngine.Vector2Int(-1, 0), new UnityEngine.Vector2Int(0, 1), new UnityEngine.Vector2Int(0, -1) };
int faces = 0, deepEnough = 0; float deepest = 0f;
for (int x = 0; x < grid.Size.x; x++)
for (int y = 0; y < grid.Size.y; y++)
{
    var at = new UnityEngine.Vector2Int(x, y);
    if (climb.KindAt(at) != Game.Bosses.SurfaceKind.Rock || climb.HeightAt(at) < 0.75f) { continue; }
    foreach (var d in dirs)
    {
        var before = at - d;
        if (!grid.Contains(before) || climb.KindAt(before) == Game.Bosses.SurfaceKind.Rock) { continue; }
        faces++;
        int run = 0; var c2 = at;
        while (grid.Contains(c2) && climb.KindAt(c2) == Game.Bosses.SurfaceKind.Rock) { run++; c2 += d; }
        float depth = run * cell;
        deepest = UnityEngine.Mathf.Max(deepest, depth);
        if (depth >= need) { deepEnough++; }
    }
}
string result = $"model scale {scale:0.00}; clips: up fwd {upF:0.00} face {upA:0.00}, down fwd {downF:0.00} face {downA:0.00}; a rock top must run {need:0.00} m. Rock faces {faces}, deep enough {deepEnough}, deepest {deepest:0.0} m (cell {cell}).";
if (Game.Core.Services.TryGet(out Game.Levels.LevelContext context) && context.Base != null)
{
    var b = context.Base.Position;
    var bc = grid.WorldToCell(b);
    int half = 0;
    while (climb.IsFootprint(bc + new UnityEngine.Vector2Int(half + 1, 0))) { half++; }
    float halfM = (half + 0.5f) * cell;
    result += $" Base: footprint half {halfM:0.0} m; a climb starts up to {upA:0.00} m before its centre and ends {upF - upA:0.00} m or more past it ({upF - upA - halfM:0.00} m past its far edge); climbing down lands {upF - upA + downF:0.00} m past the centre.";
    foreach (var d in dirs)
    {
        var dv = new UnityEngine.Vector3(d.x, 0f, d.y);
        float reach = 0f;
        while (reach < 4f && climb.IsFootprint(grid.WorldToCell(b + dv * (reach + cell * 0.25f)))) { reach += cell * 0.25f; }
        var land = b + dv * UnityEngine.Mathf.Min(downF, reach + cell);
        var lc = grid.WorldToCell(land);
        bool clear = grid.Contains(lc) && !climb.IsFootprint(lc) && climb.KindAt(lc) != Game.Bosses.SurfaceKind.Rock;
        result += $" Now: top on the centre; down towards ({d.x},{d.y}) lands {UnityEngine.Mathf.Min(downF, reach + cell):0.0} m out at {lc} " + (clear ? "(clear)" : "(BLOCKED)") + ";";
    }
}
System.IO.File.WriteAllText("Logs/r18_climb_probe.txt", result);
return result;
