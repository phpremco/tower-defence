// Round 15: with "pick" in r15_smash_arg.txt, centres the camera (at the level's play zoom) on a cluster of breakable
// crystals within reach of the road and remembers it; with "break", smashes every breakable piece within 2.5 m of that
// spot as a boss body would (EnvironmentInstancer.Break), so the remains can be judged at the game camera.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r15_smash_arg.txt").Trim();
var instancer = UnityEngine.Object.FindAnyObjectByType<Game.Levels.EnvironmentInstancer>();
if (instancer == null) { return "no instancer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var list = (System.Collections.IList)typeof(Game.Levels.EnvironmentInstancer).GetField("_breakable", flags).GetValue(instancer);
if (mode == "break")
{
    var parts = System.IO.File.ReadAllText("Tools/PlayTest/r15_smash_spot.txt").Trim().Split(' ');
    var spot = new UnityEngine.Vector3(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    var broken = new System.Collections.Generic.List<Game.Levels.BrokenDecor>();
    int count = instancer.Break(spot, 2.5f, broken);
    string kinds = "";
    foreach (var piece in broken) { kinds += piece.Kind + " r" + piece.Radius.ToString("0.00") + " "; }
    return "broke " + count + " (" + kinds + ") remains " + instancer.RemainsCount;
}
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
// The crystal with the most crystal neighbours within 2.5 m, among those within 6 m of a road cell.
var road = context.Level.MainRoad;
UnityEngine.Vector3 best = UnityEngine.Vector3.zero;
int bestCount = -1;
var positions = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (var item in list)
{
    var tuple = (System.Runtime.CompilerServices.ITuple)item;
    if ((Game.Levels.DecorRemains)tuple[3] != Game.Levels.DecorRemains.Crystal) { continue; }
    positions.Add(((UnityEngine.Matrix4x4)tuple[1]).GetPosition());
}
foreach (var p in positions)
{
    bool nearRoad = false;
    for (int i = 0; i < road.Length; i += 2) { var c = context.Level.Grid.CellToWorld(road[i]); if ((c - p).sqrMagnitude < 36f) { nearRoad = true; break; } }
    if (!nearRoad) { continue; }
    int n = 0;
    foreach (var q in positions) { if ((q - p).sqrMagnitude < 6.25f) { n++; } }
    if (n > bestCount) { bestCount = n; best = p; }
}
System.IO.File.WriteAllText("Tools/PlayTest/r15_smash_spot.txt", best.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + best.z.ToString(System.Globalization.CultureInfo.InvariantCulture));
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
float size = pan != null ? pan.PlayZoom : 3f;
if (pan != null) { pan.enabled = false; }
rig.SetView(best, size);
return "picked " + best.ToString("0.0") + " with " + bestCount + " crystals near, of " + positions.Count + ", zoom " + size.ToString("0.00");
