// Round 39: what lies under a pixel. Tools/PlayTest/r39_pixel_arg.txt as for r39_pixel_probe.cs ("<shot> <progress>", then
// "x y" image pixels of a 1920x1080 still). Marches the shot camera's ray over the landscape's height field to the ground
// and lists every instanced decor piece and renderer whose bounds reach within 0.8 m of that point.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var level = catalog.Story.OpeningCutscene.FieldLevel(catalog);
var field = new Game.Levels.TerrainField(level, new Game.Levels.RoadShape(level));
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/r39_pixel_arg.txt");
var head = lines[0].Trim().Split(' ');
int shot = int.Parse(head[0]); float progress = float.Parse(head[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, progress);
UnityEditor.EditorApplication.Step();
player.Seek(shot, progress);
var camera = UnityEngine.Camera.main;
float aspect = camera.aspect; camera.aspect = 1920f / 1080f;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
for (int l = 1; l < lines.Length; l++)
{
    var p = lines[l].Trim().Split(' '); if (p.Length != 2) continue;
    var ray = camera.ViewportPointToRay(new UnityEngine.Vector3(int.Parse(p[0]) / 1920f, 1f - int.Parse(p[1]) / 1080f, 0f));
    UnityEngine.Vector3 hit = ray.origin; bool found = false;
    for (float t = 0.5f; t < 200f; t += 0.02f) { var q = ray.GetPoint(t); if (q.y <= field.HeightAt(q.x, q.z)) { hit = q; found = true; break; } }
    sb.Append("pixel ").Append(p[0]).Append(',').Append(p[1]).Append(found ? " ground " + hit.ToString("F2") : " no ground").Append(" slope ").Append(field.SlopeAt(hit.x, hit.z).ToString("0.00")).Append('\n');
    var probe = new UnityEngine.Bounds(hit, UnityEngine.Vector3.one * 1.6f);
    foreach (var inst in UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None))
    {
        var batches = (System.Collections.IList)typeof(Game.Levels.EnvironmentInstancer).GetField("_batches", flags).GetValue(inst);
        foreach (var b in batches)
        {
            var bt = b.GetType(); var mesh = (UnityEngine.Mesh)bt.GetField("Mesh").GetValue(b); var all = (UnityEngine.Matrix4x4[])bt.GetField("Matrices").GetValue(b);
            var mat = (UnityEngine.Material)bt.GetField("Material").GetValue(b);
            if (mesh == null || all == null) continue;
            foreach (var m in all) { var c = m.MultiplyPoint3x4(mesh.bounds.center); if (probe.Contains(c)) sb.Append("   I ").Append(mesh.name).Append(" [").Append(mat != null ? mat.shader.name : "-").Append("] at ").Append(c.ToString("F2")).Append('\n'); }
        }
    }
    foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!r.enabled || !r.gameObject.activeInHierarchy || r.bounds.size.magnitude > 20f) continue;
        if (r.bounds.Intersects(probe)) { string s = r.name; for (var q = r.transform.parent; q != null && q.name != "Set_Field"; q = q.parent) s = q.name + "/" + s; sb.Append("   R ").Append(s).Append(" [").Append(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-").Append("] ").Append(r.bounds.center.ToString("F2")).Append(" size ").Append(r.bounds.size.ToString("F2")).Append('\n'); }
    }
}
camera.aspect = aspect;
System.IO.File.WriteAllText("Tools/PlayTest/r39_ray.txt", sb.ToString());
return sb.Length > 3000 ? sb.ToString().Substring(0, 3000) : sb.ToString();
