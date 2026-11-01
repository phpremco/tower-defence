// Reports every loop on the sound player (creature beds, drones, hero hum, path objects) with its clip and volume,
// the busy one-shot voices with their kind, and how many creatures and drones are on the field.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (sfx == null) { return "no SfxPlayer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
sb.Append("t=").Append(UnityEngine.Time.unscaledTime.ToString("0.0"));
var living = (System.Collections.Generic.IReadOnlyList<Game.Enemies.Enemy>)typeof(Game.Boot.CreatureChorus).GetField("_living", flags).GetValue(typeof(Game.Boot.SfxPlayer).GetField("_chorus", flags).GetValue(sfx));
sb.Append(" enemies=").Append(living.Count);
var drones = (System.Collections.Generic.IReadOnlyList<Game.Drones.Drone>)typeof(Game.Boot.SfxPlayer).GetField("_drones", flags).GetValue(sfx);
sb.Append(" drones=").Append(drones.Count);
sb.Append("\nLOOPS:");
foreach (var source in sfx.GetComponents<UnityEngine.AudioSource>())
{
    if (source.clip == null) { continue; }
    sb.Append(' ').Append(source.clip.name).Append('=').Append(source.volume.ToString("0.000"));
}
sb.Append("\nVOICES:");
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", flags).GetValue(sfx);
var ends = (float[])typeof(Game.Boot.SfxPlayer).GetField("_voiceEnds", flags).GetValue(sfx);
var kinds = (Game.Boot.SfxKind[])typeof(Game.Boot.SfxPlayer).GetField("_voiceKinds", flags).GetValue(sfx);
var vols = (float[])typeof(Game.Boot.SfxPlayer).GetField("_voiceVolumes", flags).GetValue(sfx);
int busy = 0;
for (int i = 0; i < voices.Length; i++)
{
    if (ends[i] > UnityEngine.Time.unscaledTime) { busy++; sb.Append(' ').Append(kinds[i]).Append(':').Append(vols[i].ToString("0.00")); }
}
sb.Append(" busy=").Append(busy);
return sb.ToString();
