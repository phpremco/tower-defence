// The glowing effects painted in Blender (muzzle flashes, bolts, beams, arcs, trails, sparks, impact flipbooks,
// shards, rings). Each texture packs its parts into channels (R core, G glow, B a third part, A coverage); the
// material says what colour each channel glows in, the weapon tints the glow through _Tint, the particle colour
// fades it. Premultiplied output: with _Opacity 0 the effect only adds light, with 1 it also covers what is behind
// (shards, splashes), so one shader serves both. Flipbooks blend between frames; soft particles fade where they
// meet the ground; an optional noise layer scrolls through beams and trails; lines and quads pick a fixed atlas cell.
Shader "Game/VfxParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        [HDR] _CoreColor ("Core (R)", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow (G)", Color) = (1, 1, 1, 1)
        [HDR] _ThirdColor ("Third (B)", Color) = (0, 0, 0, 0)
        [HDR] _Tint ("Weapon tint on the glow", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _Opacity ("Opacity (0 additive, 1 covering)", Range(0, 1)) = 0
        _Grid ("Atlas columns, rows", Vector) = (1, 1, 0, 0)
        _Frame ("Atlas cell (lines and quads)", Float) = 0
        _SoftDistance ("Soft particle distance", Float) = 0.4
        _NoiseTex ("Noise", 2D) = "white" {}
        _NoiseScroll ("Noise scroll (R uv, G uv)", Vector) = (2, 0, -1, 0)
        _NoiseStrength ("Noise strength", Range(0, 1)) = 0
        [Toggle(_FLIPBOOK_BLEND)] _FlipbookBlend ("Flipbook frame blending", Float) = 0
        [Toggle(_SOFT_PARTICLES)] _SoftParticles ("Soft particles", Float) = 0
        [Toggle(_ATLAS_FRAME)] _AtlasFrame ("Fixed atlas cell", Float) = 0
        [Toggle(_NOISE)] _Noise ("Noise layer", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            Name "VfxParticle"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VfxVertex
            #pragma fragment VfxFragment
            #pragma shader_feature_local _FLIPBOOK_BLEND
            #pragma shader_feature_local _SOFT_PARTICLES
            #pragma shader_feature_local _ATLAS_FRAME
            #pragma shader_feature_local _NOISE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "VfxCommon.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _CoreColor;
            half4 _GlowColor;
            half4 _ThirdColor;
            half4 _Tint;
            half _Intensity;
            half _Opacity;
            float4 _Grid;
            float _Frame;
            float _SoftDistance;
            float4 _NoiseTex_ST;
            float4 _NoiseScroll;
            half _NoiseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                // Particles: xy this frame, zw the next frame (UV2 stream); x of the second set is the blend (AnimBlend).
                float4 uv : TEXCOORD0;
                float2 blend : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float blend : TEXCOORD1;
                float2 rawUv : TEXCOORD2;
#if defined(_SOFT_PARTICLES)
                float4 screenPos : TEXCOORD3;
                float eyeDepth : TEXCOORD4;
#endif
            };

            Varyings VfxVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.rawUv = input.uv.xy;
#if defined(_ATLAS_FRAME)
                output.uv = float4(VfxAtlasUv(input.uv.xy, _Frame, max(_Grid.xy, 1.0)), 0.0, 0.0);
#else
                output.uv = float4(TRANSFORM_TEX(input.uv.xy, _MainTex), input.uv.zw);
#endif
                output.blend = input.blend.x;
#if defined(_SOFT_PARTICLES)
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.eyeDepth = -positions.positionVS.z;
#endif
                return output;
            }

            half4 VfxFragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
#if defined(_FLIPBOOK_BLEND)
                half4 next = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.zw);
                texel = lerp(texel, next, (half)saturate(input.blend));
#endif
                half3 light = texel.r * _CoreColor.rgb + texel.g * _GlowColor.rgb * _Tint.rgb + texel.b * _ThirdColor.rgb;
#if defined(_NOISE)
                // Two layers that run along the beam or trail at their own pace, so the energy churns.
                float2 noiseUv = input.rawUv * _NoiseTex_ST.xy;
                half strands = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv + _NoiseScroll.xy * _Time.y).r;
                half crackle = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv * 1.7 + _NoiseScroll.zw * _Time.y).g;
                light *= lerp(1.0h, 0.35h + 1.3h * strands * (0.5h + crackle), _NoiseStrength);
#endif
                half coverage = texel.a * input.color.a;
#if defined(_SOFT_PARTICLES)
                coverage *= VfxSoftFade(input.screenPos, input.eyeDepth, _SoftDistance);
#endif
                half3 colour = light * input.color.rgb * _Intensity * coverage;
                return half4(colour, coverage * _Opacity);
            }
            ENDHLSL
        }
    }
}
