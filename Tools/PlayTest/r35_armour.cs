// Round 35: holds the nearest living armoured creature 2.5 m from the probe Laser Turret (its walk switched off, so it
// stays in the beam), frames both at the zoom in zoom_size.txt, runs at the time scale in r23a_speed.txt and reports the
// armour and health it has left.
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
float speed = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r23a_speed.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var tower = UnityEngine.GameObject.Find("ProbeTower_Laser Turret");
if (tower == null) { return "no probe tower"; }
Game.Enemies.Enemy held = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Health.ArmorMax > 0f) { held = enemy; break; }
}
if (held == null) { return "no armoured creature"; }
UnityEngine.Vector3 spot = tower.transform.position + new UnityEngine.Vector3(2.5f, 0f, -0.5f);
held.enabled = false;
held.Place(spot, (tower.transform.position - spot).normalized);
UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>().SetView(UnityEngine.Vector3.Lerp(tower.transform.position, spot, 0.6f), zoom);
UnityEngine.Time.timeScale = speed;
return "held " + held.Definition.name + " armour " + held.Health.ArmorFraction.ToString("0.00") + " health " + held.Health.Fraction.ToString("0.00");
