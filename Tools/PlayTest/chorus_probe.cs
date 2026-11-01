// Reports every species voicing the chorus has built: its movement weight this frame, whether it has a move loop and
// what that loop's source plays at, so a silent species can be traced to data, weighting or fading.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (sfx == null) { return "no SfxPlayer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var chorus = typeof(Game.Boot.SfxPlayer).GetField("_chorus", flags).GetValue(sfx);
var voicings = (System.Collections.IDictionary)chorus.GetType().GetField("_voicings", flags).GetValue(chorus);
var sb = new System.Text.StringBuilder();
foreach (System.Collections.DictionaryEntry entry in voicings)
{
    var voicing = entry.Value;
    var type = voicing.GetType();
    var move = type.GetField("Move").GetValue(voicing);
    sb.Append(((UnityEngine.Object)entry.Key).name).Append(" weight=").Append(type.GetField("MoveWeight").GetValue(voicing)).Append(" roll=").Append(type.GetField("RollWeight").GetValue(voicing));
    if (move == null) { sb.Append(" loop=none"); }
    else
    {
        var source = (UnityEngine.AudioSource)move.GetType().GetField("_source", flags).GetValue(move);
        sb.Append(" loop=").Append(source.clip.name).Append(" target=").Append(move.GetType().GetField("_target", flags).GetValue(move)).Append(" vol=").Append(source.volume.ToString("0.000")).Append(" playing=").Append(source.isPlaying);
    }
    var roll = type.GetField("Roll").GetValue(voicing);
    if (roll != null)
    {
        var rs = (UnityEngine.AudioSource)roll.GetType().GetField("_source", flags).GetValue(roll);
        sb.Append(" rollLoop=").Append(rs.clip.name).Append(" target=").Append(roll.GetType().GetField("_target", flags).GetValue(roll)).Append(" vol=").Append(rs.volume.ToString("0.000"));
    }
    sb.Append("; ");
}
return sb.Length == 0 ? "no voicings" : sb.ToString();
