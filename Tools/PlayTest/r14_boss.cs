// Round 14: reports the continent boss (state, position, health) and, with r14_arg.txt "near", moves it to its route's
// last leg, three and a half metres before the road end so it reaches the base at once.
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss yet t=" + UnityEngine.Time.time.ToString("0.0"); }
var enemy = brain.GetComponent<Game.Enemies.Enemy>();
string arg = System.IO.File.Exists("Tools/PlayTest/r14_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r14_arg.txt").Trim() : "";
if (arg == "near")
{
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var waypoints = (UnityEngine.Vector3[])typeof(Game.Enemies.Enemy).GetField("_waypoints", flags).GetValue(enemy);
    int last = waypoints.Length - 1;
    var back = (waypoints[System.Math.Max(0, last - 1)] - waypoints[last]).normalized;
    var at = waypoints[last] + back * 3.5f;
    enemy.transform.position = new UnityEngine.Vector3(at.x, enemy.transform.position.y, at.z);
    typeof(Game.Enemies.Enemy).GetField("_nextWaypoint", flags).SetValue(enemy, last);
    System.IO.File.WriteAllText("Tools/PlayTest/r14_arg.txt", "");
}

return "boss " + brain.State + " at " + enemy.transform.position.ToString("0.0") + " hp " + enemy.Health.Fraction.ToString("0.00") + " t=" + UnityEngine.Time.time.ToString("0.0");
