// Round 39: who draws these pixels? Tools/PlayTest/r39_pixel_arg.txt: first line "<shot> <progress>", then one "x y" per
// line (image coordinates of a 1920x1080 still, origin top left). Renders the moment, then hides each renderer whose
// screen box covers a pixel (and each EnvironmentInstancer, which draws without renderers) one at a time and reports
// the ones whose removal changes a 5x5 patch round the pixel the most.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/r39_pixel_arg.txt");
var head = lines[0].Trim().Split(' ');
int shot = int.Parse(head[0]); float progress = float.Parse(head[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, progress);
UnityEditor.EditorApplication.Step();
player.Seek(shot, progress);
var camera = UnityEngine.Camera.main;
if (System.IO.File.Exists("Tools/PlayTest/r39_pixel_cam.txt")) { var cw = System.Array.ConvertAll(System.IO.File.ReadAllText("Tools/PlayTest/r39_pixel_cam.txt").Trim().Split(' '), s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)); var cpos = new UnityEngine.Vector3(cw[0], cw[1], cw[2]); camera.transform.SetPositionAndRotation(cpos, UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(cw[3], cw[4], cw[5]) - cpos)); camera.fieldOfView = cw[6]; }
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
float aspect = camera.aspect;
System.Func<UnityEngine.Color32[]> render = () =>
{
    camera.targetTexture = texture; camera.aspect = 1920f / 1080f; camera.Render(); camera.targetTexture = null; camera.aspect = aspect;
    UnityEngine.RenderTexture.active = texture; image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); UnityEngine.RenderTexture.active = null;
    return image.GetPixels32();
};
var pixels = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
for (int i = 1; i < lines.Length; i++) { var p = lines[i].Trim().Split(' '); if (p.Length == 2) pixels.Add(new UnityEngine.Vector2Int(int.Parse(p[0]), 1079 - int.Parse(p[1]))); }
var baseline = render();
System.Func<UnityEngine.Color32[], UnityEngine.Vector2Int, float> diff = (img, px) =>
{
    float d = 0f;
    for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
    {
        int k = (px.y + dy) * 1920 + px.x + dx; if (k < 0 || k >= img.Length) continue;
        d += UnityEngine.Mathf.Abs(img[k].r - baseline[k].r) + UnityEngine.Mathf.Abs(img[k].g - baseline[k].g) + UnityEngine.Mathf.Abs(img[k].b - baseline[k].b);
    }
    return d / 25f;
};
var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Transform, string> path = t => { string s = t.name; for (var q = t.parent; q != null && q.name != "Set_Field"; q = q.parent) s = q.name + "/" + s; return s; };
foreach (var px in pixels)
{
    var results = new System.Collections.Generic.List<(float, string)>();
    var pixel = new UnityEngine.Vector2(px.x / 1920f * camera.pixelWidth, px.y / 1080f * camera.pixelHeight);
    foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
        var b = r.bounds; var min = new UnityEngine.Vector2(9999, 9999); var max = new UnityEngine.Vector2(-9999, -9999); bool front = false;
        for (int c = 0; c < 8; c++)
        {
            var corner = new UnityEngine.Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
            var s = camera.WorldToScreenPoint(corner); if (s.z > 0) front = true;
            min = UnityEngine.Vector2.Min(min, s); max = UnityEngine.Vector2.Max(max, s);
        }
        if (!front || pixel.x < min.x || pixel.x > max.x || pixel.y < min.y || pixel.y > max.y) continue;
        r.enabled = false; var img = render(); r.enabled = true;
        float d = diff(img, px); if (d > 2f) results.Add((d, "R " + path(r.transform) + " [" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name + " q" + r.sharedMaterial.renderQueue : "-") + "] y " + b.min.y.ToString("0.00") + ".." + b.max.y.ToString("0.00")));
    }
    foreach (var inst in UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!inst.enabled) continue;
        inst.enabled = false; var img = render(); inst.enabled = true;
        float d = diff(img, px); if (d > 2f) results.Add((d, "I " + path(inst.transform)));
    }
    results.Sort((a, c) => c.Item1.CompareTo(a.Item1));
    sb.Append("pixel ").Append(px.x).Append(',').Append(1079 - px.y).Append(':');
    for (int i = 0; i < UnityEngine.Mathf.Min(6, results.Count); i++) sb.Append(" | ").Append(results[i].Item1.ToString("0")).Append(' ').Append(results[i].Item2);
    sb.Append('\n');
}
texture.Release();
System.IO.File.WriteAllText("Tools/PlayTest/r39_pixel_probe.txt", sb.ToString());
return sb.ToString();
