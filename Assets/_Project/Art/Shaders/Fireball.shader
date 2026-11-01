// The translucent shell of a blast: brightest along the silhouette (fresnel), thin in the middle so what is behind
// still shows, and churned by two scrolling noise layers so it reads as a ball of fire rather than a tinted glass
// sphere. Additive and unlit, so it glows on dark ground and costs nothing in lighting.
Shader "Game/Fireball"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (1, 0.6, 0.25, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _RimPower ("Rim power", Range(0.5, 6)) = 1.8
        _RimStrength ("Rim strength", Range(0, 3)) = 1.4
        _Fill ("Centre fill", Range(0, 1)) = 0.25
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Scroll ("Scroll speed", Float) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Fireball"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FireballVertex
            #pragma fragment FireballFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _NoiseTex_ST;
            half _RimPower;
            half _RimStrength;
            half _Fill;
            half _Alpha;
            half _Scroll;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings FireballVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positions.positionWS);
                output.uv = input.texcoord;
                return output;
            }

            half4 FireballFragment(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - facing, _RimPower) * _RimStrength;
                float t = _Time.y * _Scroll;
                half churn = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 2.0 + float2(t, -t * 0.7)).r;
                half flicker = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 3.7 + float2(-t * 1.3, t * 0.4)).r;
                half turbulence = 0.45h + 0.55h * (churn * 0.6h + flicker * 0.4h);
                half shell = saturate(rim + _Fill) * turbulence;
                return half4(_Color.rgb * shell, saturate(shell * _Alpha));
            }
            ENDHLSL
        }
    }
}
