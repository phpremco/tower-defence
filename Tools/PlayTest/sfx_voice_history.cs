// Every one-shot voice keeps the last clip it played, so the set of clip names across all voices is a short history
// of what sounded recently: enough to confirm a rare cue such as a boss pulse without catching it in the act.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (sfx == null) { return "no SfxPlayer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", flags).GetValue(sfx);
var kinds = (Game.Boot.SfxKind[])typeof(Game.Boot.SfxPlayer).GetField("_voiceKinds", flags).GetValue(sfx);
var vols = (float[])typeof(Game.Boot.SfxPlayer).GetField("_voiceVolumes", flags).GetValue(sfx);
var seen = new System.Collections.Generic.Dictionary<string, string>();
for (int i = 0; i < voices.Length; i++)
{
    if (voices[i].clip != null) { seen[voices[i].clip.name] = kinds[i] + "=" + vols[i].ToString("0.00"); }
}
var sb = new System.Text.StringBuilder();
foreach (var pair in seen) { sb.Append(pair.Key).Append('/').Append(pair.Value).Append(' '); }
return sb.ToString();
