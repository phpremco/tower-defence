// Round 63: a per-frame recorder for the opening's played run (Application.onBeforeRender). Mode from
// Tools/PlayTest/r63_rec_arg.txt: "start" installs it, "stop" removes it and reports:
//  - sound: per shot, every cutscene source heard (clip name, max and mean volume while > 0.001), and every one-shot start;
//  - the Queen: per frame her root's move and her body's lowest point against the ground while she climbs out (a pop = a
//    move over 0.15 m in one frame), and the frame she first shows above the ground;
//  - Spencer in the shiver shots: his feet's slide on the ground (max per frame and summed) and how much his spine moves
//    (summed turn per second), so a frozen pose or a skating brace shows.
// Output: Logs/r63/rec_report.txt.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r63_rec_arg.txt").Trim();
var domain = System.AppDomain.CurrentDomain;
var old = domain.GetData("r63rec") as UnityEngine.Events.UnityAction;
if (old != null) { UnityEngine.Application.onBeforeRender -= old; domain.SetData("r63rec", null); }
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (mode == "stop")
{
    var report = domain.GetData("r63recout") as System.Func<string>;
    if (report == null) return "no recording";
    string text = report();
    System.IO.Directory.CreateDirectory("Logs/r63");
    System.IO.File.WriteAllText("Logs/r63/rec_report.txt", text);
    domain.SetData("r63recout", null);
    return "report written, " + text.Length + " chars";
}

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var heard = new System.Collections.Generic.SortedDictionary<string, double[]>();
var starts = new System.Collections.Generic.List<string>();
var playing = new System.Collections.Generic.Dictionary<UnityEngine.AudioSource, bool>();
var queenLines = new System.Text.StringBuilder();
double[] queen = { 0, -1, 0, 0 }; // max frame move, first shown clock, frames climbing, pops
UnityEngine.Vector3 queenLast = UnityEngine.Vector3.zero; bool queenSeen = false;
double[] feet = { 0, 0, 0, 0 }; // max slide per frame, summed slide, summed spine turn, frames
UnityEngine.Vector3[] feetLast = new UnityEngine.Vector3[2]; bool feetSeen = false;
UnityEngine.Quaternion spineLast = UnityEngine.Quaternion.identity;
int lastShot = -1;

