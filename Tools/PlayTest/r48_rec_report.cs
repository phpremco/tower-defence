// Round 47: reads Logs/r48/<tag>.csv (r47_rec_start.cs) and writes Logs/r48/<tag>_report.txt: per rig label the
// visible frames, pops (a frame whose bone change per game second is over PopRatio times the median of its eight neighbours, over SpikeRatio times both direct
// neighbours and over PopFloor degrees: a one-frame jump, not a fast but smooth swing), the clip switch on or just before each pop, frozen frames (no bone moved
// between two frames that moved: stepping), yaw snaps (over YawSnap degrees in one frame, faster than YawSnapRate) and the foot slip of the
// travel cycles (|foot speed - ground speed| / ground speed over 8-frame windows). Tag from r48_arg.txt.
string tag = System.IO.File.ReadAllText("Tools/PlayTest/r48_arg.txt").Trim();
var inv = System.Globalization.CultureInfo.InvariantCulture;
const float PopRatio = 3f;
const float PopFloor = 10f;
const float YawSnap = 20f;
// A turn faster than this (game time) is a snap, not a quick turn (the hero turns at a few hundred degrees a second).
const float YawSnapRate = 1000f;
const int Window = 8;
const float SpikeRatio = 2f;
var byId = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string[]>>();
foreach (string line in System.IO.File.ReadLines("Logs/r48/" + tag + ".csv"))
{
    if (line.StartsWith("frame")) continue;
    string[] c = line.Split(',');
    if (!byId.TryGetValue(c[5], out var list)) { list = new System.Collections.Generic.List<string[]>(); byId[c[5]] = list; }
    list.Add(c);
}

