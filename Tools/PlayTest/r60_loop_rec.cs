// Round 60: records the levels of the opening's set-driven loops during a played run (Application.onBeforeRender, every
// frame): the quake rumble (ShakeRumble), the drill (DrillRumble) and the builders' loops (BuildHammerLoop, BuildDrillLoop),
// as their AudioSource volume (share x gain x mix). Mode from Tools/PlayTest/r60_loop_arg.txt: "start" installs it, "stop"
// removes it and reports per shot: min / mean / max of each loop (the quake rumble's minimum also after the shot's first
// 2 s, where a mid-quake dip would show). Output: Logs/r60/loop_rec.csv.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r60_loop_arg.txt").Trim();
var domain = System.AppDomain.CurrentDomain;
var old = domain.GetData("r60loop") as UnityEngine.Events.UnityAction;
if (old != null) { UnityEngine.Application.onBeforeRender -= old; domain.SetData("r60loop", null); }
var oldWriter = domain.GetData("r60loopw") as System.IO.StreamWriter;
if (oldWriter != null) { oldWriter.Dispose(); domain.SetData("r60loopw", null); }
var inv = System.Globalization.CultureInfo.InvariantCulture;
string[] names = { "ShakeRumble", "DrillRumble", "BuildHammerLoop", "BuildDrillLoop" };
if (mode == "stop")
{
    if (!System.IO.File.Exists("Logs/r60/loop_rec.csv")) return "no recording";
    var lines = System.IO.File.ReadAllLines("Logs/r60/loop_rec.csv");
    var stats = new System.Collections.Generic.SortedDictionary<int, float[]>();
    for (int i = 1; i < lines.Length; i++)
    {
        var f = lines[i].Split(',');
        int shot = int.Parse(f[0]);
        float t = float.Parse(f[1], inv);
        if (!stats.TryGetValue(shot, out var s)) { s = new float[names.Length * 4 + 2]; for (int k = 0; k < names.Length; k++) { s[k * 4] = 9f; s[k * 4 + 3] = 9f; } stats[shot] = s; }
        s[names.Length * 4] += 1f;
        for (int k = 0; k < names.Length; k++)
        {
            float v = float.Parse(f[2 + k], inv);
            s[k * 4] = System.Math.Min(s[k * 4], v); s[k * 4 + 1] += v; s[k * 4 + 2] = System.Math.Max(s[k * 4 + 2], v);
            if (t > 2f) s[k * 4 + 3] = System.Math.Min(s[k * 4 + 3], v);
        }
        s[names.Length * 4 + 1] = System.Math.Max(s[names.Length * 4 + 1], t);
    }
    var sb = new System.Text.StringBuilder();
    foreach (var pair in stats)
    {
        var s = pair.Value; float n = s[names.Length * 4];
        sb.Append("shot ").Append(pair.Key).Append(" (").Append(s[names.Length * 4 + 1].ToString("0.0", inv)).Append(" s):");
        for (int k = 0; k < names.Length; k++)
        {
            if (s[k * 4 + 2] < 0.0005f) continue;
            sb.Append(' ').Append(names[k]).Append(' ').Append(s[k * 4].ToString("0.000", inv)).Append('/').Append((s[k * 4 + 1] / n).ToString("0.000", inv)).Append('/').Append(s[k * 4 + 2].ToString("0.000", inv));
            if (k == 0) sb.Append(" (min after 2 s ").Append(s[3] > 8f ? "-" : s[3].ToString("0.000", inv)).Append(')');
        }
        sb.Append(" ;; ");
    }
    return sb.ToString();
}

System.IO.Directory.CreateDirectory("Logs/r60");
var writer = new System.IO.StreamWriter("Logs/r60/loop_rec.csv", false);
writer.WriteLine("shot,stime," + string.Join(",", names));
domain.SetData("r60loopw", writer);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sequencerField = typeof(Game.UI.CutscenePlayer).GetField("_sequencer", flags);
Game.UI.CutscenePlayer player = null;
var found = new UnityEngine.AudioSource[names.Length];
int frame = 0;
UnityEngine.Events.UnityAction handler = () =>
{
    if (!UnityEngine.Application.isPlaying) return;
    frame++;
    if (player == null)
    {
        player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
        if (player == null) return;
    }
    var sequencer = sequencerField.GetValue(player) as Game.Levels.CutsceneSequencer;
    if (sequencer == null) return;
    for (int k = 0; k < names.Length; k++)
    {
        if (found[k] != null) continue;
        foreach (var source in player.GetComponentsInChildren<UnityEngine.AudioSource>(true)) if (source.name == names[k]) found[k] = source;
        if (found[k] == null) foreach (var source in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None)) if (source.name == names[k]) found[k] = source;
    }
    var row = new System.Text.StringBuilder();
    row.Append(sequencer.ShotIndex.ToString(inv)).Append(',').Append(sequencer.LocalTime.ToString("0.000", inv));
    for (int k = 0; k < names.Length; k++) row.Append(',').Append(found[k] != null ? found[k].volume.ToString("0.0000", inv) : "0");
    writer.WriteLine(row.ToString());
    if (frame % 120 == 0) writer.Flush();
};
domain.SetData("r60loop", handler);
UnityEngine.Application.onBeforeRender += handler;
return "recording loops";
