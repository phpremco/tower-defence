// Round 74: per campaign level and nest, the road cell its walkers join at and whether another cell is equally near (a tie,
// broken since round 74 by RoadCorridor.CompareAlongRoad instead of the cell set's order). "set order" is what the old
// rule (first in the set, strict nearer) would pick. Run: unity command eval_file --file Tools/PlayTest/nest_join_probe.cs
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var text = new System.Text.StringBuilder();
foreach (Game.Levels.MapNode node in catalog.Nodes)
{
    Game.Levels.LevelDefinition level = node.Level;
    if (level == null || level.Nests == null)
    {
        continue;
    }

    bool[] behind = Game.Levels.RearNests.BehindGate(level);
    for (int n = 0; n < level.Nests.Length; n++)
    {
        UnityEngine.Vector3 from = level.Grid.CellToWorld(level.Nests[n].Cell);
        UnityEngine.Vector2Int old = default;
        float oldSqr = float.MaxValue;
        foreach (UnityEngine.Vector2Int cell in level.Road.Cells)
        {
            float sqr = (level.Grid.CellToWorld(cell) - from).sqrMagnitude;
            if (sqr < oldSqr)
            {
                oldSqr = sqr;
                old = cell;
            }
        }

        int ties = 0;
        foreach (UnityEngine.Vector2Int cell in level.Road.Cells)
        {
            ties += UnityEngine.Mathf.Abs((level.Grid.CellToWorld(cell) - from).sqrMagnitude - oldSqr) <= 1e-4f ? 1 : 0;
        }

        UnityEngine.Vector2Int now = Game.Levels.OffRoadRoute.NearestRoadCell(level, from);
        text.Append(level.name).Append(" nest ").Append(n + 1).Append(' ').Append(level.Nests[n].Cell).Append(" joins ").Append(now)
            .Append(now != old ? " (set order: " + old + ", CHANGED)" : string.Empty).Append(ties > 1 ? " tie of " + ties : string.Empty)
            .Append(n < behind.Length && behind[n] ? " behind the gate" : string.Empty).Append("; ");
    }
}

return text.ToString();
