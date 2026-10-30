// Lists every level's nests with how far along the road their creatures join, as a share of the whole road,
// so nests that come up too close to the base stand out. Run in the editor after the generator.
var sb = new System.Text.StringBuilder();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
foreach (var node in catalog.Nodes)
{
    var level = node.Level;
    foreach (var nest in level.Nests)
    {
        UnityEngine.Vector3 start = level.Grid.CellToWorld(nest.Cell);
        UnityEngine.Vector3[] route = Game.Levels.OffRoadRoute.Build(level, start, out float progress);
        float remaining = 0f;
        for (int i = 1; i < route.Length; i++)
        {
            remaining += UnityEngine.Vector3.Distance(route[i - 1], route[i]);
        }

        sb.Append(level.name).Append(' ').Append(nest.Element).Append(' ').Append(nest.Cell).Append(" share ").Append((progress / (progress + remaining)).ToString("0.00")).Append('\n');
    }
}

return sb.ToString();
