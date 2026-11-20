// Round 88a: puts the play camera on the first living creature named in Tools/PlayTest/enemy_arg.txt (first word) at the
// level's own play zoom times Tools/PlayTest/zoom_size.txt (1 = as played), so a still shows it as the player sees it.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
float factor = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig != null ? rig.GetComponent<Game.Levels.CameraPanZoom>() : null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == wanted)
    {
        if (pan != null) pan.enabled = false;
        rig.SetView(enemy.Position, (pan != null ? pan.PlayZoom : 10f) * factor);
        return "focus on " + wanted + " at " + enemy.Position.ToString("0.0") + " hp " + enemy.Health.Current.ToString("0");
    }
}
return "no living " + wanted;
