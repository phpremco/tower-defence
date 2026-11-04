var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition", new[] { "Assets/_Project/Data/Art" }))
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    sb.Append(art.name).Append(" scale=").Append(art.Scale).Append(" top=").Append(art.Height.ToString("F2")).Append(" lod1=").Append(art.Lod1 != null).Append(" fade=").Append(art.DeathFade).Append(" mat=").Append(art.Material != null ? string.Join(",", art.Material.shaderKeywords) : "-").Append(" | ");
    foreach (var c in art.Clips) sb.Append(c.Role).Append(':').Append(c.Clip.name).Append(c.FarClip != c.Clip ? "+far" : "").Append(c.DistancePerLoop > 0 ? "/d" + c.DistancePerLoop : "").Append(c.EventSeconds > 0 ? "@" + c.EventSeconds.ToString("F2") : "").Append(c.Trigger.Length > 0 ? "[" + c.Trigger + "]" : "").Append(' ');
    sb.AppendLine();
}
System.IO.File.WriteAllText("Tools/PlayTest/imp_art_probe.txt", sb.ToString());
return "ok";
