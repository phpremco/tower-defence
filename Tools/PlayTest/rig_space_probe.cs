var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
if (catalog == null) { var g = UnityEditor.AssetDatabase.FindAssets("t:CampaignCatalog"); catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0])); }
var sb = new System.Text.StringBuilder();
foreach (var node in catalog.Nodes) {
  var level = node.Level; if (level == null) continue;
  var foot = Game.Levels.FixedCameraRig.GroundFootprint(level, 16f/9f);
  var size = level.WorldSize;
  var basePos = Game.Levels.LevelVisualBuilder.BasePosition(level);
  sb.AppendLine(level.name + " grid " + level.Grid.Size + " cell " + level.CellSize + " world " + size + " foot " + foot + " base " + basePos + " pitch " + level.Camera.PitchDegrees + " yaw " + level.Camera.YawDegrees + " cellToWorld(0,0) " + level.Grid.CellToWorld(UnityEngine.Vector2Int.zero));
}
return sb.ToString();
