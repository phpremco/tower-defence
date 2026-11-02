// Reports the beam loop voices of the sound player: which are live and their current volumes, plus the hero death set.
var player = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (player == null) { return "no sfx player"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var voices = (Game.Boot.LoopVoice[])typeof(Game.Boot.SfxPlayer).GetField("_beamVoices", flags).GetValue(player);
var last = (float[])typeof(Game.Boot.SfxPlayer).GetField("_beamLastTick", flags).GetValue(player);
var sb = new System.Text.StringBuilder();
for (int i = 0; i < voices.Length; i++)
{
    var source = (UnityEngine.AudioSource)typeof(Game.Boot.LoopVoice).GetField("_source", flags).GetValue(voices[i]);
    sb.Append("voice ").Append(i).Append(": clip ").Append(source.clip != null ? source.clip.name : "none").Append(" vol ").Append(source.volume.ToString("0.00")).Append(" target ").Append(voices[i].Target.ToString("0.00")).Append(" since ").Append((UnityEngine.Time.unscaledTime - last[i]).ToString("0.00")).Append(" | ");
}
return sb.ToString();
