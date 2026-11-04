// Heat and shock distortion painted in Blender: RG hold a UV offset (0.5 is none), B a mask. The picture behind the
// particle or quad is sampled from the URP opaque texture with that offset, so the air shimmers over a blast or a
// ring of refraction runs outward. The particle colour's alpha fades the strength. Drawn before the other
// transparent effects (the opaque texture holds no particles), so fire and smoke stay on top. High profile only:
// the low profile renders no opaque texture and plays no distortion.
Shader "Game/VfxDistortion"
{
    Properties
    {
        _MainTex ("Offset (RG), mask (B)", 2D) = "gray" {}
        _Strength ("Strength", Float) = 0.03
        _Scroll ("Scroll (uv per second)", Vector) = (0, 0, 0, 0)
        _RadialMask ("Round mask over the quad", Range(0, 1)) = 0
        _UseMask ("Use the B mask", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "VfxDistortion"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DistortVertex
            #pragma fragment DistortFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Strength;
            float4 _Scroll;
            half _RadialMask;
            half _UseMask;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings DistortVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv.xy;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 DistortFragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv, _MainTex) + _Scroll.xy * _Time.y);
                float2 centred = input.uv * 2.0 - 1.0;
                half round = lerp(1.0h, (half)saturate(1.0 - dot(centred, centred)), _RadialMask);
                half mask = lerp(1.0h, texel.b, _UseMask) * round * input.color.a;
                float2 offset = (texel.rg * 2.0 - 1.0) * mask * _Strength;
                float2 screenUv = input.screenPos.xy / max(input.screenPos.w, 1e-5);
                half3 scene = SampleSceneColor(screenUv + offset);
                // Only where the picture actually bends: the copy of the opaque scene lacks the mist and the other
                // transparent layers, so covering the whole quad with it would show as a flat disc.
                half bend = (half)saturate(length(texel.rg * 2.0 - 1.0) * 2.5);
                return half4(scene, saturate(mask * bend * 2.0h));
            }
            ENDHLSL
        }
    }
}
