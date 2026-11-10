// QA round 32: the nearest free buildable cell to the placement cursor, as d-pad steps (dx dy).
var t = typeof(Game.Levels.PlacementController);
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var cur = (UnityEngine.Vector2Int)t.GetField("_cursor", fl).GetValue(pc);
var can = t.GetMethod("CanBuildTower", fl);
UnityEngine.Vector2Int best = cur; int bestD = 999;
for (int dx = -6; dx <= 6; dx++) for (int dy = -6; dy <= 6; dy++)
{
    var cell = new UnityEngine.Vector2Int(cur.x + dx, cur.y + dy);
    if ((bool)can.Invoke(pc, new object[] { cell }) && System.Math.Abs(dx) + System.Math.Abs(dy) < bestD) { bestD = System.Math.Abs(dx) + System.Math.Abs(dy); best = cell; }
}
return (best.x - cur.x) + " " + (best.y - cur.y);
