// Lists the renderers whose bounds come within 3 units of the main camera (name, distance, material), to find what fills a shot.
var cam = UnityEngine.Camera.main;
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    float d = UnityEngine.Mathf.Sqrt(r.bounds.SqrDistance(cam.transform.position));
    if (d < 3f) sb.Append(r.name + "(" + (r.transform.parent != null ? r.transform.parent.name : "-") + ") d=" + d.ToString("0.0") + " size=" + r.bounds.size.ToString("0.0") + " | ");
}
return "cam " + cam.transform.position + " bg " + cam.backgroundColor + " fog " + UnityEngine.RenderSettings.fog + " " + UnityEngine.RenderSettings.fogColor + " :: " + sb;
