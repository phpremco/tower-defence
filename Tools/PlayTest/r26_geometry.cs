// Round 26: the plan model's geometry per level: the defence gate, where it sits on the main road, and the approach/behind
// cover of a few spots (from Tools/PlayTest/geometry_arg.txt: "LEVELKEY x,y x,y ..." per line). Written to Tools/PlayTest/geometry_dump.txt.
var sb = new System.Text.StringBuilder();
foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/geometry_arg.txt"))
{
    string[] parts = line.Split(' ');
    if (parts.Length == 0 || parts[0].Length == 0) continue;
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Level_" + parts[0] + ".asset");
    if (level == null) { sb.AppendLine("no level " + parts[0]); continue; }
    Game.Balance.LevelGeometry g = Game.Levels.BalanceLedger.Geometry(level);
    sb.Append("== ").Append(level.name).Append(" main road ").Append(g.MainRoad.Count).Append(" cells; defence gate ");
    if (g.DefenceGate.HasValue)
    {
        var gate = g.DefenceGate.Value;
        int at = -1; float best = float.MaxValue;
        for (int i = 0; i < g.MainRoad.Count; i++) { float dx = g.MainRoad[i].X - gate.X, dy = g.MainRoad[i].Y - gate.Y; float d = dx * dx + dy * dy; if (d < best) { best = d; at = i; } }
        sb.Append('(').Append(gate.X).Append(',').Append(gate.Y).Append(") nearest main-road index ").Append(at).Append(" dist2 ").Append(best.ToString("0.0"));
    }
    else sb.Append("none");
    sb.AppendLine();
    sb.Append("   gates:");
    foreach (var c in g.Gates) sb.Append(" (").Append(c.X).Append(',').Append(c.Y).Append(')');
    sb.AppendLine();
    sb.Append("   road:");
    for (int i = 0; i < g.MainRoad.Count; i += 4) sb.Append(" ").Append(i).Append(":(").Append(g.MainRoad[i].X).Append(',').Append(g.MainRoad[i].Y).Append(')');
    sb.AppendLine();
    for (int p = 1; p < parts.Length; p++)
    {
        string[] xy = parts[p].Split(',');
        var spot = new Game.Balance.Cell(int.Parse(xy[0]), int.Parse(xy[1]));
        foreach (float reach in new[] { 3.5f, 4.5f })
        {
            Game.Balance.SpotCover cover = g.CoverAt(spot, reach);
            sb.Append("   spot (").Append(spot.X).Append(',').Append(spot.Y).Append(") reach ").Append(reach).Append(": ground ").Append(cover.GroundPass.ToString("0.0")).Append(" approach ").Append(cover.ApproachPass.ToString("0.0")).Append(" behind ").Append(cover.BehindPass.ToString("0.0")).Append(" gate ").Append(cover.Gate).Append(" base ").Append(cover.Base).AppendLine();
        }
    }
}
System.IO.File.WriteAllText("Tools/PlayTest/geometry_dump.txt", sb.ToString());
return "written " + sb.Length;
