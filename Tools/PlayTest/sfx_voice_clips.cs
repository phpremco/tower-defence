// Lists the one-shot voices that are sounding right now with their clip name, kind and volume, plus every loop that
// is above silence, so a specific cue (a boss pulse, a species loop) can be confirmed by name.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (sfx == null) { return "no SfxPlayer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
sb.Append("t=").Append(UnityEngine.Time.unscaledTime.ToString("0.0")).Append(" voices:");
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", flags).GetValue(sfx);
var ends = (float[])typeof(Game.Boot.SfxPlayer).GetField("_voiceEnds", flags).GetValue(sfx);
var kinds = (Game.Boot.SfxKind[])typeof(Game.Boot.SfxPlayer).GetField("_voiceKinds", flags).GetValue(sfx);
for (int i = 0; i < voices.Length; i++)
{
    if (ends[i] > UnityEngine.Time.unscaledTime && voices[i].clip != null) { sb.Append(' ').Append(voices[i].clip.name).Append('/').Append(kinds[i]).Append('=').Append(voices[i].volume.ToString("0.00")); }
}
sb.Append(" loops:");
foreach (var source in sfx.GetComponents<UnityEngine.AudioSource>())
{
    if (source.clip != null && source.loop && source.volume > 0.001f) { sb.Append(' ').Append(source.clip.name).Append('=').Append(source.volume.ToString("0.000")); }
}
return sb.ToString();