// label -> frames, pops, frozen, moving, yawSnaps, maxYawRate, slipSum, slipCount
var popLines = new System.Text.StringBuilder();
var stats = new System.Collections.Generic.SortedDictionary<string, double[]>();
var causes = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.Dictionary<string, int>>();
double dtSum = 0; int dtCount = 0; var frameDts = new System.Collections.Generic.Dictionary<int, float>();
var handoffs = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.List<float>>();
foreach (var list in byId.Values)
{
    string label = list[0][4];
    if (label.StartsWith("Handoff/"))
    {
        if (!handoffs.TryGetValue(label, out var gaps)) { gaps = new System.Collections.Generic.List<float>(); handoffs[label] = gaps; }
        gaps.Add(float.Parse(list[0][7], inv));
        continue;
    }

    if (!stats.TryGetValue(label, out double[] s)) { s = new double[11]; stats[label] = s; causes[label] = new System.Collections.Generic.Dictionary<string, int>(); }
    int n = list.Count;
    var frame = new int[n]; var dt = new float[n]; var rot = new float[n]; var pos = new float[n]; var yaw = new float[n];
    var vis = new bool[n]; var otime = new float[n]; var shot = new string[n];
    var x = new float[n]; var z = new float[n]; var rate = new float[n]; var ideal = new float[n];
    for (int i = 0; i < n; i++)
    {
        string[] c = list[i];
        frame[i] = int.Parse(c[0]); dt[i] = float.Parse(c[1], inv); rot[i] = float.Parse(c[7], inv); pos[i] = float.Parse(c[8], inv);
        yaw[i] = float.Parse(c[9], inv); x[i] = float.Parse(c[10], inv); z[i] = float.Parse(c[11], inv);
        rate[i] = c[15].Length > 0 ? float.Parse(c[15], inv) : 0f; ideal[i] = c[16].Length > 0 ? float.Parse(c[16], inv) : 0f;
        frameDts[frame[i]] = float.Parse(c[2], inv);
        vis[i] = c[6] == "1";
        otime[i] = c.Length > 18 && c[18].Length > 0 ? float.Parse(c[18], inv) : -1f;
        shot[i] = c.Length > 19 ? c[19] : "-1";
    }

    // Per game second over the longer of this and the last frame: a rig ticked in LateUpdate shows the last frame's step.
    System.Func<int, float> Speed = k => k >= 1 && k < n && dt[k] > 0f ? rot[k] / System.Math.Max(dt[k], dt[k - 1]) : 0f;
    for (int i = 1; i < n; i++)
    {
        if (frame[i] != frame[i - 1] + 1)
        {
            // Switched on again (a pooled corpse or creature): a frame of exactly its last life's pose, then a jump, is stale.
            s[8]++;
            if (rot[i] < 0.01f && i + 1 < n && frame[i + 1] == frame[i] + 1 && rot[i + 1] > PopFloor) s[9]++;
            continue;
        }

        if (frame[i] != frame[i - 1] + 1 || dt[i] <= 0f) continue;
        if (!vis[i]) continue;
        if (!vis[i - 1])
        {
            // The rig was culled last frame: a stale pose now and a jump on the next frame is a wake pop.
            s[8]++;
            if (rot[i] < 0.01f && i + 1 < n && frame[i + 1] == frame[i] + 1 && rot[i + 1] > PopFloor) s[9]++;
            continue;
        }

        if (shot[i] != shot[i - 1]) { s[10]++; continue; }

        s[0]++;
        if (rot[i] > 0.01f) s[3]++;
        float v = Speed(i);
        var near = new System.Collections.Generic.List<float>();
        for (int k = i - Window / 2; k <= i + Window / 2; k++)
        {
            if (k == i || k < 1 || k >= n || frame[k] - frame[i] != k - i || dt[k] <= 0f) continue;
            near.Add(Speed(k));
        }

        if (near.Count >= Window / 2)
        {
            near.Sort();
            float median = near[near.Count / 2];
            float side = System.Math.Max(Speed(i - 1), Speed(i + 1));
            if (v > PopRatio * median && v > SpikeRatio * side && rot[i] > PopFloor + median * dt[i])
            {
                s[1]++;
                string[] a = list[i - 1], b = list[i];
                string cause = "none";
                if (a[12] != b[12]) cause = "cycle " + a[12] + ">" + b[12];
                else if (a[13] != b[13]) cause = "once " + a[13] + ">" + b[13];
                else if (b[13] != "-" && otime[i] >= 0f && otime[i] < otime[i - 1]) cause = "restart " + b[13];
                else if (i >= 2 && a[13] != "-" && otime[i - 1] >= 0f && otime[i - 1] < otime[i - 2]) cause = "restart(prev) " + a[13];
                else if (a[14] != b[14]) cause = "fade " + a[14] + ">" + b[14];
                else if (i >= 2 && list[i - 2][13] != a[13]) cause = "once(prev) " + list[i - 2][13] + ">" + a[13];
                else if (i >= 2 && list[i - 2][12] != a[12]) cause = "cycle(prev) " + list[i - 2][12] + ">" + a[12];
                else cause = "none on " + b[12] + "/" + b[13];
                if (popLines.Length < 200000)
                {
                    popLines.Append(label + " " + frame[i] + " shot " + shot[i] + " t " + list[i][20] + " [" + cause + "]:");
                    for (int k = System.Math.Max(1, i - 4); k <= System.Math.Min(n - 1, i + 4); k++) popLines.Append(" " + (k == i ? "*" : "") + rot[k].ToString("0.0", inv) + "/" + list[k][13]);
                    popLines.AppendLine();
                }

                var d = causes[label];
                d[cause] = d.TryGetValue(cause, out int count) ? count + 1 : 1;
            }
        }

        if (rot[i] == 0f && pos[i] == 0f && i + 1 < n && frame[i + 1] == frame[i] + 1 && rot[i - 1] > 0.5f && rot[i + 1] > 0.5f) s[2]++;
        float turn = UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(yaw[i - 1], yaw[i]));
        if (turn > YawSnap && turn / dt[i] > YawSnapRate)
        {
            s[4]++;
            if (popLines.Length < 200000) popLines.AppendLine("YAW " + label + " " + frame[i] + " shot " + shot[i] + " t " + list[i][20] + " " + yaw[i - 1].ToString("0.0", inv) + ">" + yaw[i].ToString("0.0", inv));
        }
        s[5] = System.Math.Max(s[5], turn / dt[i]);
        if (i >= Window && frame[i] - frame[i - Window] == Window && ideal[i] > 0f && ideal[i - Window] > 0f)
        {
            double ground = 0, foot = 0, time = 0;
            for (int k = i - Window + 1; k <= i; k++)
            {
                ground += System.Math.Sqrt((x[k] - x[k - 1]) * (x[k] - x[k - 1]) + (z[k] - z[k - 1]) * (z[k] - z[k - 1]));
                foot += rate[k] * ideal[k] * dt[k];
                time += dt[k];
            }

            if (ground / time > 0.1)
            {
                s[6] += System.Math.Abs(foot - ground) / ground;
                s[7]++;
            }
        }
    }
}

