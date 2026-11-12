// Round 51 / 49I: the clips of the bat and Skyray rows (role, length, event, trigger) and their bone counts, as one line per row,
// so the rows can be compared before and after the 49I import (clip and event parity).
var ids = new[] { "Bat", "Bat_Moss", "Bat_Dust", "Bat_Tide", "Bat_Frost", "Bat_Corrupted", "Skyray", "Skyray_Corrupted", "Shalebat", "Shalebat_Corrupted" };
var sb = new System.Text.StringBuilder();
foreach (var id in ids)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
    if (art == null) { sb.Append(id).Append(": MISSING || "); continue; }
    sb.Append(id).Append(":");
    foreach (var clip in art.Clips)
    {
        sb.Append(' ').Append(clip.Clip != null ? clip.Clip.name : "-").Append('/').Append(clip.Role).Append(' ').Append(clip.Length.ToString("0.000"));
        if (clip.EventSeconds > 0f) { sb.Append(" ev ").Append(clip.EventSeconds.ToString("0.000")); }
    }

    int near = art.Model != null ? art.Model.GetComponentsInChildren<UnityEngine.Transform>(true).Length : 0;
    int far = art.Lod1 != null ? art.Lod1.GetComponentsInChildren<UnityEngine.Transform>(true).Length : 0;
    sb.Append(" transforms ").Append(near).Append('/').Append(far).Append(" height ").Append(art.Height.ToString("0.000")).Append(" || ");
}

return sb.ToString();
