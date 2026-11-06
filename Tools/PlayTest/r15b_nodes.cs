// Round 15b: lists the campaign nodes (index, level, region, theme), to pick each continent's first level.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:CampaignCatalog")[0]));
var sb = new System.Text.StringBuilder();
for (int i = 0; i < catalog.Nodes.Count; i++)
{
    var n = catalog.Nodes[i];
    sb.Append(i).Append(' ').Append(n.Level != null ? n.Level.name : "-").Append(' ').Append(n.Level != null && n.Level.Theme != null ? n.Level.Theme.name : "-").Append(" | ");
}
var arena = catalog.BossArena;
foreach (var boss in arena.Bosses) { sb.Append(" arena ").Append(boss.name).Append('=').Append(arena.LevelFor(boss).name).Append('/').Append(arena.LevelFor(boss).Theme.name); }
return sb.ToString();
