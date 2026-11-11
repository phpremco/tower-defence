// Round 39: do the parked haulers and the landed builders stand on the ground they are drawn on? For each moment in
// Tools/PlayTest/r39_ground_arg.txt ("<shot> <progress>" per line) every Hauler*/Builder* holder's meshes are baked in
// their pose and each vertex is held against the landscape's own height (TerrainField, the field the ground mesh is
// built from). Prints per drone the lowest clearance (negative: below the ground), how many vertices sink more than
// 2 cm, and the ground's spread under its footprint.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var level = catalog.Story.OpeningCutscene.FieldLevel(catalog);
var field = new Game.Levels.TerrainField(level, new Game.Levels.RoadShape(level));
var sb = new System.Text.StringBuilder();
foreach (var line in System.IO.File.ReadAllLines("Tools/PlayTest/r39_ground_arg.txt"))
{
    var parts = line.Trim().Split(' ');
    if (parts.Length < 2) continue;
    int shot = int.Parse(parts[0]); float p = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
    player.Seek(shot, p);
    UnityEditor.EditorApplication.Step();
    player.Seek(shot, p);
    sb.Append("shot ").Append(shot).Append(" p ").Append(p.ToString("0.00")).Append(":");
    int worst = 0;
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!(t.name.StartsWith("Hauler") || t.name.StartsWith("Builder")) || !t.gameObject.activeInHierarchy || t.parent == null || t.parent.name != "Set_Field") continue;
        float minClear = 99f, gMin = 99f, gMax = -99f; int sunk = 0; float lowest = 99f;
        foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>(false))
        {
            if (!r.enabled || r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.LineRenderer) continue;
            UnityEngine.Vector3[] v; UnityEngine.Matrix4x4 m;
            if (r is UnityEngine.SkinnedMeshRenderer sk) { var mesh = new UnityEngine.Mesh(); sk.BakeMesh(mesh, true); v = mesh.vertices; m = sk.transform.localToWorldMatrix; }
            else { var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue; v = mf.sharedMesh.vertices; m = r.transform.localToWorldMatrix; }
            for (int i = 0; i < v.Length; i++)
            {
                var w = m.MultiplyPoint3x4(v[i]);
                float g = field.HeightAt(w.x, w.z);
                float c = w.y - g;
                if (c < minClear) minClear = c;
                if (w.y < lowest) lowest = w.y;
                if (c < -0.02f) sunk++;
                if (c < 0.3f) { gMin = UnityEngine.Mathf.Min(gMin, g); gMax = UnityEngine.Mathf.Max(gMax, g); }
            }
        }
        if (minClear > 5f) continue;
        if (minClear < -0.02f) worst++;
        sb.Append(" | ").Append(t.name).Append(" clear ").Append(minClear.ToString("0.000")).Append(" sunk ").Append(sunk).Append(" groundSpread ").Append((gMax - gMin).ToString("0.00")).Append(" y ").Append(t.position.y.ToString("0.00"));
    }
    sb.Append(" || sinking ").Append(worst).Append('\n');
}
System.IO.File.WriteAllText("Tools/PlayTest/r39_ground_probe.txt", sb.ToString());
return sb.Length > 3000 ? sb.ToString().Substring(0, 3000) : sb.ToString();
