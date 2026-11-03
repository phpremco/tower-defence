// Lists every enabled light in the scene (type, intensity, forward, culling mask) and the ambient settings.
var sb = new System.Text.StringBuilder();
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>())
{
    if (!l.isActiveAndEnabled) continue;
    sb.Append(l.name + "/" + l.type + " i=" + l.intensity.ToString("0.00") + " fwd=" + l.transform.forward.ToString("0.00") + " mask=" + l.cullingMask + "; ");
}
sb.Append(" sun=" + (UnityEngine.RenderSettings.sun != null ? UnityEngine.RenderSettings.sun.name : "none"));
sb.Append(" amb=" + UnityEngine.RenderSettings.ambientSkyColor + " refl=" + UnityEngine.RenderSettings.reflectionIntensity);
return sb.ToString();
