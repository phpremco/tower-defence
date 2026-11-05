// The ground marks bosses leave (ArtSource README round 29: crack, lava and rot trails, furrow, sinking road, frost).
// Two maps per mark: an sRGB albedo whose alpha is the coverage, and a linear pack with R the glow mask and G a progress
// threshold. One float (_Progress) moves the threshold: the crack opens from its fissure out, a trail cools or decays from
// its edges in, frost grows out along its branches. Strips may open along their length: UV2.x is the distance along the
// path, and a vertex lies _OpenRamp metres behind the open end before it is fully open. UV2.y lifts a vertex (the
// heaved lips of a crack) as it opens, so the mark stands out of the ground and catches the light. Lit by the main light
// and the ambient light like the ground under it, alpha-blended over it.
Shader "Game/BossDecal"
{
    Properties
    {
        _BaseMap ("Albedo (alpha = coverage)", 2D) = "white" {}
        _GlowMap ("Pack (R glow, G threshold)", 2D) = "black" {}
        _Tint ("Albedo tint", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor ("Glow colour", Color) = (1, 0.4, 0.1, 1)
        [Enum(Open,0,Decay,1,Heat,2,Grow,3,Always,4)] _Mode ("Threshold mode", Float) = 0
        _Progress ("Progress", Range(0, 1)) = 1
        _Softness ("Threshold softness", Range(0.001, 0.5)) = 0.08
        _OpenDistance ("Open to (m along UV2.x; negative: whole strip)", Float) = -1
        _OpenRamp ("Opening ramp behind the open end (m)", Float) = 2
        _Fade ("Fade (alpha multiplier)", Range(0, 1)) = 1
        _GlowFade ("Glow multiplier", Range(0, 4)) = 1
        _Depth ("Darkens the gap along the strip's middle", Range(0, 1)) = 0
        _DepthFrom ("Dark gap half width (share of the strip)", Range(0, 0.5)) = 0.22
        _EdgeGlow ("Glow away from the fissure (share)", Range(0, 1)) = 1
        _CoreGlow ("Glow of the thin molten core (share)", Range(0, 1)) = 1
        _LipGlow ("Glow along the lips of the dark gap (share)", Range(0, 2)) = 0
        _LipBand ("Half width of the lit lip band (share of the strip)", Range(0.005, 0.2)) = 0.03
        _HeadTaper ("Pinch to a point over this many m behind the open end (0: off)", Float) = 0
        _LipHeight ("Lip height at UV2.y = 1 (m)", Float) = 0
        _Pulse ("Glow pulse (amplitude, speed)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "BossDecal"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DecalVertex
            #pragma fragment DecalFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GlowMap); SAMPLER(sampler_GlowMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Tint;
            half4 _EmissionColor;
            half _Mode;
            half _Progress;
            half _Softness;
            float _OpenDistance;
            float _OpenRamp;
            half _Fade;
            half _GlowFade;
            half _Depth;
            half _DepthFrom;
            half _EdgeGlow;
            half _CoreGlow;
            half _LipGlow;
            half _LipBand;
            float _HeadTaper;
            float _LipHeight;
            float4 _Pulse;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 path : TEXCOORD1;
                float2 side : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half across : TEXCOORD5;
                half3 normalWS : TEXCOORD2;
                half open : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            // How far open this vertex is: the whole strip at _Progress, or ramping in behind the open end.
            half OpenAt(float along)
            {
                if (_OpenDistance < 0.0)
                {
                    return 1.0h;
                }

                return (half)saturate((_OpenDistance - along) / max(_OpenRamp, 0.001));
            }

            Varyings DecalVertex(Attributes input)
            {
                Varyings output;
                half open = OpenAt(input.path.x);
                float3 position = input.positionOS.xyz;
                position.y += _LipHeight * input.path.y * open * _Progress;
                // A tearing fissure comes to a point at its open end instead of a square-cut ribbon (UV3: offset from the centre).
                if (_HeadTaper > 0.0 && _OpenDistance >= 0.0)
                {
                    float pinch = smoothstep(0.0, 1.0, saturate((_OpenDistance - input.path.x) / _HeadTaper));
                    position.xz -= input.side * (1.0 - pinch);
                }
                VertexPositionInputs positions = GetVertexPositionInputs(position);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = (half3)TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.across = (half)abs(input.uv.y - 0.5);
                output.open = open;
                output.fog = (half)ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half Threshold(half g, half progress)
            {
                half soft = _Softness;
                if (_Mode < 0.5)
                {
                    return smoothstep(1.0h - progress - soft, 1.0h - progress, g + soft * 0.5h) * step(0.001h, progress);
                }

                if (_Mode < 1.5)
                {
                    return smoothstep(progress - soft, progress, g);
                }

                if (_Mode < 2.5)
                {
                    return 1.0h;
                }

                if (_Mode < 3.5)
                {
                    return 1.0h - smoothstep(progress - soft, progress, g);
                }

                return 1.0h;
            }

            half4 DecalFragment(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _Tint;
                half4 pack = SAMPLE_TEXTURE2D(_GlowMap, sampler_GlowMap, input.uv);
                half progress = _Progress * input.open;
                half shown = Threshold(pack.g, progress);
                half alpha = albedo.a * shown * _Fade * input.color.a;

                // A deep fissure: the middle of the crack is dark, only its molten core (R) lights it.
                half depth = _Depth * (1.0h - smoothstep(0.05h, _DepthFrom, input.across));
                half3 baseColour = albedo.rgb * (1.0h - depth);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 normal = normalize(input.normalWS);
                half ndl = saturate(dot(normal, (half3)mainLight.direction));
                half3 lit = baseColour * (mainLight.color * ndl * mainLight.shadowAttenuation + SampleSH(normal));

                // Heat: the glow lives while G is above the cooling front; elsewhere the glow follows what is shown.
                half heat = _Mode > 1.5 && _Mode < 2.5 ? smoothstep(progress - _Softness, progress, pack.g) : shown;
                // A crack glows in its fissure; the cracks in its heaved plates only smoulder. A torn fissure (lip glow on) is a
                // dark gap whose two lips are lit from the melt below, unevenly along their length, over a faint deep core.
                half core = lerp(_EdgeGlow * (1.0h - depth * saturate(_LipGlow)), _CoreGlow, 1.0h - smoothstep(0.006h, 0.03h, input.across));
                half lip = 1.0h - smoothstep(0.0h, _LipBand, abs(input.across - _DepthFrom * 0.9h));
                half ragged = 0.7h + 0.3h * sin(input.uv.x * 61.0 + sin(input.uv.x * 13.0) * 2.0);
                half pulse = 1.0h + _Pulse.x * sin(_Time.y * _Pulse.y + input.positionWS.x * 0.7 + input.positionWS.z * 0.5);
                // Vertex alpha also dims the glow: a strip faded off a footprint must not glow under it.
                half3 glow = (pack.r * core + _LipGlow * lip * ragged) * heat * _EmissionColor.rgb * _GlowFade * pulse * input.open * input.color.a;
                half3 colour = MixFog(lit + glow, input.fog);
                // The glow may outshine a thin coverage at the fissure's edge.
                alpha = saturate(max(alpha, saturate(dot(glow, half3(0.3h, 0.4h, 0.3h))) * shown * _Fade));
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
