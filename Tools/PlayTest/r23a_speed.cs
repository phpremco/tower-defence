// Round 23a: points the camera at the probe tower beside the road at the zoom in zoom_size.txt and runs the game at the
// speed in r23a_speed.txt (time scale), to watch the laser and a plated creature's blue line at x3.
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
float speed = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r23a_speed.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var tower = UnityEngine.GameObject.Find("ProbeTower_" + System.IO.File.ReadAllText("Tools/PlayTest/r23a_tower_name.txt").Trim());
if (tower == null) { return "no probe tower"; }
UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>().SetView(tower.transform.position, zoom);
UnityEngine.Time.timeScale = speed;
int plated = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.HasPlating) { plated++; }
}
return "speed " + speed + ", plated alive " + plated + ", tower at " + tower.transform.position.ToString("0.0");
