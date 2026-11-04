// The gameplay markers painted in Blender (ArtSource README round 25b): ground decals, placement and range markers, hero
// posts, route light trails, health bars, projectiles and beams. Each texture is a linear channel pack (R core, G glow,
// B a third part, A coverage); the material says how strongly each channel glows, the view tints the glow per renderer
// through _Tint (a property block). Premultiplied output like Game/VfxParticle: _Opacity 0 only adds light, 1 covers
// what is behind (bars, the blob shadow). Optional: a scrolling UV, a second sample for chosen channels that scrolls or
// ripples outwards on its own, a fill that cuts the piece from the right along UV2.x, B darkening the result (ticks,
// membrane spots) and the coverage taken from R instead of A (a flyer's shadow tightening as it climbs).
Shader "Game/MarkerPack"
{
    Properties
    {
        _MainTex ("Channel pack", 2D) = "black" {}
        [HDR] _CoreColor ("Core (R)", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow (G)", Color) = (1, 1, 1, 1)
        [HDR] _ThirdColor ("Third (B)", Color) = (0, 0, 0, 0)
        [HDR] _Tint ("Tint (glow and third; alpha = strength)", Color) = (1, 1, 1, 1)
        _CoreTint ("Core takes the tint (0 white core, 1 tinted)", Range(0, 1)) = 0
        _Intensity ("Intensity", Float) = 1
        _Opacity ("Opacity (0 additive, 1 covering)", Range(0, 1)) = 0
        _Scroll ("UV scroll per second (xy)", Vector) = (0, 0, 0, 0)
        _SecondScroll ("Second sample: scroll per second (xy), ripple speed (z)", Vector) = (0, 0, 0, 0)
        _SecondMask ("Second sample: channels it replaces (rgb)", Vector) = (0, 0, 0, 0)
        _Darken ("B darkens the colour", Range(0, 1)) = 0
        _Fill ("Fill (cuts UV2.x above it)", Float) = 2
        _CoverFromCore ("Coverage from R instead of A", Range(0, 1)) = 0
        [Toggle(_SECOND_SAMPLE)] _SecondSample ("Second sample", Float) = 0
        [Toggle(_SECOND_RIPPLE)] _SecondRipple ("Second sample ripples outwards", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull [_Cull]

        Pass
        {
            Name "MarkerPack"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MarkerVertex
            #pragma fragment MarkerFragment
            #pragma shader_feature_local _SECOND_SAMPLE
            #pragma shader_feature_local _SECOND_RIPPLE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _CoreColor;
            half4 _GlowColor;
            half4 _ThirdColor;
            half4 _Tint;
            half _CoreTint;
            half _Intensity;
            half _Opacity;
            float4 _Scroll;
            float4 _SecondScroll;
            half4 _SecondMask;
            half _Darken;
            float _Fill;
            half _CoverFromCore;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 rawUv : TEXCOORD1;
                float along : TEXCOORD2;
            };

            Varyings MarkerVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.rawUv = input.uv;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex) + _Scroll.xy * _Time.y;
                output.along = input.uv2.x;
                return output;
            }

            half4 MarkerFragment(Varyings input) : SV_Target
            {
                clip(_Fill - input.along);
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
#if defined(_SECOND_SAMPLE)
    #if defined(_SECOND_RIPPLE)
                // Rings that run outwards from the centre and start again (the slow field's ripples).
                float2 secondUv = 0.5 + (input.rawUv - 0.5) * frac(-_Time.y * _SecondScroll.z);
    #else
                float2 secondUv = TRANSFORM_TEX(input.rawUv, _MainTex) + _SecondScroll.xy * _Time.y;
    #endif
                half4 second = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, secondUv);
                texel.rgb = lerp(texel.rgb, second.rgb, _SecondMask.rgb);
#endif
                half3 tint = _Tint.rgb;
                half3 light = texel.r * _CoreColor.rgb * lerp(1.0h, tint, _CoreTint) + (texel.g * _GlowColor.rgb + texel.b * _ThirdColor.rgb) * tint;
                light *= 1.0h - _Darken * texel.b;
                half coverage = lerp(texel.a, texel.r, _CoverFromCore) * _Tint.a * input.color.a;
                half3 colour = light * input.color.rgb * _Intensity * coverage;
                return half4(colour, coverage * _Opacity);
            }
            ENDHLSL
        }
    }
}
