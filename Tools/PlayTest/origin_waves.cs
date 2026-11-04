// Lists, per campaign level on Medium, the waves (1-based) that send creatures out of a nest, with each nest group's delay.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var text = new System.Text.StringBuilder();
for (int n = 0; n < catalog.Nodes.Count; n++)
{
    var level = catalog.Nodes[n].Level;
    if (level == null) continue;
    var waves = level.GetWaves(catalog.Difficulties[1]);
    text.Append(n).Append(" ").Append(level.name).Append(" nests ").Append(level.Nests.Length).Append(" origins ").Append(level.SpawnOrigins != null).Append(": ");
    for (int w = 0; w < waves.Length; w++)
    {
        foreach (var group in waves[w].Groups)
        {
            if (group.Source == Game.Levels.SpawnSource.Nest) text.Append("w").Append(w + 1).Append("@").Append(group.Delay).Append("x").Append(group.Count).Append(" ");
        }
    }
    text.Append(" | ");
}
return text.ToString();
