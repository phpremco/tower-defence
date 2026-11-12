// Round 55: a top-down debug view over the RuinGate. "on" (Tools/PlayTest/r55_topdown.txt) turns the main camera straight
// down over the gate and draws, above everything: the gate's axis out of its opening (yellow), the giant's route to the
// road from the entrance cell (magenta, the planner BackToRoad uses), the direction towards the play camera (cyan) and the
// gate's footprint rectangle (white). "off" removes the lines; r53_view.cs puts the play camera back.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r55_topdown.txt").Trim();
var old = UnityEngine.GameObject.Find("R55_TopDown");
if (old != null) { UnityEngine.Object.Destroy(old); }
if (mode != "on") { return "lines removed"; }
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
const float Above = 12f;
var root = new UnityEngine.GameObject("R55_TopDown");
var material = new UnityEngine.Material(UnityEngine.Shader.Find("Sprites/Default"));
System.Action<UnityEngine.Color, float, UnityEngine.Vector3[]> line = (colour, width, points) =>
{
    var go = new UnityEngine.GameObject("Line");
    go.transform.SetParent(root.transform, false);
    var lr = go.AddComponent<UnityEngine.LineRenderer>();
    lr.sharedMaterial = material;
    lr.startColor = colour; lr.endColor = colour;
    lr.startWidth = width; lr.endWidth = width;
    lr.positionCount = points.Length;
    for (int i = 0; i < points.Length; i++) { points[i].y = Above; }
    lr.SetPositions(points);
};
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var level = context.Level;
var spot = level.Grid.CellToWorld(level.BossEntrance.Cell);
var forward = gate.transform.forward;
var right = gate.transform.right;
var tip = spot + forward * 5f;
line(UnityEngine.Color.yellow, 0.18f, new[] { gate.transform.position, tip });
line(UnityEngine.Color.yellow, 0.18f, new[] { tip - forward * 0.9f + right * 0.6f, tip, tip - forward * 0.9f - right * 0.6f });
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
string route = "";
if (brain != null)
{
    var field = (Game.Bosses.BossField)typeof(Game.Bosses.ContinentBossBrain).GetProperty("Field", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(brain);
    var points = new System.Collections.Generic.List<UnityEngine.Vector3>();
    if (Game.Bosses.BossRoutes.WalkToRoad(field, spot, true, points) && points.Count > 1) { line(UnityEngine.Color.magenta, 0.12f, points.ToArray()); route = " route " + points.Count + " points"; }
}

var toCamera = -UnityEngine.Camera.main.transform.forward; toCamera.y = 0f; toCamera.Normalize();
line(UnityEngine.Color.cyan, 0.12f, new[] { gate.transform.position, gate.transform.position + toCamera * 4f });
var half = gate.FootprintHalfSize;
var c = gate.transform.position;
line(UnityEngine.Color.white, 0.06f, new[] { c + right * half.x + forward * half.y, c - right * half.x + forward * half.y, c - right * half.x - forward * half.y, c + right * half.x - forward * half.y, c + right * half.x + forward * half.y });
var camera = UnityEngine.Camera.main;
camera.transform.SetPositionAndRotation(gate.transform.position + UnityEngine.Vector3.up * 40f + forward * 1.5f, UnityEngine.Quaternion.Euler(90f, 0f, 0f));
camera.orthographicSize = 6.5f;
return "top-down over the gate; axis " + forward.ToString("0.00") + " toCamera " + toCamera.ToString("0.00") + " angle " + UnityEngine.Vector3.Angle(forward, toCamera).ToString("0") + "deg" + route;
