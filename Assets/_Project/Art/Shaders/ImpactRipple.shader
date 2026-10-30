// A ring of refraction that runs outward from an impact: the picture under the quad is sampled with an offset along
// the ring, so the ground, road and creatures bend for a moment where something hit. Needs the URP opaque texture.
Shader "Game/ImpactRipple"
{
    Properties
    {
        _Strength ("Strength", Float) = 0.04
        _Progress ("Progress", Range(0, 1)) = 0
        _Tint ("Tint", Color) = (1, 1, 1, 0.25)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Ripple"
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            float _Strength;
            float _Progress;
            float4 _Tint;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centred = input.uv * 2.0 - 1.0;
                float dist = length(centred);
                // The ring sits at the progress radius, a few percent wide, and dies out as it reaches the edge.
                float ring = saturate(1.0 - abs(dist - _Progress) * 7.0) * (1.0 - _Progress) * step(dist, 1.0);
                float2 direction = dist > 0.0001 ? centred / dist : float2(0.0, 0.0);
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half3 scene = SampleSceneColor(screenUV + direction * ring * _Strength);
                return half4(scene + _Tint.rgb * ring * _Tint.a, 1.0);
            }
            ENDHLSL
        }
    }
}
