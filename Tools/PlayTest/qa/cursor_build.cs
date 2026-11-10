// QA round 32: puts the cursor on the free buildable cell nearest the screen centre.
var t = typeof(Game.Levels.PlacementController);
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var can = t.GetMethod("CanBuildTower", fl);
var world = t.GetMethod("CellWorld", fl);
var cam = UnityEngine.Camera.main;
var centre = new UnityEngine.Vector2(UnityEngine.Screen.width * 0.5f, UnityEngine.Screen.height * 0.5f);
float best = float.MaxValue; UnityEngine.Vector2Int bestC = UnityEngine.Vector2Int.zero;
for (int x = 0; x < 80; x++) for (int y = 0; y < 60; y++)
{
    var cell = new UnityEngine.Vector2Int(x, y);
    if (!(bool)can.Invoke(pc, new object[] { cell })) continue;
    var p = cam.WorldToScreenPoint((UnityEngine.Vector3)world.Invoke(pc, new object[] { cell }));
    float d = ((UnityEngine.Vector2)p - centre).sqrMagnitude;
    if (p.z > 0 && d < best) { best = d; bestC = cell; }
}
t.GetField("_cursor", fl).SetValue(pc, bestC);
t.GetMethod("Refresh", fl).Invoke(pc, null);
return "cursor " + bestC;
