// Reports how the pipeline's default particle material blends, so view code can tell additive from alpha blending.
var m = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
var sb = new System.Text.StringBuilder(m.shader.name + " queue=" + m.renderQueue);
foreach (var p in new[] { "_Blend", "_SrcBlend", "_DstBlend", "_ZWrite", "_Surface", "_ColorMode", "_Cull" })
{
    if (m.HasProperty(p)) sb.Append(" " + p + "=" + m.GetFloat(p));
}
sb.Append(" keywords=" + string.Join(",", m.shaderKeywords));
sb.Append(" tex=" + (m.GetTexture("_BaseMap") ? m.GetTexture("_BaseMap").name : "none"));
return sb.ToString();
