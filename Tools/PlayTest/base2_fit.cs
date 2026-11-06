// Round 14: for every level, what lies under the expanded base's footprint (cells whose centre the model covers):
// road cells (beyond the road end's own corridor), gates, mines, nests, liquid, cells off the grid, and buildable cells.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (level.MainRoad.Length == 0) { continue; }
    var main = level.MainRoad;
    var endCell = main[main.Length - 1];
    var along = level.Road.DirectionAt(endCell);
    var facing = UnityEngine.Quaternion.LookRotation(-along);
    var centre = Game.Levels.LevelVisualBuilder.BasePosition(level);
    var grid = level.Grid;
    int road = 0, build = 0, off = 0, liquid = 0;
    var notes = new System.Text.StringBuilder();
    for (int x = endCell.x - 6; x <= endCell.x + 6; x++)
    for (int y = endCell.y - 6; y <= endCell.y + 6; y++)
    {
        var cell = new UnityEngine.Vector2Int(x, y);
        var world = grid.CellToWorld(cell);
        if (!art.Covers(world, centre, facing, 0f)) { continue; }
        if (!grid.Contains(cell)) { off++; continue; }
        if (level.Road.IsRoad(cell)) { road++; notes.Append(" road").Append(cell); continue; }
        if (level.Terrain.IsLiquid(cell)) { liquid++; }
        foreach (var gate in level.Blockades) { if (gate.Cell == cell) notes.Append(" GATE").Append(cell); }
        foreach (var mine in level.MineCells) { if (mine == cell) notes.Append(" MINE").Append(cell); }
        foreach (var nest in level.Nests) { if (nest.Cell == cell) notes.Append(" NEST").Append(cell); }
        build++;
    }
    foreach (var gate in level.Blockades)
    {
        if (art.Covers(level.Road.CentreOf(gate.Cell), centre, facing, 1.5f)) notes.Append(" gate-near ").Append(gate.Cell);
    }
    sb.Append(level.name).Append(" grid ").Append(level.GridSize).Append(" end ").Append(endCell).Append(" along ").Append(along.ToString("0.0")).Append(" centre ").Append(centre.ToString("0.0"))
      .Append(" | road ").Append(road).Append(" offgrid ").Append(off).Append(" liquid ").Append(liquid).Append(" landcells ").Append(build).Append(notes).Append('\n');
}
System.IO.File.WriteAllText("Tools/PlayTest/base2_fit.txt", sb.ToString());
return "ok";
