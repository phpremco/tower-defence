// Round 55b: an ASCII map of the Arena_Ruins level (edit mode): R road (digit = its index along the main road / 10 for the
// first row), B buildable, # blocked (rock formation), ~ liquid, N nest, E boss entrance, F the entrance's gate footprint.
var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Arena/Arena_Ruins.asset");
var build = new Game.Levels.BuildGrid(level);
var blocked = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>(level.BlockedCells);
var nests = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
foreach (var n in level.Nests) { nests.Add(n.Cell); }
var main = level.MainRoad;
var sb = new System.Text.StringBuilder("grid " + level.GridSize + " entrance " + level.BossEntrance.Cell + " main road " + main.Length + " cells, start " + main[0] + " camera yaw " + level.Camera.YawDegrees + "\n");
for (int y = level.GridSize.y - 1; y >= 0; y--)
{
    sb.Append(y.ToString("00")).Append(' ');
    for (int x = 0; x < level.GridSize.x; x++)
    {
        var c = new UnityEngine.Vector2Int(x, y);
        char ch = '.';
        if (c == level.BossEntrance.Cell) ch = 'E';
        else if (nests.Contains(c)) ch = 'N';
        else if (build.IsRoad(c)) ch = 'R';
        else if (blocked.Contains(c)) ch = '#';
        else if (level.Terrain.IsLiquid(c)) ch = '~';
        else if (build.CanBuild(c)) ch = 'B';
        sb.Append(ch);
    }
    sb.Append('\n');
}
System.IO.File.WriteAllText("Tools/PlayTest/r55b_map.txt", sb.ToString());
return "written";
