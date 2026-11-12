// Round 51: the opening cutscene's SpaceKit glows and lines in three looks, for a before/after on the same frame. Every
// active renderer whose material is on Game/VfxParticle with _SpriteCoverage set by SpaceKit is switched to the look named
// in Tools/PlayTest/r51_variant.txt:
//   r51  coverage 0 (this round: the sprite's colour alone shapes it);
//   r50  coverage 1 (round 50: the sprite's alpha applied on top);
//   r49  the round-49 material, rebuilt here: the URP default particle material set additive (One One), the sprite as base map
//        and the colour its three channel colours make, times the intensity and turned back to gamma, as base colour (editor only; that material is null in a build).
// The originals are kept in the AppDomain so "r51" after "r49" puts them back. Reports how many renderers it switched.
var variant = System.IO.File.ReadAllText("Tools/PlayTest/r51_variant.txt").Trim();
var domain = System.AppDomain.CurrentDomain;
var kept = domain.GetData("r51_space") as System.Collections.Generic.Dictionary<UnityEngine.Renderer, UnityEngine.Material>;
if (kept == null) { kept = new System.Collections.Generic.Dictionary<UnityEngine.Renderer, UnityEngine.Material>(); domain.SetData("r51_space", kept); }
int coverage = UnityEngine.Shader.PropertyToID("_SpriteCoverage");
int switched = 0;
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!kept.TryGetValue(renderer, out var original))
    {
        original = renderer.sharedMaterial;
        if (original == null || original.shader.name != "Game/VfxParticle" || !original.HasProperty(coverage) || original.GetFloat(coverage) > 0.5f) { continue; }
        kept[renderer] = original;
    }

    switch (variant)
    {
        case "r49":
            var old = new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial);
            old.SetFloat("_Surface", 1f);
            old.SetFloat("_Blend", 2f);
            old.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            old.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            old.SetFloat("_ZWrite", 0f);
            old.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            old.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            old.SetTexture("_BaseMap", original.GetTexture("_MainTex"));
            var tint = new UnityEngine.Color(original.GetColor("_CoreColor").r, original.GetColor("_GlowColor").g, original.GetColor("_ThirdColor").b);
            old.SetColor("_BaseColor", (tint * original.GetFloat("_Intensity")).gamma);
            renderer.sharedMaterial = old;
            break;
        case "r50":
            renderer.sharedMaterial = original;
            original.SetFloat(coverage, 1f);
            break;
        default:
            renderer.sharedMaterial = original;
            original.SetFloat(coverage, 0f);
            break;
    }

    switched++;
}

return variant + " on " + switched + " renderers";
