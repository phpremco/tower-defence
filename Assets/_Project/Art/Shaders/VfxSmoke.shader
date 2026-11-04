// Fire and smoke simulated in Blender and baked as six-way light maps: Pos holds the light from the right, the top
// and behind, Neg the light from the left, the bottom and the camera side, so the level's sun lights each puff from
// wherever it stands. Neg.A is the temperature: it glows through a fire ramp from deep red over orange to white, and
// the particle's heat (Custom1.x, a curve over its life) overdrives it at the start so a blast leaves white-hot and
// cools quickly to orange before only smoke is left. Billboards face the camera, so the camera axes are the
// billboard axes. Premultiplied alpha: the lit smoke covers, the fire adds light.
Shader "Game/VfxSmoke"
{
    Properties
    {
        _PosMap ("Six-way Pos (R right, G top, B back, A alpha)", 2D) = "black" {}
        _NegMap ("Six-way Neg (R left, G bottom, B front, A temperature)", 2D) = "black" {}
        _Albedo ("Smoke colour", Color) = (0.2, 0.18, 0.16, 1)
        _LightStrength ("Sun strength", Float) = 1.2
        _AmbientStrength ("Ambient strength", Float) = 0.6
        [HDR] _FireLow ("Fire, cool", Color) = (0.7, 0.08, 0.02, 1)
        [HDR] _FireMid ("Fire, orange", Color) = (2.4, 0.8, 0.15, 1)
        [HDR] _FireHigh ("Fire, yellow", Color) = (4, 2.6, 1, 1)
        [HDR] _FireWhite ("Fire, white-hot", Color) = (8, 7.2, 6, 1)
        _FireStrength ("Fire strength", Float) = 1
        _Opacity ("Opacity", Range(0, 1)) = 1
        _AlphaCurve ("Softness (alpha power, above 1 thins the edges)", Range(0.5, 4)) = 1
        _SoftDistance ("Soft particle distance", Float) = 0.6
        _Grid ("Atlas columns, rows", Vector) = (8, 8, 0, 0)
        _EdgeFade ("Fade at the cell edges", Range(0, 0.3)) = 0.08
        [Toggle(_FLIPBOOK_BLEND)] _FlipbookBlend ("Flipbook frame blending", Float) = 1
        [Toggle(_SOFT_PARTICLES)] _SoftParticles ("Soft particles", Float) = 0
        [Toggle(_HEAT_STREAM)] _HeatStream ("Heat from Custom1.x", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "VfxSmoke"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SmokeVertex
            #pragma fragment SmokeFragment
            #pragma shader_feature_local _FLIPBOOK_BLEND
            #pragma shader_feature_local _SOFT_PARTICLES
            #pragma shader_feature_local _HEAT_STREAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VfxCommon.hlsl"

            TEXTURE2D(_PosMap); SAMPLER(sampler_PosMap);
            TEXTURE2D(_NegMap); SAMPLER(sampler_NegMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _PosMap_ST;
            float4 _NegMap_ST;
            half4 _Albedo;
            half _LightStrength;
            half _AmbientStrength;
            half4 _FireLow;
            half4 _FireMid;
            half4 _FireHigh;
            half4 _FireWhite;
            half _FireStrength;
            half _Opacity;
            half _AlphaCurve;
            float _SoftDistance;
            float4 _Grid;
            half _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                // x: AnimBlend, y: Custom1.x (heat).
                float2 extra : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float2 extra : TEXCOORD1;
                half3 ambient : TEXCOORD2;
#if defined(_SOFT_PARTICLES)
                float4 screenPos : TEXCOORD3;
                float eyeDepth : TEXCOORD4;
#endif
            };

            Varyings SmokeVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.uv = float4(TRANSFORM_TEX(input.uv.xy, _PosMap), TRANSFORM_TEX(input.uv.zw, _PosMap));
                output.extra = input.extra;
                float3 towardsCamera = UNITY_MATRIX_V[2].xyz;
                output.ambient = SampleSH(towardsCamera) * _AmbientStrength;
#if defined(_SOFT_PARTICLES)
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.eyeDepth = -positions.positionVS.z;
#endif
                return output;
            }

            half3 FireRamp(half heat)
            {
                half3 colour = lerp(half3(0, 0, 0), _FireLow.rgb, saturate(heat / 0.3h));
                colour = lerp(colour, _FireMid.rgb, saturate((heat - 0.3h) / 0.3h));
                colour = lerp(colour, _FireHigh.rgb, saturate((heat - 0.6h) / 0.35h));
                return lerp(colour, _FireWhite.rgb, saturate((heat - 0.95h) / 0.5h));
            }

            half4 SmokeFragment(Varyings input) : SV_Target
            {
                half4 pos = SAMPLE_TEXTURE2D(_PosMap, sampler_PosMap, input.uv.xy);
                half4 neg = SAMPLE_TEXTURE2D(_NegMap, sampler_NegMap, input.uv.xy);
#if defined(_FLIPBOOK_BLEND)
                half blend = (half)saturate(input.extra.x);
                pos = lerp(pos, SAMPLE_TEXTURE2D(_PosMap, sampler_PosMap, input.uv.zw), blend);
                neg = lerp(neg, SAMPLE_TEXTURE2D(_NegMap, sampler_NegMap, input.uv.zw), blend);
#endif
                Light sun = GetMainLight();
                float3 towardsLight = sun.direction;
                half x = (half)dot(towardsLight, UNITY_MATRIX_V[0].xyz);
                half y = (half)dot(towardsLight, UNITY_MATRIX_V[1].xyz);
                half z = (half)dot(towardsLight, UNITY_MATRIX_V[2].xyz);
                half lit = (x > 0.0h ? pos.r : neg.r) * x * x
                         + (y > 0.0h ? pos.g : neg.g) * y * y
                         + (z > 0.0h ? neg.b : pos.b) * z * z;
                half average = (pos.r + pos.g + pos.b + neg.r + neg.g + neg.b) / 6.0h;
                half3 shade = sun.color * lit * _LightStrength + input.ambient * average;

                // Some simulations grow to the edge of their cell; fading the rim hides the straight cut.
                float2 cell = frac(input.uv.xy * max(_Grid.xy, 1.0));
                half rim = (half)(smoothstep(0.0, _EdgeFade, cell.x) * smoothstep(0.0, _EdgeFade, 1.0 - cell.x) * smoothstep(0.0, _EdgeFade, 1.0 - cell.y) * smoothstep(0.0, _EdgeFade * 0.4, cell.y));
                // Raising the painted alpha to a power thins the wispy rims faster than the dense core: softer edges, same middle.
                half alpha = pow(max(pos.a, 0.0h), _AlphaCurve) * input.color.a * _Opacity * rim;
#if defined(_SOFT_PARTICLES)
                alpha *= VfxSoftFade(input.screenPos, input.eyeDepth, _SoftDistance);
#endif
#if defined(_HEAT_STREAM)
                half heat = neg.a * (half)input.extra.y;
#else
                half heat = neg.a;
#endif
                half3 fire = FireRamp(heat) * _FireStrength * saturate(pos.a * 1.6h) * input.color.a * rim;
                half3 colour = _Albedo.rgb * input.color.rgb * shade * alpha + fire;
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
