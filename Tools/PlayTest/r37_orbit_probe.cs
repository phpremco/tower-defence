// Round 37: does anything in the orbit shot pass through the mothership? Seeks the cutscene (the shot index in
// Tools/PlayTest/r37_probe_arg.txt, default 2) in fine steps and, with temporary mesh colliders on the ship's meshes, tests
// every visible plume's axis (mouth to tip), each capsule and booster body, the strobe and steam/smoke sprite centres
// (read from the sprite batches) against the hull. Prints the shot list once and every hit with its moment.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var sb = new System.Text.StringBuilder();
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var shots = catalog.Story.OpeningCutscene.Shots;
for (int s = 0; s < shots.Count; s++) sb.Append(s).Append(':').Append(shots[s].Action).Append(' ').Append(shots[s].CameraLength.ToString("0.0")).Append("s ");
sb.Append('\n');
int shot = 2;
if (System.IO.File.Exists("Tools/PlayTest/r37_probe_arg.txt")) shot = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r37_probe_arg.txt").Trim());
player.Seek(shot, 0.01f);
var ship = UnityEngine.GameObject.Find("Mothership");
if (ship == null) return sb + "no ship";
var colliders = new System.Collections.Generic.List<UnityEngine.Collider>();
foreach (var r in ship.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    UnityEngine.Mesh mesh = null;
    var mf = r.GetComponent<UnityEngine.MeshFilter>();
    if (mf != null) mesh = mf.sharedMesh;
    var smr = r as UnityEngine.SkinnedMeshRenderer;
    if (smr != null) { mesh = new UnityEngine.Mesh(); smr.BakeMesh(mesh, true); }
    if (mesh == null || r.name.StartsWith("RunningLight")) continue;
    var mc = r.gameObject.AddComponent<UnityEngine.MeshCollider>();
    mc.sharedMesh = mesh;
    colliders.Add(mc);
}
sb.Append("ship colliders ").Append(colliders.Count).Append(" bounds ");
var shipBounds = new UnityEngine.Bounds(ship.transform.position, UnityEngine.Vector3.zero);
foreach (var c in colliders) shipBounds.Encapsulate(c.bounds);
sb.Append(shipBounds.center.ToString("0.0")).Append(' ').Append(shipBounds.size.ToString("0.0")).Append('\n');
UnityEngine.Physics.SyncTransforms();
var batchField = typeof(Game.UI.SpriteBatch).GetField("_centres", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var countField = typeof(Game.UI.SpriteBatch).GetField("_count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var shapesField = typeof(Game.UI.SpriteBatch).GetField("_shapes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var colourField = typeof(Game.UI.SpriteBatch).GetField("_colours", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var set = player.CurrentSet;
var capsField = set.GetType().GetField("_capsules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
object drop = capsField != null ? capsField.GetValue(set) : null;
var batches = new System.Collections.Generic.List<(string, Game.UI.SpriteBatch)>();
if (drop != null)
{
    batches.Add(("smoke", (Game.UI.SpriteBatch)drop.GetType().GetField("_smoke", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(drop)));
    batches.Add(("glints", (Game.UI.SpriteBatch)drop.GetType().GetField("_glints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(drop)));
}
int hits = 0; var seen = new System.Collections.Generic.HashSet<string>();
var cam = UnityEngine.Camera.main;
for (int step = 1; step < 200; step++)
{
    float p = step / 200f;
    player.Seek(shot, p);
    UnityEngine.Physics.SyncTransforms();
    // Plumes: the outer cone's axis from its mouth to its tip (its scale z is the reach).
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!t.gameObject.activeInHierarchy) continue;
        string n = t.name;
        if (n.EndsWith("Plume") || n.EndsWith("Core"))
        {
            UnityEngine.Vector3 a = t.position, b = t.position + t.forward * t.localScale.z;
            float r = t.localScale.x;
            // Start a little past the mouth: the ship's own engines sit in their bells.
            UnityEngine.Vector3 from = a + t.forward * UnityEngine.Mathf.Min(0.4f, t.localScale.z * 0.1f);
            bool hit = UnityEngine.Physics.Linecast(from, b, out var info) || UnityEngine.Physics.Linecast(b, from, out info);
            if (!hit) hit = UnityEngine.Physics.CheckCapsule(from, b, r * 0.5f);
            if (hit) { hits++; if (seen.Add(n + (p * 20).ToString("0"))) sb.Append("HIT plume ").Append(n).Append(" p=").Append(p.ToString("0.000")).Append(" reach ").Append(t.localScale.z.ToString("0.0")).Append(" r ").Append(r.ToString("0.00")).Append('\n'); }
        }
        else if (!n.Contains("Smoke") && !n.Contains("Sparks") && (n.StartsWith("Capsule") && t.parent != null && !t.parent.name.StartsWith("Capsule")) || (n.StartsWith("Booster") && !n.Contains("Strobe") && !n.EndsWith("Flare") && !n.EndsWith("Shimmer") && t.childCount > 0))
        {
            var rs = t.GetComponentsInChildren<UnityEngine.Renderer>();
            if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var rr in rs) bb.Encapsulate(rr.bounds);
            if (UnityEngine.Physics.CheckBox(bb.center, bb.extents * 0.7f))
            { hits++; if (seen.Add(n + (p * 20).ToString("0"))) sb.Append("HIT body ").Append(n).Append(" p=").Append(p.ToString("0.000")).Append(" size ").Append(bb.size.ToString("0.0")).Append('\n'); }
        }
    }
    foreach (var (bn, batch) in batches)
    {
        if (batch == null) continue;
        var centres = (UnityEngine.Vector3[])batchField.GetValue(batch);
        var shapes = (UnityEngine.Vector4[])shapesField.GetValue(batch);
        var colours = (UnityEngine.Color[])colourField.GetValue(batch);
        int count = (int)countField.GetValue(batch);
        for (int i = 0; i < count; i++)
        {
            int v = i * 4;
            if (colours[v].a < 0.03f && colours[v].maxColorComponent < 0.05f) continue;
            float size = shapes[v].x * 0.5f;
            // A sprite behind the hull from the camera is hidden by depth; one whose middle is inside the hull or that lies
            // in front of the hull but crosses it is what reads as passing through.
            if (UnityEngine.Physics.CheckSphere(centres[v], UnityEngine.Mathf.Max(0.05f, size)))
            { hits++; string key = bn + (p * 40).ToString("0"); if (seen.Add(key)) sb.Append("HIT sprite ").Append(bn).Append('#').Append(i).Append(" p=").Append(p.ToString("0.000")).Append(" size ").Append(size.ToString("0.00")).Append(" a ").Append(colours[v].a.ToString("0.00")).Append('\n'); }
        }
    }
}
foreach (var c in colliders) UnityEngine.Object.DestroyImmediate(c);
sb.Append("total hits ").Append(hits);
return sb.ToString();
