// Tuning helper for the planet shaders: applies "shader property value [g b a]" lines from Tools/PlayTest/fb1_tune.txt to every live
// material on that shader (Planet, PlanetClouds, PlanetAtmosphere, SpaceSky), then reports the current sun of each planet surface.
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/fb1_tune.txt");
var inv = System.Globalization.CultureInfo.InvariantCulture;
var report = new System.Text.StringBuilder();
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
{
    var m = renderer.sharedMaterial;
    if (m == null || m.shader == null) continue;
    foreach (var raw in lines)
    {
        var p = raw.Trim().Split(' ');
        if (p.Length < 3 || p[0].StartsWith("#")) continue;
        if (m.shader.name != "Game/" + p[0]) continue;
        if (p.Length >= 5) m.SetColor(p[1], new UnityEngine.Color(float.Parse(p[2], inv), float.Parse(p[3], inv), float.Parse(p[4], inv), p.Length > 5 ? float.Parse(p[5], inv) : 1f));
        else m.SetFloat(p[1], float.Parse(p[2], inv));
    }
    if (m.shader.name == "Game/Planet") report.Append(renderer.transform.parent.name + " sun " + m.GetVector("_SunDirection") + " col " + m.GetColor("_SunColor") + "; ");
}
return report.ToString();
