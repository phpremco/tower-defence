// Measures the music loops: RMS of the first and last half second against the track's median level, so a leftover
// fade at either end shows as a number well under one. Runs in the editor without Play Mode.
var sb = new System.Text.StringBuilder();
foreach (string name in new[] { "menu_loop", "game_calm_loop", "game_boss_loop" })
{
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/_Project/Audio/Approved/Music/" + name + ".wav");
    if (clip == null) { sb.Append(name).Append(": missing; "); continue; }
    clip.LoadAudioData();
    int waited = 0;
    while (clip.loadState == UnityEngine.AudioDataLoadState.Loading && waited < 20000) { System.Threading.Thread.Sleep(50); waited += 50; }
    var data = new float[clip.samples * clip.channels];
    clip.GetData(data, 0);
    int win = clip.frequency * clip.channels / 2;
    int count = data.Length / win;
    var rms = new System.Collections.Generic.List<float>();
    for (int w = 0; w < count; w++) { double s = 0; for (int i = w * win; i < (w + 1) * win; i++) { s += data[i] * data[i]; } rms.Add((float)System.Math.Sqrt(s / win)); }
    var sorted = new System.Collections.Generic.List<float>(rms); sorted.Sort();
    float median = sorted[sorted.Count / 2];
    sb.Append(name).Append(": len=").Append(clip.length.ToString("0.0")).Append("s median=").Append(median.ToString("0.000")).Append(" first=").Append((rms[0] / median).ToString("0.00")).Append(" last=").Append((rms[count - 1] / median).ToString("0.00")).Append("; ");
}
return sb.ToString();
