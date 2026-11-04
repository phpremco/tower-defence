var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:CampaignCatalog")[0]));
var sb = new System.Text.StringBuilder();
foreach (var node in catalog.Nodes) {
  var level = node.Level; if (level == null) continue;
  var sw = System.Diagnostics.Stopwatch.StartNew();
  var c = new Game.Levels.StructureClearance(level);
  sb.AppendLine(level.name + " rig " + c.HasColonyRig + " at " + c.ColonyRigPosition + " base " + Game.Levels.LevelVisualBuilder.BasePosition(level) + " ms " + sw.ElapsedMilliseconds);
}
return sb.ToString();
