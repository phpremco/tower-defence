// Reports where the C1_01 base stands on the grid (inside or past the edge, on a blocked cell or not) and its clearance radius.
var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Level_C1_01.asset");
var pos = Game.Levels.LevelVisualBuilder.BasePosition(level);
var cell = level.Grid.WorldToCell(pos);
bool blocked = System.Array.IndexOf(level.BlockedCells, cell) >= 0;
var near = new System.Text.StringBuilder();
foreach (var b in level.BlockedCells) { if ((b - cell).sqrMagnitude <= 4) near.Append(b + " "); }
float r = 0; foreach (var z in new Game.Levels.StructureClearance(level).Zones) if (z.Name == "Base") r = z.Radius;
return "base " + pos + " cell " + cell + " grid " + level.GridSize + " blocked " + blocked + " blocked near: " + near + " clearance " + r;
