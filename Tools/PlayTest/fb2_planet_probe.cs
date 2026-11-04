// Lists every live Game/Planet material: its object, keyword, maps (with sizes), sun, ambient and relief, plus the camera.
var sb = new System.Text.StringBuilder();
var cam = UnityEngine.Camera.main;
if (cam != null) sb.Append("cam " + cam.transform.position + "; ");
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
{
    var m = r.sharedMaterial;
    if (m == null || m.shader == null || m.shader.name != "Game/Planet") continue;
    sb.Append(r.name + " at " + r.bounds.center + " r " + r.bounds.extents.x.ToString("0.0") + " tex " + m.IsKeywordEnabled("_PLANET_TEXTURED"));
    foreach (var n in new[] { "_AlbedoMap", "_NormalMap", "_MaskMap", "_RegionMap", "_LightsMap", "_ColonyMap", "_CloudMap" })
    {
        var t = m.GetTexture(n);
        sb.Append(" " + n + "=" + (t == null ? "null" : t.name + "/" + t.width));
    }
    sb.Append(" sun " + m.GetVector("_SunDirection") + " col " + m.GetColor("_SunColor") + " amb " + m.GetFloat("_Ambient") + " bump " + m.GetFloat("_BumpStrength") + " city " + m.GetColor("_CityColor") + "; ");
}
return sb.ToString();
