// Round 41: rebakes the terrain pictures of every campaign level whose continent wears a colony rig (the levelled
// ground under the rig changes their painting), without running the full generator.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var type = typeof(Game.Editor.ProjectGenerator);
var bake = type.GetMethod("BakeTerrain", flags);
var remember = type.GetMethod("RememberBake", flags);
var sb = new System.Text.StringBuilder();
foreach (var node in catalog.Nodes)
{
    var level = node.Level;
    if (level == null || level.Theme == null || level.Theme.Environment == null || level.Theme.Environment.ColonyRig == null) continue;
    bake.Invoke(null, new object[] { level });
    remember.Invoke(null, new object[] { level });
    sb.Append(level.name).Append(' ');
}
UnityEditor.AssetDatabase.SaveAssets();
return "baked " + sb;
