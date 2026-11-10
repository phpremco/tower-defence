// Round 35: frames the probe Laser Turret and the nearest living armoured creature at the zoom in zoom_size.txt, runs the
// game at the time scale in r23a_speed.txt and reports the creature's armour and health and the laser's beam.
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
float speed = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r23a_speed.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var tower = UnityEngine.GameObject.Find("ProbeTower_Laser Turret");
if (tower == null) { return "no probe tower"; }
Game.Enemies.Enemy best = null;
float bestDistance = float.MaxValue;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Health.ArmorMax <= 0f) { continue; }
    float d = UnityEngine.Vector3.Distance(enemy.transform.position, tower.transform.position);
    if (d < bestDistance) { bestDistance = d; best = enemy; }
}
UnityEngine.Vector3 focus = best != null ? UnityEngine.Vector3.Lerp(tower.transform.position, best.transform.position, 0.6f) : tower.transform.position;
UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>().SetView(focus, zoom);
UnityEngine.Time.timeScale = speed;
int beams = 0;
foreach (var line in tower.GetComponentsInChildren<UnityEngine.LineRenderer>())
{
    if (line.enabled && line.gameObject.activeInHierarchy) { beams++; }
}
return "speed " + speed + " beams " + beams + (best != null ? " armour " + best.Health.ArmorFraction.ToString("0.00") + " health " + best.Health.Fraction.ToString("0.00") + " dist " + bestDistance.ToString("0.0") : " no armoured creature");
