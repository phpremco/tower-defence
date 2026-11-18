// Round 80: per level of the first continent the main road's length in cells, the defence gate's index on it, every gate's
// nearest road index and the index each nest's walkers join at. Run: unity command eval_file --file Tools/PlayTest/r80_gates.cs
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var text = new System.Text.StringBuilder();
int count = 0;
foreach (Game.Levels.MapNode node in catalog.Nodes)
{
    Game.Levels.LevelDefinition level = node.Level;
    if (level == null || count >= 6) { continue; }
    count++;
    Game.Balance.LevelGeometry geometry = Game.Levels.BalanceLedger.Geometry(level);
    Game.Balance.LevelGeometry.Route route = geometry.GroundRoutes[0];
    var shape = Game.Levels.BalanceLedger.Shape(level);
    text.Append(level.name).Append(": road cells ").Append(route.Cells.Count).Append(", defence gate index ").Append(route.GateIndex).Append(", gates at");
    foreach (var spot in level.Blockades)
    {
        int best = -1; float bestSqr = float.MaxValue;
        for (int i = 0; i < route.Cells.Count; i++)
        {
            float dx = route.Cells[i].X - spot.Cell.x; float dy = route.Cells[i].Y - spot.Cell.y; float sqr = dx * dx + dy * dy;
            if (sqr < bestSqr) { bestSqr = sqr; best = i; }
        }
        text.Append(' ').Append(best);
    }
    text.Append("; nests join at");
    for (int n = 0; n < shape.NestRemaining.Count; n++)
    {
        text.Append(' ').Append(Game.Levels.RearNests.JoinIndex(route.Cells.Count, shape.NestRemaining[n]));
    }
    text.Append(" | ");
}
return text.ToString();
