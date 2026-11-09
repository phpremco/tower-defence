// Round 28: puts the camera on the first living enemy named in Tools/PlayTest/r28_view.txt ("<Enemy> <ortho half height>")
// without freezing time (zoom_enemy.cs freezes it), so its walk can be stepped and photographed; "report" prints the
// camera's ortho size at the current zoom (the game zoom when nothing moved it).
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r28_view.txt").Trim().Split(' ');
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
if (rig == null) return "no rig";
float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
if (args[0] == "report") return "ortho " + UnityEngine.Camera.main.orthographicSize.ToString("0.00") + " zoom " + rig.Zoom.ToString("0.00");
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
if (pan != null) pan.enabled = false;
float size = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == args[0])
    {
        rig.SetView(enemy.Position, fit / size);
        return "view on " + args[0] + " at " + enemy.Position.ToString("0.0");
    }
}

return "no living " + args[0];
