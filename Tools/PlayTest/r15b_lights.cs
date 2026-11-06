// Round 15b: every enabled light in the scene (name under its root, type, intensity, shadows, culling mask) and the scene's
// sun, to see which display-stage lights reach the field.
var sb = new System.Text.StringBuilder();
sb.Append("sun=").Append(UnityEngine.RenderSettings.sun != null ? UnityEngine.RenderSettings.sun.transform.root.name + "/" + UnityEngine.RenderSettings.sun.name : "none").Append(" | ");
int directional = 0;
foreach (var light in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
{
    if (!light.enabled || light.type != UnityEngine.LightType.Directional) { continue; }
    directional++;
    sb.Append(light.transform.root.name).Append('/').Append(light.name).Append(' ').Append(light.intensity.ToString("0.00")).Append(' ').Append(light.shadows).Append(" mask ").Append(light.cullingMask == -1 ? "all" : light.cullingMask.ToString("X")).Append(" | ");
}
int previews = UnityEngine.Object.FindObjectsByType<Game.UI.ModelPreview>(UnityEngine.FindObjectsSortMode.None).Length;
return "directional " + directional + ", previews " + previews + " | " + sb;
