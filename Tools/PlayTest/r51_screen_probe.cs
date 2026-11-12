// Round 51: which renderers cover a region of the 1920x1080 still (r50_still.cs framing): Tools/PlayTest/r51_region.txt holds
// "x0 y0 x1 y1" in still pixels, top-left origin. Lists every enabled renderer whose screen bounds overlap it, with its path,
// type, material and world bounds, so a stray line or plane in a still can be named.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/r51_region.txt").Trim().Split(' ');
var ci = System.Globalization.CultureInfo.InvariantCulture;
float x0 = float.Parse(spec[0], ci), y0 = float.Parse(spec[1], ci), x1 = float.Parse(spec[2], ci), y1 = float.Parse(spec[3], ci);
var camera = UnityEngine.Camera.main;
float aspect = camera.aspect;
camera.aspect = 1920f / 1080f;
var sb = new System.Text.StringBuilder();
int hits = 0;
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) { continue; }
    var b = renderer.bounds;
    float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
    bool behind = false;
    for (int i = 0; i < 8; i++)
    {
        var corner = new UnityEngine.Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        var v = camera.WorldToViewportPoint(corner);
        if (v.z < 0f) { behind = true; }
        float px = v.x * 1920f, py = (1f - v.y) * 1080f;
        minX = UnityEngine.Mathf.Min(minX, px); maxX = UnityEngine.Mathf.Max(maxX, px);
        minY = UnityEngine.Mathf.Min(minY, py); maxY = UnityEngine.Mathf.Max(maxY, py);
    }

    if (behind || maxX < x0 || minX > x1 || maxY < y0 || minY > y1) { continue; }
    // Skip what covers most of the screen (ground, terrain): only effects and props are of interest.
    if ((maxX - minX) * (maxY - minY) > 1920f * 1080f * 0.25f) { continue; }
    hits++;
    var t = renderer.transform;
    string path = t.name;
    for (var p = t.parent; p != null && path.Length < 160; p = p.parent) { path = p.name + "/" + path; }
    var m = renderer.sharedMaterial;
    sb.Append(renderer.GetType().Name).Append(' ').Append(path).Append(" mat=").Append(m != null ? m.name + "(" + m.shader.name + ")" : "none");
    sb.Append(" screen=").Append(minX.ToString("0")).Append(',').Append(minY.ToString("0")).Append('-').Append(maxX.ToString("0")).Append(',').Append(maxY.ToString("0"));
    sb.Append(" world=").Append(b.center.ToString("0.00")).Append(" size=").Append(b.size.ToString("0.00"));
    if (renderer is UnityEngine.LineRenderer line && line.positionCount > 0)
    {
        sb.Append(" p0=").Append(line.GetPosition(0).ToString("0.00")).Append(" pN=").Append(line.GetPosition(line.positionCount - 1).ToString("0.00"));
    }

    sb.Append(" || ");
}

camera.aspect = aspect;
return hits + " renderers: " + sb.ToString();
