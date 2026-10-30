// Prints every level as text: road rows (=), plazas (P), gates (G), hero stands (H), base (B), spawn (S),
// raised ground (^), water (~), lava (*), quicksand (:), blocked cells (X), mines (M) and build cells (.).
// Row 0 is at the bottom, as in the game. Run after the generator to check a layout without playing it.
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    Game.Levels.RoadCorridor road = level.Road;
    var errors = new System.Collections.Generic.List<string>();
    Game.Levels.LevelValidator.Validate(level, errors);
    var marks = new System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, char>();
    foreach (UnityEngine.Vector2Int c in level.BlockedCells) marks[c] = 'X';
    foreach (UnityEngine.Vector2Int c in level.MineCells) marks[c] = 'M';
    foreach (var kv in level.Terrain.Cells) marks[kv.Key] = kv.Value == Game.Levels.TerrainKind.Raised ? '^' : kv.Value == Game.Levels.TerrainKind.Water ? '~' : kv.Value == Game.Levels.TerrainKind.Lava ? '*' : ':';
    foreach (UnityEngine.Vector2Int c in road.Cells) marks[c] = road.IsPlaza(c) ? 'P' : '=';
    foreach (Game.Levels.EnemyPath p in level.Paths) if (!p.Flying && p.Waypoints.Length > 0) marks[p.Waypoints[0]] = 'S';
    foreach (Game.Levels.BlockadeSpot spot in level.Blockades)
    {
        UnityEngine.Vector3 along = road.DirectionAt(spot.Cell);
        var step = new UnityEngine.Vector2Int(UnityEngine.Mathf.RoundToInt(along.x), UnityEngine.Mathf.RoundToInt(along.z));
        foreach (UnityEngine.Vector2Int c in road.Rung(spot.Cell)) marks[c] = 'G';
        foreach (UnityEngine.Vector2Int c in road.Rung(spot.Cell - step)) marks[c] = 'H';
    }
    UnityEngine.Vector2Int[] main = level.MainRoad;
    foreach (UnityEngine.Vector2Int c in road.Rung(main[main.Length - 1])) marks[c] = 'B';
    sb.AppendLine($"== {level.name} {level.GridSize.x}x{level.GridSize.y} width {road.Width} errors {errors.Count}");
    foreach (string e in errors) sb.AppendLine("   ! " + e);
    for (int y = level.GridSize.y - 1; y >= 0; y--)
    {
        sb.Append(y.ToString().PadLeft(2)).Append(' ');
        for (int x = 0; x < level.GridSize.x; x++)
        {
            sb.Append(marks.TryGetValue(new UnityEngine.Vector2Int(x, y), out char m) ? m : '.');
        }
        sb.AppendLine();
    }
    sb.Append("   ");
    for (int x = 0; x < level.GridSize.x; x++) sb.Append(x % 10);
    sb.AppendLine();
}
UnityEngine.Debug.Log(sb.ToString());
return sb.ToString();
