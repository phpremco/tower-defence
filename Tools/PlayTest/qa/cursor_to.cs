// QA round 32: puts the placement cursor on a cell: "base" (the main road's last rung), "post <i>" (hero post i) or "x y".
var t = typeof(Game.Levels.PlacementController);
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var c = (Game.Levels.LevelContext)t.GetField("_context", fl).GetValue(pc);
var a = System.IO.File.ReadAllText("Tools/PlayTest/qa/cursor_arg.txt").Trim().Split(' ');
UnityEngine.Vector2Int cell;
if (a[0] == "base") { var main = c.Level.MainRoad; cell = main[main.Length - 1]; }
else if (a[0] == "post") { cell = c.HeroPostCells[int.Parse(a[1])]; }
else { cell = new UnityEngine.Vector2Int(int.Parse(a[0]), int.Parse(a[1])); }
t.GetField("_cursor", fl).SetValue(pc, cell);
t.GetMethod("Refresh", fl).Invoke(pc, null);
return "cursor " + cell;
