// Round 88a: what lights the level in this editor (quality level, ambient, lights), to compare a stage still with the game.
var sb = new System.Text.StringBuilder();
sb.Append("quality ").Append(UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()]);
sb.Append(" ambient ").Append(UnityEngine.RenderSettings.ambientMode).Append(' ').Append(UnityEngine.RenderSettings.ambientLight).Append(" x").Append(UnityEngine.RenderSettings.ambientIntensity);
sb.Append(" device ").Append(UnityEngine.SystemInfo.graphicsDeviceType).Append(" colorspace ").Append(UnityEngine.QualitySettings.activeColorSpace);
foreach (var light in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
{
    if (light.type == UnityEngine.LightType.Directional) sb.Append(" | dir ").Append(light.name).Append(' ').Append(light.intensity.ToString("0.00")).Append(' ').Append(light.color).Append(light.enabled ? "" : " off");
}
var cam = UnityEngine.Camera.main;
var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
sb.Append(" | post ").Append(data != null && data.renderPostProcessing).Append(" hdr ").Append(cam.allowHDR).Append(" ortho ").Append(cam.orthographicSize.ToString("0.0"));
foreach (var volume in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None))
{
    sb.Append(" | volume ").Append(volume.name).Append(" w").Append(volume.weight);
    if (volume.profile != null) foreach (var c in volume.profile.components) sb.Append(' ').Append(c.GetType().Name).Append(c.active ? "" : "(off)");
}
return sb.ToString();
