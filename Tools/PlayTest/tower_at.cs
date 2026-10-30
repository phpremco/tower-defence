// Builds the first available tower on the cell "x y" given in Tools/PlayTest/cell_arg.txt, through the real
// placement path (road clearance included), and reports where it ended up.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

string[] parts = System.IO.File.ReadAllText("Tools/PlayTest/cell_arg.txt").Trim().Split(' ');
var cell = new UnityEngine.Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var create = typeof(Game.Levels.PlacementController).GetMethod("CreateTower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var tower = (Game.Towers.Tower)create.Invoke(placement, new object[] { context.AvailableTowers[0], cell, false });
return "tower at " + cell + " standing at " + tower.transform.position.ToString("0.00") + " (cell centre " + context.Level.Grid.CellToWorld(cell).ToString("0.00") + ")";
