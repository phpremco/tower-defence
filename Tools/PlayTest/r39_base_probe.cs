// Round 39: how the field's built things meet the ground. Seeks "<shot> <progress>" from Tools/PlayTest/r39_base_arg.txt
// and, per top-level piece of the field set (drill, capsule, base modules, gates, beacon, props), bakes its opaque meshes
// and holds every vertex against the landscape's own height: the lowest clearance (negative: below the ground), the
// highest clearance among its bottom 3 cm (a positive value there means the underside floats) and the ground's spread
// under that underside.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var level = catalog.Story.OpeningCutscene.FieldLevel(catalog);
var field = new Game.Levels.TerrainField(level, new Game.Levels.RoadShape(level));
var parts = System.IO.File.ReadAllText("Tools/PlayTest/r39_base_arg.txt").Trim().Split(' ');
int shot = int.Parse(parts[0]); float p = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, p);
UnityEditor.EditorApplication.Step();
player.Seek(shot, p);
UnityEngine.Transform set = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) if (t.name == "Set_Field") set = t;
if (set == null) return "no field set";
var sb = new System.Text.StringBuilder();
foreach (UnityEngine.Transform top in set)
{
    if (!top.gameObject.activeInHierarchy || top.name == "Terrain" || top.name == "Swarm" || top.name == "Mercer" || top.name.StartsWith("Hauler") || top.name.StartsWith("Builder")) continue;
    var renderers = top.GetComponentsInChildren<UnityEngine.Renderer>(false);
    var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var r in renderers)
    {
        if (!r.enabled || r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.LineRenderer || r.sharedMaterial == null || r.sharedMaterial.renderQueue >= 2500) continue;
        if (r is UnityEngine.SkinnedMeshRenderer sk) { var mesh = new UnityEngine.Mesh(); sk.BakeMesh(mesh, true); foreach (var v in mesh.vertices) verts.Add(sk.transform.localToWorldMatrix.MultiplyPoint3x4(v)); }
        else { var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount > 200000) continue; var m = r.transform.localToWorldMatrix; foreach (var v in mf.sharedMesh.vertices) verts.Add(m.MultiplyPoint3x4(v)); }
    }
    if (verts.Count == 0) continue;
    float minY = 99f; foreach (var w in verts) minY = UnityEngine.Mathf.Min(minY, w.y);
    float minClear = 99f, bottomMaxClear = -99f, gMin = 99f, gMax = -99f; int sunk = 0;
    foreach (var w in verts)
    {
        float g = field.HeightAt(w.x, w.z); float c = w.y - g;
        minClear = UnityEngine.Mathf.Min(minClear, c); if (c < -0.03f) sunk++;
        if (w.y < minY + 0.03f) { bottomMaxClear = UnityEngine.Mathf.Max(bottomMaxClear, c); gMin = UnityEngine.Mathf.Min(gMin, g); gMax = UnityEngine.Mathf.Max(gMax, g); }
    }
    sb.Append(top.name).Append(": verts ").Append(verts.Count).Append(" minClear ").Append(minClear.ToString("0.00")).Append(" sunk ").Append(sunk).Append(" underside floats up to ").Append(bottomMaxClear.ToString("0.00")).Append(" ground spread ").Append((gMax - gMin).ToString("0.00")).Append(" bottom y ").Append(minY.ToString("0.00")).Append('\n');
}
System.IO.File.WriteAllText("Tools/PlayTest/r39_base_probe.txt", sb.ToString());
return sb.Length > 3000 ? sb.ToString().Substring(0, 3000) : sb.ToString();
