// Moves every living walker to the waypoint on its route just before the first gate, so attacks can be watched at once.
// The hero's own post is the gate of interest, so the fight can be watched where the hero stands.
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
UnityEngine.Vector3 gate = context.Hero.transform.position;

var waypointField = typeof(Game.Enemies.Enemy).GetField("_waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var nextField = typeof(Game.Enemies.Enemy).GetField("_nextWaypoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
int moved = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.Flies) continue;
    var waypoints = (UnityEngine.Vector3[])waypointField.GetValue(enemy);
    int best = -1;
    float bestDistance = float.MaxValue;
    for (int i = 0; i < waypoints.Length; i++)
    {
        float d = UnityEngine.Vector3.Distance(waypoints[i], gate);
        if (d > 1.8f && d < bestDistance) { bestDistance = d; best = i; }
    }

    if (best < 0) continue;
    enemy.transform.position = waypoints[best];
    nextField.SetValue(enemy, System.Math.Min(best + 1, waypoints.Length - 1));
    moved++;
}

return "moved " + moved + " walkers next to the gate at " + gate.ToString("0.0");