UnityEngine.Events.UnityAction tick = () =>
{
    var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
    if (player == null || player.Sequencer == null || player.Sequencer.IsFinished) return;
    int shot = player.Sequencer.ShotIndex;
    float t = player.Sequencer.LocalTime;
    string key = shot.ToString("00") + " " + player.Sequencer.Current.Action;
    foreach (var source in player.GetComponentsInChildren<UnityEngine.AudioSource>(true))
    {
        bool on = source.isPlaying;
        bool was = playing.TryGetValue(source, out bool w) && w;
        playing[source] = on;
        if (!on || source.volume <= 0.001f) continue;
        string clip = source.clip != null ? source.clip.name : source.name;
        if (!was) starts.Add(key + " t=" + t.ToString("0.00", inv) + " " + source.name + " " + clip);
        string k = key + " | " + clip;
        if (!heard.TryGetValue(k, out double[] s)) { s = new double[3]; heard[k] = s; }
        s[0] = System.Math.Max(s[0], source.volume); s[1] += source.volume; s[2] += 1;
    }

    var set = player.CurrentSet as Game.UI.FieldSet;
    if (set == null) { lastShot = shot; return; }
    var swarm = typeof(Game.UI.FieldSet).GetField("_swarm", flags).GetValue(set);
    var q = swarm.GetType().GetField("_queen", flags).GetValue(swarm);
    if (q != null)
    {
        var enemy = (Game.Enemies.Enemy)q.GetType().GetField("Enemy").GetValue(q);
        float emerge = (float)q.GetType().GetField("Emerge").GetValue(q);
        float rise = (float)q.GetType().GetField("Rise").GetValue(q);
        if (enemy != null && enemy.gameObject.activeInHierarchy && player.Sequencer.Current.Action == Game.Levels.CutsceneAction.SwarmPours)
        {
            var p = enemy.transform.position;
            if (queenSeen && shot == lastShot)
            {
                double move = (p - queenLast).magnitude;
                queen[0] = System.Math.Max(queen[0], move);
                if (move > 0.15) { queen[3] += 1; queenLines.Append("  pop t=" + t.ToString("0.00", inv) + " move " + move.ToString("0.000", inv) + "\n"); }
            }

            if (t >= emerge && t <= emerge + rise)
            {
                queen[2] += 1;
                float top = float.MinValue;
                foreach (var r in enemy.GetComponentsInChildren<UnityEngine.Renderer>()) if (r.enabled && !(r is UnityEngine.ParticleSystemRenderer)) top = UnityEngine.Mathf.Max(top, r.bounds.max.y);
                if (queen[1] < 0 && top > 0.05f) queen[1] = t;
                if (((int)queen[2]) % 12 == 1) queenLines.Append("  climb t=" + t.ToString("0.00", inv) + " root y " + p.y.ToString("0.00", inv) + " top " + top.ToString("0.00", inv) + "\n");
            }

            queenLast = p; queenSeen = true;
        }
    }

    var hero = typeof(Game.UI.FieldSet).GetField("_hero", flags).GetValue(set);
    if (hero != null && player.Sequencer.Current.Action == Game.Levels.CutsceneAction.GroundShivers)
    {
        var f = (UnityEngine.Transform[])hero.GetType().GetField("_feet", flags).GetValue(hero);
        var spine = (UnityEngine.Transform)hero.GetType().GetField("_spine", flags).GetValue(hero);
        if (f.Length == 2 && f[0] != null && f[1] != null && spine != null)
        {
            if (feetSeen && shot == lastShot)
            {
                for (int i = 0; i < 2; i++)
                {
                    var d = f[i].position - feetLast[i]; d.y = 0f;
                    feet[0] = System.Math.Max(feet[0], d.magnitude); feet[1] += d.magnitude;
                }

                feet[2] += UnityEngine.Quaternion.Angle(spineLast, spine.rotation); feet[3] += UnityEngine.Time.unscaledDeltaTime;
            }

            feetLast[0] = f[0].position; feetLast[1] = f[1].position; spineLast = spine.rotation; feetSeen = true;
        }
    }

    lastShot = shot;
};

System.Func<string> build = () =>
{
    var sb = new System.Text.StringBuilder();
    sb.Append("== sounds heard per shot (clip: max / mean volume)\n");
    foreach (var pair in heard) sb.Append(pair.Key).Append(": ").Append(pair.Value[0].ToString("0.000", inv)).Append(" / ").Append((pair.Value[1] / pair.Value[2]).ToString("0.000", inv)).Append("\n");
    sb.Append("== starts\n");
    foreach (var s in starts) sb.Append(s).Append("\n");
    sb.Append("== queen: max move per frame ").Append(queen[0].ToString("0.000", inv)).Append(" m, pops ").Append(queen[3]).Append(", first above ground at attack t=").Append(queen[1].ToString("0.00", inv)).Append(", climbing frames ").Append(queen[2]).Append("\n");
    sb.Append(queenLines);
    sb.Append("== spencer in the shiver shots: max foot slide per frame ").Append(feet[0].ToString("0.0000", inv)).Append(" m, summed ").Append(feet[1].ToString("0.000", inv)).Append(" m, spine turn ").Append((feet[3] > 0 ? feet[2] / feet[3] : 0).ToString("0.0", inv)).Append(" deg/s over ").Append(feet[3].ToString("0.0", inv)).Append(" s\n");
    return sb.ToString();
};

UnityEngine.Application.onBeforeRender += tick;
domain.SetData("r63rec", tick);
domain.SetData("r63recout", build);
return "recording";
