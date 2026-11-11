// Round 39: the drill's deck against the slope it stands on. Seeks the core shot (everything built) and prints the
// drill root, the vertex count per 10 cm band of height above the root (to find the deck's underside), the deck's
// footprint and the landscape's height under its corners and middle.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var level = catalog.Story.OpeningCutscene.FieldLevel(catalog);
var field = new Game.Levels.TerrainField(level, new Game.Levels.RoadShape(level));
player.Seek(7, 0.3f);
UnityEditor.EditorApplication.Step();
player.Seek(7, 0.3f);
UnityEngine.Transform drill = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) if (t.name == "Drill" && t.parent != null && t.parent.name == "Set_Field") drill = t;
if (drill == null) return "no drill";
var sb = new System.Text.StringBuilder();
sb.Append("root ").Append(drill.position.ToString("F2")).Append(" yaw ").Append(drill.eulerAngles.y.ToString("0")).Append(" scale ").Append(drill.localScale.x.ToString("0.00")).Append('\n');
var bands = new int[20]; float minX = 99, maxX = -99, minZ = 99, maxZ = -99;
foreach (var r in drill.GetComponentsInChildren<UnityEngine.Renderer>(false))
{
    if (!(r is UnityEngine.SkinnedMeshRenderer sk)) continue;
    sb.Append("skinned ").Append(r.name).Append('\n');
    var mesh = new UnityEngine.Mesh(); sk.BakeMesh(mesh, true);
    foreach (var v in mesh.vertices)
    {
        var w = sk.transform.localToWorldMatrix.MultiplyPoint3x4(v);
        var l = drill.InverseTransformPoint(w) * drill.localScale.x;
        int b = UnityEngine.Mathf.FloorToInt((l.y + 0.5f) / 0.1f);
        if (b >= 0 && b < 20) bands[b]++;
        if (l.y > -0.05f && l.y < 0.6f) { minX = UnityEngine.Mathf.Min(minX, l.x); maxX = UnityEngine.Mathf.Max(maxX, l.x); minZ = UnityEngine.Mathf.Min(minZ, l.z); maxZ = UnityEngine.Mathf.Max(maxZ, l.z); }
    }
}
for (int b = 0; b < 20; b++) sb.Append((b * 0.1f - 0.5f).ToString("0.0")).Append(':').Append(bands[b]).Append(' ');
sb.Append("\nfootprint x ").Append(minX.ToString("0.00")).Append("..").Append(maxX.ToString("0.00")).Append(" z ").Append(minZ.ToString("0.00")).Append("..").Append(maxZ.ToString("0.00")).Append('\n');
foreach (var c in new[] { new UnityEngine.Vector2(minX, minZ), new UnityEngine.Vector2(maxX, minZ), new UnityEngine.Vector2(minX, maxZ), new UnityEngine.Vector2(maxX, maxZ), UnityEngine.Vector2.zero })
{
    var w = drill.position + drill.rotation * new UnityEngine.Vector3(c.x, 0f, c.y);
    sb.Append("ground at ").Append(c.ToString("F2")).Append(" = ").Append((field.HeightAt(w.x, w.z) - drill.position.y).ToString("0.00")).Append('\n');
}
return sb.ToString();
