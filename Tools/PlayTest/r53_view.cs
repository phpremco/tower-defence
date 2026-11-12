// Round 53: points the play camera (its fixed angle) at the RuinGate, or at the first living enemy named in
// Tools/PlayTest/enemy_arg.txt when there is no gate or r53_view.txt says "enemy", at the zoom in zoom_size.txt.
string mode = System.IO.File.Exists("Tools/PlayTest/r53_view.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r53_view.txt").Trim() : "gate";
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
if (mode == "gate")
{
    var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
    if (gate != null) { rig.SetView(gate.transform.position, zoom); return "view on RuinGate at " + gate.transform.position.ToString("0.00") + " zoom " + zoom; }
}

string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == wanted) { rig.SetView(enemy.Position, zoom); return "view on " + wanted + " at " + enemy.Position.ToString("0.00"); }
}

return "nothing to view";
