// Round 50 / 49H: syncs and rebuilds only the ray boss rows (export StormMatriarch: FBX, LOD1, report, body maps, Low,
// Skins/Corrupted) with BlenderArtBuilder.RebuildOnly, then lists their clips, sockets and the Emitter in both rigs.
int worn = Game.Editor.BlenderArtBuilder.RebuildOnly("RayBoss", "RayBoss_Corrupted");
UnityEditor.AssetDatabase.SaveAssets();
var sb = new System.Text.StringBuilder("worn by " + worn + "; ");
foreach (var id in new[] { "RayBoss", "RayBoss_Corrupted" })
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
    sb.Append(id).Append(": ");
    foreach (var clip in art.Clips) sb.Append(clip.Clip != null ? clip.Clip.name : "-").Append("/").Append(clip.Role).Append(clip.Trigger.Length > 0 ? "[" + clip.Trigger + " ev " + clip.EventSeconds.ToString("0.000") + " sock " + clip.Socket + " sustain " + clip.Sustain.ToString("0.000") + " len " + clip.Length.ToString("0.000") + "]" : "").Append(" ");
    bool near = false, far = false;
    foreach (var t in art.Model.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == "Emitter") near = true;
    if (art.Lod1 != null) foreach (var t in art.Lod1.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == "Emitter") far = true;
    sb.Append(" sockets ").Append(string.Join(",", art.BeamSockets)).Append(" Emitter near ").Append(near).Append(" far ").Append(far).Append(" height ").Append(art.Height.ToString("0.000")).Append("; ");
}

return sb.ToString();
