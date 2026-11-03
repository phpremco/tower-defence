// Prints the ruggedness (GlobePainter) at every level site of region 0 and the route's total, for the node placer.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var sb = new System.Text.StringBuilder();
float total = 0f;
for (int i = 0; i < 20; i++)
{
    var node = catalog.Nodes[i];
    float r = Game.UI.GlobePainter.Ruggedness(Game.Levels.PlanetCoordinates.ToDirection(node.Longitude, node.Latitude));
    total += r;
    sb.Append(i + 1).Append(':').Append(node.Longitude.ToString("0.0")).Append(',').Append(node.Latitude.ToString("0.0")).Append('=').Append(r.ToString("0.00")).Append(' ');
}
var region = catalog.Regions[0];
System.Func<Game.Balance.GlobePoint, float> rug = p => Game.UI.GlobePainter.Ruggedness(Game.Levels.PlanetCoordinates.ToDirection(p.Longitude, p.Latitude));
var plain = Game.Balance.NodePlacer.Route(region.Longitude, region.Latitude, region.RadiusDegrees);
float plainTotal = 0f;
foreach (var p in plain) plainTotal += rug(p);
sb.Append(" total ").Append(total.ToString("0.00")).Append(" plain ").Append(plainTotal.ToString("0.00"));
// A coarse map of ruggedness over the continent: rows north to south, 4 degree cells.
for (float lat = region.Latitude + 28f; lat >= region.Latitude - 28f; lat -= 4f)
{
    sb.Append(" | ");
    for (float lon = region.Longitude - 28f; lon <= region.Longitude + 28f; lon += 4f)
    {
        float r = Game.UI.GlobePainter.Ruggedness(Game.Levels.PlanetCoordinates.ToDirection(lon, lat));
        sb.Append(r > 0.45f ? '#' : r > 0.2f ? '+' : r > 0.05f ? '.' : ' ');
    }
}
return sb.ToString();
