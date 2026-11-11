// Round 41: lists every playing AudioSource with its clip, volume, mixer group and owner (menu sea-sound hunt).
var sb = new System.Text.StringBuilder();
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
{
    if (!s.isPlaying) continue;
    sb.Append(s.gameObject.name).Append(" <").Append(s.transform.parent != null ? s.transform.parent.name : "-").Append("> clip=")
      .Append(s.clip != null ? s.clip.name : "null").Append(" len=").Append(s.clip != null ? s.clip.length.ToString("0.0") : "-")
      .Append(" vol=").Append(s.volume.ToString("0.00")).Append(" blend=").Append(s.spatialBlend.ToString("0.0"))
      .Append(" group=").Append(s.outputAudioMixerGroup != null ? s.outputAudioMixerGroup.name : "none").Append(" | ");
}
return sb.Length > 0 ? sb.ToString() : "nothing playing";
