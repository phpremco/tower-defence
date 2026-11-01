// Reports every ambience and species loop that is sounding, with its volume, pitch and stereo pan.
var music = UnityEngine.Object.FindAnyObjectByType<Game.Boot.MusicPlayer>();
if (music == null) { return "no MusicPlayer"; }
var sb = new System.Text.StringBuilder();
foreach (var s in music.GetComponents<UnityEngine.AudioSource>())
{
    if (s.clip != null && s.loop && s.volume > 0.001f) { sb.Append(s.clip.name).Append(" vol=").Append(s.volume.ToString("0.000")).Append(" pitch=").Append(s.pitch.ToString("0.00")).Append(" pan=").Append(s.panStereo.ToString("0.00")).Append("; "); }
}
return sb.ToString();
