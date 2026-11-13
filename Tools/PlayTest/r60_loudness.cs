// Round 60: loudness (90th percentile of 50 ms RMS windows, as the generator measures) of the in-game build sound and its
// neighbours in the mix, times the level each is played at, so a hot one shows. Edit mode.
var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.SfxLibrary>("Assets/_Project/Data/Audio/SfxLibrary.asset");
System.Func<UnityEngine.AudioClip, float> p90 = clip =>
{
    var data = new float[clip.samples * clip.channels];
    clip.GetData(data, 0);
    int window = UnityEngine.Mathf.Max(1, clip.frequency * clip.channels / 20);
    var list = new System.Collections.Generic.List<float>();
    for (int start = 0; start + window <= data.Length; start += window)
    {
        double sum = 0; for (int i = start; i < start + window; i++) sum += data[i] * data[i];
        list.Add((float)System.Math.Sqrt(sum / window));
    }
    if (list.Count == 0) return 0f;
    list.Sort();
    return list[(int)(list.Count * 0.9f) < list.Count ? (int)(list.Count * 0.9f) : list.Count - 1];
};
var sb = new System.Text.StringBuilder();
var build = Game.Core.ProceduralSfx.Build();
float b = p90(build);
sb.Append("build(procedural) p90 ").Append(b.ToString("0.000")).Append(" len ").Append(build.length.ToString("0.00")).Append(" x0.7 => ").Append((b * 0.7f).ToString("0.000"));
foreach (var name in new[] { "Upgrades", "ScrapPickups", "Cash", "MineExplosions" })
{
    var prop = lib.GetType().GetProperty(name);
    if (prop == null) { sb.Append(" | no " + name); continue; }
    var set = (Game.Core.SoundSet)prop.GetValue(lib);
    if (set == null || set.IsEmpty) { sb.Append(" | " + name + " empty"); continue; }
    var clip = set[0];
    sb.Append(" | ").Append(name).Append(" p90*gain ").Append((p90(clip) * set.Gain).ToString("0.000"));
}
return sb.ToString();
