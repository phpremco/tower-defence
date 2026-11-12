// Round 50: rebuilds the ray boss rows (RayBoss, RayBoss_Corrupted) from the export already in Assets, without syncing from
// ArtSource (a Blender agent is reworking that export), so the catalog's new Release clip and beam sockets land in the art.
var build = typeof(Game.Editor.BlenderArtBuilder).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var sb = new System.Text.StringBuilder();
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures)
{
    if (entry.Id != "RayBoss" && entry.Id != "RayBoss_Corrupted") continue;
    var art = (Game.Core.ModelArtDefinition)build.Invoke(null, new object[] { entry });
    sb.Append(entry.Id).Append(": ");
    if (art == null) { sb.Append("not built; "); continue; }
    foreach (var clip in art.Clips) sb.Append(clip.Clip != null ? clip.Clip.name : "-").Append("/").Append(clip.Role).Append(clip.Trigger.Length > 0 ? "[" + clip.Trigger + " ev " + clip.EventSeconds.ToString("0.00") + "]" : "").Append(" ");
    sb.Append("sockets ").Append(string.Join(",", art.BeamSockets)).Append(" height ").Append(art.Height.ToString("0.000")).Append("; ");
}

UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