foreach (float f in frameDts.Values) { dtSum += f; dtCount++; }
var sorted = new System.Collections.Generic.List<float>(frameDts.Values); sorted.Sort();
var report = new System.Text.StringBuilder();
report.AppendLine($"{tag}: {dtCount} frames, mean frame {1000 * dtSum / System.Math.Max(1, dtCount):0.00} ms, median {(sorted.Count > 0 ? 1000 * sorted[sorted.Count / 2] : 0):0.00} ms, p95 {(sorted.Count > 0 ? 1000 * sorted[(int)(sorted.Count * 0.95f)] : 0):0.00} ms");
report.AppendLine("label | frames | pops | pops/1k | frozen | yaw snaps | max yaw deg/s | slip % (windows) | wakes (stale) | cut jumps | top pop causes");
double[] total = new double[11];
foreach (var pair in stats)
{
    double[] s = pair.Value;
    // The hero's weapons are props swung by code (a slash), not clips: listed, but left out of the totals.
    bool prop = pair.Key.Contains("_Sword") || pair.Key.Contains("_Pistol");
    if (!prop)
    {
        for (int k = 0; k < 11; k++) total[k] += k == 5 ? 0 : s[k];
    }
    var top = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(causes[pair.Key]);
    top.Sort((p, q) => q.Value.CompareTo(p.Value));
    var topText = new System.Text.StringBuilder();
    for (int k = 0; k < top.Count && k < 4; k++) topText.Append(top[k].Key + " x" + top[k].Value + "; ");
    report.AppendLine($"{pair.Key} | {s[0]} | {s[1]} | {1000 * s[1] / System.Math.Max(1, s[0]):0.0} | {s[2]} | {s[4]} | {s[5]:0} | {(s[7] > 0 ? 100 * s[6] / s[7] : 0):0.0} ({s[7]}) | {s[8]} ({s[9]}) | {s[10]} | {topText}");
}

report.AppendLine($"TOTAL | {total[0]} | {total[1]} | {1000 * total[1] / System.Math.Max(1, total[0]):0.0} | {total[2]} | {total[4]} | - | {(total[7] > 0 ? 100 * total[6] / total[7] : 0):0.0} ({total[7]}) | {total[8]} ({total[9]}) | {total[10]} |");
report.AppendLine("Death handoff (pose gap between the body's last frame and its corpse's first, summed over the bones, degrees):");
foreach (var pair in handoffs)
{
    pair.Value.Sort();
    double sum = 0;
    foreach (float g in pair.Value) sum += g;
    report.AppendLine($"{pair.Key} | n {pair.Value.Count} | mean {sum / pair.Value.Count:0.0} | median {pair.Value[pair.Value.Count / 2]:0.0} | max {pair.Value[pair.Value.Count - 1]:0.0}");
}

System.IO.File.WriteAllText("Logs/r48/" + tag + "_report.txt", report.ToString());
System.IO.File.WriteAllText("Logs/r48/" + tag + "_pops.txt", popLines.ToString());
return "report written, " + stats.Count + " labels, pops " + total[1] + ", frozen " + total[2] + ", yaw snaps " + total[4];
