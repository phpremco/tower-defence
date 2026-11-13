// Round 59: records the opening's one-shot sound starts during a played run (Application.onBeforeRender, every frame). Mode
// from Tools/PlayTest/r59_sound_arg.txt: "start" installs the recorder, "stop" removes it and reports. For every source under
// the CutsceneAudio (Voice*, Swell*, Timed*), a start is logged when it goes from silent to playing, with the shot, the shot
// time and the source's volume on that frame and the next frames, so a swell can be seen rising from silence. The report
// counts the starts in the launch shot by kind and lists each swell's first volumes. Output: Logs/r59/sound_rec.csv.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r59_sound_arg.txt").Trim();
var domain = System.AppDomain.CurrentDomain;
var old = domain.GetData("r59snd") as UnityEngine.Events.UnityAction;
if (old != null) { UnityEngine.Application.onBeforeRender -= old; domain.SetData("r59snd", null); }
var oldWriter = domain.GetData("r59sndw") as System.IO.StreamWriter;
if (oldWriter != null) { oldWriter.Dispose(); domain.SetData("r59sndw", null); }
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (mode == "stop")
{
    if (!System.IO.File.Exists("Logs/r59/sound_rec.csv")) return "no recording";
    var lines = System.IO.File.ReadAllLines("Logs/r59/sound_rec.csv");
    int voices = 0, swells = 0, timed = 0; var swellStarts = new System.Text.StringBuilder(); int launchShot = -1;
    var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
    var shots = catalog.Story.OpeningCutscene.Shots;
    for (int s = 0; s < shots.Count; s++) if (shots[s].Action == Game.Levels.CutsceneAction.CapsulesFall) launchShot = s;
    int expectSwells = 0, expectHard = 0;
    foreach (var cue in shots[launchShot].Cues) { if (cue.Kind != Game.Levels.CueKind.Sound) continue; if (cue.FadeIn > 0f) expectSwells++; else expectHard++; }
    for (int i = 1; i < lines.Length; i++)
    {
        var f = lines[i].Split(',');
        if (f[0] != "start" || int.Parse(f[2]) != launchShot) continue;
        if (f[4].StartsWith("Voice")) voices++;
        else if (f[4].StartsWith("Swell")) { swells++; swellStarts.Append(f[3]).Append("s ").Append(f[4]).Append(" vol ").Append(f[5]).Append('/').Append(f[6]).Append('/').Append(f[7]).Append("; "); }
        else if (f[4].StartsWith("Timed")) timed++;
    }
    return "launch shot " + launchShot + ": swell starts " + swells + " (cued " + expectSwells + "), voice starts " + voices + " (cued hard " + expectHard + ", one-shots overlapping on a voice merge), timed " + timed + " | " + swellStarts;
}

var writer = new System.IO.StreamWriter("Logs/r59/sound_rec.csv", false);
writer.WriteLine("kind,frame,shot,stime,source,v0,v1,v2");
domain.SetData("r59sndw", writer);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sequencerField = typeof(Game.UI.CutscenePlayer).GetField("_sequencer", flags);
Game.UI.CutscenePlayer player = null;
Game.UI.CutsceneAudio audio = null;
UnityEngine.AudioSource[] sources = null;
bool[] playing = null;
int frame = 0;
// A start waits for two more frames of volume before it is written.
var pending = new System.Collections.Generic.List<object[]>();
UnityEngine.Events.UnityAction handler = () =>
{
    if (!UnityEngine.Application.isPlaying) return;
    frame++;
    if (player == null || audio == null)
    {
        player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
        audio = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutsceneAudio>();
        if (audio == null) return;
        sources = audio.GetComponentsInChildren<UnityEngine.AudioSource>(true);
        playing = new bool[sources.Length];
    }
    var sequencer = player != null ? sequencerField.GetValue(player) as Game.Levels.CutsceneSequencer : null;
    string shot = sequencer != null ? sequencer.ShotIndex.ToString(inv) : "-1";
    string time = sequencer != null ? sequencer.LocalTime.ToString("0.000", inv) : "0";
    for (int p = pending.Count - 1; p >= 0; p--)
    {
        var row = pending[p];
        var src = (UnityEngine.AudioSource)row[1];
        var vols = (System.Collections.Generic.List<string>)row[2];
        vols.Add(src != null ? src.volume.ToString("0.000", inv) : "-");
        if (vols.Count >= 3) { writer.WriteLine((string)row[0] + "," + string.Join(",", vols)); pending.RemoveAt(p); }
    }
    for (int i = 0; i < sources.Length; i++)
    {
        var source = sources[i];
        if (source == null || source.loop) continue;
        bool now = source.isPlaying;
        if (now && !playing[i])
        {
            pending.Add(new object[] { "start," + frame + "," + shot + "," + time + "," + source.name, source, new System.Collections.Generic.List<string> { source.volume.ToString("0.000", inv) } });
        }
        playing[i] = now;
    }
    if (frame % 120 == 0) writer.Flush();
};
UnityEngine.Application.onBeforeRender += handler;
domain.SetData("r59snd", handler);
return "recording";
