// Round 59: the salvo, sought through the launch shot (shot index from Tools/PlayTest/r59_probe_arg.txt, default 3) in steps of
// 1/60 s. For every capsule of the drop it follows the capsule body, its booster, its head glow, air glow, marker light and
// trails: what the lens sees of each (pixels across at 1080 lines, on screen, hidden behind the planet), and counts a POP
// when something seen at 2 px or more appears or vanishes between two steps. It also measures how fast each capsule's
// axis and its path on screen turn (a heading snap is a turn above 720 deg/s), lists each capsule's release, where it
// leaves the frame or sinks behind the rim, and writes the per-step table to Logs/r59/salvo_probe.csv.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
int shot = 3;
if (System.IO.File.Exists("Tools/PlayTest/r59_probe_arg.txt")) shot = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r59_probe_arg.txt").Trim());
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var spec = catalog.Story.OpeningCutscene.Shots[shot];
float length = spec.CameraLength;
player.Seek(shot, 0f);
var set = player.CurrentSet;
object drop = set.GetType().GetField("_capsules", flags).GetValue(set);
var list = (System.Collections.IList)drop.GetType().GetField("_capsules", flags).GetValue(drop);
System.Type capsuleType = list[0].GetType();
System.Func<object, string, object> get = (o, n) => capsuleType.GetField(n, flags).GetValue(o);
var stageRoot = typeof(Game.UI.CapsuleStage).GetField("_root", flags);
var centre = Game.UI.OrbitSet.PlanetCentre; float radius = Game.UI.OrbitSet.PlanetRadius;
int n = list.Count;
string[] parts = { "body", "booster", "head", "air", "marker", "trail", "halo" };
var lastPx = new float[n, parts.Length];
var pops = new System.Text.StringBuilder(); int popCount = 0;
var lastUp = new UnityEngine.Vector3[n]; var lastScreen = new UnityEngine.Vector3[n]; var lastDir = new UnityEngine.Vector3[n];
var hasLast = new bool[n]; var hasDir = new bool[n];
var maxTurn = new float[n]; var maxPathTurn = new float[n]; var snaps = new int[n];
var firstSeen = new float[n]; var lastSeen = new float[n]; var lastState = new string[n];
for (int i = 0; i < n; i++) { firstSeen[i] = -1f; lastSeen[i] = -1f; lastState[i] = ""; }
var csv = new System.IO.StreamWriter("Logs/r59/salvo_probe.csv", false);
csv.WriteLine("t,capsule,part,px,onscreen,hidden,x,y");
var ship = UnityEngine.GameObject.Find("Mothership");
var colliders = new System.Collections.Generic.List<UnityEngine.Collider>();
foreach (var r in ship.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    UnityEngine.Mesh mesh = null;
    var mf = r.GetComponent<UnityEngine.MeshFilter>();
    if (mf != null) mesh = mf.sharedMesh;
    var smr = r as UnityEngine.SkinnedMeshRenderer;
    if (smr != null) { mesh = new UnityEngine.Mesh(); smr.BakeMesh(mesh, true); }
    if (mesh == null || r.name.StartsWith("RunningLight") || r.name.StartsWith("Livery")) continue;
    var mc = r.gameObject.AddComponent<UnityEngine.MeshCollider>();
    mc.sharedMesh = mesh;
    colliders.Add(mc);
}
UnityEngine.Physics.SyncTransforms();
var lastActive = new bool[n, 7]; int jumps = 0; int inside = 0; float deepest = 0f;
float dt = 1f / 60f;
var cam = UnityEngine.Camera.main;
System.Func<UnityEngine.Vector3, bool> hidden = p =>
{
    UnityEngine.Vector3 o = cam.transform.position, d = p - o; float dist = d.magnitude; d /= dist;
    UnityEngine.Vector3 oc = o - centre; float b = UnityEngine.Vector3.Dot(oc, d); float c = oc.sqrMagnitude - radius * radius;
    float disc = b * b - c; if (disc < 0f) return false;
    float hit = -b - UnityEngine.Mathf.Sqrt(disc);
    return hit > 0f && hit < dist - 0.01f;
};
System.Func<UnityEngine.Vector3, UnityEngine.Vector2> screen = p =>
{
    UnityEngine.Vector3 local = cam.transform.InverseTransformPoint(p);
    float half = UnityEngine.Mathf.Tan(cam.fieldOfView * 0.5f * UnityEngine.Mathf.Deg2Rad);
    if (local.z <= 0.01f) return new UnityEngine.Vector2(-9999f, -9999f);
    return new UnityEngine.Vector2(960f + local.x / (local.z * half) * 540f, 540f - local.y / (local.z * half) * 540f);
};
System.Func<UnityEngine.Vector3, float, float> pixels = (p, size) =>
{
    float depth = cam.transform.InverseTransformPoint(p).z;
    if (depth <= 0.01f) return 0f;
    return size * 1080f / (2f * depth * UnityEngine.Mathf.Tan(cam.fieldOfView * 0.5f * UnityEngine.Mathf.Deg2Rad));
};
for (float t = 0f; t <= length; t += dt)
{
    player.Seek(shot, t / length);
    for (int i = 0; i < n; i++)
    {
        object cap = list[i];
        var root = (UnityEngine.Transform)get(cap, "Root");
        var head = (UnityEngine.Transform)get(cap, "Head");
        var air = (UnityEngine.Transform)get(cap, "AirGlow");
        var marker = (UnityEngine.Transform)get(cap, "Marker");
        var trail = (UnityEngine.LineRenderer)get(cap, "Trail");
        var halo = (UnityEngine.LineRenderer)get(cap, "Halo");
        object stage = get(cap, "Stage");
        var booster = stage != null ? (UnityEngine.Transform)stageRoot.GetValue(stage) : null;
        var px = new float[parts.Length]; var at = new UnityEngine.Vector3[parts.Length];
        var vis = new bool[parts.Length];
        var share = new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        // Body and booster: the renderers' bounds.
        UnityEngine.Transform[] meshes = { root, booster };
        for (int m = 0; m < 2; m++)
        {
            var tr = meshes[m];
            if (tr == null || !tr.gameObject.activeInHierarchy) continue;
            var rs = tr.GetComponentsInChildren<UnityEngine.Renderer>();
            if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            at[m] = bb.center; px[m] = pixels(bb.center, bb.size.magnitude * 0.6f); vis[m] = true;
            // The share of the body the lens sees: nine points of its box, each hidden by the planet or the hull.
            int open = 0;
            for (int q = 0; q < 9; q++)
            {
                UnityEngine.Vector3 o = q == 8 ? UnityEngine.Vector3.zero : new UnityEngine.Vector3((q & 1) == 0 ? -0.35f : 0.35f, (q & 2) == 0 ? -0.35f : 0.35f, (q & 4) == 0 ? -0.35f : 0.35f);
                UnityEngine.Vector3 pt = bb.center + UnityEngine.Vector3.Scale(o, bb.size);
                if (!hidden(pt) && !UnityEngine.Physics.Linecast(cam.transform.position, pt)) open++;
            }
            share[m] = open / 9f;
            if (m == 1 && UnityEngine.Vector3.Distance(bb.center, centre) < radius) { inside++; deepest = UnityEngine.Mathf.Max(deepest, radius - UnityEngine.Vector3.Distance(bb.center, centre)); }
        }
        UnityEngine.Transform[] glows = { head, air, marker };
        for (int g = 0; g < 3; g++)
        {
            var tr = glows[g];
            if (tr == null || !tr.gameObject.activeInHierarchy) continue;
            at[2 + g] = tr.position; px[2 + g] = pixels(tr.position, tr.lossyScale.x * 0.5f); vis[2 + g] = true;
        }
        UnityEngine.LineRenderer[] lines = { trail, halo };
        for (int l = 0; l < 2; l++)
        {
            var line = lines[l];
            if (line == null || !line.enabled) continue;
            at[5 + l] = line.GetPosition(0); px[5 + l] = pixels(line.GetPosition(0), line.widthMultiplier); vis[5 + l] = true;
        }
        for (int k = 0; k < parts.Length; k++)
        {
            UnityEngine.Vector2 s = vis[k] ? screen(at[k]) : new UnityEngine.Vector2(-1, -1);
            bool on = vis[k] && s.x > -px[k] && s.x < 1920f + px[k] && s.y > -px[k] && s.y < 1080f + px[k];
            bool behind = vis[k] && hidden(at[k]);
            float seen = on && !behind ? px[k] * share[k] : 0f;
            // A pop: something switches on or off while the lens sees 2 px or more of it. A jump: what is seen of it halves or
            // doubles (by 6 px or more) between two steps while it stays on.
            bool toggled = vis[k] != lastActive[i, k];
            float before = lastPx[i, k];
            if (toggled && t > dt && (vis[k] ? seen : before) >= 2f)
            {
                popCount++;
                if (popCount < 40) pops.Append("POP ").Append(parts[k]).Append(i).Append(vis[k] ? " on" : " off").Append(" t=").Append(t.ToString("0.000", inv)).Append(' ').Append(before.ToString("0.0", inv)).Append("px -> ").Append(seen.ToString("0.0", inv)).Append("px\n");
            }
            else if (!toggled && vis[k] && UnityEngine.Mathf.Abs(seen - before) >= 6f && (seen > before * 2f || seen < before * 0.5f))
            {
                jumps++;
                if (jumps < 30) pops.Append("JUMP ").Append(parts[k]).Append(i).Append(" t=").Append(t.ToString("0.000", inv)).Append(' ').Append(before.ToString("0.0", inv)).Append("px -> ").Append(seen.ToString("0.0", inv)).Append("px\n");
            }
            lastActive[i, k] = vis[k];
            lastPx[i, k] = seen;
            if (vis[k] && (k == 0 || k == 2)) csv.WriteLine(string.Join(",", t.ToString("0.000", inv), i, parts[k], px[k].ToString("0.0", inv), on ? 1 : 0, behind ? 1 : 0, s.x.ToString("0", inv), s.y.ToString("0", inv)));
        }
        // The body's state for the report: where it is seen and where it leaves.
        string state = !vis[0] ? "gone" : (hidden(at[0]) ? "behind rim" : (screen(at[0]).x < 0 || screen(at[0]).x > 1920 || screen(at[0]).y < 0 || screen(at[0]).y > 1080 ? "off frame" : "seen"));
        if (state == "seen") { if (firstSeen[i] < 0f) firstSeen[i] = t; lastSeen[i] = t; }
        if (state != lastState[i]) { pops.Append("  capsule").Append(i).Append(' ').Append(state).Append(" at t=").Append(t.ToString("0.00", inv)).Append(vis[0] ? " (" + px[0].ToString("0.0", inv) + "px, " + (UnityEngine.Vector3.Distance(at[0], centre) - radius).ToString("0.0", inv) + " m up)" : "").Append('\n'); lastState[i] = state; }
        // Heading: the axis in the world, and the direction of the path (world steps of 1 cm or more).
        if (vis[0])
        {
            UnityEngine.Vector3 up = root.up;
            UnityEngine.Vector3 sc = root.position;
            if (hasLast[i])
            {
                float turn = UnityEngine.Vector3.Angle(lastUp[i], up) / dt;
                if (turn > maxTurn[i]) maxTurn[i] = turn;
                if (turn > 720f) { snaps[i]++; pops.Append("SNAP axis").Append(i).Append(" t=").Append(t.ToString("0.000", inv)).Append(' ').Append(turn.ToString("0", inv)).Append(" deg/s\n"); }
                UnityEngine.Vector3 dir = sc - lastScreen[i];
                float stepLength = dir.magnitude;
                if (stepLength > 0.01f)
                {
                    dir.Normalize();
                    if (hasDir[i]) { float pathTurn = UnityEngine.Vector3.Angle(lastDir[i], dir) / dt; if (pathTurn > maxPathTurn[i]) maxPathTurn[i] = pathTurn; if (pathTurn > 720f) { snaps[i]++; pops.Append("SNAP path").Append(i).Append(" t=").Append(t.ToString("0.000", inv)).Append(' ').Append(pathTurn.ToString("0", inv)).Append(" deg/s step ").Append(stepLength.ToString("0.000", inv)).Append("m\n"); } }
                    lastDir[i] = dir; hasDir[i] = true;
                }
            }
            lastUp[i] = up; lastScreen[i] = sc; hasLast[i] = true;
        }
        else { hasLast[i] = false; hasDir[i] = false; }
    }
}
csv.Dispose();
var sb = new System.Text.StringBuilder();
sb.Append("shot ").Append(shot).Append(' ').Append(spec.Action).Append(' ').Append(length.ToString("0.00", inv)).Append("s capsules ").Append(n).Append('\n');
var release = drop.GetType().GetMethod("Release");
for (int i = 0; i < n; i++)
{
    sb.Append("capsule").Append(i).Append(" release ").Append(((float)release.Invoke(drop, new object[] { i })).ToString("0.00", inv))
      .Append(" seen ").Append(firstSeen[i].ToString("0.00", inv)).Append("..").Append(lastSeen[i].ToString("0.00", inv))
      .Append(" max axis turn ").Append(maxTurn[i].ToString("0", inv)).Append(" deg/s, max path turn ").Append(maxPathTurn[i].ToString("0", inv)).Append(" deg/s, snaps ").Append(snaps[i]).Append('\n');
}
foreach (var c in colliders) UnityEngine.Object.DestroyImmediate(c);
sb.Append(pops).Append("pops ").Append(popCount);
sb.Append(" jumps ").Append(jumps).Append(" booster steps inside the planet ").Append(inside).Append(" (deepest ").Append(deepest.ToString("0.00", inv)).Append(" m)");
System.IO.File.WriteAllText("Logs/r59/salvo_probe.txt", sb.ToString());
return sb.ToString().Replace("\n", " | ");
