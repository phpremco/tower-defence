// Lists the sound player's voices with their clips and end times, plus the boss approach timers, regardless of pause.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", flags).GetValue(sfx);
var ends = (float[])typeof(Game.Boot.SfxPlayer).GetField("_voiceEnds", flags).GetValue(sfx);
var sb = new System.Text.StringBuilder();
sb.Append("now=").Append(UnityEngine.Time.unscaledTime.ToString("0.0"));
sb.Append(" approachUntil=").Append(((float)typeof(Game.Boot.SfxPlayer).GetField("_approachUntil", flags).GetValue(sfx)).ToString("0.0"));
sb.Append(" nextApproach=").Append(((float)typeof(Game.Boot.SfxPlayer).GetField("_nextApproach", flags).GetValue(sfx)).ToString("0.0"));
sb.Append(" listenerPause=").Append(UnityEngine.AudioListener.pause);
for (int i = 0; i < voices.Length; i++)
{
    if (ends[i] > 0f)
    {
        sb.Append(" v").Append(i).Append(voices[i].clip != null ? voices[i].clip.name : "oneshot").Append("@").Append(ends[i].ToString("0.0")).Append(voices[i].isPlaying ? "*" : "");
    }
}

return sb.ToString();
