// Moves every living walker to the waypoint on its route a few cells before the end, so the siege of the base can
// be watched at once instead of after a long walk.
var waypointField = typeof(Game.Enemies.Enemy).GetField("_waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var nextField = typeof(Game.Enemies.Enemy).GetField("_nextWaypoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
int moved = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive) continue;
    var waypoints = (UnityEngine.Vector3[])waypointField.GetValue(enemy);
    int index = System.Math.Max(0, waypoints.Length - 2);
    enemy.transform.position = waypoints[index] + UnityEngine.Vector3.up * (enemy.Position.y - enemy.transform.position.y + (enemy.Definition.Flies ? 0f : 0f));
    enemy.transform.position = new UnityEngine.Vector3(waypoints[index].x, enemy.transform.position.y, waypoints[index].z);
    nextField.SetValue(enemy, System.Math.Min(index + 1, waypoints.Length - 1));
    moved++;
}

return "moved " + moved + " enemies next to the base";
