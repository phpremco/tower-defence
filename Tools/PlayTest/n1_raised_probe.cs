var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var sb = new System.Text.StringBuilder(); int i = 0;
foreach (var node in catalog.Nodes) { var l = node.Level; if (l == null) continue; int n = 0; foreach (var kv in l.Terrain.Cells) if (kv.Value == Game.Levels.TerrainKind.Raised) n++; sb.Append(i++ + " " + l.name + " raised " + n + "; "); }
return sb.ToString();
