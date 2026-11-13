// Round 60: every spawn prop of every campaign level (nests, route gates at the path starts, the boss entrance) and the
// pairs that stand closer than 3 cells, in edit mode.
var sb = new System.Text.StringBuilder();
var inv = System.Globalization.CultureInfo.InvariantCulture;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Game.Levels.LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    var names = new System.Collections.Generic.List<string>();
    var spots = new System.Collections.Generic.List<UnityEngine.Vector3>();
    sb.Append("== ").Append(level.name).Append(" (").Append(level.DisplayName).Append(") cell ").Append(level.CellSize.ToString(inv)).Append("\n");
    for (int i = 0; i < level.Nests.Length; i++)
    {
        var n = level.Nests[i];
        var kind = level.Terrain.KindAt(n.Cell);
        names.Add("nest" + i + " " + n.Element + " " + n.Cell + " " + kind + (level.SpawnOrigins != null && level.SpawnOrigins.For(kind) != null ? " living" : ""));
        spots.Add(level.Grid.CellToWorld(n.Cell));
    }
    for (int p = 0; p < level.Paths.Length; p++)
    {
        var route = level.Road.Route(p);
        names.Add("gate" + p + (level.Paths[p].Flying ? " air" : " ground") + " " + route[0].ToString("F1"));
        spots.Add(new UnityEngine.Vector3(route[0].x, 0f, route[0].z));
    }
    var e = level.BossEntrance;
    if (e.Kind != Game.Levels.EntranceKind.Gate) { names.Add("boss " + e.Kind + " " + e.Cell); spots.Add(level.Grid.CellToWorld(e.Cell)); }
    foreach (var s in names) sb.Append("  ").Append(s).Append("\n");
    for (int a = 0; a < spots.Count; a++)
        for (int b = a + 1; b < spots.Count; b++)
        {
            var da = new UnityEngine.Vector2(spots[a].x - spots[b].x, spots[a].z - spots[b].z).magnitude / level.CellSize;
            if (da < 3f) sb.Append("  CLOSE ").Append(names[a]).Append(" <-> ").Append(names[b]).Append(" ").Append(da.ToString("F2", inv)).Append(" cells\n");
        }
}
return sb.ToString();
