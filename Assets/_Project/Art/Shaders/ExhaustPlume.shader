// A rocket plume on a flared cone along local +Z (0 at the nozzle, 1 at the tail; UV.y runs with it). Additive: the
// glow is brightest where the view ray crosses the most hot gas (through the middle, by the facing of the surface) and
// fades out to nothing at the silhouette and towards the tail, so the plume has a white-hot core and a soft edge.
// Shock diamonds sit in the first part, and a flicker runs down the length on the cutscene clock (_Clock), never on
// real time, so a sought frame looks the same however it was reached.
Shader "Game/ExhaustPlume"
{
    Properties
    {
        [HDR] _CoreColor ("Core", Color) = (1, 0.95, 0.85, 1)
        [HDR] _EdgeColor ("Edge", Color) = (0.3, 0.6, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _EdgePower ("Edge softness", Range(0.5, 8)) = 2.2
        _LengthFade ("Fade along the plume", Range(0.3, 6)) = 1.4
        _CoreLength ("Share of the length that is white-hot", Range(0.02, 1)) = 0.35
        _Diamonds ("Shock diamonds", Range(0, 2)) = 0.5
        _DiamondCount ("Diamonds along the hot part", Float) = 3
        _Flicker ("Flicker", Range(0, 1)) = 0.25
        _Clock ("Cutscene clock", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ExhaustPlume"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PlumeVertex
            #pragma fragment PlumeFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _CoreColor;
            half4 _EdgeColor;
            half _Intensity;
            half _EdgePower;
            half _LengthFade;
            half _CoreLength;
            half _Diamonds;
            half _DiamondCount;
            half _Flicker;
            float _Clock;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings PlumeVertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 PlumeFragment(Varyings input) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = (half)abs(dot(normalize(input.normalWS), view));
                half body = pow(facing, _EdgePower);
                half along = (half)input.uv.y;
                // A short fade-in at the bell's lip, so the cone never shows a hard ring there.
                half lip = saturate(along * 25.0h);
                half fade = pow(saturate(1.0h - along), _LengthFade) * lip;
                half hotAlong = saturate(1.0h - along / max(_CoreLength, 0.02h));
                half hot = hotAlong * hotAlong * pow(facing, _EdgePower * 2.0h);
                float swirl = sin(input.uv.x * 6.2831853 * 3.0 + _Clock * 9.0) * 0.5;
                half flicker = 1.0h - _Flicker * (half)(0.5 + 0.5 * sin(along * 19.0 - _Clock * 47.0 + swirl));
                half diamonds = 1.0h + _Diamonds * pow(abs(cos(along / max(_CoreLength, 0.02h) * _DiamondCount * 3.14159h)), 8.0h) * hotAlong;
                half3 colour = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(hot + body * 0.25h));
                half3 glow = colour * (body * fade * flicker * diamonds) + _CoreColor.rgb * (hot * lip * diamonds);
                return half4(glow * _Intensity, 0.0h);
            }
            ENDHLSL
        }
    }
}
