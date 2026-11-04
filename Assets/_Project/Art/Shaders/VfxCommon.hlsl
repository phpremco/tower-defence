// Shared helpers of the effect shaders (VfxParticle, VfxSmoke, VfxDistortion): flipbook sampling with frame blending,
// a static atlas cell for lines and quads, and the soft-particle fade against the scene depth. The camera is
// orthographic in a level, so the scene depth is linear there and LinearEyeDepth would be wrong.
#ifndef GAME_VFX_COMMON_INCLUDED
#define GAME_VFX_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// The UV of one atlas cell: cells run row by row from the top left (Texture Sheet Animation order).
float2 VfxAtlasUv(float2 uv, float frame, float2 grid)
{
    float cells = max(1.0, grid.x * grid.y);
    float index = fmod(floor(max(frame, 0.0)), cells);
    float column = fmod(index, grid.x);
    float row = floor(index / grid.x);
    return (float2(column, grid.y - 1.0 - row) + saturate(uv)) / grid;
}

// Eye depth of the opaque scene under a pixel, for a perspective or an orthographic camera.
float VfxSceneEyeDepth(float2 screenUv)
{
    float raw = SampleSceneDepth(screenUv);
    if (unity_OrthoParams.w > 0.5)
    {
#if UNITY_REVERSED_Z
        raw = 1.0 - raw;
#endif
        return lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
    }

    return LinearEyeDepth(raw, _ZBufferParams);
}

// 0 where the particle touches the ground or a model, 1 a fade distance in front of it.
half VfxSoftFade(float4 screenPos, float eyeDepth, float fadeDistance)
{
    float2 screenUv = screenPos.xy / max(screenPos.w, 1e-5);
    float scene = VfxSceneEyeDepth(screenUv);
    return (half)saturate((scene - eyeDepth) / max(fadeDistance, 1e-3));
}

#endif
